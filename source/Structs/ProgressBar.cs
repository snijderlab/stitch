using System;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace AssemblyNameSpace
{
    public class ProgressBar
    {
        object ValueKey = new object();
        int max_value;
        int current_value = 0;
        Stopwatch stopwatch;
        bool free = true;
        Timer timer;
        int interval = 1000;
        long estimate = 0;
        long lasttask = 0;

        public ProgressBar()
        {
            stopwatch = new Stopwatch();
        }

        public void Start(int max)
        {
            max_value = max;
            stopwatch.Start();
            Draw(false);
            timer = new Timer(Tick, null, interval, Timeout.Infinite);
        }

        public void Update(int add = 1)
        {
            lock (ValueKey)
            {
                current_value += add;
                lasttask = stopwatch.ElapsedMilliseconds;

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
                interval = (int)(interval * 1.1);
                timer?.Change(interval, Timeout.Infinite);
            }
        }

        void Draw(bool clear = true)
        {
            if (free)
            {
                free = false;

                if (clear)
                {
                    try
                    {
                        Console.SetCursorPosition(0, Console.CursorTop);
                    }
                    catch { }
                }

                int width;
                try
                {
                    width = Console.BufferWidth;
                }
                catch
                {
                    width = 30;
                }

                int value;
                lock (ValueKey)
                {
                    value = current_value;
                }

                var tail = $"| {Math.Round((double)value / max_value * 100),3}% {HelperFunctionality.DisplayTime(stopwatch.ElapsedMilliseconds)} Left ~{HelperFunctionality.DisplayTime(GetEstimate())}";
                var barlength = width - tail.Length - 1;
                var position = (int)Math.Round((double)value / max_value * barlength);
                var stem = new String('-', position);
                var empty = new String(' ', barlength - position);
                Console.Write($"{stem}>{empty}{tail}");

                free = true;
            }
        }

        long GetEstimate()
        {
            int value;
            lock (ValueKey)
            {
                value = current_value;
            }

            long current_estimate;
            try
            {
                current_estimate = (long)Math.Round((double)lasttask / value * (max_value - value) - (stopwatch.ElapsedMilliseconds - lasttask) * 0.6);
                current_estimate = Math.Max(0, current_estimate);
            }
            catch
            {
                current_estimate = 0;
            }

            estimate = estimate == 0 ? current_estimate : (long)(estimate * 0.7 + current_estimate * 0.3);

            if (value == max_value) estimate = 0;

            return estimate;
        }
    }
}