using UnityEngine;

namespace Stats.Samples.ItemModifiers
{
    [RequireComponent(typeof(StatSheetBehaviour))]
    public sealed class ItemModifierExample : MonoBehaviour
    {
        [SerializeField] private SampleItem item;
        [SerializeField] private StatDefinition trackedStat;

        private StatSheet sheet;
        private readonly object ringA = new object();
        private readonly object ringB = new object();

        private void Start()
        {
            sheet = GetComponent<StatSheetBehaviour>().Sheet;
            if (sheet == null || item == null || trackedStat == null)
            {
                Debug.LogWarning("Assign a preset, an item, and a tracked stat.");
                return;
            }

            Log("base");
            sheet.ApplyGroup(item.modifiers, ringA);
            Log("equipped ring A");
            sheet.ApplyGroup(item.modifiers, ringB);
            Log("equipped ring B (identical, separate instance)");
            sheet.RemoveModifiersFromSource(ringA);
            Log("removed ring A only");
            sheet.RemoveModifiersFromSource(ringB);
            Log("removed ring B");
        }

        private void Log(string step)
        {
            Debug.Log($"[{step}] {trackedStat.DisplayName} = {sheet.GetValue(trackedStat.ToStatId())}");
        }
    }
}
