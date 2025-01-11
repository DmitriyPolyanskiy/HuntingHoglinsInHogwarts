
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Tests
{
    internal class Program
    {
        const int maxActions = 200000;

        static Hallway? hallway;
        static int hoglinsTotal, hoglinsCaught, actionsDone;
        static bool watchTroughMode;

        static void Main(string[] args)
        {
            var random = new Random();
            var shouldExit = false;
            while (!shouldExit)
            {
                Console.WriteLine("Availible commands are:");
                Console.WriteLine("1. run N K");
                Console.WriteLine("2. debug N K");
                Console.WriteLine("3. game N K");
                Console.WriteLine("4. help");
                Console.WriteLine("5. exit");
                Console.WriteLine();

                string? commandStr = Console.ReadLine();

                if (commandStr is null)
                {
                    shouldExit = true;
                    continue;
                }

                var command = commandStr.Split(' ');

                switch (command[0])
                {
                    case "run":
                    case "debug":
                        if (!Initialize(ref command))
                            break;
                        RunProgram();
                        break;
                    case "game":
                        if (!Initialize(ref command))
                            break;
                        RunGame();
                        break;
                    case "help":
                        continue;
                    case "exit":
                        shouldExit = true;
                        continue;
                    default:
                        Console.WriteLine($"No such command: {command[0]}");
                        break;
                }
                Console.WriteLine();
                Console.WriteLine("Press enter to continue");
                Console.ReadLine();
                Console.Clear();
            }
        }

        static bool Initialize(ref readonly string[] command)
        {
            if (command.Length < 2 || !long.TryParse(command[1], out long n))
            {
                Console.WriteLine($"Wrong argument {nameof(n)}");
                return false;
            }

            if (command.Length < 3 || !int.TryParse(command[2], out int k))
            {
                Console.WriteLine($"Wrong argument {nameof(k)}");
                return false;
            }

            hallway = new Hallway(n);
            hoglinsTotal = k;
            actionsDone = 0;
            hoglinsCaught = 0;
            watchTroughMode = command[0] == "debug";

            return true;
        }

        static void RunGame()
        {
            Console.Clear();
            bool shouldEnd = false;
            if (hallway!.Length > Console.BufferWidth)
            {
                Console.WriteLine($"Hallway is too long for this window. Game will not run :(((");
                shouldEnd = true;
            }

            var positions = new List<int> ((int)hallway.Length + 10);
            var sounds    = new List<bool>((int)hallway.Length + 10);

            int cursorLeft = (int)hallway.Length >> 1;
            int cursorTop = 0, lineTop;

            while (!shouldEnd)
            {
                Console.Clear();
                Console.WriteLine($"Chasing ({hoglinsCaught + 1} / {hoglinsTotal})");
                for (int i = 1; i <= hallway.Length; i++)
                {
                    if (i % 10 == 1)
                    {
                        Console.BackgroundColor = (ConsoleColor)(i / 10);
                        Console.Write(i % 10);
                        Console.ResetColor();
                    }
                    else
                        Console.Write(i % 10);
                }
                Console.WriteLine();
                lineTop = Console.CursorTop;
                for (int i = 1; i <= hallway.Length; i++)
                {
                    var moveNum = positions.FindIndex((match) => (match == i));
                    if (moveNum == -1)
                    {
                        Console.Write('_');
                    }
                    else
                    {
                        Console.BackgroundColor = (ConsoleColor) (moveNum / 10);
                        Console.Write((moveNum + 1) % 10);
                        Console.ResetColor();
                    }
                }
                Console.WriteLine();
                for (int i = 0; i < positions.Count; i++)
                {
                    var padding = (int)Math.Log10(hallway.Length) + 1;
                    
                    Console.BackgroundColor = (ConsoleColor) (i / 10);
                    Console.Write($"{{0, {padding}}}", i + 1);
                    Console.ResetColor();
                    Console.Write($". {{0, {padding}}} {{1, -5}}", positions[i], sounds[i]);
                    if (i % 5 == 4)
                        Console.WriteLine();
                    else
                        Console.Write(new string(' ', padding + 1));
                    
                }
                Console.WriteLine();
                int position = 0, result = 0;
                bool moveMade = false;
                cursorTop = Console.CursorTop + 1;
                Console.CursorTop = lineTop;
                while (!moveMade && !shouldEnd)
                {
                    Console.CursorLeft = (cursorLeft + (int)hallway.Length) % (int)hallway.Length;
                    cursorLeft = Console.CursorLeft;
                    var key = Console.ReadKey();
                    switch (key.Key)
                    {
                        case ConsoleKey.Spacebar:
                            if (Console.CursorLeft > hallway.Length)
                                continue;
                            position = cursorLeft + 1;
                            break;
                        case ConsoleKey.P:
                            position = 0;
                            break;
                        case ConsoleKey.E:
                            shouldEnd = true;
                            continue;
                        case ConsoleKey.Enter:
                            Console.CursorTop = cursorTop;
                            Console.CursorLeft = 0;
                            Console.WriteLine("Enter cell to block:");
                            int input;
                            if(int.TryParse(Console.ReadLine(), out input) && input >= 0 && input <= hallway.Length)
                            {
                                position = input;
                                break;
                            }
                            moveMade = true;
                            result = -2;
                            continue;
                        case ConsoleKey.RightArrow:
                            cursorLeft++;
                            continue;
                        case ConsoleKey.LeftArrow:
                            cursorLeft--;
                            continue;
                        default:
                            continue;
                    }
                    moveMade = true;
                    result = Hunt(position);
                    positions.Add(position);
                    sounds.Add(result > 0);
                }
                if(result == 2 || result == 3)
                {
                    Console.CursorLeft = position - 1;
                    Console.CursorTop = lineTop;
                    Console.Write("\u25bc");
                }
                Console.CursorTop = cursorTop;
                Console.CursorLeft = 0;
                Console.WriteLine(new string(' ', Console.BufferWidth));
                Console.WriteLine(new string(' ', Console.BufferWidth));
                Console.CursorTop = cursorTop;
                if (shouldEnd)
                {
                    Console.WriteLine("Exiting game...");
                }
                else if (result == 2)
                {
                    Console.WriteLine("Hoglin caught!");
                    Console.ReadLine();
                    positions.Clear();
                    sounds.Clear();
                    cursorLeft = (int)hallway.Length >> 1;
                }
                else if (result == -1)
                {
                    Console.WriteLine("You lost :_(\nTry again next time!");
                    break;
                }
                else if (result == 3)
                {
                    Console.WriteLine("You caught all the hoglins!\nCongrats!");
                    break;
                }
            }
        }

        static void RunProgram()
        {
            Console.Clear();
            using (Process hoglinHunt = new Process())
            {
#if DEBUG
                var folder = "Debug";
#else
                var folder = "Release";
#endif
                //const string processName = "HuntingHoglinsInHogwarts";
                const string processName = "HuntingOnTrees";

                hoglinHunt.StartInfo.FileName = $"C:\\Users\\human\\source\\repos\\HuntingHoglinsInHogwarts\\{processName}\\bin\\{folder}\\net8.0\\{processName}.exe";
                hoglinHunt.StartInfo.UseShellExecute = false;
                hoglinHunt.StartInfo.CreateNoWindow = true;
                hoglinHunt.StartInfo.RedirectStandardInput = true;
                hoglinHunt.StartInfo.RedirectStandardOutput = true;

                hoglinHunt.Start();
                StreamWriter hoglinHuntInput = hoglinHunt.StandardInput;
                StreamReader hoglinHuntOutput = hoglinHunt.StandardOutput;
#if DEBUG
                var debuggerCode = hoglinHuntOutput.ReadLine();
                Console.WriteLine(debuggerCode);
#endif

                // start hunt
                hoglinHuntInput.WriteLine($"{hallway!.Length} {hoglinsTotal}");

                while (actionsDone < maxActions && hoglinsCaught < hoglinsTotal)
                {
                    actionsDone++;
                    var guess = long.Parse(hoglinHuntOutput.ReadLine()!);

                    if (watchTroughMode)
                    {
                        Console.WriteLine($"Hoglin ({hoglinsCaught + 1} / {hoglinsTotal}) is in {hallway!.Hoglin!.Location}");
                        Console.WriteLine($"Hermione guesses {guess}");

                        hallway.Show(guess);
                    }

                    var result = Hunt(guess);
                    hoglinHuntInput.WriteLine(result);

                    if (watchTroughMode)
                    {
                        hallway.Show(hallway!.Hoglin!.TargetLocation);
                        if (result == 0 || result == 1)
                            Console.WriteLine($"Hoglin's actual boundary is {hallway.PrintBoundaries()}");
                        switch (result)
                        {
                            case 0:
                                Console.WriteLine($"Hoglin moves successfully to {hallway!.Hoglin!.Location}");
                                break;
                            case 1:
                                Console.WriteLine($"Hoglin bumps and updates boundary {hallway.Hoglin.PrintBoundaries()}");
                                break;
                            case 2:
                                Console.WriteLine($"Hoglin ({hoglinsCaught} / {hoglinsTotal}) is caught!");
                                break;
                            case 3:
                                Console.WriteLine($"Last hoglin is caught!");
                                break;
                            default:
                                Console.WriteLine($"Fail!");
                                break;
                        }
                        Console.ReadLine();
                        Console.Clear();
                    }
                }
                // end hunt


                hoglinHunt.WaitForExit();
                PresentFullSatistics(hoglinHunt);
            }
        }

        static int Hunt(long guess)
        {
            if (hallway!.Hoglin!.Location == guess)
            {
                if (++hoglinsCaught >= hoglinsTotal)
                {
                    return 3;
                }
                hallway.Reset();
                return 2;
            }

            if (actionsDone >= maxActions)
            {
                return -1;
            }

            if (!hallway.HoglinRuns(guess))
            {
                return 1;
            }

            return 0;
        }

        static long LongRandom(long min, long max, Random rand)
        {
            byte[] buf = new byte[8];
            rand.NextBytes(buf);
            long longRand = BitConverter.ToInt64(buf, 0);
            return (Math.Abs(longRand % (max - min)) + min);
        }

        static void PresentFullSatistics(Process ps)
        {
            var processorTime    = ps.TotalProcessorTime.TotalMilliseconds;
            var result           = (hoglinsCaught == hoglinsTotal) ? 1 : 0;
            var timePerHoglin    = hoglinsCaught <= 0 ? 0 : processorTime / hoglinsCaught;
            var actionsPerHoglin = hoglinsCaught <= 0 ? 0 : actionsDone / hoglinsCaught;

            Console.WriteLine($"Running: {ps.StartInfo.FileName}");
            Console.WriteLine($"Hallway length: {hallway!.Length} cells");
            Console.WriteLine($"Hoglins caught: {hoglinsCaught}/{hoglinsTotal}");
            Console.WriteLine($"Processor time: {processorTime} ms." + (processorTime > 1000 ? $" ({processorTime/1000} s.)" : null));
            Console.WriteLine($"Average time per hoglin: {timePerHoglin} ms");
            Console.WriteLine($"Actions total: {actionsDone}");
            Console.WriteLine($"Average actions per hoglin: {actionsPerHoglin}");
            var statisticsStream = new FileStream("statistics.csv", FileMode.Append);
            using (StreamWriter writer = new StreamWriter(statisticsStream))
            {
                writer.WriteLine($"{result};{maxActions};{hallway.Length};{hoglinsTotal};{hoglinsCaught};{processorTime};{actionsDone};{timePerHoglin};{actionsPerHoglin}; ; ");
            }
        }

        private class Hoglin
        {
            public long Location { get; private set; }
            public long TargetLocation { get; private set; }
            long leftBoundary;
            long rightBoundary;
            string? boundariesPresentation;

            public Hoglin(Random random, Hallway hallway)
            {
                leftBoundary = 1;
                rightBoundary = hallway.Length;
                Location = ChooseLocation(random);
                if (hallway.CanShow())
                {
                    boundariesPresentation = new string('_', (int)hallway.Length);
                }
            }

            public long ChooseTargetLocation(Random random)
            {
                TargetLocation = ChooseLocation(random);
                return TargetLocation;
            }

            private long ChooseLocation(Random random)
            {
                return LongRandom(leftBoundary, rightBoundary + 1, random);
            }

            //private void SetTargetLocation(Random random)
            //{
            //    targetLocation = ChooseLocation(random);
            //}

            public bool Move(long barrier)
            {
                bool result;

                if (barrier == 0)
                {
                    Location = TargetLocation;
                    result = true;
                }
                else
                {
                    UpdateBoundariesPresentation((int)barrier);

                    if (TargetLocation < Location)
                        leftBoundary = barrier;
                    else
                        rightBoundary = barrier;
                    result = false;
                }

                return result;
            }

            private void UpdateBoundariesPresentation(int newBarrier)
            {
                if (boundariesPresentation is null) return;

                StringBuilder sb = new StringBuilder(boundariesPresentation);

                int left, right;

                if (TargetLocation < Location) {
                    left = (int)leftBoundary - 1;
                    right = newBarrier - 1;
                } else {
                    left = newBarrier + 1;
                    right = (int)rightBoundary;
                }

                for (int i = left; i < right; i++)
                {
                    sb[i] = '\u2591';
                }

                boundariesPresentation = sb.ToString();
            }

            public string? ShowBoundaries()
            {
                return boundariesPresentation;
            }

            public string PrintBoundaries()
            {
                return $"[{leftBoundary}, {rightBoundary}]";
            }

        }

        private class Hallway
        {
            static int maxBufferSize = 100;

            public long Length { get; private set; }
            long leftActualBoundary;
            long rightActualBoundary;
            public Hoglin? Hoglin { get; private set; }
            Random random;
            bool[]? blocked;

            public Hallway(long length)
            {
                random = new Random();
                Length = length;
                Reset();
            }

            public void Reset()
            {
                leftActualBoundary = 1;
                rightActualBoundary = Length;
                blocked = CanShow() ? new bool[Length] : null;
                Hoglin = new Hoglin(random, this);
            }

            public bool HoglinRuns(long newBarrier)
            {
                if (blocked is not null)
                {
                    if (newBarrier > 0)
                    {
                        blocked[newBarrier - 1] = true;
                    }
                }

                if (Hoglin!.Location < newBarrier && newBarrier <= rightActualBoundary)
                    rightActualBoundary = newBarrier - 1;
                else if (newBarrier < Hoglin.Location && leftActualBoundary <= newBarrier)
                    leftActualBoundary = newBarrier + 1;

                var destination = Hoglin.ChooseTargetLocation(random);

                long barrier = 0;
                if (leftActualBoundary > destination)
                    barrier = leftActualBoundary;
                else if (rightActualBoundary < destination)
                    barrier = rightActualBoundary;

                var result = Hoglin.Move(barrier);
                return result;
            }

            public bool CanShow()
            {
                return Length <= Console.BufferWidth;
            }

            public void Show(long target = 0)
            {
                if (blocked is null) return;

                string boundaries = Hoglin!.ShowBoundaries()!;

                for (int i = 0; i < Length; i++)
                {
                    Console.Write((i + 1) % 10);
                }
                Console.WriteLine();

                for (int i = 0; i < Length; i++)
                {
                    if (blocked[i])
                    {
                        Console.Write("%");
                    }
                    else if (i == Hoglin.Location - 1)
                    {
                        Console.Write("\u25bc");
                    }
                    else if (i == target - 1)
                    {
                        Console.Write("?");
                    }
                    else
                    {
                        Console.Write(boundaries[i]);
                    }
                }
                Console.WriteLine();
            }

            public string PrintBoundaries()
            {
                return $"[{leftActualBoundary}, {rightActualBoundary}]";
            }

        }
    }
}
