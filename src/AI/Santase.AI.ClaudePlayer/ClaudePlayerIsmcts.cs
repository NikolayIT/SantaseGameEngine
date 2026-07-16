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
        private const int EdgeCapacity = 1 << 18;
        private const int IterationCap = 5_000_000;

        // Node store (struct-of-arrays). A node is the position reached by a public play sequence;
        // its statistics are pooled over every determinization that passed through it.
        private int[] nodeVisits;
        private double[] nodeValue;
        private int[] nodeAvailability;
        private bool[] nodeMover;
        private int[] nodeChildHead;

        // Edge store: a singly-linked list of children per node (variable fan-out — an opponent
        // decision node accumulates every card the opponent could play across determinizations).
        private int[] edgeMove;
        private int[] edgeChild;
        private int[] edgeNext;

        private int[] pathBuffer;
        private int nodeCount;
        private int edgeCount;

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

        protected override int RunSearch(PlayerTurnContext context, ICollection<Card> possibleCards)
        {
            if (!this.ConfigureMove(context))
            {
                return -1;
            }

            this.EnsurePool();
            this.nodeCount = 0;
            this.edgeCount = 0;
            var rootId = this.NewNode(true);

            var start = Stopwatch.GetTimestamp();
            var limitTicks = (long)this.TimeLimitMilliseconds * Stopwatch.Frequency / 1000L;
            var iterations = 0;

            do
            {
                this.RunIteration(rootId, this.SampleWorld());
                iterations++;
            }
            while (iterations < IterationCap && Stopwatch.GetTimestamp() - start < limitTicks);

            if (this.PolicyRecorder != null)
            {
                this.RecordRootPolicy(context, rootId);
            }

            return this.PickRootMove(rootId);
        }

        // Gathers the root children (our legal moves) and their visit counts, then hands them to the
        // base recorder, which turns them into a (features, visit-distribution) distillation sample.
        private void RecordRootPolicy(PlayerTurnContext context, int rootId)
        {
            var count = 0;
            for (var e = this.nodeChildHead[rootId]; e != -1; e = this.edgeNext[e])
            {
                this.rootMoveScratch[count] = this.edgeMove[e];
                this.rootVisitScratch[count] = this.nodeVisits[this.edgeChild[e]];
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
                if (IsTerminal(state))
                {
                    break;
                }

                var legalMask = this.GenMovesMask(state);
                if (legalMask == 0L)
                {
                    break;
                }

                var moverIsMe = this.nodeMover[nodeId];

                // Walk existing children once: bump availability for those legal in this world, pick
                // the best by ISMCTS-UCB, and record which legal moves are already in the tree.
                long covered = 0L;
                var bestChild = -1;
                var bestMove = -1;
                var bestUcb = double.NegativeInfinity;
                for (var e = this.nodeChildHead[nodeId]; e != -1; e = this.edgeNext[e])
                {
                    var move = this.edgeMove[e];
                    if (((legalMask >> move) & 1L) == 0L)
                    {
                        continue;
                    }

                    var child = this.edgeChild[e];
                    covered |= 1L << move;
                    var availability = ++this.nodeAvailability[child];
                    var visits = this.nodeVisits[child];
                    var mean = this.nodeValue[child] / visits;
                    var exploit = moverIsMe ? mean : 1d - mean;
                    var ucb = exploit + (this.ExplorationConstant * Math.Sqrt(Math.Log(availability) / visits));
                    if (ucb > bestUcb)
                    {
                        bestUcb = ucb;
                        bestChild = child;
                        bestMove = move;
                    }
                }

                var untried = legalMask & ~covered;
                if (untried != 0L && this.nodeCount < NodeCapacity && this.edgeCount < EdgeCapacity)
                {
                    // Expand one untried legal move. The pick is arbitrary (lowest hash) on
                    // purpose: expanding the rollout policy's preferred move first measured
                    // -0.7pp in mirror A/B (see the ISMCTS notes in CLAUDE.md).
                    var move = BitOperations.TrailingZeroCount((ulong)untried);
                    state = this.ApplyMove(state, move);
                    var childId = this.NewNode(state.MyTurn);
                    this.nodeAvailability[childId] = 1;
                    this.AddEdge(nodeId, move, childId);
                    nodeId = childId;
                    this.pathBuffer[pathLen++] = nodeId;
                    break;
                }

                if (bestChild < 0)
                {
                    // Tree is full and this node has no child legal in this world — roll out here.
                    break;
                }

                state = this.ApplyMove(state, bestMove);
                nodeId = bestChild;
                this.pathBuffer[pathLen++] = nodeId;
            }

            var reward = this.Rollout(state);

            for (var k = 0; k < pathLen; k++)
            {
                var id = this.pathBuffer[k];
                this.nodeVisits[id]++;
                this.nodeValue[id] += reward;
            }
        }

        private int PickRootMove(int rootId)
        {
            // The root's children are our own (fully known) legal moves, so each is available in every
            // determinization; the most-visited is the robust choice, mean as tiebreak.
            var best = -1;
            var bestVisits = -1;
            var bestMean = double.NegativeInfinity;
            for (var e = this.nodeChildHead[rootId]; e != -1; e = this.edgeNext[e])
            {
                var child = this.edgeChild[e];
                var visits = this.nodeVisits[child];
                var mean = visits > 0 ? this.nodeValue[child] / visits : 0d;
                if (visits > bestVisits || (visits == bestVisits && mean > bestMean))
                {
                    bestVisits = visits;
                    bestMean = mean;
                    best = this.edgeMove[e];
                }
            }

            return best;
        }

        private void AddEdge(int parent, int move, int child)
        {
            var e = this.edgeCount++;
            this.edgeMove[e] = move;
            this.edgeChild[e] = child;
            this.edgeNext[e] = this.nodeChildHead[parent];
            this.nodeChildHead[parent] = e;
        }

        private int NewNode(bool moverIsMe)
        {
            var id = this.nodeCount++;
            this.nodeVisits[id] = 0;
            this.nodeValue[id] = 0d;
            this.nodeAvailability[id] = 0;
            this.nodeMover[id] = moverIsMe;
            this.nodeChildHead[id] = -1;
            return id;
        }

        private void EnsurePool()
        {
            if (this.nodeVisits != null)
            {
                return;
            }

            this.nodeVisits = new int[NodeCapacity];
            this.nodeValue = new double[NodeCapacity];
            this.nodeAvailability = new int[NodeCapacity];
            this.nodeMover = new bool[NodeCapacity];
            this.nodeChildHead = new int[NodeCapacity];

            this.edgeMove = new int[EdgeCapacity];
            this.edgeChild = new int[EdgeCapacity];
            this.edgeNext = new int[EdgeCapacity];

            // A round is at most 24 plies, so any root-to-leaf path fits comfortably.
            this.pathBuffer = new int[32];

            // Root fan-out is our own legal moves (<= hand size); 24 is a safe upper bound.
            this.rootMoveScratch = new int[24];
            this.rootVisitScratch = new int[24];
        }
    }
}
