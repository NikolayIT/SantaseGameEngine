#if STORE_SHOTS
namespace Santase.UI
{
    using System.Globalization;
    using System.Runtime.CompilerServices;

    using Santase.AI.ClaudePlayer;
    using Santase.Logic.Players;
    using Santase.UI.Game;
    using Santase.UI.Localization;
    using Santase.UI.Pages;

    /// <summary>
    /// Plays the real app on an iPhone simulator, for the App Store screenshots and as the check
    /// that the app works ahead-of-time compiled (.github/workflows/ios.yml): whole games against
    /// every opponent and a two-player game through the table's own commands, then the store's
    /// scenes on the real pages. It asks for each screenshot by writing its name to a file in the
    /// app's Documents, which the script outside takes with "simctl io" and deletes. Only built
    /// with -p:StoreShots=true, which also targets the simulator, so it can never reach a store build.
    /// </summary>
    internal static class StoreShotsDriver
    {
        private const string DoneFile = "shots-done.txt";

        private const string ErrorFile = "shots-error.txt";

        private const string LogFile = "shots-log.txt";

        private const string RequestFile = "shot-request.txt";

        // The embedded policy of ClaudePlayerNeural: 36,120 float32. Without it the player would
        // quietly fall back to untrained weights.
        private const string WeightsResource = "Santase.AI.ClaudePlayer.Neural.weights.bin";

        private const long WeightsBytes = 36120 * sizeof(float);

        private static readonly string Folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        private static readonly Random Random = new(20261106);

        private static readonly string[] TableScenes = { "01-gameplay", "02-marriage", "03-hint", "04-closed", "05-result", "06-victory", "06-gameover" };

        private static string? Language => Environment.GetEnvironmentVariable("SANTASE_SHOT_LANGUAGE");

        private static bool IsBulgarian => Language == LocalizationManager.Bulgarian;

        private static IEnumerable<string> Scenes => (Environment.GetEnvironmentVariable("SANTASE_SHOT_SCENES") ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries);

        // Before anything reads the settings: the start page shows them when it is built.
        public static void Prepare()
        {
            Log($"Device culture {CultureInfo.CurrentUICulture.Name}; preferred {string.Join(' ', Foundation.NSLocale.PreferredLanguages)}");
            Log($"Language without a saved choice: {LocalizationManager.Instance.Language}");
            Log($"Dynamic code: supported {RuntimeFeature.IsDynamicCodeSupported}, compiled {RuntimeFeature.IsDynamicCodeCompiled}");
            AppDomain.CurrentDomain.UnhandledException += (_, args) => Log("UNHANDLED: " + args.ExceptionObject);
            TaskScheduler.UnobservedTaskException += (_, args) => Log("UNOBSERVED: " + args.Exception);
            if (string.IsNullOrEmpty(Language))
            {
                return;
            }

            LocalizationManager.Instance.SetLanguage(Language);
            AppSettings.HapticsEnabled = false;
            AppSettings.Speed = GameSpeed.Fast;
            AppSettings.PlayerName = IsBulgarian ? "Ники" : "Niki";
        }

        public static void Start() => _ = Task.Run(() => MainThread.InvokeOnMainThreadAsync(RunAsync));

        private static async Task RunAsync()
        {
            try
            {
                await Until(() => Shell.Current?.CurrentPage is StartPage, "the start page");
                await Task.Delay(1000);
                CheckNeuralWeights();

                // Each language plays its own games: the statistics scene shows them.
                var games = int.Parse(Environment.GetEnvironmentVariable("SANTASE_SHOT_GAMES") ?? "0", CultureInfo.InvariantCulture);
                if (games > 0)
                {
                    await PlayEveryOpponentAsync(games);
                    await PlayHotSeatAsync();
                }

                var scenes = new HashSet<string>(Scenes);
                await ShootTableAsync(scenes);
                await ShootPagesAsync(scenes);
                Log($"Saved: rating {PlayerRatingStore.CurrentElo}, games {PlayerRatingStore.GamesPlayed}, wins {PlayerRatingStore.Wins}, history {MatchHistoryStore.All().Count}");
                Log("Finished");
            }
            catch (Exception exception)
            {
                Log(exception.ToString());
                await File.WriteAllTextAsync(Path.Combine(Folder, ErrorFile), exception.ToString());
            }
            finally
            {
                await File.WriteAllTextAsync(Path.Combine(Folder, DoneFile), DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            }
        }

        private static void CheckNeuralWeights()
        {
            using var weights = typeof(ClaudePlayerNeural).Assembly.GetManifestResourceStream(WeightsResource);
            Log($"Neural weights: {weights?.Length.ToString(CultureInfo.InvariantCulture) ?? "MISSING"} bytes");
            if (weights?.Length != WeightsBytes)
            {
                throw new InvalidOperationException("The neural player's weights did not reach the app.");
            }
        }

        // Whole games at no pace through a real table, with the person playing what the hint says
        // (so a swap and a close come up too), and hints asked for on the way: the code paths of
        // every computer player that nothing else on this list would reach.
        private static async Task PlayEveryOpponentAsync(int gamesPerOpponent)
        {
            foreach (var opponent in AiOpponents.All)
            {
                for (var game = 0; game < gamesPerOpponent; game++)
                {
                    var started = DateTime.UtcNow;
                    var session = new GameSession(GameMode.VsAi, AppSettings.PlayerName, opponent.DisplayName, opponent.CreatePlayer(), GamePace.Instant);
                    var decisions = await PlayGameAsync(session, opponent, opponent.Id);
                    Log($"Played a game against {opponent.Id}: {decisions} decisions, {(DateTime.UtcNow - started).TotalSeconds:F1} s; rating {PlayerRatingStore.CurrentElo}");
                }
            }
        }

        private static async Task PlayHotSeatAsync()
        {
            var session = new GameSession(GameMode.HotSeat, "A", "B", null, GamePace.Instant);
            var decisions = await PlayGameAsync(session, null, "two players");
            Log($"Played a two-player game: {decisions} decisions");
        }

        private static async Task<int> PlayGameAsync(GameSession session, AiOpponent? opponent, string name)
        {
            Exception? error = null;
            session.GameError += exception => error = exception;
            using var table = new GameViewModel(session, opponent, new QuietHost());
            table.StartGame();
            var decisions = 0;
            var hinted = false;
            while (true)
            {
                await Until(() => table.IsGameOverlayVisible || table.IsRoundOverlayVisible || table.IsHandoffOverlayVisible || table.IsMyTurn || error != null, "a decision");
                if (error != null)
                {
                    throw new InvalidOperationException($"The game against {name} failed.", error);
                }

                if (table.IsGameOverlayVisible)
                {
                    return decisions;
                }

                if (table.IsRoundOverlayVisible)
                {
                    table.RoundOverlayContinueCommand.Execute(null);
                }
                else if (table.IsHandoffOverlayVisible)
                {
                    table.HandoffContinueCommand.Execute(null);
                }
                else
                {
                    // A move on its way to the session leaves the turn showing for a moment.
                    if (Move(session, table) && ++decisions % 7 == 0)
                    {
                        hinted = true;
                    }
                }

                if (hinted && table.IsHintVisible)
                {
                    hinted = false;
                    table.HintCommand.Execute(null);
                }

                await Task.Yield();
            }
        }

        private static async Task ShootPagesAsync(HashSet<string> scenes)
        {
            if (scenes.Remove("07-opponents"))
            {
                // The start page refreshes its rating when it appears.
                await ReopenStartPageAsync();
                await ShootAsync("07-opponents");
            }

            if (scenes.Remove("08-statistics"))
            {
                await Shell.Current.GoToAsync("StatisticsPage");
                await Until(() => Shell.Current.CurrentPage is StatisticsPage, "the statistics page");
                await ShootAsync("08-statistics");
                await Shell.Current.GoToAsync("..");
            }

            if (scenes.Remove("09-rules"))
            {
                await Shell.Current.GoToAsync("RulesPage");
                await Until(() => Shell.Current.CurrentPage is RulesPage, "the rules page");
                await ShootAsync("09-rules");
                await Shell.Current.GoToAsync("..");
            }

            if (scenes.Remove("10-settings"))
            {
                await Shell.Current.GoToAsync("SettingsPage");
                await Until(() => Shell.Current.CurrentPage is SettingsPage, "the settings page");
                await ShootAsync("10-settings");
                await Shell.Current.GoToAsync("..");
            }
        }

        private static async Task ReopenStartPageAsync()
        {
            await Shell.Current.GoToAsync("RulesPage");
            await Until(() => Shell.Current.CurrentPage is RulesPage, "the rules page");
            await Shell.Current.GoToAsync("..");
            await Until(() => Shell.Current.CurrentPage is StartPage, "the start page");
        }

        private static async Task ShootTableAsync(HashSet<string> scenes)
        {
            var wanted = scenes.Where(TableScenes.Contains).ToHashSet();
            if (wanted.Count == 0)
            {
                return;
            }

            // The Strategist plays what the hint plays, so a win is a coin flip: games end quickly
            // enough for the victory scene to come up.
            var query = $"?mode={GameMode.VsAi}&first={Uri.EscapeDataString(AppSettings.PlayerName)}&opponent=claude";
            await Shell.Current.GoToAsync($"GamePage{query}");
            await Until(() => Shell.Current.CurrentPage is GamePage { BindingContext: GameViewModel, Session: not null }, "the table");
            var page = (GamePage)Shell.Current.CurrentPage;
            var table = (GameViewModel)page.BindingContext;
            var session = page.Session!;
            Exception? error = null;
            session.GameError += exception => error = exception;

            CheckEdgeSwipeIsRefused();

            var deadline = DateTime.UtcNow.AddMinutes(25);
            while (wanted.Count > 0)
            {
                if (DateTime.UtcNow > deadline)
                {
                    throw new TimeoutException("Scenes never seen: " + string.Join(", ", wanted));
                }

                await Until(() => table.IsGameOverlayVisible || table.IsRoundOverlayVisible || table.IsMyTurn || error != null, "a decision or a result");
                if (error != null)
                {
                    throw new InvalidOperationException("The game on the table failed.", error);
                }

                if (table.IsGameOverlayVisible)
                {
                    await ShootGameOverAsync(table, wanted);
                    table.PlayAgainCommand.Execute(null);
                    await Task.Delay(300);
                    continue;
                }

                if (table.IsRoundOverlayVisible)
                {
                    if (table.RoundIWon && table.HasAnnounces && wanted.Remove("05-result"))
                    {
                        await ShootAsync("05-result");
                    }

                    table.RoundOverlayContinueCommand.Execute(null);
                    await Task.Delay(100);
                    continue;
                }

                if (await ShootTurnAsync(table, wanted))
                {
                    await Task.Delay(100);
                    continue;
                }

                Move(session, table);
                await Task.Delay(50);
            }

            table.LeaveCommand.Execute(null);
            await Until(() => Shell.Current.CurrentPage is StartPage, "the start page after leaving the table");
        }

        private static async Task ShootGameOverAsync(GameViewModel table, HashSet<string> wanted)
        {
            if (wanted.Remove("06-gameover"))
            {
                await ShootAsync("06-gameover");
            }

            if (table.GameOverlayTitle == LocalizationManager.Instance["GameOver_Victory"] && wanted.Remove("06-victory"))
            {
                await ShootAsync("06-victory");
            }
        }

        // Takes whatever the person's turn offers; true if it made a move on the way.
        private static async Task<bool> ShootTurnAsync(GameViewModel table, HashSet<string> wanted)
        {
            // Closing's own banner would cover the table for most of the round.
            if (wanted.Contains("04-closed") && table.GameClosedByMe && table.IsToastVisible)
            {
                await Until(() => !table.IsToastVisible, "the banner to go");
            }

            if (table.IsToastVisible || !table.IsMyTurn)
            {
                return false;
            }

            var inHand = table.MyHand.Count;
            var playable = table.MyHand.Count(x => x.IsPlayable);
            if (wanted.Contains("01-gameplay") && table.OpponentPlayedCard != null && table.HasLastTrick && inHand == 6 && table.DeckCount >= 4)
            {
                wanted.Remove("01-gameplay");
                await ShootAsync("01-gameplay");
            }
            else if (wanted.Contains("02-marriage") && table.MyHand.Any(x => x.HasAnnounce))
            {
                wanted.Remove("02-marriage");
                await ShootAsync("02-marriage");
            }
            else if (wanted.Contains("03-hint") && table.OpponentPlayedCard == null && table.HasLastTrick && inHand == 6)
            {
                table.HintCommand.Execute(null);
                if (table.MyHand.Any(x => x.IsHinted))
                {
                    // The mark stays for 2.5 s.
                    wanted.Remove("03-hint");
                    await ShootAsync("03-hint", settleMs: 400);
                }
            }
            else if (wanted.Contains("04-closed") && !wanted.Contains("01-gameplay") && !wanted.Contains("02-marriage") && !wanted.Contains("03-hint"))
            {
                if (table.GameClosedByMe && playable > 0 && playable < inHand && inHand >= 3)
                {
                    wanted.Remove("04-closed");
                    await ShootAsync("04-closed");
                }
                else if (table.CanCloseGame)
                {
                    table.CloseGameCommand.Execute(null);
                    return true;
                }
            }

            return false;
        }

        // What iOS's swipe from the edge of the screen asks of the Shell (ShellSectionRenderer.ShouldPop).
        private static void CheckEdgeSwipeIsRefused()
        {
            var shell = Shell.Current;
            var section = shell.CurrentItem?.CurrentItem;
            var stack = section?.Stack.ToList() ?? new List<Page>();
            stack.RemoveAt(stack.Count - 1);
            var allowed = ((IShellController)shell).ProposeNavigation(ShellNavigationSource.Pop, shell.CurrentItem, section, section?.CurrentItem, stack, true);
            Log($"Swipe from the edge on the table: {(allowed ? "ALLOWED" : "refused")}");
            if (allowed)
            {
                throw new InvalidOperationException("A swipe from the edge would leave the game without asking.");
            }
        }

        // Against the computer what the hint says; two people play random legal moves. A move
        // already made may still be on its way to the session, which then has no hint to give:
        // false if nothing was asked of the table.
        private static bool Move(GameSession session, GameViewModel table)
        {
            var playable = table.MyHand.Where(x => x.IsPlayable).ToList();
            if (session.Mode == GameMode.HotSeat)
            {
                if (table.CanChangeTrump && Random.Next(2) == 0)
                {
                    table.ChangeTrumpCommand.Execute(null);
                }
                else if (table.CanCloseGame && Random.Next(8) == 0)
                {
                    table.CloseGameCommand.Execute(null);
                }
                else if (playable.Count > 0)
                {
                    table.TapCardCommand.Execute(playable[Random.Next(playable.Count)]);
                }
                else
                {
                    return false;
                }

                return true;
            }

            var hint = session.GetHint();
            switch (hint?.Type)
            {
                case PlayerActionType.ChangeTrump:
                    table.ChangeTrumpCommand.Execute(null);
                    break;
                case PlayerActionType.CloseGame:
                    table.CloseGameCommand.Execute(null);
                    break;
                case PlayerActionType.PlayCard:
                    table.TapCardCommand.Execute(table.MyHand.First(x => x.Card == hint.Card));
                    break;
                default:
                    return false;
            }

            return true;
        }

        private static async Task ShootAsync(string name, int settleMs = 1500)
        {
            // The animations settle first.
            await Task.Delay(settleMs);
            var request = Path.Combine(Folder, RequestFile);
            var temporary = request + ".tmp";
            await File.WriteAllTextAsync(temporary, name);
            File.Move(temporary, request);
            // simctl io has been seen to take over a minute on a hosted runner.
            await Until(() => !File.Exists(request), "the screenshot " + name, 300);
            Log("Shot " + name);
        }

        private static async Task Until(Func<bool> condition, string what, int seconds = 120)
        {
            var deadline = DateTime.UtcNow.AddSeconds(seconds);
            while (!condition())
            {
                if (DateTime.UtcNow > deadline)
                {
                    throw new TimeoutException("Never got to " + what);
                }

                await Task.Delay(10);
            }
        }

        private static void Log(string line) =>
            File.AppendAllText(Path.Combine(Folder, LogFile), DateTime.UtcNow.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " " + line + Environment.NewLine);

        private sealed class QuietHost : IGameTableHost
        {
            public void After(TimeSpan delay, Action action) => _ = MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Task.Delay(delay);
                action();
            });

            public void Vibrate(bool isLong)
            {
            }

            public void Leave()
            {
            }
        }
    }
}
#endif
