namespace Santase.AI.ClaudePlayer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Numerics;

    using Santase.Logic.Cards;
    using Santase.Logic.Players;

    /// <summary>
    /// Single-observer Information Set Monte-Carlo Tree Search (SO-ISMCTS), Cowling/Powley/Whitehouse.
    /// Unlike plain Perfect-Information Monte Carlo (PIMC) — which builds an independent
    /// perfect-information tree per determinization and votes the results — this builds <em>one</em>
    /// tree keyed by the public play history and re-samples a fresh determinization every iteration.
    /// Because the same node accumulates statistics across many different opponent hands, the search
    /// never gets to commit to a line that only works in one specific world — which is exactly the
    /// strategy-fusion bias that caps PIMC (it beats a PIMC build of this same simulator/rollout by a
    /// wide margin, and beats every other player in the repo including the neural net).
    ///
    /// The technical core is UCB with <em>availability counts</em>: at a node we only consider the
    /// children whose move is legal in the current determinization, and a child's exploration term
    /// uses how many times it was <em>available</em> for selection (n') rather than the parent visit
    /// count — <c>value + C * sqrt(ln(n') / n)</c>. Everything else (simulator, strong rollout,
    /// exact endgame solve, gates, bookkeeping) is shared via <see cref="ClaudeSearchPlayerBase"/>.
    /// </summary>
    public class ClaudePlayerIsmcts : ClaudeSearchPlayerBase
    {
        private const int NodeCapacity = 1 << 17;
        private const int DefaultMaxIterations = 5_000_000;
        private const int NoNode = -1;

        // Math.Log(n) for small n: the availability counts in the UCB exploration term. Filled with
        // Math.Log itself, so the search is bit-identical to calling it; larger counts (root-level
        // children late in a long search) fall back to the call.
        private const int LogTableSize = 1 << 16;

        private static readonly double[] LogTable = BuildLogTable();

        // Node store (array-of-structs, 32 bytes a node). A node is the position reached by a public
        // play sequence; its statistics are pooled over every determinization that passed through it.
        // Children form a singly-linked list (variable fan-out — an opponent decision node
        // accumulates every card the opponent could play across determinizations), and since every
        // node has exactly one parent, the incoming move and the sibling link live in the child
        // itself: selection reads one struct per child instead of touching eight parallel arrays.
        private Node[] nodes;

        private int[] pathBuffer;
        private int nodeCount;

        // Scratch for emitting the root visit distribution to a PolicyRecorder (distillation only).
        private int[] rootMoveScratch;
        private int[] rootVisitScratch;

        public ClaudePlayerIsmcts()
        {
            // ISMCTS wants far less exploration than the PIMC variant (which defaults to 1.4): the
            // one shared tree accumulates reliable statistics and per-iteration re-determinization
            // already supplies natural exploration (different worlds make different children legal),
            // so it pays to exploit. Tuned vs ClaudePlayer at 100ms: C=0.2 → ~89%, 0.4 → ~88%,
            // 0.7 → ~81%, 1.4 → ~75%; C=0 collapses to ~52% (pure greedy locks onto a lucky line).
            this.ExplorationConstant = 0.2;
        }

        public override string Name => "Claude Player (ISMCTS)";

        /// <summary>
        /// Hard cap on search iterations per move, on top of <see cref="ClaudeSearchPlayerBase.TimeLimitMilliseconds"/>.
        /// With a generous time limit and a seeded <see cref="ClaudeSearchPlayerBase.Rng"/> this makes
        /// the search fully deterministic and machine-independent (tests, benchmarks).
        /// </summary>
        public int MaxIterations { get; set; } = DefaultMaxIterations;

        protected override int RunSearch(PlayerTurnContext context, ICollection<Card> possibleCards)
        {
            if (!this.ConfigureMove(context))
            {
                return -1;
            }

            this.EnsurePool();
            this.nodeCount = 0;
            var rootId = this.NewNode(true, -1);

            var start = Stopwatch.GetTimestamp();
            var limitTicks = (long)this.TimeLimitMilliseconds * Stopwatch.Frequency / 1000L;
            var iterations = 0;

            do
            {
                this.RunIteration(rootId, this.SampleWorld());
                iterations++;
            }
            while (iterations < this.MaxIterations && Stopwatch.GetTimestamp() - start < limitTicks);

            if (this.PolicyRecorder != null)
            {
                this.RecordRootPolicy(context, rootId);
            }

            return this.PickRootMove(rootId);
        }

        private static double[] BuildLogTable()
        {
            var table = new double[LogTableSize];
            for (var n = 0; n < table.Length; n++)
            {
                table[n] = Math.Log(n);
            }

            return table;
        }

        // Gathers the root children (our legal moves) and their visit counts, then hands them to the
        // base recorder, which turns them into a (features, visit-distribution) distillation sample.
        private void RecordRootPolicy(PlayerTurnContext context, int rootId)
        {
            var count = 0;
            for (var c = this.nodes[rootId].FirstChild; c != NoNode; c = this.nodes[c].NextSibling)
            {
                this.rootMoveScratch[count] = this.nodes[c].Move;
                this.rootVisitScratch[count] = this.nodes[c].Visits;
                count++;
            }

            this.RecordPolicy(context, this.rootMoveScratch, this.rootVisitScratch, count);
        }

        private void RunIteration(int rootId, SimState state)
        {
            var nodeId = rootId;
            var pathLen = 0;
            this.pathBuffer[pathLen++] = nodeId;

            while (true)
            {
                if (IsTerminal(in state))
                {
                    break;
                }

                var legalMask = this.GenMovesMask(in state);
                if (legalMask == 0L)
                {
                    break;
                }

                var moverIsMe = this.nodes[nodeId].MoverIsMe;

                // Walk existing children once: bump availability for those legal in this world, pick
                // the best by ISMCTS-UCB, and record which legal moves are already in the tree.
                long covered = 0L;
                var bestChild = NoNode;
                var bestMove = -1;
                var bestUcb = double.NegativeInfinity;
                for (var c = this.nodes[nodeId].FirstChild; c != NoNode; c = this.nodes[c].NextSibling)
                {
                    ref var child = ref this.nodes[c];
                    var move = child.Move;
                    if (((legalMask >> move) & 1L) == 0L)
                    {
                        continue;
                    }

                    covered |= 1L << move;
                    var availability = ++child.Availability;
                    var visits = child.Visits;
                    var mean = child.Value / visits;
                    var exploit = moverIsMe ? mean : 1d - mean;
                    var logAvailability = availability < LogTableSize ? LogTable[availability] : Math.Log(availability);
                    var ucb = exploit + (this.ExplorationConstant * Math.Sqrt(logAvailability / visits));
                    if (ucb > bestUcb)
                    {
                        bestUcb = ucb;
                        bestChild = c;
                        bestMove = move;
                    }
                }

                var untried = legalMask & ~covered;
                if (untried != 0L && this.nodeCount < NodeCapacity)
                {
                    // Expand one untried legal move. The pick is arbitrary (lowest hash) on
                    // purpose: expanding the rollout policy's preferred move first measured
                    // -0.7pp in mirror A/B (see the ISMCTS notes in CLAUDE.md).
                    var move = BitOperations.TrailingZeroCount((ulong)untried);
                    this.ApplyMoveInPlace(ref state, move);
                    var childId = this.NewNode(state.MyTurn, move);
                    this.nodes[childId].Availability = 1;
                    this.AddChild(nodeId, childId);
                    nodeId = childId;
                    this.pathBuffer[pathLen++] = nodeId;
                    break;
                }

                if (bestChild == NoNode)
                {
                    // Tree is full and this node has no child legal in this world — roll out here.
                    break;
                }

                this.ApplyMoveInPlace(ref state, bestMove);
                nodeId = bestChild;
                this.pathBuffer[pathLen++] = nodeId;
            }

            var reward = this.Rollout(state);

            for (var k = 0; k < pathLen; k++)
            {
                ref var node = ref this.nodes[this.pathBuffer[k]];
                node.Visits++;
                node.Value += reward;
            }
        }

        private int PickRootMove(int rootId)
        {
            // The root's children are our own (fully known) legal moves, so each is available in every
            // determinization; the most-visited is the robust choice, mean as tiebreak.
            var best = -1;
            var bestVisits = -1;
            var bestMean = double.NegativeInfinity;
            for (var c = this.nodes[rootId].FirstChild; c != NoNode; c = this.nodes[c].NextSibling)
            {
                var visits = this.nodes[c].Visits;
                var mean = visits > 0 ? this.nodes[c].Value / visits : 0d;
                if (visits > bestVisits || (visits == bestVisits && mean > bestMean))
                {
                    bestVisits = visits;
                    bestMean = mean;
                    best = this.nodes[c].Move;
                }
            }

            return best;
        }

        // Prepends: the newest child is walked first, which is part of the (deterministic) search.
        private void AddChild(int parent, int child)
        {
            this.nodes[child].NextSibling = this.nodes[parent].FirstChild;
            this.nodes[parent].FirstChild = child;
        }

        private int NewNode(bool moverIsMe, int move)
        {
            var id = this.nodeCount++;
            this.nodes[id] = new Node
            {
                Move = move,
                FirstChild = NoNode,
                NextSibling = NoNode,
                MoverIsMe = moverIsMe,
            };

            return id;
        }

        private void EnsurePool()
        {
            if (this.nodes != null)
            {
                return;
            }

            this.nodes = new Node[NodeCapacity];

            // A round is at most 24 plies, so any root-to-leaf path fits comfortably.
            this.pathBuffer = new int[32];

            // Root fan-out is our own legal moves (<= hand size); 24 is a safe upper bound.
            this.rootMoveScratch = new int[24];
            this.rootVisitScratch = new int[24];
        }

        private struct Node
        {
            // Sum of rollout rewards (our perspective, in [0, 1]) over the iterations through here.
            public double Value;
            public int Visits;

            // Iterations in which this node's move was legal while its parent was being selected.
            public int Availability;

            // The card played from the parent to reach this node (-1 at the root).
            public int Move;
            public int FirstChild;
            public int NextSibling;

            // Whether we are the player to move AT this node.
            public bool MoverIsMe;
        }
    }
}
