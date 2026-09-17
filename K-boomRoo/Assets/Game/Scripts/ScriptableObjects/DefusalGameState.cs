using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefusalGame.Data
{
    [Serializable]
    public class DefusalSaveData
    {
        public string bombId = "BOMB_CHAMBER_01";
        public bool isDefused = false;
        public bool isExploded = false;
        public float timeRemaining = 300f;
        public List<string> collectedClueIds = new List<string>();
    }

    public static class DefusalGameStateManager
    {
        private const string SAVE_KEY = "DEFUSAL_GAME_SAVE";
        public static DefusalSaveData CurrentState = new DefusalSaveData();

        public static void ResetState(float startTime = 300f, string bombId = "BOMB_CHAMBER_01")
        {
            CurrentState = new DefusalSaveData
            {
                bombId = bombId,
                isDefused = false,
                isExploded = false,
                timeRemaining = startTime,
                collectedClueIds = new List<string>()
            };
        }

        public static void SaveToPlayerPrefs()
        {
            string json = JsonUtility.ToJson(CurrentState);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
            Debug.Log("[DefusalGame] Estado guardado en PlayerPrefs.");
        }

        public static bool LoadFromPlayerPrefs()
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                string json = PlayerPrefs.GetString(SAVE_KEY);
                CurrentState = JsonUtility.FromJson<DefusalSaveData>(json);
                Debug.Log("[DefusalGame] Estado cargado desde PlayerPrefs.");
                return true;
            }
            return false;
        }

        public static void MarkClueCollected(string clueId)
        {
            if (!CurrentState.collectedClueIds.Contains(clueId))
            {
                CurrentState.collectedClueIds.Add(clueId);
            }
        }
    }
}
