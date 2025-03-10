//!CompilerOption|AddRef|SharpDX.Direct2D1.dll
//!CompilerOption|AddRef|GameOverlay.dll
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.SimpleEXtensions;
using GameOverlay.Drawing;
using GameOverlay.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace FollowBot.Class
{
    public class OverlayWindow : IDisposable
    {
        public static OverlayWindow Instance { get; private set; }
        private readonly GraphicsWindow _window;
        private readonly Dictionary<string, SolidBrush> _brushes;
        private readonly Dictionary<string, Font> _fonts;
        private readonly Dictionary<string, Image> _images;
        private Geometry _gridGeometry;
        private Rectangle _gridBounds;
        private Random _random;
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();


        public OverlayWindow(IntPtr parentPtr)
        {
            _brushes = new Dictionary<string, SolidBrush>();
            _fonts = new Dictionary<string, Font>();
            _images = new Dictionary<string, Image>();
            _random = LokiPoe.Random;

            var gfx = new Graphics()
            {
                MeasureFPS = true,
                PerPrimitiveAntiAliasing = true,
                TextAntiAliasing = true
            };

            _window = new StickyWindow(parentPtr, gfx)
            {
                FPS = FollowBotSettings.Instance.FPS,
                IsTopmost = true,
                IsVisible = true
            };

            _window.DestroyGraphics += _window_DestroyGraphics;
            _window.DrawGraphics += _window_DrawGraphics;
            _window.SetupGraphics += _window_SetupGraphics;
            _window.PropertyChanged += _window_PropertyChanged;
            Instance = this;
        }

        private void _window_PropertyChanged(object sender, OverlayPropertyChangedEventArgs e)
        {
            GlobalLog.Warn($"[_window_PropertyChanged] {e.PropertyName}: {e.Value}");
        }

        private void _window_SetupGraphics(object sender, SetupGraphicsEventArgs e)
        {
            var gfx = e.Graphics;

            if (e.RecreateResources)
            {
                foreach (var pair in _brushes) pair.Value.Dispose();
                foreach (var pair in _images) pair.Value.Dispose();
            }

            _brushes["black"] = gfx.CreateSolidBrush(0, 0, 0);
            _brushes["transparent_black"] = gfx.CreateSolidBrush(0, 0, 0, FollowBotSettings.Instance.OverlayTransparency);
            _brushes["white"] = gfx.CreateSolidBrush(255, 255, 255);
            _brushes["red"] = gfx.CreateSolidBrush(255, 0, 0);
            _brushes["green"] = gfx.CreateSolidBrush(0, 255, 0);
            _brushes["blue"] = gfx.CreateSolidBrush(0, 0, 255);
            _brushes["background"] = gfx.CreateSolidBrush(0x33, 0x36, 0x3F);
            _brushes["grid"] = gfx.CreateSolidBrush(255, 255, 255, 0.2f);
            _brushes["random"] = gfx.CreateSolidBrush(0, 0, 0);

            if (e.RecreateResources) return;

            _fonts["arial"] = gfx.CreateFont("Arial", 12);
            _fonts["consolas"] = gfx.CreateFont("Consolas", 14);

            _gridBounds = new Rectangle(20, 60, gfx.Width - 20, gfx.Height - 20);
            _gridGeometry = gfx.CreateGeometry();

            for (float x = _gridBounds.Left; x <= _gridBounds.Right; x += 20)
            {
                var line = new Line(x, _gridBounds.Top, x, _gridBounds.Bottom);
                _gridGeometry.BeginFigure(line);
                _gridGeometry.EndFigure(false);
            }

            for (float y = _gridBounds.Top; y <= _gridBounds.Bottom; y += 20)
            {
                var line = new Line(_gridBounds.Left, y, _gridBounds.Right, y);
                _gridGeometry.BeginFigure(line);
                _gridGeometry.EndFigure(false);
            }

            _gridGeometry.Close();

        }

        private void _window_DestroyGraphics(object sender, DestroyGraphicsEventArgs e)
        {
            foreach (var pair in _brushes) pair.Value.Dispose();
            foreach (var pair in _fonts) pair.Value.Dispose();
            foreach (var pair in _images) pair.Value.Dispose();
        }

        private void _window_DrawGraphics(object sender, DrawGraphicsEventArgs e)
        {
            var gfx = e.Graphics;
            gfx.ClearScene();
            if (!FollowBotSettings.Instance.EnableOverlay)
            {
                return;
            }

            if (!FollowBotSettings.Instance.DrawInBackground && LokiPoe.ClientWindowHandle != GetForegroundWindow())
            {
                return;
            }

            const int padding = 16;
            var infoText = new StringBuilder()
                .Append("FPS: ").Append(gfx.FPS.ToString().PadRight(padding))
                .Append("FrameTime: ").Append(e.FrameTime.ToString().PadRight(padding))
                .Append("FrameCount: ").Append(e.FrameCount.ToString().PadRight(padding))
                .Append("DeltaTime: ").Append(e.DeltaTime.ToString().PadRight(padding))
                //.Append("KitePositions: ").Append(kitePositions.Count.ToString().PadRight(padding))
                //.Append("Monsters: ").Append(monsterPositions.Count.ToString().PadRight(padding))
                .ToString();

            var botInfoText = new StringBuilder()
                .Append($"{"Follow: ",-16}  {$"{FollowBotSettings.Instance.ShouldFollow}",-10}  {"",5}").AppendLine()
                .Append($"{"Loot: ",-16}  {$"{FollowBotSettings.Instance.ShouldLoot}",-10}  {"",5}").AppendLine()
                .Append($"{"Attack: ",-16}  {$"{FollowBotSettings.Instance.ShouldKill}",-10}  {"",5}").AppendLine()
                .Append($"{"Sentinel: ",-16}  {$"{FollowBotSettings.Instance.UseStalkerSentinel}",-10}  {"",5}").AppendLine()
                .Append($"{"Auto Teleport: ",-16}  {$"{!FollowBotSettings.Instance.DontPortOutofMap}",-10}  {"",5}").AppendLine()
                .Append(
                    $"{"Follow Dist: ",-16}  {$"{FollowBotSettings.Instance.FollowDistance}/{FollowBotSettings.Instance.MaxFollowDistance}",-10}  {"",5}").AppendLine()
                .Append($"{"Combat Dist: ",-16}  {$"{FollowBotSettings.Instance.MaxCombatDistance}",-10}  {"",5}").AppendLine()
                .Append($"{"Loot Dist: ",-16}  {$"{FollowBotSettings.Instance.MaxLootDistance}",-10}  {"",5}").AppendLine()
                .ToString();


            gfx.DrawTextWithBackground(_fonts["consolas"], _brushes["green"], _brushes["transparent_black"], 58, 20, infoText);
            gfx.DrawTextWithBackground(_fonts["consolas"], _brushes["green"], _brushes["transparent_black"], FollowBotSettings.Instance.OverlayXCoord, FollowBotSettings.Instance.OverlayYCoord, botInfoText);
            if (!LokiPoe.IsInGame) return;

            if (FollowBotSettings.Instance.DrawMobs || FollowBotSettings.Instance.DrawCorpses)
                DrawMobs(gfx);
        }

        private SolidBrush GetHpBasedBrush(float hpPct, Graphics gfx)
        {
            if (hpPct > 50)
            {
                var factor = 100 - hpPct;
                return gfx.CreateSolidBrush(factor * 3, 255, 0);
            }
            else if (hpPct == 50)
            {
                return gfx.CreateSolidBrush(255, 255, 0);
            }
            else
            {
                var factor = 49 - hpPct;
                return gfx.CreateSolidBrush(255, hpPct * 3, 0);
            }
        }
        private void DrawMobs(Graphics gfx)
        {
            var monsts = LokiPoe.ObjectManager.MetadataDictionary;
            foreach (var networkObject in monsts.SelectMany(monst => monst.Value))
            {
                try
                {
                    if (networkObject.Distance > 85) continue;
                    var monter = networkObject as Monster;
                    if (monter == null) continue;
                    switch (monter.IsActiveDead)
                    {
                        case true when !FollowBotSettings.Instance.DrawCorpses:
                        case false when !FollowBotSettings.Instance.DrawMobs:
                            continue;
                    }

                    LokiPoe.ClientFunctions.WorldToScreen(monter.Position.MapToWorld3(), out var monsterX, out var monsterY);
                    var name = monter.Name;

                    gfx.DrawText(_fonts["arial"], GetHpBasedBrush(monter.HealthPercent, gfx), monsterX, monsterY, name);
                }
                catch
                {
                }
            }
        }



        private SolidBrush GetRandomColor()
        {
            var brush = _brushes["random"];

            brush.Color = new Color(_random.Next(0, 256), _random.Next(0, 256), _random.Next(0, 256));

            return brush;
        }
        public void FitTo(IntPtr ptr)
        {
            _window.FitTo(ptr);
        }
        public void UpdateOverlayPosition(int x, int y, int w, int h)
        {
            _window.X = x;
            _window.Y = y;
            _window.Width = w;
            _window.Height = h;
        }

        public void Run()
        {
            _window.Create();
            _window.Join();
        }
        public void Start()
        {
            _window.Create();
        }

        ~OverlayWindow()
        {
            Dispose(false);
        }

        #region IDisposable Support
        private bool _disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue) return;
            _window.Dispose();
            _disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        public void SetFps(int fps)
        {
            if (_window == null) return;
            _window.FPS = fps;
        }

        public void SetTransparency(int overlayTransparency)
        {
            if ((_window) == null) return;
            if (_brushes == null || !_brushes.ContainsKey("transparent_black")) return;
            _brushes["transparent_black"] = _window.Graphics.CreateSolidBrush(0, 0, 0, FollowBotSettings.Instance.OverlayTransparency);
        }
    }
}
