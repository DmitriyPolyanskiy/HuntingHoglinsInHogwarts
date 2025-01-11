// See https://aka.ms/new-console-template for more information#
//#define V1
//#define V2
#define V3
//#define V4

using Microsoft.VisualBasic;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

internal class Program
{
    const int maxActions = 200000;

    static long n;
    static int k;
#if V1
    static SortedList<double, long[]>? rs;
#elif V2
    static PriorityQueue<HallRegion, int>? rs;
#elif V3 || V4
    static List<HallRegion>? rs;
#if V3
    static List<int>? qualities;
//#elif V4
//    static List<Quality>? qualities;
#endif
    static int log2n;
#endif

    private static void Main(string[] args)
    {
#if DEBUG
        if (!Debugger.IsAttached)
            Debugger.Launch();
        if (Debugger.IsAttached)
            Console.WriteLine("DEBUGGER_ATTACHED");
        else
            Console.WriteLine("DEBUGGER_NOT_ATTACHED");
#endif
        var inputs = Console.ReadLine()!.Split(' ');
        n = long.Parse(inputs[0]);
        k = int.Parse(inputs[1]);

        long p = 0;
        var ansr = 0;
        var huntActions = 0;

#if V1
        double agingCoef = Math.Log10(n) + 1, correction = Math.Pow(10, -agingCoef);
#elif V2
        const int basePunishment = 2, baseReward = 3;
#elif V3 || V4
#if V3
        const int basePunishment = 0, baseReward = 1, depth = 3;
#elif V4
        const int fine = 1, reward = 1;
#endif
        log2n = (int)Math.Log2(n);
        var comparer = new HallRegionComparer();
#endif
        Reset();
        if (rs is null) return;
#if V3
        if (qualities is null) return;
#endif

        while (true)
        {
            huntActions++;
            //make a guess
#if V1
            var r = 0;
            if (ansr != 1)
                r = rs.Count() - 1;
            var rb = rs.GetValueAtIndex(r);
            rs.RemoveAt(r);
#elif V2
            HallRegion r = rs.Dequeue();
            var rb = r.GetBoundaries();
#elif V3 || V4
            HallRegion r = rs.Last();
            rs.RemoveAt(rs.Count - 1);
            var rb = r.GetBoundaries();
#if V3
            qualities.Add(qualities[r.Quality]);
#elif V4
            (int Quality, HallRegion Parent) proto = (r.Value - 1, r);
#endif
#endif
            p = rb[0] + Convert.ToInt64((rb[1] - rb[0]) >> 1);


            Console.WriteLine(p);
            ansr = int.Parse(Console.ReadLine()!);

            if (ansr == -1 || ansr > 2)
            {
                break;
            }
            else if (ansr == 2) // set up for finding next
            {
                ansr = 0;
                huntActions = 0;
                Reset();
                continue;
            }
            else if (ansr == 1)
            {
#if V2
                r.Quality += baseReward;
#elif V3
                var maxReward = baseReward + depth;
                for (int i = qualities.Count - 1, j = 0; i >= 0 && j < depth; i--, j++)
                {
                    qualities[i] += maxReward - j;
                    //qualities[i] += baseReward * (depth - j);
                }
#elif V4
                int j;
                HallRegion i;
                proto.Quality += reward;
                for (i = proto.Parent, j = 1; (i is not null) && j < reward; i = i.Parent!, j++)
                {
                    i.Value += reward - j;
                    if (!i.Sound && i.Brother is not null)
                    {
                        i.Brother.Value -= reward - j - 1;
                    }
                }
#endif
            }
            else
            {
#if V2
                r.Quality -= basePunishment;
#elif V3
                var maxPunishment = basePunishment + depth;
                for (int i = qualities.Count - 1, j = 0; i >= 0 && j < depth; i--, j++)
                {

                    qualities[i] -= maxPunishment - j;
                    //qualities[i] -= basePunishment * (depth - j);
                }
#elif V4
                int j;
                HallRegion i;
                proto.Quality -= fine;
                for (i = proto.Parent, j = 1; (i is not null) && j < fine; i = i.Parent!, j++)
                {
                    i.Value -= fine - j;
                    if (i.Sound && i.Brother is not null)
                    {
                        i.Brother.Value += fine - j - 1;
                    }
                }
#endif
            }

#if V1
            double oldness = agingCoef / huntActions, quality;
            long length;
#elif V3
            var qualityIndex = qualities.Count - 1;
#endif

#if V4
            HallRegion? r1 = null, r2 = null;
            if (p != rb[0])
            {
                r1 = new HallRegion([rb[0], p - 1], proto.Quality, proto.Parent, ansr > 0);
                rs.Add(r1);
            }
            if (p != rb[1])
            {
                r2 = new HallRegion([p + 1, rb[1]], proto.Quality, proto.Parent, ansr > 0);
                rs.Add(r2);
            }
            if (r2 is not null && r1 is not null)
            {
                r1.Brother = r2;
                r2.Brother = r1;
            }
#else
            var nrs = new List<long[]>();
            if (p != rb[0])
                nrs.Add([rb[0], p - 1]);
            if (p != rb[1])
                nrs.Add([p + 1, rb[1]]);
            for (int i = 0; i < nrs.Count; i++)
            {
#if V1
                length = nrs[i][1] - nrs[i][0] + 1;
                quality = oldness * length + i * correction;

                while (!rs.TryAdd(quality, nrs[i]))
                {
                    quality += correction;
                }
#elif V2
                rs.Enqueue(new HallRegion(nrs[i], r.Quality), -r.Quality);
#elif V3
                rs.Add(new HallRegion(nrs[i], qualityIndex));
#endif
            } 
#endif

#if V3 || V4
            if (ansr == 0) // when ansr == 1 elements are almost always sorted where it matters
                rs.Sort(comparer);
#endif
        }
    }

#if V2 || V3 || V4
    internal class HallRegionComparer : IComparer<HallRegion>
    {
        public int Compare(HallRegion? region1, HallRegion? region2)
        {
            if (region1 is null || region2 is null)
                throw new ArgumentNullException($"{nameof(region1)}, {nameof(region2)}");
#if V2
            return region1.Quality.CompareTo(region2.Quality);
#elif V3
            return qualities![region1.Quality].CompareTo(qualities[region2.Quality]);
#elif V4
            return region1.Value.CompareTo(region2.Value);
#endif
        }
    }

    internal class HallRegion {
        public long Left;
        public long Right;
#if V4
        public int Value;
        public HallRegion? Parent;
        public HallRegion? Brother;
        public bool Sound;
#else
        public int Quality;
#endif

#if V4
        public HallRegion(long[] boundaries, int quality, HallRegion? parent, bool Sound)
        {
            Left = boundaries[0];
            Right = boundaries[1];
            Value = quality;
            Parent = parent;
        }
#else
        public HallRegion(long[] boundaries, int quality)
        {
            Left = boundaries[0];
            Right = boundaries[1];
            Quality = quality;
        }
#endif

        public long[] GetBoundaries()
        {
            return [Left, Right];
        }
    }

#endif

    static void Reset()
    {
#if V1
        rs = new SortedList<double, long[]>()!;
        rs.Add(n, [1, n]);
#elif V2
        rs = new PriorityQueue<HallRegion, int>()!;
        rs.Enqueue(new HallRegion([1, n], 0), 0);
#elif V3 || V4
    rs = new List<HallRegion>()!;
#if V3
        qualities = new List<int>(log2n)!;
        qualities.Add(0);
        rs.Add(new HallRegion([1, n], qualities[0]));
#elif V4
    //qualities = new List<Quality>(log2n)!;
    //qualities.Add(new Quality(0, null));
    rs.Add(new HallRegion([1, n], 0, null, false));
#endif
#endif
    }
}
