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
            ["Start_YourRating"] = "Your rating",
            ["Start_NoGames"] = "No games yet — beat Dummy to climb",
            ["Start_RecordFormat"] = "{0} games · {1}W – {2}L",
            ["Start_You"] = "You",
            ["Start_ChooseOpponent"] = "CHOOSE OPPONENT",
            ["Start_RecentGames"] = "RECENT GAMES",
            ["Start_NoHistory"] = "No games played yet",
            ["Start_HotSeat"] = "2-Player Hot-Seat",
            ["Start_Player1"] = "Player 1",
            ["Start_Player2"] = "Player 2",
            ["Start_Subtitle"] = "66 · Schnapsen · Sechsundsechzig",
            ["Start_P2"] = "P2",

            ["Opp_Dummy_Name"] = "Dummy",
            ["Opp_Smart_Name"] = "Smart Player",
            ["Opp_Claude_Name"] = "Claude",
            ["Opp_Neural_Name"] = "Claude Neural",
            ["Opp_Ismcts_Name"] = "Claude MCTS",
            ["Opp_Dummy_Tag"] = "Plays random legal cards. A gentle warm-up.",
            ["Opp_Smart_Tag"] = "Counts cards and follows solid heuristics.",
            ["Opp_Claude_Tag"] = "Hand-tuned heuristics with an exact endgame solver.",
            ["Opp_Neural_Tag"] = "A PPO-trained neural-network policy.",
            ["Opp_Ismcts_Tag"] = "Information-set Monte-Carlo tree search. The strongest.",

            ["Game_Round"] = "Round",
            ["Game_Game"] = "Game",
            ["Game_Deck"] = "Deck",
            ["Game_SwapTrump"] = "Swap 9 ↔ trump",
            ["Game_Close"] = "Close",
            ["Game_Menu"] = "Menu",
            ["Game_ClosedByYou"] = "GAME CLOSED",
            ["Game_ClosedByOpp"] = "OPPONENT CLOSED",

            ["Status_Dealing"] = "Dealing…",
            ["Status_NewRound"] = "New round",
            ["Status_YourTurn"] = "Your turn",
            ["Status_OpponentTurn"] = "{0}'s turn…",

            ["Toast_SwapTrump"] = "{0} swapped the trump 9",
            ["Toast_YouClosed"] = "You closed the game",
            ["Toast_OppClosed"] = "{0} closed the game",
            ["Toast_Announce20"] = "{0} announced marriage 20!",
            ["Toast_Announce40"] = "{0} announced trump marriage 40!",
            ["Word_You"] = "You",

            ["Handoff_Pass"] = "Pass the device to {0}\nTap when ready to start your turn",
            ["Handoff_Ready"] = "I'm ready",

            ["Round_YouWon"] = "You won the round!",
            ["Round_OppWon"] = "{0} won the round",
            ["Round_ScoreHeader"] = "ROUND POINTS",
            ["Round_Winner"] = "WINNER",
            ["Round_GamePointsHeader"] = "GAME POINTS",
            ["Round_Marriages"] = "MARRIAGES",
            ["Common_Continue"] = "Continue",
            ["Common_Match"] = "MATCH",

            ["GameOver_Victory"] = "Victory!",
            ["GameOver_Defeat"] = "Game Over",
            ["GameOver_WonGame"] = "{0} won the game",
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
        };

        private static readonly Dictionary<string, string> Bg = new()
        {
            ["Start_YourRating"] = "Твоят рейтинг",
            ["Start_NoGames"] = "Още няма игри — победи Dummy, за да се изкачиш",
            ["Start_RecordFormat"] = "{0} игри · {1}П – {2}З",
            ["Start_You"] = "Ти",
            ["Start_ChooseOpponent"] = "ИЗБЕРИ ПРОТИВНИК",
            ["Start_RecentGames"] = "ПОСЛЕДНИ ИГРИ",
            ["Start_NoHistory"] = "Все още няма изиграни игри",
            ["Start_HotSeat"] = "Двама на едно устройство",
            ["Start_Player1"] = "Играч 1",
            ["Start_Player2"] = "Играч 2",
            ["Start_Subtitle"] = "66 · Шнапсен · Сантасе",
            ["Start_P2"] = "И2",

            ["Opp_Dummy_Name"] = "Балък",
            ["Opp_Smart_Name"] = "Умен играч",
            ["Opp_Claude_Name"] = "Claude",
            ["Opp_Neural_Name"] = "Claude Невронен",
            ["Opp_Ismcts_Name"] = "Claude MCTS",
            ["Opp_Dummy_Tag"] = "Играе случайни позволени карти. Лесно загряване.",
            ["Opp_Smart_Tag"] = "Брои картите и следва солидни правила.",
            ["Opp_Claude_Tag"] = "Ръчно настроена логика с точно изчисление на финала.",
            ["Opp_Neural_Tag"] = "Невронна мрежа, обучена с PPO.",
            ["Opp_Ismcts_Tag"] = "Монте-Карло дървесно търсене. Най-силният.",

            ["Game_Round"] = "Ръка",
            ["Game_Game"] = "Игра",
            ["Game_Deck"] = "Тесте",
            ["Game_SwapTrump"] = "Смени 9 ↔ коз",
            ["Game_Close"] = "Затвори",
            ["Game_Menu"] = "Меню",
            ["Game_ClosedByYou"] = "ТИ ЗАТВОРИ",
            ["Game_ClosedByOpp"] = "ПРОТИВНИКЪТ ЗАТВОРИ",

            ["Status_Dealing"] = "Раздаване…",
            ["Status_NewRound"] = "Нова ръка",
            ["Status_YourTurn"] = "Твой ред",
            ["Status_OpponentTurn"] = "Ред на {0}…",

            ["Toast_SwapTrump"] = "{0} смени козовата 9",
            ["Toast_YouClosed"] = "Затвори играта",
            ["Toast_OppClosed"] = "{0} затвори играта",
            ["Toast_Announce20"] = "{0} обяви двойка 20!",
            ["Toast_Announce40"] = "{0} обяви козова двойка 40!",
            ["Word_You"] = "Ти",

            ["Handoff_Pass"] = "Подай устройството на {0}\nДокосни, когато си готов",
            ["Handoff_Ready"] = "Готов съм",

            ["Round_YouWon"] = "Спечели ръката!",
            ["Round_OppWon"] = "{0} спечели ръката",
            ["Round_ScoreHeader"] = "ТОЧКИ В РЪКАТА",
            ["Round_Winner"] = "ПОБЕДИТЕЛ",
            ["Round_GamePointsHeader"] = "ТОЧКИ ЗА ИГРАТА",
            ["Round_Marriages"] = "ДВОЙКИ",
            ["Common_Continue"] = "Продължи",
            ["Common_Match"] = "МАЧ",

            ["GameOver_Victory"] = "Победа!",
            ["GameOver_Defeat"] = "Край на играта",
            ["GameOver_WonGame"] = "{0} спечели играта",
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
