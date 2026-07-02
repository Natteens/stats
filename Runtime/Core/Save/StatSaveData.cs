using System;
using System.Collections.Generic;

namespace Stats
{
    [Serializable]
    public sealed class StatBaseSaveData
    {
        public string statId;
        public float baseValue;
        public string statKey;
    }

    [Serializable]
    public sealed class StatModifierSaveData
    {
        public string statId;
        public int operation;
        public float value;
        public string sourceId;
        public bool timed;
        public float remainingSeconds;
        public int order;
        public string statKey;
    }

    [Serializable]
    public sealed class RuntimeResourceSaveData
    {
        public string key;
        public string maxStatId;
        public float current;
    }

    [Serializable]
    public sealed class StatSheetSaveData
    {
        public int version;
        public List<StatBaseSaveData> bases = new List<StatBaseSaveData>();
        public List<StatModifierSaveData> modifiers = new List<StatModifierSaveData>();
        public List<RuntimeResourceSaveData> resources = new List<RuntimeResourceSaveData>();
    }
}
