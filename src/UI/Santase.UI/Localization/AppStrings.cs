namespace Santase.UI.Localization
{
    using System.Collections.Generic;

    /// <summary>
    /// The English + Bulgarian string tables. Looked up by <see cref="LocalizationManager"/>.
    /// Kept as plain in-code dictionaries (no .resx / satellite assemblies) so the lookup is
    /// trimming/AOT-safe and identical on every MAUI platform. Missing keys fall back to English,
    /// then to the key itself.
    /// </summary>
    internal static class AppStrings
    {
        private static readonly Dictionary<string, string> En = new()
        {
            // ----- Start page -----
            ["Start_YourRating"] = "Your rating",
            ["Start_NoGames"] = "Play a ranked game to start earning rating",
            ["Start_RecordFormat"] = "{0} games · {1} won · {2} lost",
            ["Start_Statistics"] = "Statistics",
            ["Start_Settings"] = "Settings",
            ["Start_HowToPlay"] = "How to play",
            ["Start_YourName"] = "YOUR NAME",
            ["Start_ChooseOpponent"] = "CHOOSE YOUR OPPONENT",
            ["Start_RecentGames"] = "RECENT GAMES",
            ["Start_SeeAll"] = "See all",
            ["Start_NoHistory"] = "No games played yet — pick an opponent above",
            ["Start_HotSeatTitle"] = "TWO PLAYERS · ONE DEVICE",
            ["Start_HotSeatHint"] = "Pass-and-play with a friend on this device.",
            ["Start_HotSeat"] = "Start two-player game",
            ["Start_Player1"] = "Player 1",
            ["Start_Player2"] = "Player 2",
            ["Start_Subtitle"] = "66 · Schnapsen · Sechsundsechzig",

            // ----- Opponents -----
            ["Opp_Dummy_Name"] = "Lucky",
            ["Opp_Smart_Name"] = "Veteran",
            ["Opp_Claude_Name"] = "Strategist",
            ["Opp_Neural_Name"] = "Prodigy",
            ["Opp_Ismcts_Name"] = "Grandmaster",
            ["Opp_Dummy_Tag"] = "Plays completely at random — a friendly first opponent.",
            ["Opp_Smart_Tag"] = "Counts every card and plays a solid, classic game.",
            ["Opp_Claude_Tag"] = "Finely tuned strategy with a flawless endgame.",
            ["Opp_Neural_Tag"] = "A neural network that taught itself to play.",
            ["Opp_Ismcts_Tag"] = "Simulates thousands of deals before every move. The strongest opponent.",
            ["Diff_1"] = "Beginner",
            ["Diff_2"] = "Intermediate",
            ["Diff_3"] = "Advanced",
            ["Diff_4"] = "Expert",
            ["Diff_5"] = "Master",
            ["Opp_RecordFormat"] = "Your record: {0}W – {1}L",

            // ----- Game page -----
            ["Game_Round"] = "Round",
            ["Game_Game"] = "Game",
            ["Game_Deck"] = "Deck",
            ["Game_SwapTrump"] = "Swap 9 ↔ trump",
            ["Game_Close"] = "Close",
            ["Game_Menu"] = "Menu",
            ["Game_ClosedByYou"] = "CLOSED BY YOU",
            ["Game_ClosedByOpp"] = "CLOSED BY OPPONENT",
            ["Game_LastTrick"] = "Last trick",
            ["Game_Hint"] = "💡 Hint",

            ["Status_Dealing"] = "Dealing…",
            ["Status_NewRound"] = "New round",
            ["Status_YourTurn"] = "Your turn — tap a card",
            ["Status_OpponentTurn"] = "Waiting for {0}…",

            ["Toast_SwapTrump"] = "{0} swapped the trump 9",
            ["Toast_YouClosed"] = "You closed the game",
            ["Toast_OppClosed"] = "{0} closed the game",
            ["Toast_Announce20"] = "{0} announced 20!",
            ["Toast_Announce40"] = "{0} announced 40!",
            ["Word_You"] = "You",

            ["Hint_SwapTrump"] = "Hint: swap the 9 for the trump card",
            ["Hint_CloseGame"] = "Hint: close the game and race to 66",
            ["Hint_None"] = "No hint available right now",

            ["Close_Title"] = "Close the game?",
            ["Close_Message"] = "No more cards will be drawn and strict rules begin. If you fail to reach 66 points, {0} wins 3 game points.",
            ["Close_Confirm"] = "Close",
            ["Common_Cancel"] = "Cancel",

            ["Handoff_Pass"] = "Pass the device to {0}\nTap when ready to start your turn",
            ["Handoff_Ready"] = "I'm ready",

            // ----- Round / game end -----
            ["Round_YouWon"] = "You won the round!",
            ["Round_OppWon"] = "{0} won the round",
            ["Round_ScoreHeader"] = "ROUND POINTS",
            ["Round_Winner"] = "WINNER",
            ["Round_GamePointsHeader"] = "GAME POINTS",
            ["Round_Marriages"] = "MARRIAGES",
            ["Common_Continue"] = "Continue",
            ["Common_Match"] = "MATCH",

            ["GameOver_Victory"] = "Victory!",
            ["GameOver_Defeat"] = "Defeat",
            ["GameOver_WonGame"] = "{0} wins the game",
            ["GameOver_FinalScore"] = "FINAL SCORE",
            ["GameOver_PlayAgain"] = "Play again",
            ["GameOver_BackToMenu"] = "Back to menu",

            ["Leave_Title"] = "Leave game?",
            ["Leave_Message"] = "The current game will be abandoned.",
            ["Leave_Confirm"] = "Leave",
            ["Leave_Cancel"] = "Cancel",

            ["Rating_Change"] = "Your rating  {0}  →  {1}   ({2})",
            ["Award_Format"] = "{0}   +{1} {2}",
            ["Word_PointSingular"] = "game point",
            ["Word_PointPlural"] = "game points",

            ["Error_Title"] = "Game error",
            ["Error_Body"] = "An unexpected error stopped the game.\n\n{0}: {1}",

            ["History_Win"] = "W",
            ["History_Loss"] = "L",
            ["History_Vs"] = "vs",

            // ----- Settings page -----
            ["Settings_Title"] = "Settings",
            ["Settings_Gameplay"] = "GAMEPLAY",
            ["Settings_Language"] = "Language",
            ["Settings_GameSpeed"] = "Game speed",
            ["Settings_GameSpeedHint"] = "How quickly moves play out and tricks clear. Pacing only — it never affects how well the computer plays.",
            ["Speed_Relaxed"] = "Relaxed",
            ["Speed_Normal"] = "Normal",
            ["Speed_Fast"] = "Fast",
            ["Settings_Haptics"] = "Vibration",
            ["Settings_HapticsHint"] = "Gentle feedback on your turn and at round end.",
            ["Settings_Assists"] = "Beginner assists",
            ["Settings_AssistsHint"] = "Show 20/40 badges on your cards and the hint button.",
            ["Settings_Data"] = "DATA",
            ["Settings_ResetStats"] = "Reset statistics",
            ["Reset_Title"] = "Reset statistics?",
            ["Reset_Message"] = "Your rating, game history and records will be permanently deleted.",
            ["Reset_Confirm"] = "Reset",
            ["Reset_Done"] = "Statistics were reset",
            ["Settings_About"] = "ABOUT",
            ["Settings_Version"] = "Version {0}",
            ["Settings_OpenSource"] = "Open source on GitHub",
            ["Settings_EngineNote"] = "Powered by the SantaseGameEngine — the same engine that trains the AI opponents.",

            // ----- Statistics page -----
            ["Stats_Title"] = "Statistics",
            ["Stats_Rating"] = "RATING",
            ["Stats_Current"] = "Current",
            ["Stats_Peak"] = "Peak",
            ["Stats_Games"] = "Games",
            ["Stats_WinRate"] = "Win rate",
            ["Stats_Wins"] = "Wins",
            ["Stats_Losses"] = "Losses",
            ["Stats_CurrentStreak"] = "Streak",
            ["Stats_BestStreak"] = "Best win streak",
            ["Stats_ByOpponent"] = "RESULTS BY OPPONENT",
            ["Stats_HistoryHeader"] = "GAME HISTORY",
            ["Stats_Empty"] = "No ranked games yet. Beat an opponent to start your record!",
            ["Stats_NotPlayed"] = "Not played yet",
            ["Stats_GamesFormat"] = "{0} games",

            // ----- How to play page -----
            ["Rules_Title"] = "How to play",
            ["Rules_Intro"] = "Santase (66, Schnapsen) is a two-player trick-taking card game. Win rounds to collect game points — the first player to reach 11 game points wins.",
            ["Rules_Cards_Title"] = "Cards & values",
            ["Rules_Cards_Body"] = "The deck has 24 cards: 9, J, Q, K, 10 and A in each suit. Card points: Ace 11 · Ten 10 · King 4 · Queen 3 · Jack 2 · Nine 0. Each player is dealt six cards, and one card is turned face up — its suit is the trump suit for the round.",
            ["Rules_Play_Title"] = "Playing tricks",
            ["Rules_Play_Body"] = "The leader plays a card and the opponent answers. The higher card of the led suit wins the trick, but any trump beats any non-trump. While cards remain in the deck you may answer with any card — no need to follow suit. After each trick both players draw a card, and the trick winner leads next. Points from the two cards in every trick you win count toward 66.",
            ["Rules_Marriages_Title"] = "Marriages — 20 & 40",
            ["Rules_Marriages_Body"] = "Holding the King and Queen of the same suit is a marriage. Lead either of them and the marriage is announced automatically: 20 points, or 40 in the trump suit. The badges on your cards show when a lead would announce a marriage.",
            ["Rules_Nine_Title"] = "The trump nine",
            ["Rules_Nine_Body"] = "If you hold the 9 of trumps, it is your lead, and more than two cards remain in the deck, you may swap the 9 for the face-up trump card — trading your weakest trump for a stronger one.",
            ["Rules_Closing_Title"] = "Closing the game",
            ["Rules_Closing_Body"] = "When it is your lead (with more than two cards in the deck) you may close: no more cards are drawn and strict rules begin immediately. Close when your hand looks strong enough to reach 66 — if you fall short, your opponent scores 3 game points.",
            ["Rules_Endgame_Title"] = "When the deck is out",
            ["Rules_Endgame_Body"] = "Once the deck is exhausted or the game is closed, strict rules apply: you must follow suit, you must beat the led card if you can, and you must trump when you cannot follow suit. If nobody closed, winning the very last trick earns a +10 bonus.",
            ["Rules_Scoring_Title"] = "Winning a round",
            ["Rules_Scoring_Body"] = "The moment you collect 66 or more points, the round ends in your favour. The winner earns game points: 1 if the loser has 33 or more, 2 if the loser has fewer than 33, and 3 if the loser took no tricks at all. If nobody reaches 66 by the last trick, the higher total wins 1 game point.",
            ["Rules_Match_Title"] = "Winning the game",
            ["Rules_Match_Body"] = "Game points add up round after round. The first player to reach 11 game points wins. Ranked games against the computer also move your ELO rating — beat stronger opponents to climb faster.",
        };

        private static readonly Dictionary<string, string> Bg = new()
        {
            // ----- Start page -----
            ["Start_YourRating"] = "Твоят рейтинг",
            ["Start_NoGames"] = "Изиграй игра срещу компютъра, за да трупаш рейтинг",
            ["Start_RecordFormat"] = "{0} игри · {1} победи · {2} загуби",
            ["Start_Statistics"] = "Статистика",
            ["Start_Settings"] = "Настройки",
            ["Start_HowToPlay"] = "Как се играе",
            ["Start_YourName"] = "ТВОЕТО ИМЕ",
            ["Start_ChooseOpponent"] = "ИЗБЕРИ ПРОТИВНИК",
            ["Start_RecentGames"] = "ПОСЛЕДНИ ИГРИ",
            ["Start_SeeAll"] = "Виж всички",
            ["Start_NoHistory"] = "Още няма изиграни игри — избери противник отгоре",
            ["Start_HotSeatTitle"] = "ДВАМА ИГРАЧИ · ЕДНО УСТРОЙСТВО",
            ["Start_HotSeatHint"] = "Играй с приятел, като си подавате устройството.",
            ["Start_HotSeat"] = "Започни игра за двама",
            ["Start_Player1"] = "Играч 1",
            ["Start_Player2"] = "Играч 2",
            ["Start_Subtitle"] = "66 · Шнапсен · Сантасе",

            // ----- Opponents -----
            ["Opp_Dummy_Name"] = "Късметлия",
            ["Opp_Smart_Name"] = "Ветеран",
            ["Opp_Claude_Name"] = "Стратег",
            ["Opp_Neural_Name"] = "Феномен",
            ["Opp_Ismcts_Name"] = "Гросмайстор",
            ["Opp_Dummy_Tag"] = "Играе напълно произволно — идеалният първи противник.",
            ["Opp_Smart_Tag"] = "Брои излезлите карти и играе солидна, класическа игра.",
            ["Opp_Claude_Tag"] = "Прецизно настроена стратегия с безупречен финал.",
            ["Opp_Neural_Tag"] = "Невронна мрежа, която сама се е научила да играе.",
            ["Opp_Ismcts_Tag"] = "Симулира хиляди раздавания преди всеки ход. Най-силният противник.",
            ["Diff_1"] = "Начинаещ",
            ["Diff_2"] = "Среден",
            ["Diff_3"] = "Напреднал",
            ["Diff_4"] = "Експерт",
            ["Diff_5"] = "Майстор",
            ["Opp_RecordFormat"] = "Твоят резултат: {0}П – {1}З",

            // ----- Game page -----
            ["Game_Round"] = "Ръка",
            ["Game_Game"] = "Игра",
            ["Game_Deck"] = "Тесте",
            ["Game_SwapTrump"] = "Смени 9 ↔ коз",
            ["Game_Close"] = "Затвори",
            ["Game_Menu"] = "Меню",
            ["Game_ClosedByYou"] = "ЗАТВОРЕНА ОТ ТЕБ",
            ["Game_ClosedByOpp"] = "ЗАТВОРЕНА ОТ ПРОТИВНИКА",
            ["Game_LastTrick"] = "Последна взятка",
            ["Game_Hint"] = "💡 Съвет",

            ["Status_Dealing"] = "Раздаване…",
            ["Status_NewRound"] = "Нова ръка",
            ["Status_YourTurn"] = "Твой ред — избери карта",
            ["Status_OpponentTurn"] = "Изчакваме {0}…",

            ["Toast_SwapTrump"] = "{0} смени козовата 9",
            ["Toast_YouClosed"] = "Ти затвори играта",
            ["Toast_OppClosed"] = "{0} затвори играта",
            ["Toast_Announce20"] = "{0} обяви 20!",
            ["Toast_Announce40"] = "{0} обяви 40!",
            ["Word_You"] = "Ти",

            ["Hint_SwapTrump"] = "Съвет: смени деветката с коза",
            ["Hint_CloseGame"] = "Съвет: затвори играта и гони 66",
            ["Hint_None"] = "В момента няма съвет",

            ["Close_Title"] = "Затваряне на играта?",
            ["Close_Message"] = "Спира тегленето на карти и влизат строгите правила. Ако не стигнеш 66 точки, {0} печели 3 точки.",
            ["Close_Confirm"] = "Затвори",
            ["Common_Cancel"] = "Отказ",

            ["Handoff_Pass"] = "Подай устройството на {0}\nДокосни, когато си готов",
            ["Handoff_Ready"] = "Готов съм",

            // ----- Round / game end -----
            ["Round_YouWon"] = "Спечели ръката!",
            ["Round_OppWon"] = "{0} спечели ръката",
            ["Round_ScoreHeader"] = "ТОЧКИ В РЪКАТА",
            ["Round_Winner"] = "ПОБЕДИТЕЛ",
            ["Round_GamePointsHeader"] = "ТОЧКИ ЗА ИГРАТА",
            ["Round_Marriages"] = "АНОНСИ",
            ["Common_Continue"] = "Продължи",
            ["Common_Match"] = "МАЧ",

            ["GameOver_Victory"] = "Победа!",
            ["GameOver_Defeat"] = "Загуба",
            ["GameOver_WonGame"] = "{0} печели играта",
            ["GameOver_FinalScore"] = "КРАЕН РЕЗУЛТАТ",
            ["GameOver_PlayAgain"] = "Нова игра",
            ["GameOver_BackToMenu"] = "Към менюто",

            ["Leave_Title"] = "Напускане на играта?",
            ["Leave_Message"] = "Текущата игра ще бъде прекратена.",
            ["Leave_Confirm"] = "Напусни",
            ["Leave_Cancel"] = "Отказ",

            ["Rating_Change"] = "Твоят рейтинг  {0}  →  {1}   ({2})",
            ["Award_Format"] = "{0}   +{1} {2}",
            ["Word_PointSingular"] = "точка",
            ["Word_PointPlural"] = "точки",

            ["Error_Title"] = "Грешка",
            ["Error_Body"] = "Неочаквана грешка спря играта.\n\n{0}: {1}",

            ["History_Win"] = "П",
            ["History_Loss"] = "З",
            ["History_Vs"] = "срещу",

            // ----- Settings page -----
            ["Settings_Title"] = "Настройки",
            ["Settings_Gameplay"] = "ИГРА",
            ["Settings_Language"] = "Език",
            ["Settings_GameSpeed"] = "Скорост на играта",
            ["Settings_GameSpeedHint"] = "Колко бързо се разиграват ходовете и се прибират картите. Влияе само на темпото — не и на силата на компютъра.",
            ["Speed_Relaxed"] = "Спокойна",
            ["Speed_Normal"] = "Нормална",
            ["Speed_Fast"] = "Бърза",
            ["Settings_Haptics"] = "Вибрация",
            ["Settings_HapticsHint"] = "Леко потрепване при твой ред и в края на ръката.",
            ["Settings_Assists"] = "Помощ за начинаещи",
            ["Settings_AssistsHint"] = "Показва значки 20/40 върху картите и бутона за съвет.",
            ["Settings_Data"] = "ДАННИ",
            ["Settings_ResetStats"] = "Нулирай статистиката",
            ["Reset_Title"] = "Нулиране на статистиката?",
            ["Reset_Message"] = "Рейтингът, историята и резултатите ти ще бъдат изтрити завинаги.",
            ["Reset_Confirm"] = "Нулирай",
            ["Reset_Done"] = "Статистиката е нулирана",
            ["Settings_About"] = "ЗА ИГРАТА",
            ["Settings_Version"] = "Версия {0}",
            ["Settings_OpenSource"] = "Отворен код в GitHub",
            ["Settings_EngineNote"] = "Задвижвана от SantaseGameEngine — същият енджин, който обучава компютърните противници.",

            // ----- Statistics page -----
            ["Stats_Title"] = "Статистика",
            ["Stats_Rating"] = "РЕЙТИНГ",
            ["Stats_Current"] = "Сегашен",
            ["Stats_Peak"] = "Връх",
            ["Stats_Games"] = "Игри",
            ["Stats_WinRate"] = "Успеваемост",
            ["Stats_Wins"] = "Победи",
            ["Stats_Losses"] = "Загуби",
            ["Stats_CurrentStreak"] = "Серия",
            ["Stats_BestStreak"] = "Най-дълга победна серия",
            ["Stats_ByOpponent"] = "РЕЗУЛТАТИ ПО ПРОТИВНИК",
            ["Stats_HistoryHeader"] = "ИСТОРИЯ НА ИГРИТЕ",
            ["Stats_Empty"] = "Още няма изиграни игри. Победи някой противник, за да започнеш статистика!",
            ["Stats_NotPlayed"] = "Още не сте играли",
            ["Stats_GamesFormat"] = "{0} игри",

            // ----- How to play page -----
            ["Rules_Title"] = "Как се играе",
            ["Rules_Intro"] = "Сантасето (66, Шнапсен) е игра с взятки за двама. Печели ръце, за да събираш точки за играта — първият, стигнал 11 точки, печели.",
            ["Rules_Cards_Title"] = "Карти и точки",
            ["Rules_Cards_Body"] = "Тестето има 24 карти: 9, J, Q, K, 10 и A от всяка боя. Точки: Асо 11 · Десетка 10 · Поп 4 · Дама 3 · Вале 2 · Девятка 0. Всеки играч получава шест карти, а една карта се обръща — нейната боя е коз за ръката.",
            ["Rules_Play_Title"] = "Взятките",
            ["Rules_Play_Body"] = "Водещият играе карта, противникът отговаря. По-високата карта от исканата боя печели взятката, но всеки коз бие всяка друга карта. Докато в тестето има карти, може да отговаряш с каквато и да е карта — не си длъжен да отговаряш на боята. След всяка взятка двамата теглят по карта, а победителят води следващата. Точките от двете карти във всяка спечелена взятка се броят към 66.",
            ["Rules_Marriages_Title"] = "Анонси — 20 и 40",
            ["Rules_Marriages_Body"] = "Поп и дама от една боя правят анонс (двойка). Поведи с една от двете карти и анонсът се обявява автоматично: 20 точки, а в козовата боя — 40. Значките върху картите ти показват кога воденето ще обяви анонс.",
            ["Rules_Nine_Title"] = "Козовата девятка",
            ["Rules_Nine_Body"] = "Ако държиш козовата девятка, ти водиш и в тестето има повече от две карти, може да я смениш с обърнатия коз — даваш най-слабия си коз за по-силен.",
            ["Rules_Closing_Title"] = "Затваряне",
            ["Rules_Closing_Body"] = "Когато водиш (и в тестето има повече от две карти), може да затвориш: спира тегленето и веднага влизат строгите правила. Затваряй, когато ръката ти изглежда достатъчна за 66 — ако не стигнеш, противникът печели 3 точки.",
            ["Rules_Endgame_Title"] = "Когато тестето свърши",
            ["Rules_Endgame_Body"] = "Щом тестето се изчерпи или играта е затворена, важат строгите правила: длъжен си да отговаряш на боята, да качваш, ако можеш, и да цакаш с коз, когато нямаш от боята. Ако никой не е затварял, последната взятка носи бонус +10.",
            ["Rules_Scoring_Title"] = "Печелене на ръката",
            ["Rules_Scoring_Body"] = "В момента, в който събереш 66 или повече точки, ръката приключва в твоя полза. Победителят взима точки за играта: 1, ако губещият има 33 или повече; 2, ако има под 33; и 3, ако не е взел нито една взятка. Ако никой не стигне 66 до последната взятка, по-високият сбор носи 1 точка.",
            ["Rules_Match_Title"] = "Печелене на играта",
            ["Rules_Match_Body"] = "Точките за играта се трупат ръка след ръка. Първият, стигнал 11 точки, печели. Игрите срещу компютъра движат и твоя ELO рейтинг — побеждавай по-силни противници, за да се изкачваш по-бързо.",
        };

        public static string Get(string lang, string key)
        {
            var table = lang == LocalizationManager.Bulgarian ? Bg : En;
            if (table.TryGetValue(key, out var value))
            {
                return value;
            }

            return En.TryGetValue(key, out var fallback) ? fallback : key;
        }
    }
}
