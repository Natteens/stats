using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Stats.Editor
{
    public static class StatIdValidator
    {
        public static List<StatDefinition> FindMissingIds(IReadOnlyList<StatDefinition> defs)
        {
            var result = new List<StatDefinition>();
            for (int i = 0; i < defs.Count; i++)
                if (defs[i] != null && defs[i].Id.IsEmpty) result.Add(defs[i]);
            return result;
        }

        public static List<StatDefinition> FindDuplicateIds(IReadOnlyList<StatDefinition> defs)
        {
            var counts = new Dictionary<StatId, int>();
            for (int i = 0; i < defs.Count; i++)
            {
                var def = defs[i];
                if (def == null || def.Id.IsEmpty) continue;
                counts.TryGetValue(def.Id, out int c);
                counts[def.Id] = c + 1;
            }
            var result = new List<StatDefinition>();
            for (int i = 0; i < defs.Count; i++)
            {
                var def = defs[i];
                if (def == null || def.Id.IsEmpty) continue;
                if (counts[def.Id] > 1) result.Add(def);
            }
            return result;
        }

        public static List<StatDefinition> LoadAll()
        {
            var result = new List<StatDefinition>();
            foreach (var guid in AssetDatabase.FindAssets("t:StatDefinition"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<StatDefinition>(path);
                if (def != null) result.Add(def);
            }
            return result;
        }

        public static bool IsDuplicate(StatDefinition def)
        {
            if (def == null || def.Id.IsEmpty) return false;
            int count = 0;
            foreach (var other in LoadAll())
                if (other != null && other.Id == def.Id) count++;
            return count > 1;
        }

        public static void AssignNewId(StatDefinition def)
        {
            if (def == null) return;
            var so = new SerializedObject(def);
            so.FindProperty("id").stringValue = Guid.NewGuid().ToString("N");
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(def);
        }

        [MenuItem("Tools/Stats/Validate Stat IDs")]
        public static void ValidateMenu()
        {
            var all = LoadAll();
            var missing = FindMissingIds(all);
            var duplicates = FindDuplicateIds(all);

            if (missing.Count == 0 && duplicates.Count == 0)
            {
                EditorUtility.DisplayDialog("Stats", $"All {all.Count} stat IDs are valid.", "OK");
                return;
            }

            bool fix = EditorUtility.DisplayDialog(
                "Stats",
                $"Missing IDs: {missing.Count}\nDuplicate IDs: {duplicates.Count}\n\nAssign new IDs to fix them?",
                "Fix", "Cancel");
            if (!fix) return;

            foreach (var def in missing) AssignNewId(def);

            var keep = new HashSet<StatId>();
            foreach (var def in duplicates)
            {
                if (keep.Add(def.Id)) continue;
                AssignNewId(def);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Stats] Repaired {missing.Count} missing and reassigned {Mathf.Max(0, duplicates.Count - keep.Count)} duplicate stat IDs.");
        }
    }
}
