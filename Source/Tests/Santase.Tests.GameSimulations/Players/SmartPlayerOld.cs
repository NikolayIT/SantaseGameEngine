namespace Santase.Tests.GameSimulations.Players
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.Net.Http;

    using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    internal class SmartPlayerOld : IPlayer
    {
        private static readonly string[] UrlsForSourceCode =
        {
            "https://raw.githubusercontent.com/NikolayIT/SantaseGameEngine/master/Source/AI/Santase.AI.SmartPlayer/SmartPlayer.cs",
            "https://raw.githubusercontent.com/NikolayIT/SantaseGameEngine/master/Source/AI/Santase.AI.SmartPlayer/GlobalStats.cs",
            "https://raw.githubusercontent.com/NikolayIT/SantaseGameEngine/master/Source/AI/Santase.AI.SmartPlayer/Helpers/CardTracker.cs",
            "https://raw.githubusercontent.com/NikolayIT/SantaseGameEngine/master/Source/AI/Santase.AI.SmartPlayer/Helpers/OpponentSuitCardsProvider.cs",
        };

        private static readonly Type CompiledPlayerType;

        private static CSharpCodeProvider codeProvider;

        private readonly IPlayer compiledPlayer;

        static SmartPlayerOld()
        {
            var client = new HttpClient();

            var codeFiles = new List<string>();

            foreach (var url in UrlsForSourceCode)
            {
                var code = client.GetStringAsync(url).Result;
                codeFiles.Add(code);
            }

            CompilerParameters parameters = new CompilerParameters();

            // Reference to System.Drawing library
            parameters.ReferencedAssemblies.Add("System.dll");
            parameters.ReferencedAssemblies.Add("System.Core.dll");
            parameters.ReferencedAssemblies.Add("Santase.Logic.dll");

            // True - memory generation, false - external file generation
            parameters.GenerateInMemory = true;

            // True - exe file generation, false - dll file generation
            parameters.GenerateExecutable = false;

            CompilerResults results = CodeProvider2.CompileAssemblyFromSource(parameters, codeFiles.ToArray());
            if (results.Errors.HasErrors)
            {
                foreach (CompilerError error in results.Errors)
                {
                    Console.WriteLine("Error compiling old player ({0}): {1} (line: {2}, col: {3})", error.ErrorNumber, error.ErrorText, error.Line, error.Column);
                    return;
                }
            }

            var assembly = results.CompiledAssembly;
            CompiledPlayerType = assembly.GetType("Santase.AI.SmartPlayer.SmartPlayer");
        }

        public SmartPlayerOld()
        {
            this.compiledPlayer = (IPlayer)Activator.CreateInstance(CompiledPlayerType);
        }

        public string Name => "Smart Player Old";

        private static CSharpCodeProvider CodeProvider2
        {
            get
            {
                if (codeProvider == null)
                {
                    var csc = new CSharpCodeProvider();
                    try
                    {
                        var settings =
                            csc.GetType()
                                .GetField(
                                    "_compilerSettings",
                                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                                .GetValue(csc);
                        var path =
                            settings.GetType()
                                .GetField(
                                    "_compilerFullPath",
                                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                                .GetValue(settings) as string;
                        settings.GetType()
                            .GetField(
                                "_compilerFullPath",
                                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                            .SetValue(settings, path.Replace(@"bin\roslyn\", @"roslyn\"));
                    }
                    catch
                    {
                    }

                    codeProvider = csc;
                }

                return codeProvider;
            }
        }

        public void StartGame(string otherPlayerIdentifier)
        {
            this.compiledPlayer.StartGame(otherPlayerIdentifier);
        }

        public void StartRound(ICollection<Card> cards, Card trumpCard, int myTotalPoints, int opponentTotalPoints)
        {
            this.compiledPlayer.StartRound(cards, trumpCard, myTotalPoints, opponentTotalPoints);
        }

        public void AddCard(Card card)
        {
            this.compiledPlayer.AddCard(card);
        }

        public PlayerAction GetTurn(PlayerTurnContext context)
        {
            return this.compiledPlayer.GetTurn(context);
        }

        public void EndTurn(PlayerTurnContext context)
        {
            this.compiledPlayer.EndTurn(context);
        }

        public void EndRound()
        {
            this.compiledPlayer.EndRound();
        }

        public void EndGame(bool amIWinner)
        {
            this.compiledPlayer.EndGame(amIWinner);
        }
    }
}
