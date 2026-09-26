# SantaseGameEngine

A rules engine for **Santase**, also known as **66**, Сантасе, **Sixty-six**, **Sechsundsechzig** or
**Schnapsen**: the fast two-player trick-taking game played with 24 cards (Ace, Ten, King, Queen,
Jack and Nine of each suit).

The engine plays the whole game: the deal, the trump card, exchanging the trump Nine, closing the
talon, the marriages (20 and 40), the follow-suit rules once the talon is closed or empty, round
scoring (1, 2 or 3 game points) and the match to 11. It has no dependencies and is fast enough for
millions of simulated games.

```shell
dotnet add package SantaseGameEngine
```

## Write a player

A player implements `IPlayer`; `BasePlayer` keeps the hand for you. The engine asks for a move with a
`PlayerTurnContext` (trump, points, the card led, the round's phase), and the validators say which
moves are legal. Marriages are announced by the engine itself: leading a King or Queen while holding
its partner scores the 20 or 40 whenever announcing is allowed.

```csharp
using System.Linq;

using Santase.Logic.Players;

public class LowestCardPlayer : BasePlayer
{
    public override string Name => "Lowest card";

    public override PlayerAction GetTurn(PlayerTurnContext context)
    {
        var playable = this.PlayerActionValidator.GetPossibleCardsToPlay(context, this.Cards);
        return this.PlayCard(playable.OrderBy(card => card.GetValue()).First());
    }
}
```

## Play a match

`SantaseGame` plays a whole match between two players and returns the winner:

```csharp
using Santase.Logic;
using Santase.Logic.GameMechanics;

var game = new SantaseGame(new LowestCardPlayer(), new LowestCardPlayer());
PlayerPosition winner = game.Start();
Console.WriteLine($"{winner} won {game.FirstPlayerTotalPoints}-{game.SecondPlayerTotalPoints} in {game.RoundsPlayed} rounds.");
```

## Drive a match one move at a time

`SantaseMatch` never waits for a player, which is what a user interface or a game server needs: read
whose move it is, get their answer however you like (a tap, a network message, a bot), and pass it to
`Act`. The match plays forward to the next decision: it resolves the trick, draws, scores the round
and deals the next one.

```csharp
using Santase.Logic;
using Santase.Logic.GameMechanics;
using Santase.Logic.Players;

var match = new SantaseMatch(new SantaseMatchOptions { Shuffle = n => Random.Shared.Next(n) });
match.Start();

while (!match.IsFinished)
{
    PlayerPosition seat = match.ToMove;
    SantaseSeatView view = match.GetView(seat);

    // What this seat may see: its own hand, the playable cards, whether it can exchange the Nine
    // or close, both players' points, the trump, the tricks so far. Never the opponent's hand.
    PlayerAction action = PlayerAction.PlayCard(view.PlayableCards[0]);

    SantaseActResult result = match.Act(seat, action); // Ok, InvalidAction, NotYourTurn or MatchFinished
}

SantaseMatchRecord record = match.GetRecord();
```

- `Validate(seat, action)` answers what `Act` would, without changing anything.
- `Stop()` ends a match early (a resignation, a timeout, an abandoned table): no winner, and the
  final view and record become available with the unfinished round last.
- `SantaseMatchOptions` sets the first player, the rules, a logger and `Shuffle`, the random source
  for every deal (`Deck` documents the exact order it is used in, so a deal can be reproduced).
- Players passed to the constructor are observers: they get every callback except `GetTurn`.

## Views and records

`SantaseSeatView` and `SantaseMatchRecord` are plain classes whose public properties are all there is,
so you can map them to your own models and store or send them in any format.

- `view.CreateTurnContext()` rebuilds the exact `PlayerTurnContext` the engine would give that seat,
  so a view is enough to feed a validator or a bot.
- A record holds every round's deal in draw order, its tricks, who exchanged the Nine and who closed
  (and after how many tricks), and each round's result: enough to replay the match exactly.
- `CardCode` writes and parses cards as short text codes: `9C`, `10D`, `JH`, `QS`, `KC`, `AH`.

## Players that decide from a view

A player implementing `IRestorablePlayer` rebuilds its memory from a view, so a server does not have
to keep a player object alive between moves:

```csharp
PlayerAction move = new MyRestorablePlayer().ChooseMove(match.GetView(match.ToMove));
match.Act(match.ToMove, move);
```

## Rules

The default rules are the Bulgarian Santase rules (`GameRulesProvider.Santase`): 6 cards each, 66
points to win a round, 11 game points to win the match. Other variants plug in through `IGameRules`.
The full rules as the engine implements them, with the code that implements each one, are in
[RULES.md](https://github.com/NikolayIT/SantaseGameEngine/blob/master/RULES.md).

## More

The [source repository](https://github.com/NikolayIT/SantaseGameEngine) also has several computer
players built on this engine (from random to an information-set Monte Carlo tree search), a
simulator that benchmarks them against each other, and a cross-platform .NET MAUI app.
