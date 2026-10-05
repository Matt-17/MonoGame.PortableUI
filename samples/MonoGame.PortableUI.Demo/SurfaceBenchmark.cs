using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Themes;

namespace MonoGame.PortableUI.Demo
{
    /// <summary>
    ///     --benchmark-surfaces out.csv [--surface-count 100]: the cost of hosting many in-world screens
    ///     (an arcade hall): N text-mode editor stations as UISurfaces drawn into an atlas with
    ///     DrawIfNeededTo, flat (PostEffectMode.None, the host's shader does the tube) and with display
    ///     effects. Reports creation time and memory per surface, idle update/draw cost and the cost of
    ///     redrawing every surface at full and half tile size. GPU time is included: frames wait for it.
    /// </summary>
    internal static class SurfaceBenchmark
    {
        private const int Width = 640;
        private const int Height = 400;

        private sealed class StationScreen : Screen
        {
            public StationScreen(int index)
            {
                var editor = new TextBox
                {
                    IsMultiline = true,
                    Text = "program Station" + index + ";\nbegin\n  WriteLn('Night shift');\n  repeat until KeyPressed;\nend.",
                    Margin = new Thickness(8, 32, 8, 32)
                };
                var grid = new Grid();
                grid.AddChild(new TextBlock { Text = " File  Edit  Search  Run  Compile ", VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(8) });
                grid.AddChild(editor);
                grid.AddChild(new TextBlock { Text = " F1 Help  F2 Save  F9 Make  Alt+X Exit", VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(8) });
                Content = grid;
                Editor = editor;
            }

            public TextBox Editor { get; }
        }

        public static void Run(Game game, string file, int count)
        {
            var lines = new List<string>
            {
                "scenario,surfaces,create_ms_per_surface,managed_kb_per_surface,gpu_resources_per_surface,gpu_kb_per_surface,idle_update_us_per_surface,idle_draws_per_frame,idle_frame_ms,redraw_all_full_ms,redraw_all_half_ms,released_managed_kb_per_surface"
            };
            // Unreported warm-up: JIT, shaders, glyph atlases and pools are one-time costs, not per surface.
            RunScenario(game, "flat", Math.Min(count, 16));
            RunScenario(game, "crt", Math.Min(count, 16));
            foreach (var scenario in new[] { "flat", "flat-shared-theme", "crt" })
                lines.Add(RunScenario(game, scenario, count));
            File.WriteAllLines(file, lines);
        }

        private static string RunScenario(Game game, string scenario, int count)
        {
            var device = game.GraphicsDevice;
            var columns = 4;
            using var atlas = new RenderTarget2D(device, Width * columns, Height * 4, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            Rectangle Tile(int i, int scale) => new Rectangle(i % columns * Width, i / columns % 4 * Height, Width / scale, Height / scale);
            var probe = new Color[1];
            void WaitForGpu() => atlas.GetData(0, new Rectangle(0, 0, 1, 1), probe, 0, 1);

            var preset = PortableThemes.Resolve("dos");
            var sharedTheme = preset.CreateTheme();
            var time = TimeSpan.FromSeconds(1);
            var step = TimeSpan.FromMilliseconds(16);
            GameTime Next() { time += step; return new GameTime(time, step); }

            ForceGc();
            var managedBefore = GC.GetTotalMemory(true);
            var gpuBefore = GpuResources(device);
            var surfaces = new List<UISurface>(count);
            var watch = Stopwatch.StartNew();
            for (var i = 0; i < count; i++)
            {
                var station = new StationScreen(i);
                var surface = new UISurface(game, station, Width, Height, scenario == "flat-shared-theme" ? sharedTheme : preset.CreateTheme())
                {
                    InputSource = PortableUI.Input.NullInputSource.Instance,
                    TextGrid = TextGrid.Dos
                };
                if (scenario.StartsWith("flat", StringComparison.Ordinal))
                    surface.PostEffectMode = PostEffectMode.None;
                else
                    surface.PostEffects = new PostEffect[] { new CrtBarrelPostEffect { Distortion = 0.08f }, new ScanlinePostEffect() };
                station.Editor.Focus();
                surfaces.Add(surface);
            }
            watch.Stop();
            var createMs = watch.Elapsed.TotalMilliseconds / count;
            ForceGc();
            var managedPerSurface = (GC.GetTotalMemory(true) - managedBefore) / 1024.0 / count;

            // First frame of every surface (allocates scratch targets, glyphs, layer caches).
            var first = Next();
            foreach (var surface in surfaces)
                surface.Update(first);
            for (var i = 0; i < surfaces.Count; i++)
                surfaces[i].DrawIfNeededTo(atlas, Tile(i, 1), first);
            WaitForGpu();
            var gpuAfter = GpuResources(device);
            var gpuResourcesPerSurface = (gpuAfter.Count - gpuBefore.Count) / (double)count;
            var gpuKbPerSurface = (gpuAfter.Bytes - gpuBefore.Bytes) / 1024.0 / count;

            // Settle: let the first-frame grace and transitions run out.
            for (var f = 0; f < 60; f++)
            {
                var t = Next();
                for (var i = 0; i < surfaces.Count; i++)
                {
                    surfaces[i].Update(t);
                    surfaces[i].DrawIfNeededTo(atlas, Tile(i, 1), t);
                }
            }

            // Idle: every station visible, nothing changes but the carets.
            const int idleFrames = 240;
            var updateTicks = 0L;
            var drawn = 0L;
            var frameWatch = Stopwatch.StartNew();
            for (var f = 0; f < idleFrames; f++)
            {
                var t = Next();
                var u = Stopwatch.GetTimestamp();
                foreach (var surface in surfaces)
                    surface.Update(t);
                updateTicks += Stopwatch.GetTimestamp() - u;
                for (var i = 0; i < surfaces.Count; i++)
                {
                    if (surfaces[i].DrawIfNeededTo(atlas, Tile(i, 1), t))
                        drawn++;
                }
                WaitForGpu();
            }
            frameWatch.Stop();
            var idleUpdateUs = updateTicks * 1_000_000.0 / Stopwatch.Frequency / idleFrames / count;
            var idleDraws = drawn / (double)idleFrames;
            var idleFrameMs = frameWatch.Elapsed.TotalMilliseconds / idleFrames;

            double RedrawAll(int scale)
            {
                const int frames = 20;
                var w = Stopwatch.StartNew();
                for (var f = 0; f < frames; f++)
                {
                    var t = Next();
                    for (var i = 0; i < surfaces.Count; i++)
                    {
                        surfaces[i].Invalidate();
                        surfaces[i].DrawIfNeededTo(atlas, Tile(i, scale), t);
                    }
                    WaitForGpu();
                }
                return w.Elapsed.TotalMilliseconds / frames;
            }
            RedrawAll(1);
            var fullMs = RedrawAll(1);
            RedrawAll(2);
            var halfMs = RedrawAll(2);
            device.SetRenderTarget(null);

            foreach (var surface in surfaces)
                surface.Dispose();
            surfaces.Clear();
            ForceGc();
            var releasedPerSurface = (GC.GetTotalMemory(true) - managedBefore) / 1024.0 / count;

            var line = string.Format(CultureInfo.InvariantCulture, "{0},{1},{2:0.000},{3:0.0},{4:0.00},{5:0.0},{6:0.0},{7:0.00},{8:0.00},{9:0.00},{10:0.00},{11:0.0}",
                scenario, count, createMs, managedPerSurface, gpuResourcesPerSurface, gpuKbPerSurface, idleUpdateUs, idleDraws, idleFrameMs, fullMs, halfMs, releasedPerSurface);
            Console.WriteLine(line);
            return line;
        }

        /// <summary>Live textures and render targets on the device and their size (MonoGame keeps
        /// them in a private list; diagnostics only, zero when that internal changes).</summary>
        private static (int Count, long Bytes) GpuResources(GraphicsDevice device)
        {
            var field = typeof(GraphicsDevice).GetField("_resources", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field?.GetValue(device) is not System.Collections.IEnumerable list)
                return (0, 0);
            var count = 0;
            var bytes = 0L;
            lock (list)
            {
                foreach (var item in list)
                {
                    if (item is WeakReference { Target: Texture2D { IsDisposed: false } texture })
                    {
                        count++;
                        bytes += (long)texture.Width * texture.Height * 4;
                    }
                }
            }
            return (count, bytes);
        }

        private static void ForceGc()
        {
            for (var i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }
}
