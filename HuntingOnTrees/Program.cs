using System.Diagnostics;

namespace HuntingOnTrees
{
    internal class Program
    {
        const int maxActions = 200000;
        const int expectedLatency = 1; // tests said that's optimal

        static long n;
        static int k;
        static HallRegion? root;
        static List<HallRegion>? moves;
        static int lastSoundHeard;
        static int ansr;

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

            while (true) // hoglin loop
            {
                Reset();

                while (lastSoundHeard == -1)
                {
                    if (Search(root, true)) { break; }
                }

                if (ansr == -1 || ansr == 3)
                {
                    return;
                }
                else if (ansr == 2)
                {
                    continue;
                }

                //var searchComplite = false;
                var oldLastSound = lastSoundHeard;
                var currentIndex = lastSoundHeard; //
                while (!Search(moves![(currentIndex - expectedLatency > 0) ? currentIndex - expectedLatency : 0], true))
                {
                    // lastSoundHeard may has changed
                    if (oldLastSound != lastSoundHeard)
                    {
                        oldLastSound = lastSoundHeard;
                        currentIndex = lastSoundHeard;
                    }
                    else if (currentIndex == 0)
                        currentIndex = lastSoundHeard;
                    else
                        currentIndex--;
                }

                if (ansr == -1 || ansr == 3)
                {
                    return;
                }
                else if (ansr == 2)
                {
                    continue;
                }
            }
        }

        private static bool Search(HallRegion? r, bool returnOnSound = false)
        {
            var result = false;

            if (r is null)
                return false;

            if (r.IsEmpty())
            {
                //make a guess

                r.Split();

                Console.WriteLine(r.P);
                ansr = int.Parse(Console.ReadLine()!);

                if (ansr == -1 || ansr > 1)
                {
                    return true;
                }

                var moveIndex = moves!.Count;
                moves.Add(r);

                if (ansr == 1)
                {
                    r.Rating++;
                    lastSoundHeard = moveIndex;
                    if (!returnOnSound)
                        result = Search(r.R1, true) || Search(r.R2, true);
                }
                else { }
            }
            else
            {
                r.SwapRsIfGreater();
                result = Search(r.R1) || Search(r.R2);
                r.CalculateRating();
            }

            return result;
        }

        private static void Reset()
        {
            ansr = 0;
            lastSoundHeard = -1;
            root = new HallRegion();
            moves = new List<HallRegion>();
        }

        internal class HallRegion : IComparable<HallRegion>
        {
            public long Left;
            public long Right;
            public long P;
            public HallRegion? Parent;
            public HallRegion? R1;
            public HallRegion? R2;
            public int Rating;

            private HallRegion(HallRegion? parent)
            {
                Parent = parent;
                P = 0;
                Rating = 0;
            }

            public HallRegion() : this(null)
            {
                Left = 1;
                Right = n;
            }

            private HallRegion(HallRegion parent, bool left) : this(parent)
            {
                if (left)
                {
                    Left = Parent!.Left;
                    Right = Parent!.P - 1;
                }
                else
                {
                    Left = Parent!.P + 1;
                    Right = Parent!.Right;
                }
            }

            public void Split()
            {
                if (!IsEmpty()) throw new NotImplementedException();

                P = Convert.ToInt64((Right + Left) >> 1);

                if (P != Left)
                {
                    R1 = new HallRegion(this, true);
                }
                if (P != Right)
                {
                    R2 = new HallRegion(this, false);
                }
            }

            public bool IsEmpty()
            {
                return P == 0;
            }

            public void CalculateRating()
            {
                Rating = ((R1 is null ? 0 : R1.Rating) + (R2 is null ? 0 : R2.Rating)) >> 1;
            }

            public void SwapRsIfGreater()
            {
                if (R1 is not null && R1.CompareTo(R2) < 0)
                {
                    Swap(ref R1, ref R2);
                }
            }

            public static void Swap(ref HallRegion? lhs, ref HallRegion? rhs)
            {
                (lhs, rhs) = (rhs, lhs);
            }

            public int CompareTo(HallRegion? other)
            {
                if (other is null) return 0;

                return Rating - other.Rating;
            }
        }

    }
}
