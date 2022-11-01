using System;
using System.Diagnostics;
using System.Threading;

namespace Stitch {
    public class ProgressBar : IDisposable {
        /// <summary> This turns off the entire progress bar, used to make sure it does not print any more after an error has been printed. </summary>
        public static bool Off = false;

        /// <summary> The key to lock while updating or retrieving the value. </summary>
        readonly object ValueKey = new object();

        /// <summary> The maximal value of the progress bar, ie the number of ticks in total. </summary>
        int max_value;

        /// <summary> The current value of this progress bar, ie the number of steps already done. </summary>
        int current_value = 0;

        /// <summary> The stopwatch that keep the time since the Start of this progress bar. </summary>
        readonly Stopwatch stopwatch;

        /// <summary> A key to not draw twice at the same time, if false the whole Draw method will just be skipped until the actual drawing method finishes. </summary>
        bool free = true;

        /// <summary> The timer to invoke each next tick of the progress bar. </summary>
        Timer timer;

        /// <summary> The current interval between ticks (ms). </summary>
        int interval = 1000;

        /// <summary> Keeps track if the terminal currently writing too has access to certain modern features, to only raise an exception once if it does not. </summary>
        bool terminalContact = true;

        /// <summary> Keep track of the state of this progress bar. </summary>
        bool started = false;

        /// <summary> Create a new ProgressBar, it will have to be started with Start.</summary>
        public ProgressBar() {
            stopwatch = new Stopwatch();
        }

        /// <summary> Properly dispose of the timer. </summary>
        protected virtual void Dispose(bool dispose) {
            if (dispose)
                timer.Dispose();
        }

        /// <summary> Properly dispose of the timer. </summary>
        public void Dispose() {
            this.Dispose(true);
        }

        /// <summary> Start the ProgressBar. </summary>
        /// <param name="max"> The number of ticks on this progress bar. </param>
        public void Start(int max) {
            max_value = max;
            if (!started) {
                stopwatch.Start();
                Draw(false);
                timer = new Timer(Tick, null, interval, Timeout.Infinite);
            }
            started = true;
        }

        /// <summary> Update the ProgressBar with the given number of ticks. It will be redrawn immediately afterwards. </summary>
        /// <param name="add"> The number of ticks to go forward, defaults to 1. </param>
        public void Update(int add = 1) {
            lock (ValueKey) {
                current_value += add;

                if (current_value == max_value) {
                    stopwatch.Stop();
                }
            }
            Draw();
        }

        /// <summary> Update the drawn progress bar with the elapsed time and schedule the calling of this function after the current interval. </summary>
        private void Tick(object state) {
            try {
                Draw();
            } finally {
                interval = (int)(interval * 1.05); // Slowly increase the interval to not overwhelm the console with updates.
                timer?.Change(interval, Timeout.Infinite);
            }
        }

        void Draw(bool clear = true) {
            if (free && !Off) {
                free = false;

                if (clear && terminalContact) {
                    try { // Set the console back to the start of the line, to overwrite the last update.
                        Console.SetCursorPosition(0, Console.CursorTop);
                    } catch {
                        terminalContact = false;
                    }
                }

                int width = 30;
                if (terminalContact) {
                    try { // get the width of the console to make the progress bar fit the console snugly
                        width = Console.BufferWidth;
                    } catch {
                        terminalContact = false;
                    }
                }

                int value;
                lock (ValueKey) {
                    value = current_value;
                }

                // Generates the following output:
                // ----------------------------->                                                                                      |  25%  2.0 s
                var tail = $"| {Math.Round((double)value / max_value * 100),3}% {HelperFunctionality.DisplayTime(stopwatch.ElapsedMilliseconds)}";
                var bar_length = width - tail.Length - 1;
                var position = (int)Math.Round((double)value / max_value * bar_length);
                var stem = new String('-', position);
                var empty = new String(' ', bar_length - position);
                Console.Write($"{stem}>{empty}{tail}\b"); // The last \b is a backspace to make sure the cursor stays on this line and the drawing redraws over itself every update.

                free = true;
            }
        }
    }
}