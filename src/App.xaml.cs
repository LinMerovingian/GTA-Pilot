﻿using GTAFlightDeck;
using GTAPilot.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace GTAPilot
{
    public partial class App : Application
    {
        private Dictionary<System.Windows.Threading.Dispatcher, List<ICanTick>> _ticks = new Dictionary<System.Windows.Threading.Dispatcher, List<ICanTick>>();

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            /*
            var a = Math2.GetPolarHeadingFromLine(new System.Drawing.PointF(
3624, 736), new System.Drawing.PointF(

3636, 857));
*/

            /*
            FlightPlan fp = new FlightPlan();
            fp.LoadFromFile(@"c:\workspace\FlightPlan.txt");

            Timeline.Load(@"c:\workspace\run9.txt");

            var locationPoints = new List<System.Drawing.PointF>();
            for (var i = 0; i < Timeline.LatestFrameId; i++)
            {
                locationPoints.Add(Timeline.Data[i].Location);
            }

            new FlightPlanMap(fp.Points.ToArray(), locationPoints.ToArray()).Show();

            return;
            
            */


            new SystemManager();

            if (SystemManager.Instance.App.IsRunning)
            {
                new ImmersiveWindow().Show();
            }

            var t = new Thread(TickThreadProc);
            t.IsBackground = true;
            t.Start();

            t = new Thread(() =>
            {
                var a = new AnalyzerWindow();
               // a.Show();
                System.Windows.Threading.Dispatcher.Run();
            });
            t.IsBackground = true;
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            
        }

        internal static void Register(ICanTick tick)
        {
            var ticks = ((App)App.Current)._ticks;

            if (!ticks.TryGetValue(System.Windows.Threading.Dispatcher.CurrentDispatcher, out var bucket))
            {
                ticks[System.Windows.Threading.Dispatcher.CurrentDispatcher] = new List<ICanTick>();
                bucket = ticks[System.Windows.Threading.Dispatcher.CurrentDispatcher];
            }

            bucket.Add(tick);
        }

        private void TickThreadProc()
        {
            var w = Stopwatch.StartNew();
            while (true)
            {
                var start = w.Elapsed.TotalMilliseconds;
                // We do this with a background thread and synchronously in order to avoid queue starvation
                // on the dispatcher. Bindings were observed to be unserviced indefinitely.

                foreach(var bucket in _ticks.Keys.ToArray())
                {
                    bucket.Invoke(() =>
                    {
                        try
                        {
                            foreach (var t in _ticks[bucket])
                            {
                                try
                                { t.Tick();

                                }
                                catch (Exception ex)
                                {
                                    throw;
                                }
                            }

                        }
                        catch(Exception ex)
                        {
                            throw;
                        }
                    });
                }

                var dt = w.Elapsed.TotalMilliseconds - start;
                Thread.Sleep(Math.Max(1, (int)((1000 / 10) - dt)));
            }
        }
    }
}
