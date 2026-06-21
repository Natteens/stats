using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Stats.Editor
{
    [CustomEditor(typeof(StatSheetPreset))]
    public sealed class StatSheetPresetEditor : UnityEditor.Editor
    {
        static readonly FieldInfo EntriesField =
            typeof(StatSheetPreset).GetField("entries", BindingFlags.NonPublic | BindingFlags.Instance);

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            if (!StatsEditorResources.Load("StatSheetPresetEditor", root))
            {
                InspectorElement.FillDefaultInspector(root, serializedObject, this);
                return root;
            }

            var entriesProp = serializedObject.FindProperty("entries");
            var title = root.Q<Label>("ssp-title");
            var count = root.Q<Label>("ssp-count");
            var list = root.Q<VisualElement>("ssp-list");
            var empty = root.Q<Label>("ssp-empty");
            title.text = target.name;
            int lastSize = -1;

            void BuildList()
            {
                list.Clear();
                int size = entriesProp.arraySize;
                for (int i = 0; i < size; i++)
                {
                    var entry = entriesProp.GetArrayElementAtIndex(i);
                    var row = new VisualElement { name = $"ssp-row-{i}" };
                    row.AddToClassList("ssp-entry");

                    var stat = new PropertyField { name = $"ssp-stat-{i}", label = "Stat" };
                    stat.bindingPath = entry.FindPropertyRelative("stat").propertyPath;
                    stat.AddToClassList("ssp-stat-field");
                    row.Add(stat);

                    var baseField = new FloatField("Base Value");
                    baseField.bindingPath = entry.FindPropertyRelative("baseValue").propertyPath;
                    baseField.AddToClassList("ssp-base-field");
                    row.Add(baseField);

                    row.Add(ClampRow(entry, i, true));
                    row.Add(ClampRow(entry, i, false));

                    var warning = new Label { name = $"ssp-row-warning-{i}" };
                    warning.AddToClassList("ssp-row-warning");
                    row.Add(warning);

                    list.Add(row);
                }
                list.Bind(serializedObject);
            }

            VisualElement ClampRow(SerializedProperty entry, int index, bool isMin)
            {
                string valueName = isMin ? "min" : "max";
                string overrideName = isMin ? "overrideMin" : "overrideMax";
                var row = new VisualElement();
                row.AddToClassList("ssp-clamp-row");

                var toggle = new Toggle(isMin ? "Clamp Min" : "Clamp Max");
                toggle.bindingPath = entry.FindPropertyRelative(overrideName).propertyPath;
                toggle.AddToClassList("ssp-clamp-toggle");
                toggle.RegisterValueChangedCallback(_ => RefreshValidation());
                row.Add(toggle);

                var value = new FloatField { name = $"ssp-{valueName}-value-{index}" };
                value.bindingPath = entry.FindPropertyRelative(valueName).propertyPath;
                value.AddToClassList("ssp-clamp-value");
                row.Add(value);

                var inherited = new Label { name = $"ssp-{valueName}-inherited-{index}" };
                inherited.AddToClassList("ssp-clamp-inherited");
                row.Add(inherited);
                return row;
            }

            void RefreshClamp(SerializedProperty entry, StatDefinition stat, int index, bool isMin)
            {
                string valueName = isMin ? "min" : "max";
                string overrideName = isMin ? "overrideMin" : "overrideMax";
                bool overridden = entry.FindPropertyRelative(overrideName).boolValue;
                var value = list.Q<FloatField>($"ssp-{valueName}-value-{index}");
                var inherited = list.Q<Label>($"ssp-{valueName}-inherited-{index}");
                if (value == null || inherited == null) return;

                Show(value, overridden);
                Show(inherited, !overridden);
                if (overridden) return;

                if (stat == null)
                {
                    inherited.text = isMin ? "No min clamp" : "No max clamp";
                    return;
                }
                float bound = isMin ? stat.Min : stat.Max;
                bool clamped = isMin ? !float.IsNegativeInfinity(bound) : !float.IsPositiveInfinity(bound);
                inherited.text = clamped
                    ? $"{bound:0.###} (definition)"
                    : (isMin ? "No min clamp" : "No max clamp");
            }

            void RefreshValidation()
            {
                int size = entriesProp.arraySize;
                count.text = size == 1 ? "1 base stat" : $"{size} base stats";
                Show(empty, size == 0);

                var duplicates = DuplicateIndices(entriesProp);
                for (int i = 0; i < size; i++)
                {
                    var entry = entriesProp.GetArrayElementAtIndex(i);
                    var stat = entry.FindPropertyRelative("stat").objectReferenceValue as StatDefinition;
                    bool missing = stat == null;
                    bool badRange = HasBadRange(entry);

                    var row = list.Q<VisualElement>($"ssp-row-{i}");
                    var warning = list.Q<Label>($"ssp-row-warning-{i}");
                    if (row == null) continue;

                    RefreshClamp(entry, stat, i, true);
                    RefreshClamp(entry, stat, i, false);

                    var messages = new List<string>();
                    if (missing) messages.Add("Missing stat");
                    if (duplicates.Contains(i)) messages.Add("Duplicate stat");
                    if (badRange) messages.Add("Min is greater than max");
                    warning.text = string.Join("  |  ", messages);
                    Show(warning, messages.Count > 0);
                    row.EnableInClassList("ssp-entry-invalid", messages.Count > 0);
                }
            }

            void Sync()
            {
                if (entriesProp.arraySize != lastSize)
                {
                    lastSize = entriesProp.arraySize;
                    BuildList();
                }
                RefreshValidation();
            }

            root.Q<Button>("ssp-add").clicked += () =>
            {
                entriesProp.arraySize++;
                serializedObject.ApplyModifiedProperties();
                Sync();
            };

            root.Q<Button>("ssp-sort").clicked += () =>
            {
                var preset = (StatSheetPreset)target;
                Undo.RecordObject(preset, "Sort Stats");
                var sorted = ((StatSheetPreset.Entry[])EntriesField.GetValue(preset))
                    .OrderBy(e => e.stat != null ? e.stat.DisplayName : string.Empty)
                    .ToArray();
                EntriesField.SetValue(preset, sorted);
                EditorUtility.SetDirty(preset);
                serializedObject.Update();
                lastSize = -1;
                Sync();
            };

            root.Q<Button>("ssp-clean").clicked += () =>
            {
                var preset = (StatSheetPreset)target;
                Undo.RecordObject(preset, "Remove Null Entries");
                var cleaned = ((StatSheetPreset.Entry[])EntriesField.GetValue(preset))
                    .Where(e => e.stat != null)
                    .ToArray();
                EntriesField.SetValue(preset, cleaned);
                EditorUtility.SetDirty(preset);
                serializedObject.Update();
                lastSize = -1;
                Sync();
            };

            root.TrackPropertyValue(entriesProp, _ => Sync());
            Sync();
            return root;
        }

        static HashSet<int> DuplicateIndices(SerializedProperty entriesProp)
        {
            var seen = new Dictionary<string, int>();
            var duplicates = new HashSet<int>();
            for (int i = 0; i < entriesProp.arraySize; i++)
            {
                var stat = entriesProp.GetArrayElementAtIndex(i).FindPropertyRelative("stat").objectReferenceValue as StatDefinition;
                if (stat == null) continue;
                var id = stat.ToStatId();
                if (id.IsEmpty) continue;
                string key = id.ToString();
                if (seen.TryGetValue(key, out int first)) { duplicates.Add(first); duplicates.Add(i); }
                else seen[key] = i;
            }
            return duplicates;
        }

        static bool HasBadRange(SerializedProperty entry)
        {
            bool overrideMin = entry.FindPropertyRelative("overrideMin").boolValue;
            bool overrideMax = entry.FindPropertyRelative("overrideMax").boolValue;
            if (!overrideMin || !overrideMax) return false;
            return entry.FindPropertyRelative("min").floatValue > entry.FindPropertyRelative("max").floatValue;
        }

        static void Show(VisualElement element, bool visible) =>
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
