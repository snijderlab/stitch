using System;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace AssemblyNameSpace
{
    public class ProgressBar
    {
        readonly object ValueKey = new object();
        int max_value;
        int current_value = 0;
        readonly Stopwatch stopwatch;
        bool free = true;
        Timer timer;
        int interval = 1000;
        bool terminalContact = true;
        bool started = false;

        public ProgressBar()
        {
            stopwatch = new Stopwatch();
        }

        public void Start(int max)
        {
            max_value = max;
            if (!started)
            {
                stopwatch.Start();
                Draw(false);
                timer = new Timer(Tick, null, interval, Timeout.Infinite);
            }
            started = true;
        }

        public void Update(int add = 1)
        {
            lock (ValueKey)
            {
                current_value += add;

                if (current_value == max_value)
                {
                    stopwatch.Stop();
                }
            }
            Draw();
        }

        private void Tick(object state)
        {
            try
            {
                Draw();
            }
            finally
            {
                interval = (int)(interval * 1.05);
                timer?.Change(interval, Timeout.Infinite);
            }
        }

        void Draw(bool clear = true)
        {
            if (free)
            {
                free = false;

                if (clear && terminalContact)
                {
                    try
                    {
                        Console.SetCursorPosition(0, Console.CursorTop);
                    }
                    catch
                    {
                        terminalContact = false;
                    }
                }

                int width = 30;
                if (terminalContact)
                {
                    try
                    {
                        width = Console.BufferWidth;
                    }
                    catch
                    {
                        terminalContact = false;
                    }
                }

                int value;
                lock (ValueKey)
                {
                    value = current_value;
                }

                var tail = $"| {Math.Round((double)value / max_value * 100),3}% {HelperFunctionality.DisplayTime(stopwatch.ElapsedMilliseconds)}";
                var barlength = width - tail.Length - 1;
                var position = (int)Math.Round((double)value / max_value * barlength);
                var stem = new String('-', position);
                var empty = new String(' ', barlength - position);
                Console.Write($"{stem}>{empty}{tail}");

                free = true;
            }
        }
    }
}