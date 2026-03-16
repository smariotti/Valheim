using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Newtonsoft.Json;
using UnityEngine;

namespace RafTris
{
    [Serializable]
    public class RafTrisSaveData
    {
        public long   AllTimeBestScore  = 0;
        public int    AllTimeBestLevel  = 0;
        public long   TotalLinesCleared = 0;
        public int    TotalGamesPlayed  = 0;
        public long   CurrentScore      = 0;
        public int    CurrentLevel      = 0;
        public int    CurrentLines      = 0;
        public bool   SessionInProgress = false;
        public string SaveVersion       = "1.0";

        // Per-biome high scores (list index = biome theme index)
        public List<long> BiomeBestScores = new List<long>();
    }

    public static class RafTrisSaveSystem
    {
        private static readonly string SaveDirectory =
            Path.Combine(Paths.ConfigPath, "RafTris");

        private static readonly string SaveFilePath =
            Path.Combine(SaveDirectory, "raftris_save.json");

        private static RafTrisSaveData _cache;

        public static RafTrisSaveData Load()
        {
            try
            {
                if (!Directory.Exists(SaveDirectory))
                    Directory.CreateDirectory(SaveDirectory);

                if (!File.Exists(SaveFilePath))
                    return _cache = new RafTrisSaveData();

                string json = File.ReadAllText(SaveFilePath);
                _cache = JsonConvert.DeserializeObject<RafTrisSaveData>(json)
                         ?? new RafTrisSaveData();

                // Ensure biome list is sized correctly
                while (_cache.BiomeBestScores.Count < BiomeThemes.All.Count)
                    _cache.BiomeBestScores.Add(0);

                return _cache;
            }
            catch (Exception ex)
            {
                RafTrisPlugin.Log.LogWarning($"[RafTris] Could not load save: {ex.Message}");
                return _cache = new RafTrisSaveData();
            }
        }

        public static void Save(RafTrisSaveData data)
        {
            try
            {
                if (!Directory.Exists(SaveDirectory))
                    Directory.CreateDirectory(SaveDirectory);

                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(SaveFilePath, json);
                _cache = data;
            }
            catch (Exception ex)
            {
                RafTrisPlugin.Log.LogWarning($"[RafTris] Could not save data: {ex.Message}");
            }
        }

        public static void ApplyGameResultToSave(RafTrisGame game, RafTrisSaveData data)
        {
            data.TotalGamesPlayed++;
            data.TotalLinesCleared += game.LinesCleared;

            if (game.Score > data.AllTimeBestScore)
            {
                data.AllTimeBestScore = game.Score;
                data.AllTimeBestLevel = game.Level;
            }

            int biomeIdx = game.Level % BiomeThemes.All.Count;
            while (data.BiomeBestScores.Count <= biomeIdx)
                data.BiomeBestScores.Add(0);

            if (game.Score > data.BiomeBestScores[biomeIdx])
                data.BiomeBestScores[biomeIdx] = game.Score;
        }

        public static void SaveInProgressSession(RafTrisGame game, RafTrisSaveData data)
        {
            data.SessionInProgress = true;
            data.CurrentScore      = game.Score;
            data.CurrentLevel      = game.Level;
            data.CurrentLines      = game.LinesCleared;
            Save(data);
        }

        public static void ClearSession(RafTrisSaveData data)
        {
            data.SessionInProgress = false;
            data.CurrentScore      = 0;
            data.CurrentLevel      = 0;
            data.CurrentLines      = 0;
            Save(data);
        }
    }
}
