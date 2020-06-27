namespace Santase.Tests.GameSimulations.Players
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;

    using Santase.AI.SmartPlayer;
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
            "https://raw.githubusercontent.com/NikolayIT/SantaseGameEngine/master/Source/AI/Santase.AI.SmartPlayer/Strategies/BaseChooseCardStrategy.cs",
            "https://raw.githubusercontent.com/NikolayIT/SantaseGameEngine/master/Source/AI/Santase.AI.SmartPlayer/Strategies/ChooseBestCardToPlayStrategy.cs",
            "https://raw.githubusercontent.com/NikolayIT/SantaseGameEngine/master/Source/AI/Santase.AI.SmartPlayer/Strategies/IChooseCardStrategy.cs",
            "https://raw.githubusercontent.com/NikolayIT/SantaseGameEngine/master/Source/AI/Santase.AI.SmartPlayer/Strategies/PlayingFirstAndRulesApplyStrategy.cs",
            "https://raw.githubusercontent.com/NikolayIT/SantaseGameEngine/master/Source/AI/Santase.AI.SmartPlayer/Strategies/PlayingFirstAndRulesDoNotApplyStrategy.cs",
            "https://raw.githubusercontent.com/NikolayIT/SantaseGameEngine/master/Source/AI/Santase.AI.SmartPlayer/Strategies/PlayingSecondAndRulesApplyStrategy.cs",
            "https://raw.githubusercontent.com/NikolayIT/SantaseGameEngine/master/Source/AI/Santase.AI.SmartPlayer/Strategies/PlayingSecondAndRulesDoNotApplyStrategy.cs",
        };

        private static readonly Type CompiledPlayerType;

        private readonly IPlayer compiledPlayer;

        static SmartPlayerOld()
        {
            var client = new HttpClient(new HttpClientHandler { Proxy = null, UseProxy = false });
            var codeFiles = new List<string>();
            foreach (var url in UrlsForSourceCode)
            {
                var code = client.GetStringAsync(url).GetAwaiter().GetResult();
                codeFiles.Add(code);
            }

            var syntaxTrees = new List<SyntaxTree>();
            foreach (var codeFile in codeFiles)
            {
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(codeFile));
            }

            var referencedAssemblies = CollectAssemblies(Assembly.Load(new AssemblyName("netstandard")));
            var metadataReferences = new List<MetadataReference>(referencedAssemblies.Count + 1);
            foreach (var referencedAssembly in referencedAssemblies)
            {
                metadataReferences.Add(MetadataReference.CreateFromFile(referencedAssembly.Location));
            }

            metadataReferences.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
            metadataReferences.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("mscorlib")).Location));
            metadataReferences.Add(MetadataReference.CreateFromFile(typeof(IPlayer).Assembly.Location));

            // TODO: Delete assemblyName file
            var assemblyName = Path.GetRandomFileName();
            var compilation = CSharpCompilation.Create(assemblyName)
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddSyntaxTrees(syntaxTrees).AddReferences(metadataReferences);

            Assembly assembly = null;
            using (var ms = new MemoryStream())
            {
                var result = compilation.Emit(ms);
                if (!result.Success)
                {
                    var failures = result.Diagnostics.Where(
                        diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (var diagnostic in failures)
                    {
                        Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }
                }
                else
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    assembly = Assembly.Load(ms.ToArray());
                }
            }

            if (assembly == null)
            {
                throw new Exception("Remote assembly code cannot be compiled.");
            }

            CompiledPlayerType = assembly.GetType("Santase.AI.SmartPlayer.SmartPlayer");
        }

        public SmartPlayerOld()
        {
            GlobalStats.GlobalCounterValues[0]++;
            this.compiledPlayer = (IPlayer)Activator.CreateInstance(CompiledPlayerType);
        }

        public string Name => "Smart Player Old";

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

        private static IList<Assembly> CollectAssemblies(Assembly assembly)
        {
            var assemblies = new HashSet<Assembly> { assembly };

            var referencedAssemblyNames = assembly.GetReferencedAssemblies();

            foreach (var assemblyName in referencedAssemblyNames)
            {
                var loadedAssembly = Assembly.Load(assemblyName);
                assemblies.Add(loadedAssembly);
            }

            return assemblies.ToList();
        }
    }
}
