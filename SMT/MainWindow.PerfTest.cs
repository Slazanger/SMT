using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Threading;
using EVEDataUtils;
using Newtonsoft.Json;

namespace SMT
{
    /// <summary>
    /// --perftest[=results.json]: once the main window has rendered, drive the region map through a fixed script,
    /// write the timings as JSON and close. The run changes the region, toggles and layout on purpose, so in this
    /// mode SMT saves nothing on exit except the characters (refreshed ESI tokens must not be lost).
    /// </summary>
    public partial class MainWindow
    {
        private static readonly string[] PerfTestRegions =
        {
            "Delve", "The Forge", "Catch", "Fountain", "Providence", "Querious", "Pure Blind", "Esoteria", "Detorid", "Vale of the Silent"
        };

        private string perfTestOutputPath;

        private bool IsPerfTestRun => perfTestOutputPath != null;

        private void ReadPerfTestArguments()
        {
            foreach(string arg in Environment.GetCommandLineArgs().Skip(1))
            {
                if(arg == "--perftest")
                {
                    perfTestOutputPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SMT-perftest.json");
                }
                else if(arg.StartsWith("--perftest=", StringComparison.Ordinal))
                {
                    perfTestOutputPath = arg.Substring("--perftest=".Length);
                }
            }
        }

        private void MainWindow_ContentRendered(object sender, EventArgs e)
        {
            ContentRendered -= MainWindow_ContentRendered;
            PerfMetrics.MarkStartup("first frame");
            AppLog.Metrics("Startup", PerfMetrics.StartupSummary());

            if(IsPerfTestRun)
            {
                _ = RunPerfTestAsync();
            }
        }

        private async Task RunPerfTestAsync()
        {
            Dictionary<string, object> results = new Dictionary<string, object>
            {
                ["startupMs"] = PerfMetrics.StartupMarks.ToDictionary(m => m.Key, m => Math.Round(m.Value)),
            };

            try
            {
                // let the startup background work (ESI, zKillboard connection, storms) settle
                await Task.Delay(TimeSpan.FromSeconds(5));
                if(RegionLayoutDoc != null)
                {
                    RegionLayoutDoc.IsSelected = true;
                }

                await WaitForIdleAsync();

                List<string> regions = PerfTestRegions.Where(name => EVEManager.GetRegion(name) != null).ToList();
                results["regionSwitch"] = await MeasureAsync(regions, 2, name => RegionUC.SelectRegion(name));

                RegionUC.SelectRegion("Delve");
                await WaitForIdleAsync();
                results["regionCanvasChildren"] = RegionUC.MainCanvas.Children.Count;
                results["fullRedraw"] = await MeasureAsync(Enumerable.Range(0, 10).ToList(), 1, _ => RegionUC.ReDrawMap(true));
                results["partialRedraw"] = await MeasureAsync(Enumerable.Range(0, 20).ToList(), 1, _ => RegionUC.ReDrawMap(false));
                results["regionCanvasChildrenAfterPartialRedraws"] = RegionUC.MainCanvas.Children.Count;

                // left-clicks on 20 systems through the system shapes' own MouseDown handler
                List<Shape> systemShapes = RegionUC.MainCanvas.Children.OfType<Shape>().Where(s => s.DataContext is EVEData.MapSystem).Take(20).ToList();
                results["systemClick"] = await MeasureAsync(systemShapes, 1, ClickShape);

                // a data overlay toggle (an even number of toggles leaves it as it was)
                results["dataOverlayToggle"] = await MeasureAsync(Enumerable.Range(0, 6).ToList(), 1, _ => RegionUC.ShowNPCKills = !RegionUC.ShowNPCKills);

                // a setting that has nothing to do with the region map (intel window flash)
                results["unrelatedSettingChange"] = await MeasureAsync(Enumerable.Range(0, 6).ToList(), 1, _ => MapConf.FlashWindow = !MapConf.FlashWindow);

                // sitting idle with the region map in view
                results["idleRegionMap"] = await MeasureIdleAsync(TimeSpan.FromSeconds(20));

                // an overlay window for a test character: 30 refreshes
                results["overlay"] = await MeasureOverlayAsync();

                // where idle time goes: the region map without a selected system (no highlight animation), then the universe map
                RegionUC.SelectedSystem = string.Empty;
                RegionUC.ReDrawMap(false);
                await WaitForIdleAsync();
                results["idleRegionMapNoSelection"] = await MeasureIdleAsync(TimeSpan.FromSeconds(15));

                // a region with jump bridges (from this profile's list), nothing selected
                string bridgeRegion = EVEManager.JumpBridges
                    .SelectMany(jb => new[] { EVEManager.GetEveSystem(jb.From)?.Region, EVEManager.GetEveSystem(jb.To)?.Region })
                    .Where(region => region != null)
                    .GroupBy(region => region)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault();
                if(bridgeRegion != null)
                {
                    RegionUC.SelectRegion(bridgeRegion);
                    RegionUC.SelectedSystem = string.Empty;
                    RegionUC.ReDrawMap(false);
                    await WaitForIdleAsync();
                    Dictionary<string, object> bridgeIdle = await MeasureIdleAsync(TimeSpan.FromSeconds(15));
                    bridgeIdle["region"] = bridgeRegion;
                    bridgeIdle["animatedLines"] = RegionUC.MainCanvas.Children.OfType<Shape>().Count(s => s.HasAnimatedProperties);
                    results["idleJumpBridgeRegion"] = bridgeIdle;
                }

                if(UniverseLayoutDoc != null)
                {
                    UniverseLayoutDoc.IsSelected = true;
                    await WaitForIdleAsync();
                    results["idleUniverseMap"] = await MeasureIdleAsync(TimeSpan.FromSeconds(15));
                }
            }
            catch(Exception exception)
            {
                results["error"] = exception.ToString();
            }

            File.WriteAllText(perfTestOutputPath, JsonConvert.SerializeObject(results, Formatting.Indented));
            Close();
        }

        private void ClosePerfTestRun()
        {
            try
            {
                EVEManager.SaveCharacters();
            }
            catch(Exception exception)
            {
                AppLog.Error("Save characters", exception);
            }

            EVEManager.ShutDown();

            waveOutEvent?.Stop();
            waveOutEvent?.Dispose();
            audioFileReader?.Dispose();
            nIcon.Visible = false;
            nIcon.Dispose();
        }

        private static void ClickShape(Shape shape)
        {
            MouseButtonEventArgs args = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
            {
                RoutedEvent = UIElement.MouseDownEvent,
                Source = shape,
            };

            // the handler only acts on single clicks, and ClickCount has an internal setter
            typeof(MouseButtonEventArgs).GetProperty(nameof(MouseButtonEventArgs.ClickCount)).GetSetMethod(true).Invoke(args, new object[] { 1 });
            shape.RaiseEvent(args);
        }

        private static async Task WaitForIdleAsync()
        {
            await System.Windows.Threading.Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        }

        /// <summary>
        /// Runs the action for each item: the time until it returns plus a layout pass, the time until the UI
        /// thread is idle again, and how many region map redraws it caused (the 2 s refresh can add a few).
        /// </summary>
        private async Task<Dictionary<string, object>> MeasureAsync<T>(IList<T> items, int passes, Action<T> action)
        {
            List<double> syncMs = new List<double>();
            List<double> settledMs = new List<double>();
            long fullBefore = PerfMetrics.RegionFullRedraws;
            long partialBefore = PerfMetrics.RegionPartialRedraws;
            double fullMsBefore = PerfMetrics.RegionFullRedrawMs;

            for(int pass = 0; pass < passes; pass++)
            {
                foreach(T item in items)
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    action(item);
                    UpdateLayout();
                    syncMs.Add(sw.Elapsed.TotalMilliseconds);
                    await WaitForIdleAsync();
                    settledMs.Add(sw.Elapsed.TotalMilliseconds);
                }
            }

            double count = syncMs.Count;
            return new Dictionary<string, object>
            {
                ["count"] = syncMs.Count,
                ["medianMs"] = Median(syncMs),
                ["meanMs"] = Math.Round(syncMs.Average(), 2),
                ["maxMs"] = Math.Round(syncMs.Max(), 2),
                ["settledMedianMs"] = Median(settledMs),
                ["fullRedrawsPerAction"] = Math.Round((PerfMetrics.RegionFullRedraws - fullBefore) / count, 2),
                ["partialRedrawsPerAction"] = Math.Round((PerfMetrics.RegionPartialRedraws - partialBefore) / count, 2),
                // time inside ReDrawMap(true) itself, without the layout pass and rendering that follow
                ["fullRedrawOwnMsPerAction"] = Math.Round((PerfMetrics.RegionFullRedrawMs - fullMsBefore) / count, 2),
            };
        }

        /// <summary>
        /// Opens an overlay for a test character in 1DQ1-A and refreshes it 30 times. Reports the refresh time, how
        /// many ToolTipOpening handlers one system shape carries afterwards, and what opening its tooltip costs.
        /// The test character isn't ESI-linked, so it is never saved; the overlay's placement isn't stored.
        /// </summary>
        private async Task<Dictionary<string, object>> MeasureOverlayAsync()
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            EVEData.LocalCharacter pilot = new EVEData.LocalCharacter("SMT perftest pilot", string.Empty, "1DQ1-A");
            EVEData.LocalCharacter previousCharacter = ActiveCharacter;
            Overlay.SuppressPlacementSaving = true;
            EVEManager.AddCharacter(pilot);
            ActiveCharacter = pilot;
            Overlay overlay = new Overlay(this);
            try
            {
                overlay.Show();
                await Task.Delay(TimeSpan.FromSeconds(1));
                await WaitForIdleAsync();

                MethodInfo refresh = typeof(Overlay).GetMethod("RefreshCurrentView", BindingFlags.Instance | BindingFlags.NonPublic);
                List<double> refreshMs = new List<double>();
                for(int i = 0; i < 30; i++)
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    refresh.Invoke(overlay, null);
                    overlay.UpdateLayout();
                    refreshMs.Add(sw.Elapsed.TotalMilliseconds);
                    await WaitForIdleAsync();
                }

                result["refreshes"] = refreshMs.Count;
                result["refreshFirst5MedianMs"] = Median(refreshMs.Take(5).ToList());
                result["refreshLast5MedianMs"] = Median(refreshMs.Skip(25).ToList());

                // the test character's own system shape
                IDictionary systemData = (IDictionary)typeof(Overlay).GetField("systemData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(overlay);
                object pilotSystem = systemData.Contains("1DQ1-A") ? systemData["1DQ1-A"] : null;
                Shape shape = pilotSystem?.GetType().GetField("systemCanvasElement").GetValue(pilotSystem) as Shape;
                result["systemsOnOverlay"] = systemData.Count;
                if(shape != null)
                {
                    object store = typeof(UIElement).GetProperty("EventHandlersStore", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(shape);
                    MethodInfo getHandlers = store?.GetType().GetMethod("GetRoutedEventHandlers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    Array handlers = getHandlers?.Invoke(store, new object[] { ToolTipService.ToolTipOpeningEvent }) as Array;
                    result["tooltipOpeningHandlersOnOneSystem"] = handlers?.Length ?? 0;

                    // what opening that system's tooltip costs now (each handler runs)
                    ConstructorInfo openingArgs = typeof(ToolTipEventArgs).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(bool) }, null);
                    if(openingArgs != null)
                    {
                        Stopwatch sw = Stopwatch.StartNew();
                        for(int i = 0; i < 10; i++)
                        {
                            ToolTipEventArgs args = (ToolTipEventArgs)openingArgs.Invoke(new object[] { true });
                            args.RoutedEvent = ToolTipService.ToolTipOpeningEvent;
                            args.Source = shape;
                            shape.RaiseEvent(args);
                        }

                        result["tooltipOpenMs"] = Math.Round(sw.Elapsed.TotalMilliseconds / 10, 3);
                    }
                }
            }
            finally
            {
                overlay.Close();
                ActiveCharacter = previousCharacter;
                EVEManager.RemoveCharacter(pilot);
            }

            return result;
        }

        private async Task<Dictionary<string, object>> MeasureIdleAsync(TimeSpan duration)
        {
            Dictionary<string, object> idle = await MeasureIdleCoreAsync(duration);
            idle["animatedShapesOnRegionMap"] = RegionUC.MainCanvas.Children.OfType<Shape>()
                .Count(s => s.HasAnimatedProperties || (s.RenderTransform?.HasAnimatedProperties ?? false));
            return idle;
        }

        private static async Task<Dictionary<string, object>> MeasureIdleCoreAsync(TimeSpan duration)
        {
            using Process process = Process.GetCurrentProcess();
            int gen0 = GC.CollectionCount(0);
            int gen1 = GC.CollectionCount(1);
            int gen2 = GC.CollectionCount(2);
            long allocated = GC.GetTotalAllocatedBytes();
            long partialRedraws = PerfMetrics.RegionPartialRedraws;
            double partialRedrawMs = PerfMetrics.RegionPartialRedrawMs;
            TimeSpan cpu = process.TotalProcessorTime;

            await Task.Delay(duration);

            process.Refresh();
            double seconds = duration.TotalSeconds;
            return new Dictionary<string, object>
            {
                ["seconds"] = seconds,
                ["cpuMsPerSecond"] = Math.Round((process.TotalProcessorTime - cpu).TotalMilliseconds / seconds, 1),
                ["allocatedMBPerSecond"] = Math.Round((GC.GetTotalAllocatedBytes() - allocated) / 1048576.0 / seconds, 2),
                ["gcGen0"] = GC.CollectionCount(0) - gen0,
                ["gcGen1"] = GC.CollectionCount(1) - gen1,
                ["gcGen2"] = GC.CollectionCount(2) - gen2,
                ["partialRedraws"] = PerfMetrics.RegionPartialRedraws - partialRedraws,
                ["partialRedrawMsPerSecond"] = Math.Round((PerfMetrics.RegionPartialRedrawMs - partialRedrawMs) / seconds, 2),
            };
        }

        private static double Median(List<double> values)
        {
            List<double> sorted = values.OrderBy(v => v).ToList();
            return Math.Round(sorted[sorted.Count / 2], 2);
        }
    }
}
