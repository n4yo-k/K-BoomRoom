using UnityEngine;

namespace DefusalGame.Data
{
    [CreateAssetMenu(fileName = "NewClueData", menuName = "Desactiva la Bomba/Clue Data")]
    public class ClueData : ScriptableObject
    {
        [Header("Identificación")]
        public string clueId = "CLUE_01";
        public string clueName = "Evidencia";
        [TextArea(3, 6)]
        public string description = "Nota con un número anotado apresuradamente.";

        [Header("Dato para la Bomba")]
        [Tooltip("El dígito, símbolo o valor que aporta a la secuencia (ej: '4', 'BLUE', '7')")]
        public string revealedValue = "4";
        [Tooltip("Posición en la secuencia final (0 = 1er dígito, 1 = 2do dígito, etc.)")]
        public int sequenceIndex = 0;

        [Header("Ubicación")]
        public string hintLocation = "Escritorio del Estudio";

        [Header("Estado en Tiempo de Ejecución")]
        [System.NonSerialized]
        public bool isCollected = false;

        public void ResetRuntimeState()
        {
            isCollected = false;
        }
    }
}
