using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stats
{
    [CreateAssetMenu(menuName = "Stats/Stat Sheet Preset", fileName = "StatSheetPreset")]
    public sealed class StatSheetPreset : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public StatDefinition stat;
            public float baseValue;
            public bool overrideMin;
            public float min;
            public bool overrideMax;
            public float max;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        public IReadOnlyList<Entry> Entries => entries;

        public StatSheet BuildSheet(IStatCalculator calculator = null)
        {
            var sheet = new StatSheet(calculator);
            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entry.stat == null) continue;
                float min = entry.overrideMin ? entry.min : entry.stat.Min;
                float max = entry.overrideMax ? entry.max : entry.stat.Max;
                sheet.RegisterStat(entry.stat.ToStatId(), entry.baseValue, min, max);
            }
            return sheet;
        }
    }
}
