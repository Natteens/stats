using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stats
{
    [AddComponentMenu("Stats/Stats Component")]
    public sealed class StatSheetBehaviour : MonoBehaviour
    {
        [SerializeField] private StatSheetPreset preset;

        readonly Dictionary<string, RuntimeResource> resources = new Dictionary<string, RuntimeResource>();
        readonly Dictionary<string, StatId> keyToId = new Dictionary<string, StatId>(StringComparer.Ordinal);
        readonly Dictionary<StatId, string> idToKey = new Dictionary<StatId, string>();

        public StatSheet Sheet { get; private set; }
        public TimedModifierService TimedModifiers { get; private set; }
        public IReadOnlyDictionary<string, RuntimeResource> Resources => resources;

        public void RegisterResource(string key, RuntimeResource resource)
        {
            if (string.IsNullOrEmpty(key) || resource == null) return;
            resources[key] = resource;
        }

        public bool TryGetResource(string key, out RuntimeResource resource) => resources.TryGetValue(key, out resource);

        public void RebuildKeyCache()
        {
            keyToId.Clear();
            idToKey.Clear();
            if (preset == null) return;
            var entries = preset.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                var stat = entries[i].stat;
                if (stat == null || string.IsNullOrEmpty(stat.Key)) continue;
                var id = stat.ToStatId();
                if (keyToId.ContainsKey(stat.Key))
                {
                    Debug.LogWarning($"[Stats] Duplicate stat key '{stat.Key}' in preset '{preset.name}'; keeping the first entry.");
                    continue;
                }
                keyToId.Add(stat.Key, id);
                if (!idToKey.ContainsKey(id)) idToKey.Add(id, stat.Key);
            }
        }

        public bool TryGetStatId(string key, out StatId statId)
        {
            if (string.IsNullOrEmpty(key)) { statId = StatId.Empty; return false; }
            return keyToId.TryGetValue(key, out statId);
        }

        public bool TryGetKey(StatId statId, out string key) => idToKey.TryGetValue(statId, out key);

        public bool TryGetValue(string key, out float value)
        {
            if (Sheet != null && TryGetStatId(key, out var id)) return Sheet.TryGetValue(id, out value);
            value = 0f;
            return false;
        }

        public float GetValue(string key, float fallback = 0f) => TryGetValue(key, out float value) ? value : fallback;

        public ModifierHandle AddFlat(string key, float value, object source) =>
            Sheet.AddFlat(ResolveKey(key), value, source);

        public ModifierHandle AddPercentAdd(string key, float fraction, object source) =>
            Sheet.AddPercentAdd(ResolveKey(key), fraction, source);

        public ModifierHandle AddPercentMult(string key, float fraction, object source) =>
            Sheet.AddPercentMult(ResolveKey(key), fraction, source);

        public ModifierHandle AddOverride(string key, float value, object source) =>
            Sheet.AddOverride(ResolveKey(key), value, source);

        StatId ResolveKey(string key)
        {
            if (TryGetStatId(key, out var id)) return id;
            throw new KeyNotFoundException($"No stat registered for key '{key}'.");
        }

        private void Awake()
        {
            Sheet = preset != null ? preset.BuildSheet() : new StatSheet();
            TimedModifiers = new TimedModifierService(new CountdownTimerScheduler());
            RebuildKeyCache();
        }

        private void OnDestroy()
        {
            // Sheet is being torn down; only kill dangling timer callbacks, do not revert modifiers.
            TimedModifiers?.CancelSchedules();
        }
    }
}
