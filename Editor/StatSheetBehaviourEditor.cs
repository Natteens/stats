using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Stats.Editor
{
    [CustomEditor(typeof(StatSheetBehaviour))]
    public sealed class StatSheetBehaviourEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            if (!StatsEditorResources.Load("StatSheetBehaviourEditor", root))
            {
                InspectorElement.FillDefaultInspector(root, serializedObject, this);
                return root;
            }

            var presetProp = serializedObject.FindProperty("preset");
            var mode = root.Q<Label>("ssb-mode");
            var editContent = root.Q<VisualElement>("ssb-edit-content");
            var warnPreset = root.Q<HelpBox>("ssb-warn-preset");
            var preview = root.Q<VisualElement>("ssb-preview");
            var previewEmpty = root.Q<Label>("ssb-preview-empty");
            var previewCount = root.Q<Label>("ssb-preview-count");
            var runtime = root.Q<VisualElement>("ssb-runtime");
            var readout = root.Q<VisualElement>("ssb-readout");
            var runtimeEmpty = root.Q<Label>("ssb-runtime-empty");
            var runtimeCount = root.Q<Label>("ssb-runtime-count");
            bool? lastPlaying = null;
            warnPreset.messageType = HelpBoxMessageType.Warning;

            void RefreshPreview()
            {
                var preset = presetProp.objectReferenceValue as StatSheetPreset;
                Show(warnPreset, preset == null);
                preview.Clear();

                int validCount = 0;
                if (preset != null)
                {
                    var entries = preset.Entries;
                    for (int i = 0; i < entries.Count; i++)
                    {
                        var entry = entries[i];
                        preview.Add(PreviewRow(entry));
                        if (entry.stat != null) validCount++;
                    }
                    previewEmpty.text = "No base stats configured.";
                    Show(previewEmpty, entries.Count == 0);
                }
                else
                {
                    previewEmpty.text = "Assign a preset to preview stats.";
                    Show(previewEmpty, true);
                }

                previewCount.text = validCount == 1 ? "1 stat" : $"{validCount} stats";
            }

            void RefreshRuntime()
            {
                readout.Clear();
                var behaviour = (StatSheetBehaviour)target;
                var preset = presetProp.objectReferenceValue as StatSheetPreset;
                var sheet = behaviour.Sheet;
                if (sheet == null)
                {
                    runtimeEmpty.text = "StatSheet is not ready.";
                    runtimeCount.text = "Not ready";
                    Show(runtimeEmpty, true);
                    return;
                }
                if (preset == null)
                {
                    runtimeEmpty.text = "No preset assigned.";
                    runtimeCount.text = "0 stats";
                    Show(runtimeEmpty, true);
                    return;
                }

                int shown = 0;
                var entries = preset.Entries;
                for (int i = 0; i < entries.Count; i++)
                {
                    var stat = entries[i].stat;
                    if (stat == null) continue;
                    var id = stat.ToStatId();
                    if (!sheet.IsRegistered(id)) continue;
                    readout.Add(RuntimeRow(stat, sheet.GetSnapshot(id)));
                    shown++;
                }

                runtimeCount.text = shown == 1 ? "1 stat" : $"{shown} stats";
                runtimeEmpty.text = "No runtime stats to show.";
                Show(runtimeEmpty, shown == 0);
            }

            void RefreshMode()
            {
                bool playing = EditorApplication.isPlaying;
                if (lastPlaying == playing) return;
                lastPlaying = playing;
                Show(editContent, !playing);
                Show(runtime, playing);
                mode.text = playing ? "Play" : "Edit";
                mode.EnableInClassList("stats-pill-ok", playing);
                mode.EnableInClassList("stats-pill-info", !playing);
                if (playing) RefreshRuntime();
            }

            root.Q<Button>("ssb-refresh").clicked += RefreshRuntime;
            root.TrackPropertyValue(presetProp, _ => { RefreshPreview(); RefreshRuntime(); });
            root.schedule.Execute(() =>
            {
                RefreshMode();
                if (EditorApplication.isPlaying) RefreshRuntime();
            }).Every(500);

            RefreshPreview();
            RefreshMode();
            return root;
        }

        static VisualElement PreviewRow(StatSheetPreset.Entry entry)
        {
            var row = new VisualElement();
            row.AddToClassList("ssb-preview-row");
            bool missing = entry.stat == null;
            if (missing) row.AddToClassList("ssb-preview-invalid");

            var name = new Label(missing ? "(missing stat)" : StatName(entry.stat));
            name.AddToClassList("ssb-row-name");
            row.Add(name);

            string detail = missing
                ? "Assign a Stat Definition."
                : $"base {entry.baseValue:0.###}{ClampText(entry)}";
            var body = new Label(detail);
            body.AddToClassList("ssb-row-detail");
            row.Add(body);
            return row;
        }

        static VisualElement RuntimeRow(StatDefinition stat, StatSnapshot snapshot)
        {
            var row = new VisualElement();
            row.AddToClassList("ssb-preview-row");

            var name = new Label(StatName(stat));
            name.AddToClassList("ssb-row-name");
            row.Add(name);

            string detail =
                $"base {snapshot.Base:0.###}   final {snapshot.Final:0.###}\n" +
                $"flat {snapshot.SumFlat:0.###}   +% {snapshot.SumPercentAdd:0.###}   " +
                $"x% {snapshot.PercentMultProduct:0.###}   " +
                $"override {(snapshot.HasOverride ? snapshot.OverrideValue.ToString("0.###") : "none")}   " +
                $"clamped {snapshot.Clamped}";
            var body = new Label(detail);
            body.AddToClassList("ssb-row-detail");
            row.Add(body);
            return row;
        }

        static string StatName(StatDefinition stat) =>
            string.IsNullOrEmpty(stat.DisplayName) ? stat.name : stat.DisplayName;

        static string ClampText(StatSheetPreset.Entry entry)
        {
            string min = entry.overrideMin ? $"   min {entry.min:0.###}" : string.Empty;
            string max = entry.overrideMax ? $"   max {entry.max:0.###}" : string.Empty;
            return min + max;
        }

        static void Show(VisualElement element, bool visible) =>
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
