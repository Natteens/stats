using UnityEngine;

namespace Stats.Samples.BasicEntity
{
    [RequireComponent(typeof(StatSheetBehaviour))]
    public sealed class BasicEntityExample : MonoBehaviour
    {
        [SerializeField] private StatDefinition[] statsToLog;

        private void Start()
        {
            var sheet = GetComponent<StatSheetBehaviour>().Sheet;
            if (sheet == null)
            {
                Debug.LogWarning("No StatSheet was built. Assign a preset on the StatSheetBehaviour.");
                return;
            }
            foreach (var def in statsToLog)
            {
                if (def == null) continue;
                var id = def.ToStatId();
                if (sheet.TryGetValue(id, out float value))
                    Debug.Log($"{def.DisplayName} = {value}");
                else
                    Debug.Log($"{def.DisplayName} is not registered in this preset.");
            }
        }
    }
}
