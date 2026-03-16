using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RafTris
{
    public enum GameState { WaitingToStart, Playing, Paused, GameOver }

    /// <summary>
    /// Pure game-logic layer — no Unity UI, no rendering.
    /// Driven by explicit Tick() calls from the manager.
    /// </summary>
    public class RafTrisGame
    {
        // ── Board dimensions ───────────────────────────────────────────────
        public const int BoardCols = 10;
        public const int BoardRows = 22;        // top 2 rows are the spawn buffer
        public const int VisibleRows = 20;

        // ── Board state ────────────────────────────────────────────────────
        // board[row][col] = 0 means empty; > 0 is a piece type (+1 offset so I-piece = 1)
        public int[,] Board { get; private set; } = new int[BoardRows, BoardCols];

        // ── Active & next pieces ───────────────────────────────────────────
        public ActivePiece         CurrentPiece    { get; private set; }
        public TetrominoPieceType  NextPieceType   { get; private set; }
        public TetrominoPieceType  HeldPieceType   { get; private set; }
        public bool                HoldUsedThisTurn { get; private set; }

        // ── Score / progress ───────────────────────────────────────────────
        public long  Score           { get; private set; }
        public long  SessionBest     { get; private set; }
        public int   LinesCleared    { get; private set; }
        public int   Level           { get; private set; }
        public int   TotalLinesThisLevel { get; private set; }

        // ── State ──────────────────────────────────────────────────────────
        public GameState State { get; private set; } = GameState.WaitingToStart;

        public float DropTimer      { get; private set; }  // seconds since last drop
        public float LockTimer      { get; private set; }  // seconds piece has been on floor
        public bool  PieceOnFloor   { get; private set; }

        public int[]  LastClearedRows   { get; private set; } = Array.Empty<int>();
        public bool   LastMoveWasTetris { get; private set; }

        // ── Events ─────────────────────────────────────────────────────────
        public event Action<int[], TetrominoPieceType> OnLinesCleared;  // rows cleared + piece that locked
        public event Action           OnPieceLocked;
        public event Action           OnGameOver;
        public event Action<int>      OnLevelUp;        // new level index

        // ── Seven-bag randomiser ───────────────────────────────────────────
        private readonly Queue<TetrominoPieceType> _bag = new Queue<TetrominoPieceType>();

        // Set by the manager so RefillBag can ask which biome we're on
        public Func<int> GetCurrentLevel;

        // ── Timing constants ───────────────────────────────────────────────
        private float DropInterval    => Mathf.Max(0.05f, 1.0f - (Level * 0.08f));
        private const float LockDelay  = 0.5f;
        private const int   LinesPerLevel = 10;

        // ── History for persistent save ────────────────────────────────────
        public bool HasHeld => HeldPieceType != (TetrominoPieceType)(-1);

        public RafTrisGame() { }

        // ─────────────────────────────────────────────────────────────────
        //  Public API
        // ─────────────────────────────────────────────────────────────────

        public void StartNewGame(int startLevel = 0, long prevBest = 0)
        {
            Board             = new int[BoardRows, BoardCols];
            Score             = 0;
            LinesCleared      = 0;
            Level             = startLevel;
            TotalLinesThisLevel = 0;
            DropTimer         = 0;
            LockTimer         = 0;
            PieceOnFloor      = false;
            LastMoveWasTetris = false;
            HeldPieceType     = (TetrominoPieceType)(-1);
            HoldUsedThisTurn  = false;
            SessionBest       = prevBest;

            _bag.Clear();
            RefillBag();

            NextPieceType = DrawFromBag();
            SpawnPiece();

            State = GameState.Playing;
        }

        public void Pause()
        {
            if (State == GameState.Playing) State = GameState.Paused;
        }

        public void Resume()
        {
            if (State == GameState.Paused) State = GameState.Playing;
        }

        /// <summary>Called every frame by the manager with elapsed seconds.</summary>
        public void Tick(float deltaTime)
        {
            if (State != GameState.Playing) return;

            DropTimer += deltaTime;

            if (PieceOnFloor)
            {
                LockTimer += deltaTime;
                if (LockTimer >= LockDelay)
                    LockPiece();
            }
            else if (DropTimer >= DropInterval)
            {
                DropTimer = 0;
                TryMoveDown();
            }
        }

        // ── Input ──────────────────────────────────────────────────────────

        public bool MoveLeft()  => TryMove(-1, 0);
        public bool MoveRight() => TryMove(+1, 0);

        public bool SoftDrop()
        {
            bool moved = TryMove(0, +1);
            if (moved) { Score += 1; DropTimer = 0; }
            return moved;
        }

        public void HardDrop()
        {
            int dropped = 0;
            while (TryMove(0, +1)) dropped++;
            Score += dropped * 2;
            LockPiece();
        }

        public bool Rotate(bool clockwise)
        {
            if (CurrentPiece == null) return false;

            int newRotation = (CurrentPiece.Rotation + (clockwise ? 1 : -1) + 4) % 4;
            var kicks       = TetrominoDefinitions.GetKicks(CurrentPiece.Type, CurrentPiece.Rotation);

            foreach (var kick in kicks)
            {
                int testCol = CurrentPiece.Column + kick.x;
                int testRow = CurrentPiece.Row    + kick.y;

                if (FitsAt(CurrentPiece.Type, newRotation, testCol, testRow))
                {
                    CurrentPiece.Rotation = newRotation;
                    CurrentPiece.Column   = testCol;
                    CurrentPiece.Row      = testRow;
                    ResetLock();
                    return true;
                }
            }
            return false;
        }

        public void Hold()
        {
            if (HoldUsedThisTurn || CurrentPiece == null) return;

            var swapType = HeldPieceType;
            HeldPieceType    = CurrentPiece.Type;
            HoldUsedThisTurn = true;

            if (swapType == (TetrominoPieceType)(-1))
            {
                // No piece was held — draw a new one
                SpawnPiece();
            }
            else
            {
                // Swap current with held
                CurrentPiece = CreatePiece(swapType);
            }
        }

        /// <summary>Ghost (shadow) piece position for the drop preview.</summary>
        public ActivePiece GetGhostPiece()
        {
            if (CurrentPiece == null) return null;
            var ghost = CurrentPiece.Clone();
            while (FitsAt(ghost.Type, ghost.Rotation, ghost.Column, ghost.Row + 1))
                ghost.Row++;
            return ghost;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Internal helpers
        // ─────────────────────────────────────────────────────────────────

        private bool TryMove(int dc, int dr)
        {
            if (CurrentPiece == null) return false;
            if (!FitsAt(CurrentPiece.Type, CurrentPiece.Rotation,
                        CurrentPiece.Column + dc, CurrentPiece.Row + dr))
                return false;

            CurrentPiece.Column += dc;
            CurrentPiece.Row    += dr;
            ResetLock();
            return true;
        }

        private bool TryMoveDown()
        {
            if (CurrentPiece == null) return false;
            if (FitsAt(CurrentPiece.Type, CurrentPiece.Rotation,
                       CurrentPiece.Column, CurrentPiece.Row + 1))
            {
                CurrentPiece.Row++;
                PieceOnFloor = false;
                return true;
            }

            // Piece has landed
            if (!PieceOnFloor)
            {
                PieceOnFloor = true;
                LockTimer    = 0;
            }
            return false;
        }

        private void LockPiece()
        {
            if (CurrentPiece == null) return;

            var lockingType = CurrentPiece.Type;

            // Write cells to board
            foreach (var cell in CurrentPiece.Cells())
            {
                if (cell.y < 0 || cell.y >= BoardRows || cell.x < 0 || cell.x >= BoardCols)
                    continue;
                Board[cell.y, cell.x] = (int)CurrentPiece.Type + 1;
            }

            OnPieceLocked?.Invoke();
            ClearFullRows(lockingType);
            SpawnPiece();
        }

        private void ClearFullRows(TetrominoPieceType lockingType)
        {
            var cleared = new List<int>();
            for (int r = 0; r < BoardRows; r++)
            {
                bool full = true;
                for (int c = 0; c < BoardCols; c++)
                    if (Board[r, c] == 0) { full = false; break; }
                if (full) cleared.Add(r);
            }

            if (cleared.Count == 0) { LastMoveWasTetris = false; return; }

            // Collapse rows
            foreach (int r in cleared)
            {
                for (int row = r; row > 0; row--)
                    for (int c = 0; c < BoardCols; c++)
                        Board[row, c] = Board[row - 1, c];
                for (int c = 0; c < BoardCols; c++)
                    Board[0, c] = 0;
            }

            LastClearedRows   = cleared.ToArray();
            LastMoveWasTetris = (cleared.Count == 4);

            AddScore(cleared.Count);
            LinesCleared        += cleared.Count;
            TotalLinesThisLevel += cleared.Count;

            if (TotalLinesThisLevel >= LinesPerLevel)
            {
                TotalLinesThisLevel -= LinesPerLevel;
                Level++;
                OnLevelUp?.Invoke(Level);
            }

            OnLinesCleared?.Invoke(cleared.ToArray(), lockingType);
        }

        private void AddScore(int lines)
        {
            int[] points = { 0, 100, 300, 500, 800 };
            int   pts    = points[Mathf.Clamp(lines, 0, 4)] * (Level + 1);
            if (LastMoveWasTetris) pts = (int)(pts * 1.5f);   // back-to-back Tetris bonus
            Score       += pts;
            SessionBest  = Math.Max(SessionBest, Score);
        }

        private void SpawnPiece()
        {
            var type     = NextPieceType;
            NextPieceType = DrawFromBag();

            CurrentPiece = CreatePiece(type);
            PieceOnFloor = false;
            LockTimer    = 0;
            DropTimer    = 0;
            HoldUsedThisTurn = false;

            // Check for spawn collision = game over
            if (!FitsAt(CurrentPiece.Type, CurrentPiece.Rotation,
                        CurrentPiece.Column, CurrentPiece.Row))
            {
                State = GameState.GameOver;
                OnGameOver?.Invoke();
            }
        }

        private ActivePiece CreatePiece(TetrominoPieceType type)
        {
            return new ActivePiece
            {
                Type     = type,
                Column   = BoardCols / 2 - 2,
                Row      = 0,
                Rotation = 0,
            };
        }

        private bool FitsAt(TetrominoPieceType type, int rotation, int col, int row)
        {
            var offsets = TetrominoDefinitions.Rotations[type][rotation % 4];
            foreach (var o in offsets)
            {
                int c = col + o.x;
                int r = row + o.y;
                if (c < 0 || c >= BoardCols) return false;
                if (r >= BoardRows)          return false;
                if (r >= 0 && Board[r, c] != 0) return false;
            }
            return true;
        }

        private void ResetLock()
        {
            if (PieceOnFloor) LockTimer = 0;
            // Check if still on floor after move
            if (!FitsAt(CurrentPiece.Type, CurrentPiece.Rotation,
                        CurrentPiece.Column, CurrentPiece.Row + 1))
                PieceOnFloor = true;
            else
                PieceOnFloor = false;
        }

        // ── Seven-bag ──────────────────────────────────────────────────────

        private void RefillBag()
        {
            // Only spawn pieces that have a trophy icon in the current biome
            int level  = GetCurrentLevel != null ? GetCurrentLevel() : Level;
            var theme  = BiomeThemes.ForLevel(level);
            var types  = theme.GetAvailablePieceTypes();

            // Fisher-Yates shuffle
            for (int i = types.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (types[i], types[j]) = (types[j], types[i]);
            }
            foreach (var t in types) _bag.Enqueue(t);
        }

        private TetrominoPieceType DrawFromBag()
        {
            if (_bag.Count == 0) RefillBag();
            return _bag.Dequeue();
        }
    }
}
