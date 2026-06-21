using System.Collections.Generic;
using UnityEngine;

namespace Stats.Samples.ItemModifiers
{
    [CreateAssetMenu(menuName = "Stats Samples/Sample Item", fileName = "SampleItem")]
    public sealed class SampleItem : ScriptableObject
    {
        public string itemName;
        public List<StatModifierData> modifiers = new List<StatModifierData>();
    }
}
