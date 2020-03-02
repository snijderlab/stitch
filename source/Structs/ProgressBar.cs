using System;
using System.IO;
using System.Diagnostics;

namespace AssemblyNameSpace
{
    public class ProgressBar
    {
        int max_value;
        int current_value = 0;
        Stopwatch stopwatch;
        bool free = true;

        public ProgressBar()
        {
            stopwatch = new Stopwatch();
        }

        public void Start(int max)
        {
            max_value = max;
            stopwatch.Start();
            Draw(false);
        }

        public void Update(int add = 1)
        {
            current_value += add;
            if (current_value == max_value)
            {
                stopwatch.Stop();
            }
            Draw();
        }

        void Draw(bool clear = true)
        {
            if (free)
            {
                free = false;

                if (clear)
                {
                    Console.SetCursorPosition(0, Console.CursorTop);
                }

                var tail = $"| {Math.Round((double)current_value / max_value * 100),3}% {HelperFunctionality.DisplayTime(stopwatch.ElapsedMilliseconds)} ";
                var barlength = Console.BufferWidth - tail.Length - 1;
                var position = (int)Math.Round((double)current_value / max_value * barlength);
                var stem = new String('-', position);
                var empty = new String(' ', barlength - position);
                Console.Write($"{stem}>{empty}{tail}");

                free = true;
            }
        }
    }
}