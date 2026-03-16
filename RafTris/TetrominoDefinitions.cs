using System.Collections.Generic;
using UnityEngine;

namespace RafTris
{
    public enum TetrominoPieceType
    {
        I = 0,
        O = 1,
        S = 2,
        Z = 3,
        L = 4,
        J = 5,
        T = 6,
    }

    /// <summary>
    /// Defines the four rotation states of every standard Tetromino.
    /// Each rotation is a list of (col, row) offsets from the pivot cell.
    /// Origin is top-left; +col = right, +row = down.
    /// </summary>
    public static class TetrominoDefinitions
    {
        // Each piece has 4 rotation states.
        // Cells are listed as (column, row) offsets from the spawn origin.
        public static readonly Dictionary<TetrominoPieceType, Vector2Int[][]> Rotations
            = new Dictionary<TetrominoPieceType, Vector2Int[][]>
        {
            {
                TetrominoPieceType.I, new[]
                {
                    new[]{ new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1), new Vector2Int(3,1) },
                    new[]{ new Vector2Int(2,0), new Vector2Int(2,1), new Vector2Int(2,2), new Vector2Int(2,3) },
                    new[]{ new Vector2Int(0,2), new Vector2Int(1,2), new Vector2Int(2,2), new Vector2Int(3,2) },
                    new[]{ new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(1,2), new Vector2Int(1,3) },
                }
            },
            {
                TetrominoPieceType.O, new[]
                {
                    new[]{ new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(1,1), new Vector2Int(2,1) },
                    new[]{ new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(1,1), new Vector2Int(2,1) },
                    new[]{ new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(1,1), new Vector2Int(2,1) },
                    new[]{ new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(1,1), new Vector2Int(2,1) },
                }
            },
            {
                TetrominoPieceType.S, new[]
                {
                    new[]{ new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(0,1), new Vector2Int(1,1) },
                    new[]{ new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(1,2) },
                    new[]{ new Vector2Int(1,1), new Vector2Int(2,1), new Vector2Int(0,2), new Vector2Int(1,2) },
                    new[]{ new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(2,1), new Vector2Int(2,2) },
                }
            },
            {
                TetrominoPieceType.Z, new[]
                {
                    new[]{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(2,1) },
                    new[]{ new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(0,2) },
                    new[]{ new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(1,2), new Vector2Int(2,2) },
                    new[]{ new Vector2Int(2,0), new Vector2Int(1,1), new Vector2Int(2,1), new Vector2Int(1,2) },
                }
            },
            {
                TetrominoPieceType.L, new[]
                {
                    new[]{ new Vector2Int(2,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1) },
                    new[]{ new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(1,2) },
                    new[]{ new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1), new Vector2Int(0,2) },
                    new[]{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(1,2) },
                }
            },
            {
                TetrominoPieceType.J, new[]
                {
                    new[]{ new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1) },
                    new[]{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(0,2) },
                    new[]{ new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1), new Vector2Int(2,2) },
                    new[]{ new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(0,2), new Vector2Int(1,2) },
                }
            },
            {
                TetrominoPieceType.T, new[]
                {
                    new[]{ new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1) },
                    new[]{ new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(0,2) },
                    new[]{ new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1), new Vector2Int(1,2) },
                    new[]{ new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(1,2) },
                }
            },
        };

        /// <summary>Wall-kick offsets for SRS (Super Rotation System) — JLSTZ pieces.</summary>
        public static readonly Vector2Int[][] WallKicksNormal = new[]
        {
            // 0->1
            new[]{ new Vector2Int(0,0), new Vector2Int(-1,0), new Vector2Int(-1,1), new Vector2Int(0,-2), new Vector2Int(-1,-2) },
            // 1->2
            new[]{ new Vector2Int(0,0), new Vector2Int(1,0),  new Vector2Int(1,-1), new Vector2Int(0,2),  new Vector2Int(1,2)  },
            // 2->3
            new[]{ new Vector2Int(0,0), new Vector2Int(1,0),  new Vector2Int(1,1),  new Vector2Int(0,-2), new Vector2Int(1,-2) },
            // 3->0
            new[]{ new Vector2Int(0,0), new Vector2Int(-1,0), new Vector2Int(-1,-1),new Vector2Int(0,2),  new Vector2Int(-1,2) },
        };

        /// <summary>Wall-kick offsets for SRS — I-piece only.</summary>
        public static readonly Vector2Int[][] WallKicksI = new[]
        {
            new[]{ new Vector2Int(0,0), new Vector2Int(-2,0), new Vector2Int(1,0),  new Vector2Int(-2,-1), new Vector2Int(1,2)  },
            new[]{ new Vector2Int(0,0), new Vector2Int(-1,0), new Vector2Int(2,0),  new Vector2Int(-1,2),  new Vector2Int(2,-1) },
            new[]{ new Vector2Int(0,0), new Vector2Int(2,0),  new Vector2Int(-1,0), new Vector2Int(2,1),   new Vector2Int(-1,-2)},
            new[]{ new Vector2Int(0,0), new Vector2Int(1,0),  new Vector2Int(-2,0), new Vector2Int(1,-2),  new Vector2Int(-2,1) },
        };

        public static Vector2Int[] GetKicks(TetrominoPieceType type, int fromRotation)
        {
            var table = (type == TetrominoPieceType.I) ? WallKicksI : WallKicksNormal;
            return table[fromRotation % 4];
        }
    }

    /// <summary>
    /// A live falling piece — its type, board position, and current rotation state.
    /// </summary>
    public class ActivePiece
    {
        public TetrominoPieceType Type;
        public int                Column;      // left edge of bounding box on board
        public int                Row;         // top edge of bounding box
        public int                Rotation;    // 0-3

        public Vector2Int[] Cells()
        {
            var offsets = TetrominoDefinitions.Rotations[Type][Rotation % 4];
            var result  = new Vector2Int[offsets.Length];
            for (int i = 0; i < offsets.Length; i++)
                result[i] = new Vector2Int(Column + offsets[i].x, Row + offsets[i].y);
            return result;
        }

        public ActivePiece Clone()
        {
            return new ActivePiece
            {
                Type     = Type,
                Column   = Column,
                Row      = Row,
                Rotation = Rotation,
            };
        }
    }
}
