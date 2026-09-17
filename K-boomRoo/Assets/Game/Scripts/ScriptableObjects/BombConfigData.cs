using System.Collections.Generic;
using UnityEngine;

namespace DefusalGame.Data
{
    [CreateAssetMenu(fileName = "NewBombConfig", menuName = "Desactiva la Bomba/Bomb Configuration")]
    public class BombConfigData : ScriptableObject
    {
        [Header("Identificación")]
        public string bombId = "BOMB_CHAMBER_01";
        public string bombName = "Dispositivo C4 Táctico";

        [Header("Tiempos")]
        [Tooltip("Tiempo límite en segundos (ej. 300 = 5 minutos)")]
        public float timeLimitSeconds = 300f;
        [Tooltip("Penalización de segundos al introducir un código erróneo")]
        public float penaltySecondsOnFail = 30f;

        [Header("Secuencia de Desactivación")]
        [Tooltip("El código o secuencia exacta que desactiva la bomba")]
        public string targetSequence = "4826";
        [Tooltip("Cuántas evidencias componen o revelan la secuencia")]
        public int requiredCluesCount = 4;

        [Header("Evidencias Vinculadas")]
        public List<ClueData> linkedClues = new List<ClueData>();

        public bool ValidateCode(string enteredCode)
        {
            if (string.IsNullOrEmpty(enteredCode)) return false;
            return string.Equals(enteredCode.Trim(), targetSequence.Trim(), System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
