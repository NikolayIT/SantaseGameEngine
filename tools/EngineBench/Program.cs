// Engine micro-benchmark harness for Santase.Logic.
//
// Plays full games of DummyPlayerChangingTrump vs DummyPlayerChangingTrump
// single-threaded so the engine code path dominates the measurement and there
// is no Random.Shared / GC contention. The player is identical across every
// build under test, so wall-clock and allocation deltas are attributable to
// changes in Santase.Logic.
//
// Usage: EngineBench [games] [repeats]
//   games   total games per timed repeat (default 100000)
//   repeats number of timed repeats; median reported (default 4)
using System;
using System.Diagnostics;

using Santase.AI.DummyPlayer;
using Santase.Logic;
using Santase.Logic.GameMechanics;

int games = args.Length > 0 ? int.Parse(args[0]) : 100_000;
int repeats = args.Length > 1 ? int.Parse(args[1]) : 8;
int warmup = Math.Min(25_000, games);

try
{
    // Steady the clock source; CPU time is the primary signal.
    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
}
catch
{
    // best-effort; ignore if not permitted
}

long checksum = PlayMany(warmup); // JIT / tiering warmup (untimed)

Console.WriteLine($"EngineBench: {games:N0} games x {repeats} repeats, single-threaded, GC=workstation/non-concurrent");
Console.WriteLine($"workload: DummyPlayerChangingTrump vs DummyPlayerChangingTrump");
Console.WriteLine($"primary metric = games per CPU-second (wall-clock shown for reference)");

var cpuRates = new double[repeats];
var wallRates = new double[repeats];
var bytesPer = new double[repeats];
var proc = Process.GetCurrentProcess();
for (int r = 0; r < repeats; r++)
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    long allocBefore = GC.GetTotalAllocatedBytes(true);
    TimeSpan cpuBefore = proc.TotalProcessorTime;
    var sw = Stopwatch.StartNew();

    checksum += PlayMany(games);

    sw.Stop();
    TimeSpan cpuAfter = proc.TotalProcessorTime;
    long allocAfter = GC.GetTotalAllocatedBytes(true);

    double cpuSecs = (cpuAfter - cpuBefore).TotalSeconds;
    double wallSecs = sw.Elapsed.TotalSeconds;
    cpuRates[r] = games / cpuSecs;
    wallRates[r] = games / wallSecs;
    bytesPer[r] = (allocAfter - allocBefore) / (double)games;
    Console.WriteLine(
        $"  run {r + 1}: cpu {cpuSecs * 1000,7:F0} ms ({cpuRates[r],10:N0} g/cpu-s)  " +
        $"wall {wallSecs * 1000,7:F0} ms ({wallRates[r],10:N0} g/s)  {bytesPer[r],7:F0} B/game");
}

Array.Sort(cpuRates);
Array.Sort(wallRates);
Array.Sort(bytesPer);
Console.WriteLine(
    $"CPU    median {Median(cpuRates),10:N0} g/cpu-s   best {cpuRates[^1]:N0}  worst {cpuRates[0]:N0}  " +
    $"spread {((cpuRates[^1] / cpuRates[0]) - 1) * 100:F1}%");
Console.WriteLine(
    $"WALL   median {Median(wallRates),10:N0} g/s       best {wallRates[^1]:N0}  worst {wallRates[0]:N0}  " +
    $"spread {((wallRates[^1] / wallRates[0]) - 1) * 100:F1}%");
Console.WriteLine($"ALLOC  median {Median(bytesPer):F0} B/game  (spread {((bytesPer[^1] / bytesPer[0]) - 1) * 100:F2}%)");
Console.WriteLine($"checksum {checksum}");

// Machine-readable summary for the paired/alternating comparison driver.
Console.WriteLine(
    $"RESULT bestcpu={cpuRates[^1]:F1} medcpu={Median(cpuRates):F1} " +
    $"bestwall={wallRates[^1]:F1} bygame={Median(bytesPer):F1}");

static double Median(double[] sorted)
{
    int n = sorted.Length;
    return n % 2 == 1 ? sorted[n / 2] : (sorted[(n / 2) - 1] + sorted[n / 2]) / 2.0;
}

static long PlayMany(int n)
{
    long sum = 0;
    for (int i = 1; i <= n; i++)
    {
        var game = new SantaseGame(new DummyPlayerChangingTrump(), new DummyPlayerChangingTrump());
        var winner = game.Start(i % 2 == 0 ? PlayerPosition.FirstPlayer : PlayerPosition.SecondPlayer);
        sum += (int)winner + game.FirstPlayerTotalPoints + game.SecondPlayerTotalPoints + game.RoundsPlayed;
    }

    return sum;
}
