using System;
using System.Collections.Generic;
using UnityEngine;

namespace RafTris
{
    /// <summary>
    /// IMGUI-based renderer for the RafTris overlay window.
    /// All layout is driven by a single cell-size computed from the window height.
    /// </summary>
    public class RafTrisUI : MonoBehaviour
    {
        public RafTrisManager Manager;

        // Window state
        private Rect   _windowRect;
        private bool   _windowInitialised;

        // Scroll for leaderboard / biome scores panel
        private Vector2 _scoreScroll;

        // Cached trophy sprites per piece type (invalidated on biome change)
        private readonly Dictionary<TetrominoPieceType, Texture2D> _trophyTextures =
            new Dictionary<TetrominoPieceType, Texture2D>();
        private int _lastRenderedBiome = -1;

        // Animation counters
        private float _titlePulse;
        private float _tetrisFlashHue;

        // GUI skin/styles (built on first OnGUI call)
        private GUISkin  _skin;
        private GUIStyle _windowStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _smallLabelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _bigButtonStyle;
        private bool     _stylesBuilt;

        // Unique IMGUI window ID — arbitrary integer, just needs to not clash with Valheim's own windows
        private const int WindowId = 0xFA17;  // "RAFT" mnemonic in hex (F-A-1-7 → close enough)


        private const float BaseWindowWidth  = 580f;
        private const float BaseWindowHeight = 720f;

        private float Scale        => RafTrisPlugin.WindowScale.Value;
        private float WinW         => BaseWindowWidth  * Scale;
        private float WinH         => BaseWindowHeight * Scale;
        private float CellSize     => (WinH * 0.72f) / RafTrisGame.VisibleRows;
        private float BoardWidth   => CellSize * RafTrisGame.BoardCols;
        private float BoardHeight  => CellSize * RafTrisGame.VisibleRows;
        private float SidePanelW   => WinW - BoardWidth - 32f;
        private float Pad          => 8f * Scale;

        // ─────────────────────────────────────────────────────────────────

        private void Update()
        {
            _titlePulse   += Time.unscaledDeltaTime * 2f;
            _tetrisFlashHue += Time.unscaledDeltaTime * 180f;
        }

        private void OnGUI()
        {
            if (Manager == null || !Manager.IsVisible) return;

            EnsureStyles();
            EnsureWindowRect();

            // Block Valheim's own input while our window has focus
            if (_windowRect.Contains(Event.current.mousePosition))
                Input.ResetInputAxes();

            GUI.skin = _skin;
            _windowRect = GUI.Window(WindowId, _windowRect, DrawWindow, GUIContent.none, _windowStyle);
        }

        // ─────────────────────────────────────────────────────────────────
        //  Main window
        // ─────────────────────────────────────────────────────────────────

        private void DrawWindow(int id)
        {
            var game   = Manager.Game;
            var theme  = game != null ? BiomeThemes.ForLevel(game.Level)
                                      : BiomeThemes.ForLevel(0);

            // Refresh trophy textures when biome changes
            int biomeIdx = game != null ? game.Level % BiomeThemes.All.Count : 0;
            if (biomeIdx != _lastRenderedBiome)
            {
                _trophyTextures.Clear();
                _lastRenderedBiome = biomeIdx;
            }

            DrawBackground(theme);
            DrawTitle(theme);

            float topY       = 50f * Scale;
            float boardLeft  = Pad;
            float sideLeft   = boardLeft + BoardWidth + Pad;

            DrawBoard(game, theme, new Rect(boardLeft, topY, BoardWidth, BoardHeight));
            DrawSidePanel(game, theme, new Rect(sideLeft, topY, SidePanelW, BoardHeight));
            DrawControlBar(game, new Rect(0, topY + BoardHeight + Pad, WinW, 44f * Scale));

            GUI.DragWindow(new Rect(0, 0, WinW, 40f * Scale));
        }

        // ─────────────────────────────────────────────────────────────────
        //  Background
        // ─────────────────────────────────────────────────────────────────

        private void DrawBackground(BiomeTheme theme)
        {
            var bgRect = new Rect(0, 0, WinW, WinH);
            // Dark background fill
            GUI.color = new Color(theme.BackgroundColor.r, theme.BackgroundColor.g,
                                  theme.BackgroundColor.b, 0.96f);
            GUI.DrawTexture(bgRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Scanline effect: draw alternating transparent bands
            float lineH = 3f * Scale;
            var scanColor = new Color(1, 1, 1, 0.025f);
            for (float y = 0; y < WinH; y += lineH * 2)
            {
                GUI.color = scanColor;
                GUI.DrawTexture(new Rect(0, y, WinW, lineH), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Title bar
        // ─────────────────────────────────────────────────────────────────

        private void DrawTitle(BiomeTheme theme)
        {
            float pulse = (Mathf.Sin(_titlePulse) * 0.15f) + 0.85f;
            _titleStyle.normal.textColor = new Color(
                theme.AccentColor.r * pulse,
                theme.AccentColor.g * pulse,
                theme.AccentColor.b * pulse, 1f);

            var titleRect = new Rect(0, 4f * Scale, WinW * 0.55f, 42f * Scale);
            GUI.Label(titleRect, "⚓ RafTris", _titleStyle);

            // Biome name
            _smallLabelStyle.normal.textColor = theme.SecondaryColor;
            var biomeRect = new Rect(WinW * 0.55f, 10f * Scale, WinW * 0.42f, 28f * Scale);
            GUI.Label(biomeRect,
                (Manager.Game != null ? BiomeThemes.ForLevel(Manager.Game.Level).Name : "Meadows"),
                _smallLabelStyle);

            // Close button
            GUI.color = new Color(1, 0.3f, 0.3f, 1);
            if (GUI.Button(new Rect(WinW - 32f * Scale, 6f * Scale, 24f * Scale, 24f * Scale), "✕",
                           _buttonStyle))
            {
                Manager.ToggleWindow();
            }
            GUI.color = Color.white;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Board
        // ─────────────────────────────────────────────────────────────────

        private void DrawBoard(RafTrisGame game, BiomeTheme theme, Rect boardRect)
        {
            // Board border
            DrawBorderedRect(boardRect, theme.PrimaryColor, 2f * Scale);

            if (game == null)
            {
                DrawNoGameMessage(boardRect, theme);
                return;
            }

            float cellW = boardRect.width  / RafTrisGame.BoardCols;
            float cellH = boardRect.height / RafTrisGame.VisibleRows;

            // Grid lines
            GUI.color = new Color(theme.GridLineColor.r, theme.GridLineColor.g,
                                  theme.GridLineColor.b, 0.5f);
            for (int c = 1; c < RafTrisGame.BoardCols; c++)
                GUI.DrawTexture(new Rect(boardRect.x + c * cellW, boardRect.y, 1, boardRect.height),
                                Texture2D.whiteTexture);
            for (int r = 1; r < RafTrisGame.VisibleRows; r++)
                GUI.DrawTexture(new Rect(boardRect.x, boardRect.y + r * cellH, boardRect.width, 1),
                                Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Locked cells
            int rowOffset = RafTrisGame.BoardRows - RafTrisGame.VisibleRows;
            for (int r = 0; r < RafTrisGame.VisibleRows; r++)
            {
                bool isFlashRow = Manager.IsFlashing && ArrayContains(Manager.FlashRows, r + rowOffset);

                for (int c = 0; c < RafTrisGame.BoardCols; c++)
                {
                    int val = game.Board[r + rowOffset, c];
                    if (val == 0) continue;

                    var pieceType = (TetrominoPieceType)(val - 1);
                    var cellRect  = new Rect(boardRect.x + c * cellW, boardRect.y + r * cellH,
                                            cellW - 1, cellH - 1);

                    if (isFlashRow)
                    {
                        // Flash animation — cycle through hue
                        float hue = (_tetrisFlashHue % 360f) / 360f;
                        GUI.color = Color.HSVToRGB(hue, 0.9f, 1f);
                        GUI.DrawTexture(cellRect, Texture2D.whiteTexture);
                        GUI.color = Color.white;
                    }
                    else
                    {
                        DrawCell(cellRect, pieceType, theme, false);
                    }
                }
            }

            // Ghost piece
            var ghost = game.GetGhostPiece();
            if (ghost != null)
            {
                foreach (var cell in ghost.Cells())
                {
                    int visRow = cell.y - rowOffset;
                    if (visRow < 0 || visRow >= RafTrisGame.VisibleRows) continue;
                    var cellRect = new Rect(boardRect.x + cell.x * cellW,
                                           boardRect.y + visRow    * cellH,
                                           cellW - 1, cellH - 1);
                    GUI.color = new Color(1, 1, 1, 0.18f);
                    GUI.DrawTexture(cellRect, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
            }

            // Active piece
            if (game.CurrentPiece != null && !Manager.IsFlashing)
            {
                foreach (var cell in game.CurrentPiece.Cells())
                {
                    int visRow = cell.y - rowOffset;
                    if (visRow < 0 || visRow >= RafTrisGame.VisibleRows) continue;
                    var cellRect = new Rect(boardRect.x + cell.x * cellW,
                                           boardRect.y + visRow    * cellH,
                                           cellW - 1, cellH - 1);
                    DrawCell(cellRect, game.CurrentPiece.Type, theme, true);
                }
            }

            // Overlay for paused / game-over states
            if (game.State == GameState.Paused)
                DrawCentredOverlay(boardRect, "PAUSED", theme.AccentColor);
            else if (game.State == GameState.GameOver)
                DrawCentredOverlay(boardRect, "GAME OVER", new Color(1f, 0.2f, 0.2f));
        }

        private void DrawCell(Rect rect, TetrominoPieceType pieceType, BiomeTheme theme, bool active)
        {
            // Background colour for the cell
            Color bg = PieceColor(pieceType, theme);
            if (active) bg = Color.Lerp(bg, Color.white, 0.15f);

            GUI.color = bg;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            // Trophy icon overlay — rendered at 150% of cell size (50% zoom in) and
            // clipped to the cell bounds via GUI.BeginGroup so the overflow is hidden.
            var sprite = TrophyIconLoader.GetTrophySprite(theme.TrophyForPiece(pieceType), bg);
            if (sprite != null)
            {
                var tex = SpriteToTexture(sprite, pieceType);
                if (tex != null)
                {
                    float cellW    = rect.width;
                    float cellH    = rect.height;
                    float iconSize = Mathf.Min(cellW, cellH) * 1.50f;   // 50% zoom
                    float offsetX  = (cellW - iconSize) * 0.5f;          // centre, overflows edges
                    float offsetY  = (cellH - iconSize) * 0.5f;

                    // BeginGroup clips everything drawn inside to rect
                    GUI.BeginGroup(rect);
                    GUI.color = new Color(1, 1, 1, 0.92f);
                    GUI.DrawTexture(new Rect(offsetX, offsetY, iconSize, iconSize), tex);
                    GUI.color = Color.white;
                    GUI.EndGroup();
                }
            }

            // Highlight edge
            GUI.color = new Color(1, 1, 1, 0.25f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 2, rect.height), Texture2D.whiteTexture);

            GUI.color = Color.white;
        }

        private Texture2D SpriteToTexture(Sprite sprite, TetrominoPieceType key)
        {
            if (_trophyTextures.TryGetValue(key, out var cached)) return cached;

            if (sprite == null) return null;

            // Valheim trophy textures are not CPU-readable, so we can't use GetPixels().
            // Instead, blit through a RenderTexture — this works for any texture regardless
            // of its isReadable flag, because it goes through the GPU.
            var srcTex  = sprite.texture;
            var srcRect = sprite.textureRect;

            int w = Mathf.Max(1, (int)srcRect.width);
            int h = Mathf.Max(1, (int)srcRect.height);

            var rt  = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
            var dst = new Texture2D(w, h, TextureFormat.RGBA32, false);
            dst.filterMode = FilterMode.Bilinear;

            // Scale+offset so only the sprite's sub-rect of the atlas is blitted
            float scaleX  = srcRect.width  / srcTex.width;
            float scaleY  = srcRect.height / srcTex.height;
            float offsetX = srcRect.x      / srcTex.width;
            float offsetY = srcRect.y      / srcTex.height;

            Graphics.Blit(srcTex, rt, new Vector2(scaleX, scaleY), new Vector2(offsetX, offsetY));

            // Read back from RenderTexture into the readable Texture2D
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            dst.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            dst.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            _trophyTextures[key] = dst;
            return dst;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Side panel
        // ─────────────────────────────────────────────────────────────────

        private void DrawSidePanel(RafTrisGame game, BiomeTheme theme, Rect panel)
        {
            float y = panel.y;
            float x = panel.x;
            float w = panel.width;

            // ── NEXT piece ────────────────────────────────────────────────
            y = DrawSectionHeader(x, y, w, "NEXT", theme);
            float previewSize = w * 0.8f;
            DrawPiecePreview(new Rect(x + (w - previewSize) * 0.5f, y, previewSize, previewSize * 0.6f),
                             game?.NextPieceType ?? TetrominoPieceType.I, theme, game == null);
            y += previewSize * 0.65f + Pad;

            // ── HOLD piece ────────────────────────────────────────────────
            y = DrawSectionHeader(x, y, w, "HOLD", theme);
            bool hasHold = game != null && game.HasHeld;
            DrawPiecePreview(new Rect(x + (w - previewSize) * 0.5f, y, previewSize, previewSize * 0.6f),
                             hasHold ? game.HeldPieceType : TetrominoPieceType.O, theme, !hasHold);
            y += previewSize * 0.65f + Pad;

            // ── Score ─────────────────────────────────────────────────────
            y = DrawSectionHeader(x, y, w, "SCORE", theme);
            _labelStyle.normal.textColor = theme.AccentColor;
            GUI.Label(new Rect(x, y, w, 26f * Scale),
                      game != null ? game.Score.ToString("N0") : "0", _labelStyle);
            y += 28f * Scale;

            _smallLabelStyle.normal.textColor = theme.SecondaryColor;
            GUI.Label(new Rect(x, y, w, 20f * Scale),
                      $"BEST  {Manager.SaveData.AllTimeBestScore:N0}", _smallLabelStyle);
            y += 22f * Scale + Pad;

            // ── Level / Lines ──────────────────────────────────────────────
            y = DrawSectionHeader(x, y, w, "LEVEL", theme);
            _labelStyle.normal.textColor = theme.PrimaryColor;
            GUI.Label(new Rect(x, y, w, 26f * Scale),
                      game != null ? game.Level.ToString() : "0", _labelStyle);
            y += 28f * Scale;

            _smallLabelStyle.normal.textColor = theme.SecondaryColor;
            GUI.Label(new Rect(x, y, w, 20f * Scale),
                      $"LINES  {(game?.LinesCleared ?? 0)}", _smallLabelStyle);
            y += 22f * Scale + Pad;

            // ── Biome description ─────────────────────────────────────────
            if (theme.IsPlaceholder)
            {
                _smallLabelStyle.normal.textColor = new Color(1, 0.8f, 0.3f, 0.9f);
                GUI.Label(new Rect(x, y, w, 40f * Scale),
                          "⚠ Deep North\ntrophies TBA", _smallLabelStyle);
                y += 44f * Scale;
            }
        }

        private float DrawSectionHeader(float x, float y, float w, string label, BiomeTheme theme)
        {
            _smallLabelStyle.normal.textColor = new Color(
                theme.PrimaryColor.r, theme.PrimaryColor.g, theme.PrimaryColor.b, 0.7f);
            GUI.Label(new Rect(x, y, w, 18f * Scale), label, _smallLabelStyle);
            // Underline
            GUI.color = new Color(theme.GridLineColor.r, theme.GridLineColor.g,
                                  theme.GridLineColor.b, 0.6f);
            GUI.DrawTexture(new Rect(x, y + 18f * Scale, w, 1), Texture2D.whiteTexture);
            GUI.color = Color.white;
            return y + 22f * Scale;
        }

        private void DrawPiecePreview(Rect rect, TetrominoPieceType type, BiomeTheme theme, bool ghost)
        {
            // Border
            DrawBorderedRect(rect, theme.GridLineColor, 1);

            if (ghost) return;

            var offsets = TetrominoDefinitions.Rotations[type][0];
            float cellW = rect.width  / 4f;
            float cellH = rect.height / 3f;

            foreach (var o in offsets)
            {
                if (o.x < 0 || o.x >= 4 || o.y < 0 || o.y >= 3) continue;
                var cellRect = new Rect(rect.x + o.x * cellW, rect.y + o.y * cellH,
                                        cellW - 1, cellH - 1);
                DrawCell(cellRect, type, theme, false);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  Control bar
        // ─────────────────────────────────────────────────────────────────

        private void DrawControlBar(RafTrisGame game, Rect bar)
        {
            float btnW = (bar.width - Pad * 5) / 4f;
            float btnH = bar.height - Pad;
            float y    = bar.y + Pad * 0.5f;

            if (game == null || game.State == GameState.WaitingToStart ||
                game.State == GameState.GameOver)
            {
                // Show: New Game
                if (DrawButton(new Rect(bar.x + Pad, y, btnW * 2, btnH), "⚓  New Game"))
                    Manager.StartNewGame();

                if (DrawButton(new Rect(bar.x + Pad * 2 + btnW * 2, y, btnW * 1.5f, btnH), "✕  Close"))
                    Manager.ToggleWindow();
            }
            else if (game.State == GameState.Playing)
            {
                // Show: Pause | End Session | Close
                if (DrawButton(new Rect(bar.x + Pad, y, btnW, btnH), "⏸  Pause"))
                    Manager.PauseGame();

                if (DrawButton(new Rect(bar.x + Pad * 2 + btnW, y, btnW * 1.5f, btnH), "■  End Session"))
                {
                    Manager.EndSession();
                }

                if (DrawButton(new Rect(bar.x + Pad * 3 + btnW * 2.5f, y, btnW * 1.2f, btnH), "✕  Close"))
                    Manager.ToggleWindow();
            }
            else if (game.State == GameState.Paused)
            {
                // Show: Resume | New Game | End Session | Close
                if (DrawButton(new Rect(bar.x + Pad, y, btnW, btnH), "▶  Resume"))
                    Manager.ResumeGame();

                if (DrawButton(new Rect(bar.x + Pad * 2 + btnW, y, btnW, btnH), "⚓  New"))
                    Manager.StartNewGame();

                if (DrawButton(new Rect(bar.x + Pad * 3 + btnW * 2, y, btnW * 1.2f, btnH), "■  End"))
                    Manager.EndSession();

                if (DrawButton(new Rect(bar.x + Pad * 4 + btnW * 3.2f, y, btnW * 0.8f, btnH), "✕"))
                    Manager.ToggleWindow();
            }
        }

        private bool DrawButton(Rect rect, string label)
        {
            return GUI.Button(rect, label, _bigButtonStyle);
        }

        // ─────────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────────

        private void DrawNoGameMessage(Rect boardRect, BiomeTheme theme)
        {
            _labelStyle.normal.textColor = theme.SecondaryColor;
            var msg = "Press ⚓ New Game\nto begin your voyage.";
            GUI.Label(new Rect(boardRect.x + Pad, boardRect.y + boardRect.height * 0.35f,
                               boardRect.width - Pad * 2, 60f * Scale), msg, _labelStyle);
        }

        private void DrawCentredOverlay(Rect boardRect, string text, Color color)
        {
            GUI.color = new Color(0, 0, 0, 0.6f);
            GUI.DrawTexture(boardRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            _titleStyle.normal.textColor = color;
            float tw = _titleStyle.CalcSize(new GUIContent(text)).x;
            GUI.Label(new Rect(boardRect.x + (boardRect.width - tw) * 0.5f,
                               boardRect.y  + boardRect.height * 0.4f,
                               tw, 50f * Scale), text, _titleStyle);
        }

        private void DrawBorderedRect(Rect rect, Color color, float thickness)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x,                       rect.y,                        rect.width,    thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x,                       rect.y + rect.height - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x,                       rect.y,                        thickness,     rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y,                    thickness,     rect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private Color PieceColor(TetrominoPieceType type, BiomeTheme theme)
        {
            switch (type)
            {
                case TetrominoPieceType.I: return theme.AccentColor;
                case TetrominoPieceType.O: return theme.PrimaryColor;
                case TetrominoPieceType.S: return Color.Lerp(theme.PrimaryColor,   theme.SecondaryColor, 0.5f);
                case TetrominoPieceType.Z: return Color.Lerp(theme.AccentColor,    theme.PrimaryColor,   0.5f);
                case TetrominoPieceType.L: return Color.Lerp(theme.SecondaryColor, theme.AccentColor,    0.3f);
                case TetrominoPieceType.J: return Color.Lerp(theme.PrimaryColor,   theme.AccentColor,    0.7f);
                case TetrominoPieceType.T: return theme.SecondaryColor;
                default:                   return Color.white;
            }
        }

        private static bool ArrayContains(int[] arr, int val)
        {
            if (arr == null) return false;
            foreach (int v in arr) if (v == val) return true;
            return false;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Style / layout initialisation
        // ─────────────────────────────────────────────────────────────────

        private void EnsureWindowRect()
        {
            if (_windowInitialised) return;
            _windowInitialised = true;
            float cx = (Screen.width  - WinW) * 0.5f;
            float cy = (Screen.height - WinH) * 0.5f;
            _windowRect = new Rect(cx, cy, WinW, WinH);
        }

        private void EnsureStyles()
        {
            if (_stylesBuilt) return;
            _stylesBuilt = true;

            _skin = ScriptableObject.CreateInstance<GUISkin>();

            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                padding  = new RectOffset(0, 0, 0, 0),
                margin   = new RectOffset(0, 0, 0, 0),
                border   = new RectOffset(4, 4, 4, 4),
            };
            _windowStyle.normal.background  = MakeTex(1, 1, new Color(0, 0, 0, 0));

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = Mathf.RoundToInt(22f * Scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = Mathf.RoundToInt(14f * Scale),
                fontStyle = FontStyle.Bold,
                wordWrap  = true,
            };

            _smallLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = Mathf.RoundToInt(10f * Scale),
                fontStyle = FontStyle.Normal,
                wordWrap  = true,
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = Mathf.RoundToInt(11f * Scale),
                padding   = new RectOffset(4, 4, 2, 2),
            };

            _bigButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = Mathf.RoundToInt(12f * Scale),
                fontStyle = FontStyle.Bold,
                padding   = new RectOffset(6, 6, 4, 4),
            };
            _bigButtonStyle.normal.textColor  = Color.white;
            _bigButtonStyle.hover.textColor   = Color.yellow;
        }

        private Texture2D MakeTex(int w, int h, Color col)
        {
            var tex = new Texture2D(w, h);
            var pix = tex.GetPixels();
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }
    }
}
