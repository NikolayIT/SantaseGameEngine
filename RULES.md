# Rules of Santase (66) — as implemented in this engine

This document describes the rules of the two-player card game **Santase** (also known as
*66* / *Sixty-Six* / *Schnapsen* / *Sechsundsechzig*), exactly as they are encoded in
`src/Santase.Logic` (the `SantaseGameEngine` NuGet package). Where sources disagree on a
rule, this document states which variant the engine implements and why.

> **Note:** the most common English reference (Pagat — `pagat.com/marriage/66.html`)
> describes a **different** variant (game target of 7 game points, a 2-game-point penalty
> for a failed close, marriages forbidden once the talon is exhausted, etc.) and does not
> match this engine exactly. This engine follows the Bulgarian variant. The authoritative
> specification here is the **engine code itself** — every rule below was verified against
> the source and is cross-referenced to it.

---

## 1. Overview

Santase is a 6-card, two-player trick-taking game. The goal is to be the first to reach
66 points from the card values of won tricks plus announced marriages. The winner of a
deal is awarded 1, 2 or 3 *game points*. The first player to **11 game points** wins the
whole game.

The rules live in `SantaseMatch` (`src/Santase.Logic/GameMechanics/SantaseMatch.cs`), a match
driven one action at a time: `Start()`, then read `ToMove`, give that player
`CreateTurnContext()` (or their `GetView(seat)`) and pass their move to `Act`. `SantaseGame`
(`SantaseGame.cs`) plays a whole match between two `IPlayer`s with that loop:
`new SantaseGame(playerA, playerB).Start()`.

---

## 2. The pack

The game is played with a **24-card pack**: only the Nines, Jacks, Queens, Kings, Tens
and Aces of a standard deck — one of each in every suit (Clubs, Diamonds, Hearts, Spades).

In code:

- `Cards/CardType.cs` — `Nine, Ten, Jack, Queen, King, Ace`
- `Cards/CardSuit.cs` — `Club, Diamond, Heart, Spade`
- `Cards/Deck.cs` — the 24 cards, shuffled for every new deal with `Random.Shared` or the
  match's own random source (`SantaseMatchOptions.Shuffle`). The draw order is documented on
  `Deck`, so a deal can be recreated from the random numbers it was built from.

**`Card` is a flyweight** (`Cards/Card.cs`): all 24 cards are pre-instantiated in a static
array. Always use `Card.GetCard(suit, type)` (it throws for any suit or rank that is not a
Santase card) — the public constructor is marked `[Obsolete]` precisely so it is not used. This makes card equality (`==` / `Equals`) an
effective pointer compare, which the hot simulator loops rely on.

---

## 3. Card values and rank

| Card | Point value |
|:----:|:-----------:|
| Ace (A) | **11** |
| Ten (10) | **10** |
| King (K) | **4** |
| Queen (Q) | **3** |
| Jack (J) | **2** |
| Nine (9) | **0** |

Sum of all 24 card values = `4 × (11 + 10 + 4 + 3 + 2 + 0) = 4 × 30 = 120`.

In code: `CardValues = { 0, 11, 0, 0, 0, 0, 0, 0, 0, 0, 10, 2, 3, 4 }` in `Card.cs`,
indexed by `(int)CardType`.

### Rank within a suit

```
A > 10 > K > Q > J > 9
```

Rank is exactly a function of the card's point value (`Card.GetValue()`), so the table
above doubles as the rank table.

Implementation: `WinnerLogic/CardWinnerLogic.cs`:

- If **both played cards are the same suit** — the higher value wins.
- If the suits **differ and the second card is a trump** — the trump (second card) wins.
- If the suits **differ and neither is a trump** — the **first card played wins** (the
  follower neither followed suit nor trumped, so they cannot win).

Note: the follower can only win by trumping or by beating in the led suit. A trump beats
any non-trump; this is the only way the second player wins with a different suit.

---

## 4. Objective

**Objective of a deal (round)** — reach **66 or more points** from the card values of
your won tricks plus your announced marriages.

**Objective of the game** — accumulate **11 game points**
(`SantaseGameRules.GamePointsNeededForWin => 11`).

Sources disagree on the game target:

- Pagat (English): 7.
- Bulgarian rules (sa-igri.com, Wikipedia BG, santase.bg, belot.bg): 11.

This engine follows the **Bulgarian variant — 11 game points**.

---

## 5. The deal

Conceptually, before play:

- The pack is shuffled.
- Each player is dealt **6 cards**.
- The next (13th) card is turned face up — its suit is the **trump suit** for the deal.
- The remaining 11 cards lie across the turned-up trump and form the **talon** (stock).

In code this ceremony is simplified: `Deck` shuffles the 24 cards once (Fisher-Yates). The
card at position 0 is the face-up `TrumpCard` (it sits at the *bottom* of the talon and is
drawn **last**); `GetNextCard` draws from the other end. Statistically this is equivalent to
the physical 3-3 + turn-trump + 3-3 deal — every distribution is equally likely.

`Round.Start` deals each player `IGameRules.CardsAtStartOfTheRound` cards (6 under the
standard rules, `SantaseGameRules`): the first player's six, then the second player's. A
match's record keeps every deal in this draw order (`SantaseRoundRecord.Deal`).

---

## 6. Who leads

**The winner of the previous deal deals; the dealer's opponent leads.**

In code, `SantaseMatch.FirstToPlay` is the *opener of the next deal* (= the dealer's
opponent; the first deal's opener is `SantaseMatchOptions.FirstToPlay`). After each deal,
`UpdatePoints` switches it to the loser of that deal:

```csharp
// SantaseMatch.cs
case PlayerPosition.FirstPlayer:
    this.FirstPlayerTotalPoints += roundWinnerPoints.Points;
    this.FirstToPlay = PlayerPosition.SecondPlayer; // the loser opens next
    break;
case PlayerPosition.SecondPlayer:
    this.SecondPlayerTotalPoints += roundWinnerPoints.Points;
    this.FirstToPlay = PlayerPosition.FirstPlayer;
    break;
```

On a **draw** the `switch` matches no case — `FirstToPlay` is left unchanged, so the
**same player opens the next deal too** (the same dealer deals again). See §14.

---

## 7. Tricks and play

Both players play to every trick. The leader (`PlayerTurnContext.IsFirstPlayerTurn == true`)
plays one card; the follower responds with one card. The winner is decided by
`CardWinnerLogic` (see §3) and then:

- Takes both cards into their own `TrickCards`.
- Draws the first card from the talon (if the phase allows drawing).
- Leads the next trick.

The values of the two played cards are added to the trick winner's
`RoundPlayerInfo.RoundPoints`, a running total of the cards won (`TrickCards`) and the
marriages announced (`Announces`).

Whether a player may look back at the tricks already played is up to whoever presents the
game: every seat's view carries the round's tricks (`SantaseSeatView.Tricks`, which bots use),
and the `IPlayer` callbacks report each trick as it ends (`EndTurn`).

---

## 8. The two phases

Santase has two distinct phases with different rules. They are modeled with the **State
pattern** in `src/Santase.Logic/RoundStates/`, coordinated by `StateManager`:

| Phase | Class | `ShouldObserveRules` | `CanAnnounce20Or40` | `CanClose` | `CanChangeTrump` | `ShouldDrawCard` |
|:-----:|:-----:|:--------------------:|:-------------------:|:----------:|:----------------:|:----------------:|
| First trick | `StartRoundState` | false | **false** | **false** | **false** | true |
| Talon > 2 cards | `MoreThanTwoCardsLeftRoundState` | false | true | true | true | true |
| Talon = 2 cards | `TwoCardsLeftRoundState` | false | true | **false** | **false** | true |
| Talon empty or closed | `FinalRoundState` | **true** | true | false | false | **false** |

State transitions (`PlayHand` is called after each trick with the cards left in the talon):
`StartRoundState` → (after trick 1, 10 cards left) `MoreThanTwoCardsLeftRoundState` → (when
the talon hits 2) `TwoCardsLeftRoundState` → (after the next trick) `FinalRoundState`. After
the first trick the phase follows the talon, so a rule variant dealing more cards
(`CardsAtStartOfTheRound` 10 or 11) goes straight to `TwoCardsLeftRoundState` or
`FinalRoundState`. Closing jumps straight to `FinalRoundState` via `BaseRoundState.Close()`.

### 8.1 Phase 1: open talon (`ShouldObserveRules = false`)

While the talon is neither exhausted nor closed:

- Following suit is **not** required.
- Trumping (heading with a trump) is **not** required.
- The follower may play **any card** from hand.

In code, `PlayCardActionValidator.CanPlayCard(...)` returns `true` for any owned card when
`shouldObserveRules` is `false`, so `GetPossibleCardsToPlay` returns the whole hand.

### 8.2 Phase 2: strict rules (`ShouldObserveRules = true`)

Enters into force when either:

- the talon is **exhausted naturally** (after the 6th trick the talon is empty), or
- a player has **closed the talon** (see §11).

In both cases the last 12 cards (6 in each hand) are played out with no further drawing.
The follower's rules become:

1. **Must follow suit** — if you hold a card of the led suit, you must play one.
2. **Must head the trick in suit** — if you hold a card of the led suit that beats the
   led card, you must play a beating card.
3. **Must trump** — if you have no card of the led suit but hold a trump, you must play a
   trump. The trump need not out-rank the opponent's trump (relevant only if a trump was
   led, in which case rule 2 applies within the trump suit).
4. **Otherwise** — play anything.

The **leader** in Phase 2 may still lead **any** card (`isThePlayerFirst` short-circuits
to `true`). Only the follower is constrained. Implementation: `PlayCardActionValidator.cs`.

---

## 9. Marriages: King + Queen (20 / 40)

When a player is **on lead** (about to lead a trick) and holds **the King and Queen of the
same suit**, they may announce the marriage:

- **20 points** — if the suit is not trump.
- **40 points** — if the suit is trump (the *royal marriage*).

The marriage is announced by **leading one of the two cards** (King or Queen) together
with the announcement; the other card stays in hand (possibly for a second marriage later).

### 9.1 When it is allowed

- **Only when the player is on lead** (`IsFirstPlayerTurn`).
- **Not in the first trick of the deal** (`StartRoundState.CanAnnounce20Or40 = false`).
  This is enforced in `PlayerActionValidator.IsValid`, which only computes an announce
  when `context.State.CanAnnounce20Or40` is true.
- **Allowed even after the talon is closed/exhausted** —
  `MoreThanTwoCardsLeftRoundState`, `TwoCardsLeftRoundState` and `FinalRoundState` all
  have `CanAnnounce20Or40 = true`.

> *Variant note:* Pagat and some Schnapsen variants forbid marriages once the talon is
> exhausted. The Bulgarian rules (and this engine) **allow** an announcement after
> closing/exhaustion, as long as you are on lead and hold both cards.

### 9.2 "Marriage points count only after at least one trick won"

The engine does not check this explicitly, but it holds by construction: to announce you
must be on lead, and after the first trick you can only be on lead if you won the previous
trick. So any player who announces has already won at least one trick.

### 9.3 An announcement can take you to 66

If the announcement raises your total to 66+, you **win the deal at the moment of the
announcement**, without playing a second card to the trick. `Round.LeaderActs` (internal,
reached through `SantaseMatch.Act`) handles this:

```csharp
// Round.cs
leaderInfo.Cards.Remove(action.Card);

if (leaderRoundPoints >= this.gameRules.RoundPointsForGoingOut)
{
    // The announce took the leader to the target: the round ends before the follower
    // plays. Both players are told about the lead, and the leader counts as the trick
    // winner (for who draws first and who leads next).
    leaderInfo.Player?.EndTurn(this.context);
    this.Info(Other(this.leader)).Player?.EndTurn(this.context);
    this.CompleteTrick(this.leader);
    return true;
}
```

In the round's history such a lead is a trick with no answer (`SantaseTrick.FollowCard` is
null), won by the leader. It takes no cards, so it is not counted as a trick won.

Validation: `AnnounceValidator.cs` returns `Announce.Forty` or `Announce.Twenty` when the
led card is a King or Queen, the partner card is in hand, and the player is on lead.

---

## 10. Exchanging the trump with the trump Nine

A player holding **the Nine of the trump suit** may **exchange it for the face-up trump
card under the talon**.

### Conditions

- The player is **on lead** (`IsFirstPlayerTurn`).
- **Not the first trick** (`StartRoundState.CanChangeTrump = false`).
- The talon has **more than 2 cards** (`CanChangeTrump = true` only in
  `MoreThanTwoCardsLeftRoundState`).
- The exchange happens **between tricks** — never mid-trick.

In code: `ChangeTrumpActionValidator.cs` plus the `BaseRoundState` flags.

> *Variant note:* Bulgarian sources differ on the talon-size threshold; this engine uses
> the more permissive **talon > 2** (the rule is active only while in
> `MoreThanTwoCardsLeftRoundState`). Pagat also allows the exchange immediately if the
> opponent closes even without a won trick — this engine does **not** model that
> exception (after a close the state is `FinalRoundState`, where `CanChangeTrump = false`).

### How the exchange works

1. The player on lead declares the exchange.
2. The trump Nine from their hand goes to the bottom of the talon (replacing the face-up
   trump).
3. They take the old face-up trump card into hand.
4. The Nine is then drawn naturally as the last card when the talon is exhausted.

Implementation: `Round.LeaderActs` handles the `ChangeTrump` action:

```csharp
// Round.cs
case PlayerActionType.ChangeTrump:
{
    // The leader swaps the Nine of trumps for the face-up trump card and leads again.
    var oldTrumpCard = this.deck.TrumpCard;
    var nineOfTrump = Card.GetCard(oldTrumpCard.Suit, CardType.Nine);
    this.TrumpSwappedBy = this.leader;
    this.SwappedTrumpCard = oldTrumpCard;
    this.TrumpSwappedAfterTricks = this.TricksPlayed;
    this.deck.ChangeTrumpCard(nineOfTrump);
    this.context.TrumpCard = nineOfTrump;
    leaderInfo.Cards.Remove(nineOfTrump);
    leaderInfo.Cards.Add(oldTrumpCard);
    return true; // the same player is still to move: they may close, then lead
}
```

The match record keeps who exchanged, the card they took and after how many tricks
(`SantaseRoundRecord.TrumpSwappedBy`, `SwappedTrumpCard`, `TrumpSwappedAfterTricks`).

### Implicit requirement: at least one trick already won

The engine does not check "you have already won a trick" explicitly, but the condition is
automatic — to be on lead after the first trick you must have won it.

---

## 11. Closing the talon

A player on lead may **close the talon** if they believe they can reach 66 points using
only the cards in hand.

### Conditions

- The player is **on lead**.
- **Not the first trick** (`StartRoundState.CanClose = false`).
- The talon has **more than 2 cards** (`TwoCardsLeftRoundState.CanClose = false`; only
  `MoreThanTwoCardsLeftRoundState` has `CanClose = true`).

### Effect

- The face-up trump card is turned face down — the talon is "closed".
- Play moves to `FinalRoundState`: strict rules (see §8.2), no further drawing.
- The **+10 last-trick bonus is suspended** (see §12).

Implementation: `Round.LeaderActs`:

```csharp
// Round.cs
case PlayerActionType.CloseGame:
{
    // The leader closes the talon and leads again.
    this.stateManager.State.Close();   // → FinalRoundState
    this.context.State = this.stateManager.State;
    leaderInfo.GameCloser = true;      // remember who closed
    this.ClosedAfterTricks = this.TricksPlayed;
    return true;
}
```

### Multiple actions on one lead

When a player is on lead they may chain several actions before playing a card:

1. Trump-Nine exchange (`ChangeTrump`).
2. Close the talon (`CloseGame`).
3. Announce a marriage (when leading the King/Queen).
4. The card actually played.

An exchange or a close keeps the same player to move (`SantaseMatch.ToMove` does not
change), so they can exchange, then close, then lead. Neither can be repeated: after an
exchange the Nine is no longer in hand, and after a close the state is `FinalRoundState`,
which allows neither — so an exchange has to come before the close.

---

## 12. Last-trick bonus (+10)

When the deal ends by **natural talon exhaustion** (all 24 cards played and nobody
closed), the winner of the **12th (last) trick** receives a **+10 bonus**.

### When the bonus does not apply

- A player **closed the talon** — the bonus is suspended.
- The deal ends **early** (someone reached 66 before the last trick) — there is no
  bonus-bearing last trick.

Implementation: `RoundWinnerPointsPointsLogic.GetWinnerPoints`:

```csharp
private const int LastTrickBonus = 10;

if (gameClosedBy == PlayerPosition.NoOne)
{
    if (lastTrickWinner == PlayerPosition.FirstPlayer)
        firstPlayerPoints += LastTrickBonus;
    else if (lastTrickWinner == PlayerPosition.SecondPlayer)
        secondPlayerPoints += LastTrickBonus;
}
```

`lastTrickWinner` is decided in `Round.Finish()`:

```csharp
// Round.cs
var bothHandsEmpty = this.firstPlayer.Cards.Count == 0 && this.secondPlayer.Cards.Count == 0;
var lastTrickWinnerForBonus = bothHandsEmpty ? this.lastTrickWinner : PlayerPosition.NoOne;
this.Result = new RoundResult(this.firstPlayer, this.secondPlayer, lastTrickWinnerForBonus);
```

So the bonus applies only when **both hands are empty** (the deal ran to the natural end).
Edge case: if 66 is reached *exactly on the 12th trick* from card values, both hands are
empty, so the bonus still applies — which is the correct behaviour.

> *Variant note:* This is the 66/Santase rule. In Schnapsen the winner of the last trick
> after stock exhaustion gets an outright win of the deal instead of +10 card points; this
> engine implements the +10 variant.

---

## 13. Scoring at the end of a deal

`RoundWinnerPointsPointsLogic.GetWinnerPoints` decides how many **game points** each
player gets, based on:

1. `firstPlayerPoints`, `secondPlayerPoints` — card + marriage points (+10 if applicable).
2. `gameClosedBy` — who, if anyone, closed.
3. `noTricksPlayer` — a player who won no trick at all (for *schwarz* / "capot").
4. `lastTrickWinner` — for the bonus.

### 13.1 Algorithm (faithful to the code)

```
1. If nobody closed (gameClosedBy == NoOne):
       add +10 to the last-trick winner.

2. If the closer scored < 66 (failed close):
       the opponent immediately wins 3 game points. (Always 3 — see §13.3.)

3. If firstPlayerPoints == secondPlayerPoints:
       DRAW — nobody scores game points.

4. If BOTH players are below 66 (round ran out without anyone reaching 66, nobody closed):
       the player with more points wins 1 game point.
       (In normal play this can only happen at exactly 65–65, which is already a draw,
        so this branch is effectively an unreachable guard — but it is in the code.)

5. Otherwise let P = the player with more points, O = the opponent:
       if O has >= 33 points:                P wins 1 game point.
       else if O won no trick at all:         P wins 3 game points (schwarz / capot).
       else (O has 1–32 and at least a trick): P wins 2 game points.
```

(Step 4 is the block marked "Unreachable through real engine play" in
`RoundWinnerPointsPointsLogic.cs`; it is reachable through direct unit tests of the scoring
class but not through ordinary game play.)

### 13.2 Table

| Situation | Game points to the winner |
|-----------|---------------------------|
| Talon not closed, winner reached 66+ (or has more), opponent has ≥ 33 | **1** |
| Same, opponent has 1–32 and at least one trick | **2** |
| Same, opponent won no trick at all (**schwarz / capot**) | **3** |
| A player closed and **succeeded** (≥ 66); opponent has ≥ 33 | **1** |
| Same, opponent has 1–32 and at least one trick | **2** |
| Same, opponent won no trick at all | **3** |
| A player closed and **failed** (< 66) — opponent wins | **3** (always) |
| Final totals are equal, nobody reached 66 | **0** (draw) |

### 13.3 "Always 3 game points for a failed close" — what sources say

This is an **implementation choice** of this engine. Bulgarian sources split:

- *sa-igri.com, Wikipedia (BG)*: opponent gets **2** game points, or **3** if they won no
  trick.
- *santase.bg*: always **3**.

This engine follows the strict variant (**always 3**), matching the common "simple close"
rule. Pagat instead gives **2** game points (3 if the opponent had no trick when the close
happened). Implementation: the failed-close checks at the top of
`RoundWinnerPointsPointsLogic.GetWinnerPoints`, before the totals are compared.

---

## 14. Draw

When the two players' totals (including marriages and the +10 bonus) are **equal**, the
deal is a **draw**:

- **Nobody** scores game points.
- The **same player opens the next deal** — `firstToPlay` is unchanged.
- The same dealer deals again.

### Implementation

`RoundWinnerPoints.Draw()` returns `Winner = NoOne, Points = 0`:

```csharp
if (firstPlayerPoints == secondPlayerPoints)
{
    return RoundWinnerPoints.Draw();
}
```

`SantaseMatch.UpdatePoints` handles `Winner = NoOne` by falling through the `switch` — no
case runs, so `FirstToPlay` is untouched.

### When a draw actually occurs

With the +10 bonus from §12, a draw arises only if:

- Nobody closed, and
- the totals are equal after the +10 is applied (e.g. 60–70 before the bonus → 70–70 after
  if the loser of card points won the last trick).

With a closed talon a draw cannot happen: a closer below 66 loses 3 game points before the
totals are compared, and a closer who reaches 66 ends the deal at that moment, with the
opponent still below 66 (the deal ends as soon as either player reaches 66).

Tests: `RoundWinnerPointsPointsLogicTestsForSantase.GetWinnerPointsShouldYieldDrawWhenScoresStillEqualAfterBonus`
and `SantaseGameTests.DrawnRoundShouldNotAwardPointsAndShouldKeepTheSameOpener`.

---

## 15. Winning a deal and the game

### Winning a deal

A deal ends in one of two cases, checked by `Round.Continue()` before every trick (and, for
a marriage that reaches 66, right after the lead — §9.3):

```csharp
// Round.cs
if (this.firstPlayer.RoundPoints >= this.gameRules.RoundPointsForGoingOut      // someone reached 66
    || this.secondPlayer.RoundPoints >= this.gameRules.RoundPointsForGoingOut
    || (this.firstPlayer.Cards.Count == 0 && this.secondPlayer.Cards.Count == 0)) // hands exhausted
{
    this.Finish();
    return;
}
```

The deal winner is the player whose `roundWinnerPoints.Winner` equals their position
(see §13).

### Winning the game

`SantaseMatch.GetMatchWinner()`:

```csharp
if (this.FirstPlayerTotalPoints >= this.gameRules.GamePointsNeededForWin) return PlayerPosition.FirstPlayer;
if (this.SecondPlayerTotalPoints >= this.gameRules.GamePointsNeededForWin) return PlayerPosition.SecondPlayer;
return PlayerPosition.NoOne;
```

After each deal `SantaseMatch` deals the next one, until one player reaches or passes 11
game points (`SantaseGameRules.GamePointsNeededForWin`). A host can end a match early with
`SantaseMatch.Stop()` (a resignation or an abandoned table): it then has no winner.

---

## 16. Worked example: the flow of one deal

1. **Deal.** Each player gets 6 cards. The 13th card (say A♥) is turned up → trumps are
   Hearts. The remaining 11 cards + the trump card = a 12-card talon.
2. **Trick 1** (`StartRoundState`). The non-dealer (opener) leads; the dealer responds.
   The winner takes both cards, draws from the talon, then the loser draws. The talon
   drops from 12 to 10. Transition → `MoreThanTwoCardsLeftRoundState`.
3. **Tricks 2–5** (`MoreThanTwoCardsLeftRoundState`). As above, but the player on lead may
   now:
   - Announce K+Q (20 normal, 40 trump).
   - Exchange the trump Nine for the trump card.
   - Close the talon.
   After trick 5 the talon has 2 cards → transition → `TwoCardsLeftRoundState`.
4. **Trick 6** (`TwoCardsLeftRoundState`). No closing, no trump-Nine exchange.
   Announcements are still possible. After the trick the winner draws the last "normal"
   card; the loser draws the face-up trump. The talon is empty → transition →
   `FinalRoundState`.
5. **Tricks 7–12** (`FinalRoundState`). Strict rules:
   - Must follow suit.
   - Must head the trick within the led suit if possible.
   - Must trump if you have no card of the led suit.
6. **End of trick 12.** Both hands are empty. The winner of trick 12 gets the **+10
   bonus** (because the talon was never closed).
7. **Score the deal** via `RoundWinnerPointsPointsLogic.GetWinnerPoints` → 0, 1, 2 or 3
   game points.
8. **Next deal.** The deal winner becomes dealer; the opponent opens. On a draw the same
   player opens.

Deals are played until one player reaches 11 game points.

---

## 17. Code reference index

| Concept | File |
|---------|------|
| A match, one action at a time (the rules' flow) | `GameMechanics/SantaseMatch.cs` |
| A whole match between two `IPlayer`s | `GameMechanics/SantaseGame.cs` |
| One deal (round), trick by trick | `GameMechanics/Round.cs` (internal) |
| What a seat may see; the match record | `GameMechanics/SantaseSeatView.cs`, `SantaseMatchRecord.cs` |
| Deal result | `GameMechanics/RoundResult.cs` |
| Per-player deal state | `GameMechanics/RoundPlayerInfo.cs` |
| Context passed to the AI each turn | `Players/PlayerTurnContext.cs` |
| Card flyweight | `Cards/Card.cs` |
| Pack | `Cards/Deck.cs` |
| Phase state machine | `RoundStates/*.cs` |
| Validate a played card | `PlayerActionValidate/PlayCardActionValidator.cs` |
| Validate a marriage announcement | `PlayerActionValidate/AnnounceValidator.cs` |
| Validate a trump exchange | `PlayerActionValidate/ChangeTrumpActionValidator.cs` |
| Validate a close | `PlayerActionValidate/CloseGameActionValidator.cs` |
| Validate any action (dispatcher) | `PlayerActionValidate/PlayerActionValidator.cs` |
| Determine the trick winner | `WinnerLogic/CardWinnerLogic.cs` |
| Score the end of a deal | `WinnerLogic/RoundWinnerPointsPointsLogic.cs` |
| Rules configuration | `SantaseGameRules.cs`, `IGameRules.cs` |

---

## 18. Implementation decisions and variants

Where rule sources differ, the engine makes a concrete choice. They are collected here for
easy review:

| Rule | Engine's choice | Alternative variant (not implemented) |
|------|-----------------|---------------------------------------|
| Game target | 11 game points | 7 (Pagat) |
| Failed close | **Always 3** game points to the opponent | 2 (3 if the opponent had no trick) |
| Trump-Nine exchange window | Talon has **> 2** cards | Talon has > 3 cards; or "after opponent closes, even with no trick won" (Pagat) |
| Marriage in Phase 2 (closed/exhausted talon) | **Allowed** | Forbidden (Pagat / some Schnapsen variants) |
| Marriage during the first trick | **Forbidden** | Varies in some rule sets |
| Last trick after exhaustion | **+10 card points** | Outright deal win (Schnapsen) |
| Opener after a draw | Same opener (unchanged) | (Universally the same — no real variance) |
| 3-3 / turn-trump / 3-3 deal order | Simplified (single shuffle) | Step-by-step simulated deal |
| Schneider threshold | Opponent **≥ 33** → 1 pt; 1–32 with a trick → 2 pt | "< 31 (≤ 30)" cited by some Bulgarian sources |

---

## 19. What is **not** implemented in this engine

- **3- and 4-player variants** of 66 (Bauernschnapsen, Gaigel, etc.) — the engine is
  strictly two-player.
- **The Schnapsen variant** (20 cards without Nines; Jack-for-trump exchange; outright win
  on last trick; etc.).
- **The opponent-closed immediate trump-Nine exchange** exception (Pagat).
- Any physical scoring aids (sevens, side cards, etc.).
- The branching **2/3** failed-close penalty — the engine always awards 3.

---

## 20. Sources

Bulgarian rule descriptions used for cross-checking:

- [sa-igri.com — Santase 66 rules](https://www.sa-igri.com/santase-66-pravila/)
- [Wikipedia BG — Sixty-six](https://bg.wikipedia.org/wiki/%D0%A8%D0%B5%D1%81%D1%82%D0%B4%D0%B5%D1%81%D0%B5%D1%82_%D0%B8_%D1%88%D0%B5%D1%81%D1%82_(%D0%B8%D0%B3%D1%80%D0%B0_%D1%81_%D0%BA%D0%B0%D1%80%D1%82%D0%B8))
- [santase.bg — Rules of Santase (66)](http://santase.bg/pravila)
- [belot.bg — Santase rules (English)](https://belot.bg/en/rules-santase/)

English references (different variants — they do not match this engine exactly):

- [Pagat.com — 66](https://www.pagat.com/marriage/66.html)
- [Pagat.com — Schnapsen](https://www.pagat.com/marriage/schnaps.html)
- [Wikipedia — Sixty-six (card game)](https://en.wikipedia.org/wiki/Sixty-six_(card_game))

The authoritative specification for *this* engine is the source code itself; every rule
above was verified against `src/Santase.Logic` and is cross-referenced to it.
