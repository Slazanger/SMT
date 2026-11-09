using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace SMT
{
    /// <summary>
    /// Lightweight timings: the startup phases, written to the log once the main window has rendered, and
    /// counters that the --perftest mode reads. Recording costs a timestamp per call. UI thread only.
    /// </summary>
    internal static class PerfMetrics
    {
        private static readonly DateTime ProcessStart = Process.GetCurrentProcess().StartTime;
        private static readonly List<KeyValuePair<string, double>> StartupMarkList = new List<KeyValuePair<string, double>>();

        public static long RegionFullRedraws;
        public static double RegionFullRedrawMs;
        public static long RegionPartialRedraws;
        public static double RegionPartialRedrawMs;

        /// <summary>
        /// Each startup phase with the milliseconds from process start to its end.
        /// </summary>
        public static IReadOnlyList<KeyValuePair<string, double>> StartupMarks => StartupMarkList;

        /// <summary>
        /// Records that a startup phase has just finished. Ignored once the first frame has been recorded, so code
        /// that also runs later (a full map redraw) can mark its steps.
        /// </summary>
        public static void MarkStartup(string phase)
        {
            if(startupComplete)
            {
                return;
            }

            StartupMarkList.Add(new KeyValuePair<string, double>(phase, (DateTime.Now - ProcessStart).TotalMilliseconds));
            if(phase == FirstFrame)
            {
                startupComplete = true;
            }
        }

        public const string FirstFrame = "first frame";

        private static bool startupComplete;

        /// <summary>
        /// "4312 ms to the first frame (MainWindow constructor 512, InitializeComponent 650, ... ms)", each phase with its own duration.
        /// </summary>
        public static string StartupSummary()
        {
            StringBuilder phases = new StringBuilder();
            double previous = 0;
            foreach(KeyValuePair<string, double> mark in StartupMarkList)
            {
                if(phases.Length > 0)
                {
                    phases.Append(", ");
                }

                phases.Append(mark.Key).Append(' ').Append((mark.Value - previous).ToString("0", CultureInfo.InvariantCulture));
                previous = mark.Value;
            }

            return $"{previous.ToString("0", CultureInfo.InvariantCulture)} ms to the first frame ({phases} ms)";
        }

        public static void RecordRegionRedraw(bool fullRedraw, long startTimestamp)
        {
            double ms = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
            if(fullRedraw)
            {
                RegionFullRedraws++;
                RegionFullRedrawMs += ms;
            }
            else
            {
                RegionPartialRedraws++;
                RegionPartialRedrawMs += ms;
            }
        }
    }
}
