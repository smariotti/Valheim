using System.Collections.Generic;
using UnityEngine;

namespace RafTris
{
    /// <summary>
    /// Every Valheim biome level theme, including trophy prefab names
    /// and a colour palette that drives the retro scanline aesthetic.
    /// </summary>
    public static class BiomeThemes
    {
        public static readonly List<BiomeTheme> All = new List<BiomeTheme>
        {
            new BiomeTheme
            {
                Name            = "Meadows",
                Description     = "A gentle beginning beneath golden skies.",
                EnemyTrophies   = new[] { "TrophyNeck", "TrophyDeer", "TrophyBoar", "TrophySkeleton", "TrophyGreydwarf", "TrophyGreydwarfShaman" },
                EnemyCreatures  = new[] { "Neck",       "Deer",       "Boar",       "Skeleton",        "Greydwarf",       "Greydwarf_Shaman"       },
                BossTrophy      = "TrophyEikthyr",
                BossCreature    = "Eikthyr",
                PrimaryColor    = new Color(0.40f, 0.76f, 0.35f),
                SecondaryColor  = new Color(0.85f, 0.92f, 0.55f),
                AccentColor     = new Color(1.00f, 0.85f, 0.20f),
                BackgroundColor = new Color(0.06f, 0.10f, 0.06f),
                GridLineColor   = new Color(0.20f, 0.35f, 0.18f),
            },

            new BiomeTheme
            {
                Name            = "Black Forest",
                Description     = "Ancient pines hide elder secrets.",
                EnemyTrophies   = new[] { "TrophyGreydwarf", "TrophyGreydwarfBrute", "TrophyGreydwarfShaman", "TrophyTroll", "TrophySkeleton", "TrophySkeletonPoison" },
                EnemyCreatures  = new[] { "Greydwarf",       "Greydwarf_Elite",      "Greydwarf_Shaman",      "Troll",       "Skeleton",        "Skeleton_Poison"      },
                BossTrophy      = "TrophyTheElder",
                BossCreature    = "TheElder",
                PrimaryColor    = new Color(0.18f, 0.42f, 0.18f),
                SecondaryColor  = new Color(0.55f, 0.70f, 0.35f),
                AccentColor     = new Color(0.80f, 0.55f, 0.10f),
                BackgroundColor = new Color(0.04f, 0.07f, 0.04f),
                GridLineColor   = new Color(0.15f, 0.25f, 0.12f),
            },

            new BiomeTheme
            {
                Name            = "Swamp",
                Description     = "Poisoned mires where death festers.",
                EnemyTrophies   = new[] { "TrophyDraugr", "TrophyDraugrElite", "TrophyLeech", "TrophyBlob", "TrophyWraith", "TrophySurtling" },
                EnemyCreatures  = new[] { "Draugr",       "Draugr_Elite",      "Leech",       "Blob",       "Wraith",       "Surtling"       },
                BossTrophy      = "TrophyBonemass",
                BossCreature    = "Bonemass",
                PrimaryColor    = new Color(0.25f, 0.45f, 0.20f),
                SecondaryColor  = new Color(0.50f, 0.65f, 0.22f),
                AccentColor     = new Color(0.60f, 0.80f, 0.10f),
                BackgroundColor = new Color(0.03f, 0.07f, 0.03f),
                GridLineColor   = new Color(0.12f, 0.22f, 0.10f),
            },

            new BiomeTheme
            {
                Name            = "Mountains",
                Description     = "Frozen peaks where drakes soar.",
                EnemyTrophies   = new[] { "TrophyWolf", "TrophyDrake", "TrophyFenring", "TrophyStonGolem", "TrophyCultist", "TrophyUlv" },
                EnemyCreatures  = new[] { "Wolf",       "Hatchling",   "Fenring",       "StoneGolem",      "Cultist",       "Ulv"       },
                BossTrophy      = "TrophyDragonQueen",
                BossCreature    = "Dragon",
                PrimaryColor    = new Color(0.55f, 0.78f, 0.95f),
                SecondaryColor  = new Color(0.85f, 0.92f, 1.00f),
                AccentColor     = new Color(0.40f, 0.60f, 1.00f),
                BackgroundColor = new Color(0.04f, 0.05f, 0.10f),
                GridLineColor   = new Color(0.18f, 0.22f, 0.35f),
            },

            new BiomeTheme
            {
                Name            = "Plains",
                Description     = "Sun-scorched fields of the Fuling horde.",
                EnemyTrophies   = new[] { "TrophyFuling", "TrophyFulingBerserker", "TrophyFulingShaman", "TrophyGrowth", "TrophyDeathsquito", "TrophyLox" },
                EnemyCreatures  = new[] { "Goblin",       "GoblinBrute",           "GoblinShaman",       "BlobElite",    "Deathsquito",        "Lox"       },
                BossTrophy      = "TrophyGoblinKing",
                BossCreature    = "GoblinKing",
                PrimaryColor    = new Color(0.88f, 0.75f, 0.25f),
                SecondaryColor  = new Color(0.95f, 0.88f, 0.50f),
                AccentColor     = new Color(1.00f, 0.45f, 0.10f),
                BackgroundColor = new Color(0.09f, 0.07f, 0.02f),
                GridLineColor   = new Color(0.28f, 0.22f, 0.08f),
            },

            new BiomeTheme
            {
                Name            = "Mistlands",
                Description     = "Eldritch webs shroud the Seeker swarms.",
                EnemyTrophies   = new[] { "TrophySeeker", "TrophySeekerBrute", "TrophyGjall",  "TrophyTick", "TrophyHati", "TrophySkoll" },
                EnemyCreatures  = new[] { "Seeker",       "SeekerBrute",       "Gjall",         "Tick",       "Hati",       "Skoll"       },
                BossTrophy      = "TrophySeekerQueen",
                BossCreature    = "SeekerQueen",
                PrimaryColor    = new Color(0.60f, 0.35f, 0.85f),
                SecondaryColor  = new Color(0.80f, 0.60f, 1.00f),
                AccentColor     = new Color(0.90f, 0.20f, 0.90f),
                BackgroundColor = new Color(0.05f, 0.03f, 0.09f),
                GridLineColor   = new Color(0.20f, 0.12f, 0.30f),
            },

            new BiomeTheme
            {
                Name            = "Ashlands",
                Description     = "A realm of fire and eternal ash storms.",
                EnemyTrophies   = new[] { "TrophyCharredWarrior", "TrophyCharredArcher", "TrophyCharredTwitcher", "TrophyFader", "TrophyVolture", "TrophyAsksvin" },
                EnemyCreatures  = new[] { "CharredWarrior",       "CharredArcher",       "CharredTwitcher",       "Fader",       "Vulture",        "Asksvin"       },
                BossTrophy      = "TrophyFader",
                BossCreature    = "Fader",
                PrimaryColor    = new Color(0.95f, 0.35f, 0.10f),
                SecondaryColor  = new Color(1.00f, 0.60f, 0.20f),
                AccentColor     = new Color(1.00f, 0.90f, 0.10f),
                BackgroundColor = new Color(0.10f, 0.03f, 0.01f),
                GridLineColor   = new Color(0.30f, 0.10f, 0.04f),
            },

            new BiomeTheme
            {
                Name            = "Deep North",
                Description     = "The frozen void beyond the world's edge.",
                // Reusing Mountain icons until Deep North content ships
                EnemyTrophies   = new[] { "TrophyWolf", "TrophyDrake", "TrophyFenring", "TrophyStonGolem", "TrophyCultist", "TrophyUlv" },
                EnemyCreatures  = new[] { "Wolf",       "Hatchling",   "Fenring",       "StoneGolem",      "Cultist",       "Ulv"       },
                BossTrophy      = "TrophyDeepNorthBossPlaceholder",
                BossCreature    = "",
                IsPlaceholder   = true,
                PrimaryColor    = new Color(0.75f, 0.92f, 1.00f),
                SecondaryColor  = new Color(0.90f, 0.96f, 1.00f),
                AccentColor     = new Color(0.40f, 0.80f, 1.00f),
                BackgroundColor = new Color(0.04f, 0.06f, 0.10f),
                GridLineColor   = new Color(0.18f, 0.26f, 0.40f),
            },
        };

        /// <summary>Returns the theme for the given 0-based level index (wraps around).</summary>
        public static BiomeTheme ForLevel(int level)
        {
            return All[level % All.Count];
        }
    }

    public class BiomeTheme
    {
        public string   Name;
        public string   Description;
        public string[] EnemyTrophies;   // 6 standard trophies for O/S/Z/L/J/T pieces
        public string   BossTrophy;      // I-piece (straight 4)
        public bool     IsPlaceholder;

        // Creature prefab names whose death SFX plays when that piece clears a row.
        // Index matches EnemyTrophies order; BossCreature maps to the I-piece.
        public string[] EnemyCreatures;
        public string   BossCreature;

        public Color PrimaryColor;
        public Color SecondaryColor;
        public Color AccentColor;
        public Color BackgroundColor;
        public Color GridLineColor;

        /// <summary>
        /// Map a Tetromino piece type to the correct trophy prefab name for this biome.
        /// The I-piece (index 0) always maps to the boss trophy.
        /// </summary>
        public string TrophyForPiece(TetrominoPieceType type)
        {
            if (type == TetrominoPieceType.I)
                return BossTrophy;

            int idx = (int)type - 1; // I=0, skip it; O=1, S=2, Z=3, L=4, J=5, T=6
            if (EnemyTrophies == null || EnemyTrophies.Length == 0)
                return string.Empty;
            return EnemyTrophies[Mathf.Clamp(idx, 0, EnemyTrophies.Length - 1)];
        }

        /// <summary>
        /// Returns the creature prefab name whose death sound should play when
        /// a row is cleared by this piece type.
        /// </summary>
        public string CreatureForPiece(TetrominoPieceType type)
        {
            if (type == TetrominoPieceType.I)
                return BossCreature ?? string.Empty;

            int idx = (int)type - 1;
            if (EnemyCreatures == null || EnemyCreatures.Length == 0)
                return string.Empty;
            return EnemyCreatures[Mathf.Clamp(idx, 0, EnemyCreatures.Length - 1)];
        }

        /// <summary>
        /// Returns only the piece types that have a non-empty trophy assigned
        /// in this biome. Guarantees the bag never hands out an iconless piece.
        /// Always includes I (boss) unless BossTrophy is empty.
        /// </summary>
        public List<TetrominoPieceType> GetAvailablePieceTypes()
        {
            var result = new List<TetrominoPieceType>();

            // I-piece = boss trophy
            if (!string.IsNullOrEmpty(BossTrophy))
                result.Add(TetrominoPieceType.I);

            // O/S/Z/L/J/T map to EnemyTrophies[0..5]
            TetrominoPieceType[] ordered = {
                TetrominoPieceType.O, TetrominoPieceType.S, TetrominoPieceType.Z,
                TetrominoPieceType.L, TetrominoPieceType.J, TetrominoPieceType.T,
            };
            for (int i = 0; i < ordered.Length; i++)
            {
                if (EnemyTrophies != null && i < EnemyTrophies.Length
                    && !string.IsNullOrEmpty(EnemyTrophies[i]))
                    result.Add(ordered[i]);
            }

            // Safety: if somehow nothing is available fall back to all 7
            if (result.Count == 0)
            {
                result.AddRange(new[] {
                    TetrominoPieceType.I, TetrominoPieceType.O, TetrominoPieceType.S,
                    TetrominoPieceType.Z, TetrominoPieceType.L, TetrominoPieceType.J,
                    TetrominoPieceType.T,
                });
            }

            return result;
        }
    }
}
