using System;
using UnityEngine;

namespace RafTris
{
    /// <summary>
    /// MonoBehaviour that lives for the duration of the game session.
    /// Coordinates input, timing, save/load, and tells the UI what to draw.
    /// </summary>
    public class RafTrisManager : MonoBehaviour
    {
        public static RafTrisManager Instance { get; private set; }

        public RafTrisGame       Game      { get; private set; }
        public RafTrisSaveData  SaveData  { get; private set; }
        public bool             IsVisible { get; private set; }

        // Input repeat timing (for held keys)
        private float _moveLeftTimer;
        private float _moveRightTimer;
        private float _softDropTimer;
        private const float DASDelay  = 0.17f;
        private const float DASRepeat = 0.05f;

        // Autosave interval in seconds
        private const float AutoSaveInterval = 30f;
        private float _autoSaveTimer;

        // UI component
        private RafTrisUI _ui;

        // ── Flash & animation state (shared with UI) ──────────────────────
        public int[]  FlashRows    { get; private set; } = Array.Empty<int>();
        public float  FlashTimer   { get; private set; }
        public const float FlashDuration = 0.35f;
        public bool   IsFlashing   => FlashTimer > 0;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            SaveData = RafTrisSaveSystem.Load();

            _ui = gameObject.AddComponent<RafTrisUI>();
            _ui.Manager = this;

            RafTrisPlugin.Log.LogInfo("[RafTrisManager] Initialised.");
        }

        private void Update()
        {
            // ── Toggle key ────────────────────────────────────────────────
            if (RafTrisPlugin.ToggleKey.Value.IsDown())
                ToggleWindow();

            if (!IsVisible) return;

            // ── Flash countdown ───────────────────────────────────────────
            if (FlashTimer > 0)
            {
                FlashTimer -= Time.unscaledDeltaTime;
                if (FlashTimer < 0) FlashTimer = 0;
            }

            // ── Game tick ─────────────────────────────────────────────────
            if (Game != null && Game.State == GameState.Playing && FlashTimer <= 0)
                Game.Tick(Time.unscaledDeltaTime);

            // ── Keyboard input ────────────────────────────────────────────
            HandleKeyboardInput();

            // ── Autosave ──────────────────────────────────────────────────
            _autoSaveTimer += Time.unscaledDeltaTime;
            if (_autoSaveTimer >= AutoSaveInterval)
            {
                _autoSaveTimer = 0;
                if (Game != null && Game.State == GameState.Playing)
                    RafTrisSaveSystem.SaveInProgressSession(Game, SaveData);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  Window control
        // ─────────────────────────────────────────────────────────────────

        public void ToggleWindow()
        {
            IsVisible = !IsVisible;
            if (IsVisible)
            {
                // Reload save in case it changed
                SaveData = RafTrisSaveSystem.Load();

                // Clear icon cache so any sprites that failed to load before the world
                // was fully ready will be re-resolved against the now-live ObjectDB.
                TrophyIconLoader.ClearCache();

                if (RafTrisPlugin.PauseGameWhilePlaying.Value)
                    SetGamePause(true);
            }
            else
            {
                if (Game != null && Game.State == GameState.Playing)
                    RafTrisSaveSystem.SaveInProgressSession(Game, SaveData);

                if (RafTrisPlugin.PauseGameWhilePlaying.Value)
                    SetGamePause(false);
            }
        }

        private static void SetGamePause(bool paused)
        {
            // Valheim's time scale approach — safe to call even outside world
            if (paused)
                Time.timeScale = 0f;
            else
                Time.timeScale = 1f;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Session control (called by UI buttons)
        // ─────────────────────────────────────────────────────────────────

        public void StartNewGame()
        {
            TrophyIconLoader.ClearCache();
            Game = new RafTrisGame();
            Game.GetCurrentLevel = () => Game.Level;
            Game.OnLinesCleared += OnLinesCleared;
            Game.OnGameOver     += OnGameOver;
            Game.OnLevelUp      += OnLevelUp;

            int startLevel = 0;
            if (SaveData.SessionInProgress)
                startLevel = SaveData.CurrentLevel;

            Game.StartNewGame(startLevel, SaveData.AllTimeBestScore);
            SaveData.SessionInProgress = true;
            RafTrisSaveSystem.Save(SaveData);
        }

        public void PauseGame()
        {
            Game?.Pause();
        }

        public void ResumeGame()
        {
            Game?.Resume();
        }

        public void EndSession()
        {
            if (Game != null)
            {
                RafTrisSaveSystem.ApplyGameResultToSave(Game, SaveData);
                RafTrisSaveSystem.ClearSession(SaveData);
                Game = null;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  Game events
        // ─────────────────────────────────────────────────────────────────

        private void OnLinesCleared(int[] rows, TetrominoPieceType lockingPiece)
        {
            FlashRows  = rows;
            FlashTimer = FlashDuration;

            // Play the death sound of the creature whose trophy was on the locking piece
            if (Game != null)
            {
                var theme       = BiomeThemes.ForLevel(Game.Level);
                var creatureName = theme.CreatureForPiece(lockingPiece);
                PlayCreatureDeathSfx(creatureName);
            }
        }

        /// <summary>
        /// Finds the creature prefab in ZNetScene, locates its death SFX via the
        /// Character/BaseAI component's m_deathEffects, and plays it at a neutral
        /// world position (near the local player if available, otherwise at origin).
        /// </summary>
        private static void PlayCreatureDeathSfx(string creaturePrefabName)
        {
            if (string.IsNullOrEmpty(creaturePrefabName)) return;
            if (ZNetScene.instance == null) return;

            var prefab = ZNetScene.instance.GetPrefab(creaturePrefabName);
            if (prefab == null)
            {
                RafTrisPlugin.Log.LogWarning($"[RafTris] Death SFX: prefab not found — {creaturePrefabName}");
                return;
            }

            // Grab the EffectList from the Character component
            EffectList deathEffects = null;
            var character = prefab.GetComponent<Character>();
            if (character != null)
                deathEffects = character.m_deathEffects;

            if (deathEffects == null || deathEffects.m_effectPrefabs == null
                || deathEffects.m_effectPrefabs.Length == 0)
                return;

            // Play at the local player position if available, otherwise at origin
            Vector3 pos = Player.m_localPlayer != null
                ? Player.m_localPlayer.transform.position
                : Vector3.zero;

            deathEffects.Create(pos, Quaternion.identity);
        }

        private void OnGameOver()
        {
            RafTrisSaveSystem.ApplyGameResultToSave(Game, SaveData);
            RafTrisSaveSystem.ClearSession(SaveData);
            RafTrisSaveSystem.Save(SaveData);
        }

        private void OnLevelUp(int newLevel)
        {
            int prevBiome = (newLevel - 1) % BiomeThemes.All.Count;
            int nextBiome =  newLevel      % BiomeThemes.All.Count;

            if (prevBiome != nextBiome)
            {
                // Trophy icon cache is biome-specific — clear when biome changes
                TrophyIconLoader.ClearCache();

                // Show the same biome-discovery banner Valheim uses natively
                var newTheme = BiomeThemes.ForLevel(newLevel);
                ShowBiomeStinger(newTheme);
            }

            RafTrisPlugin.Log.LogInfo($"[RafTris] Level up → {newLevel} ({BiomeThemes.ForLevel(newLevel).Name})");
        }

        /// <summary>
        /// Triggers Valheim's native biome-found message HUD with the RafTris biome name.
        /// MessageHud.ShowBiomeFoundMsg handles the fade-in/hold/fade-out animation itself.
        /// </summary>
        private static void ShowBiomeStinger(BiomeTheme theme)
        {
            if (MessageHud.instance == null)
            {
                RafTrisPlugin.Log.LogWarning("[RafTris] MessageHud not available for biome stinger.");
                return;
            }

            // ShowBiomeFoundMsg accepts the biome display name — Valheim handles all the
            // animation, sound, and timing using the same path as natural biome discovery.
            MessageHud.instance.ShowBiomeFoundMsg(theme.Name, true);
        }

        // ─────────────────────────────────────────────────────────────────
        //  Keyboard input with DAS/ARR
        // ─────────────────────────────────────────────────────────────────

        private void HandleKeyboardInput()
        {
            if (Game == null || Game.State != GameState.Playing) return;

            // Rotate
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.X))
                Game.Rotate(true);
            if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.LeftControl))
                Game.Rotate(false);

            // Hold
            if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftShift))
                Game.Hold();

            // Hard drop
            if (Input.GetKeyDown(KeyCode.Space))
                Game.HardDrop();

            // Pause
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.F1))
            {
                if (Game.State == GameState.Playing) PauseGame();
                else if (Game.State == GameState.Paused) ResumeGame();
            }

            // ── DAS: Move Left ────────────────────────────────────────────
            HandleDAS(KeyCode.LeftArrow, ref _moveLeftTimer, () => Game.MoveLeft());

            // ── DAS: Move Right ───────────────────────────────────────────
            HandleDAS(KeyCode.RightArrow, ref _moveRightTimer, () => Game.MoveRight());

            // ── DAS: Soft Drop ────────────────────────────────────────────
            HandleDAS(KeyCode.DownArrow, ref _softDropTimer, () => Game.SoftDrop());
        }

        private void HandleDAS(KeyCode key, ref float timer, Func<bool> action)
        {
            if (Input.GetKeyDown(key))
            {
                action();
                timer = -DASDelay;   // negative = in DAS delay phase
            }
            else if (Input.GetKey(key))
            {
                timer += Time.unscaledDeltaTime;
                while (timer >= DASRepeat)
                {
                    action();
                    timer -= DASRepeat;
                }
            }
            else
            {
                timer = 0;
            }
        }

        private void OnDestroy()
        {
            if (Game != null && Game.State == GameState.Playing)
                RafTrisSaveSystem.SaveInProgressSession(Game, SaveData);
        }
    }
}
