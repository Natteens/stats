using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Stats.Editor
{
    [CustomEditor(typeof(StatSheetBehaviour))]
    public sealed class StatSheetBehaviourEditor : UnityEditor.Editor
    {
        const string DebugPrefix = "debug:";
        const string DebugDefault = "debug:inspector";
        static readonly string[] TabNames = { "Overview", "Stats", "Modifiers", "Resources", "Debug" };

        struct StatInfo
        {
            public string key;
            public string display;
            public bool percent;
            public float min;
            public float max;
        }

        sealed class ModRow
        {
            public string label;
            public string valueText;
            public string opText;
            public string source;
            public bool timed;
            public float remaining;
        }

        SerializedProperty presetProp;
        Label modePill;

        Label sumPreset, sumStats, sumMods, sumRes;
        readonly Button[] tabButtons = new Button[TabNames.Length];
        readonly VisualElement[] panels = new VisualElement[TabNames.Length];
        int activeTab;

        HelpBox warnPreset;
        VisualElement previewList;
        Label previewEmpty;

        ToolbarSearchField statSearch;
        ListView statsList;
        Label statsEmpty;
        VisualElement statDetail;

        ToolbarSearchField modSearch;
        ListView modsList;
        Label modsEmpty;

        VisualElement resList;
        Label resEmpty;

        VisualElement debugTools;
        Label debugHint, debugPreview;
        DropdownField debugStat;
        EnumField debugOp;
        FloatField debugValue, debugDuration;
        Toggle debugTimed;
        TextField debugSource;

        readonly List<StatId> allStats = new List<StatId>();
        readonly List<StatId> statItems = new List<StatId>();
        readonly List<ModRow> allMods = new List<ModRow>();
        readonly List<ModRow> modItems = new List<ModRow>();
        readonly List<StatId> debugStatIds = new List<StatId>();
        readonly Dictionary<StatId, StatInfo> infoById = new Dictionary<StatId, StatInfo>();

        string statFilter = string.Empty;
        string modFilter = string.Empty;
        StatId selected;
        bool hasSelected;
        bool? lastPlaying;

        StatSheetBehaviour Behaviour => (StatSheetBehaviour)target;
        StatSheet Sheet => Behaviour.Sheet;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            if (!StatsEditorResources.Load("StatSheetBehaviourEditor", root))
            {
                InspectorElement.FillDefaultInspector(root, serializedObject, this);
                return root;
            }

            presetProp = serializedObject.FindProperty("preset");
            modePill = root.Q<Label>("ssb-mode");
            var body = root.Q<VisualElement>("ssb-body");

            body.Add(BuildSummary());
            body.Add(BuildTabs());

            var content = new VisualElement();
            panels[0] = BuildOverview();
            panels[1] = BuildStats();
            panels[2] = BuildModifiers();
            panels[3] = BuildResources();
            panels[4] = BuildDebug();
            for (int i = 0; i < panels.Length; i++) content.Add(panels[i]);
            body.Add(content);

            root.TrackPropertyValue(presetProp, _ => RefreshAll());
            root.schedule.Execute(Tick).Every(500);

            RefreshMode(true);
            return root;
        }

        VisualElement BuildSummary()
        {
            var card = new VisualElement();
            card.AddToClassList("stats-card");
            card.AddToClassList("ssb-summary");
            sumPreset = SummaryItem(card, "Preset");
            sumStats = SummaryItem(card, "Stats");
            sumMods = SummaryItem(card, "Modifiers");
            sumRes = SummaryItem(card, "Resources");
            return card;
        }

        static Label SummaryItem(VisualElement parent, string caption)
        {
            var item = new VisualElement();
            item.AddToClassList("ssb-summary-item");
            var cap = new Label(caption);
            cap.AddToClassList("ssb-summary-caption");
            var val = new Label("-");
            val.AddToClassList("ssb-summary-value");
            item.Add(cap);
            item.Add(val);
            parent.Add(item);
            return val;
        }

        VisualElement BuildTabs()
        {
            var bar = new VisualElement();
            bar.AddToClassList("ssb-tabs");
            for (int i = 0; i < TabNames.Length; i++)
            {
                int index = i;
                var button = new Button(() => SetTab(index)) { text = TabNames[i] };
                button.AddToClassList("ssb-tab");
                tabButtons[i] = button;
                bar.Add(button);
            }
            return bar;
        }

        VisualElement BuildOverview()
        {
            var panel = Panel();
            var setup = Card("Setup");
            setup.Add(new PropertyField(presetProp, "Preset"));
            warnPreset = new HelpBox("No preset assigned. An empty StatSheet is built on Awake.", HelpBoxMessageType.Info);
            setup.Add(warnPreset);
            panel.Add(setup);

            var preview = Card("Base Stats Preview");
            previewList = new ScrollView { style = { maxHeight = 200 } };
            preview.Add(previewList);
            previewEmpty = Empty();
            preview.Add(previewEmpty);
            panel.Add(preview);
            return panel;
        }

        VisualElement BuildStats()
        {
            var panel = Panel();
            var card = Card(null);
            statSearch = new ToolbarSearchField();
            statSearch.AddToClassList("ssb-search");
            statSearch.RegisterValueChangedCallback(e => { statFilter = e.newValue ?? string.Empty; ApplyStatFilter(); });
            var bar = Bar();
            bar.Add(statSearch);
            card.Add(bar);
            statsEmpty = Empty();
            card.Add(statsEmpty);
            statsList = new ListView(statItems, 22, MakeStatRow, BindStatRow) { selectionType = SelectionType.Single };
            statsList.AddToClassList("ssb-list");
            statsList.selectionChanged += objects =>
            {
                foreach (var o in objects) { selected = (StatId)o; hasSelected = true; }
                UpdateStatDetail();
            };
            card.Add(statsList);
            statDetail = new VisualElement();
            statDetail.AddToClassList("ssb-detail");
            statDetail.style.display = DisplayStyle.None;
            card.Add(statDetail);
            panel.Add(card);
            return panel;
        }

        VisualElement BuildModifiers()
        {
            var panel = Panel();
            var card = Card(null);
            modSearch = new ToolbarSearchField();
            modSearch.AddToClassList("ssb-search");
            modSearch.RegisterValueChangedCallback(e => { modFilter = e.newValue ?? string.Empty; ApplyModFilter(); });
            var bar = Bar();
            bar.Add(modSearch);
            card.Add(bar);
            modsEmpty = Empty();
            card.Add(modsEmpty);
            modsList = new ListView(modItems, 34, MakeModRow, BindModRow) { selectionType = SelectionType.None };
            modsList.AddToClassList("ssb-list");
            card.Add(modsList);
            panel.Add(card);
            return panel;
        }

        VisualElement BuildResources()
        {
            var panel = Panel();
            var card = Card(null);
            resList = new ScrollView { style = { maxHeight = 200 } };
            card.Add(resList);
            resEmpty = Empty();
            card.Add(resEmpty);
            panel.Add(card);
            return panel;
        }

        VisualElement BuildDebug()
        {
            var panel = Panel();
            var card = Card("Add Modifier");
            debugHint = new Label("Debug tools are available in Play Mode.");
            debugHint.AddToClassList("ssb-hint");
            card.Add(debugHint);

            debugTools = new VisualElement();
            debugStat = new DropdownField("Stat");
            debugStat.RegisterValueChangedCallback(_ => UpdateDebugPreview());
            debugOp = new EnumField("Operation", ModifierOperation.Flat);
            debugOp.RegisterValueChangedCallback(_ => UpdateDebugPreview());
            debugValue = new FloatField("Value");
            debugValue.RegisterValueChangedCallback(_ => UpdateDebugPreview());
            debugSource = new TextField("Source Id") { value = DebugDefault };
            debugTimed = new Toggle("Timed");
            debugDuration = new FloatField("Duration") { value = 5f };
            debugDuration.style.display = DisplayStyle.None;
            debugTimed.RegisterValueChangedCallback(e => debugDuration.style.display = e.newValue ? DisplayStyle.Flex : DisplayStyle.None);

            debugTools.Add(debugStat);
            debugTools.Add(debugOp);
            debugTools.Add(debugValue);
            debugTools.Add(debugSource);
            debugTools.Add(debugTimed);
            debugTools.Add(debugDuration);

            debugPreview = new Label();
            debugPreview.AddToClassList("ssb-hint");
            debugTools.Add(debugPreview);

            var buttons = Bar();
            var add = new Button(AddDebugModifier) { text = "Add Modifier" };
            add.AddToClassList("ssb-btn");
            var clear = new Button(ClearDebugModifiers) { text = "Clear Debug Modifiers" };
            clear.AddToClassList("ssb-btn");
            buttons.Add(add);
            buttons.Add(clear);
            debugTools.Add(buttons);
            card.Add(debugTools);
            panel.Add(card);
            return panel;
        }

        void SetTab(int index)
        {
            activeTab = index;
            SessionState.SetInt("Stats.SSB.Tab", index);
            for (int i = 0; i < panels.Length; i++)
            {
                Show(panels[i], i == index);
                tabButtons[i].EnableInClassList("ssb-tab-active", i == index);
            }
            RefreshAll();
        }

        void Tick()
        {
            RefreshMode(false);
            RefreshAll();
        }

        void RefreshMode(bool force)
        {
            bool playing = EditorApplication.isPlaying;
            if (!force && lastPlaying == playing) return;
            lastPlaying = playing;
            modePill.text = playing ? "Play" : "Edit";
            modePill.EnableInClassList("stats-pill-ok", playing);
            modePill.EnableInClassList("stats-pill-info", !playing);
            Show(debugTools, playing);
            Show(debugHint, !playing);
            SetTab(playing ? 1 : SessionState.GetInt("Stats.SSB.Tab", 0));
        }

        void RefreshAll()
        {
            BuildInfoMap();
            GatherStats();
            GatherMods();
            RefreshSummary();
            switch (activeTab)
            {
                case 0: RefreshPreview(); break;
                case 1: ApplyStatFilter(); UpdateStatDetail(); break;
                case 2: ApplyModFilter(); break;
                case 3: RefreshResources(); break;
                case 4: RefreshDebugChoices(); UpdateDebugPreview(); break;
            }
        }

        void GatherStats()
        {
            allStats.Clear();
            var sheet = Sheet;
            if (sheet == null) return;
            var registered = sheet.GetRegisteredStats();
            for (int i = 0; i < registered.Count; i++) allStats.Add(registered[i]);
        }

        void GatherMods()
        {
            allMods.Clear();
            var sheet = Sheet;
            if (sheet == null) return;
            var timedHandles = new HashSet<ModifierHandle>();
            var service = Behaviour.TimedModifiers;
            if (service != null)
            {
                foreach (var t in service.GetActiveTimedModifiers())
                {
                    timedHandles.Add(t.Handle);
                    allMods.Add(MakeMod(t.Stat, t.Operation, t.Value, t.SourceId ?? "(no id)", true, t.RemainingSeconds));
                }
            }
            foreach (var e in sheet.GetModifierEntries())
            {
                if (timedHandles.Contains(e.Handle)) continue;
                allMods.Add(MakeMod(e.Handle.Stat, e.Operation, e.Value, SourceLabel(e.Source), false, 0f));
            }
        }

        ModRow MakeMod(StatId stat, ModifierOperation op, float value, string source, bool timed, float remaining)
        {
            bool percent = GetInfo(stat).percent;
            return new ModRow
            {
                label = LabelFor(stat),
                opText = Op(op),
                valueText = ModValueText(op, value, percent),
                source = source,
                timed = timed,
                remaining = remaining
            };
        }

        void RefreshSummary()
        {
            var preset = presetProp.objectReferenceValue as StatSheetPreset;
            sumPreset.text = preset != null ? preset.name : "None";
            bool playing = EditorApplication.isPlaying && Sheet != null;
            if (playing)
            {
                sumStats.text = allStats.Count.ToString();
                sumMods.text = allMods.Count.ToString();
            }
            else
            {
                int valid = 0;
                if (preset != null) { var e = preset.Entries; for (int i = 0; i < e.Count; i++) if (e[i].stat != null) valid++; }
                sumStats.text = valid.ToString();
                sumMods.text = "-";
            }
            sumRes.text = playing ? CountResources().ToString() : "-";
        }

        int CountResources()
        {
            int c = 0;
            foreach (var _ in Behaviour.Resources) c++;
            return c;
        }

        void ApplyStatFilter()
        {
            statItems.Clear();
            for (int i = 0; i < allStats.Count; i++)
            {
                var id = allStats[i];
                var info = GetInfo(id);
                if (Match(statFilter, info.key, info.display, id.ToString())) statItems.Add(id);
            }
            statsList.Rebuild();
            bool play = EditorApplication.isPlaying && Sheet != null;
            Show(statsList, play && statItems.Count > 0);
            Show(statsEmpty, !play || statItems.Count == 0);
            statsEmpty.text = !play ? "Enter Play Mode to inspect runtime stats."
                : (allStats.Count == 0 ? "No runtime stats." : "No stats match the filter.");
        }

        void ApplyModFilter()
        {
            modItems.Clear();
            for (int i = 0; i < allMods.Count; i++)
            {
                var m = allMods[i];
                if (Match(modFilter, m.label, m.source, m.opText)) modItems.Add(m);
            }
            modsList.Rebuild();
            bool play = EditorApplication.isPlaying && Sheet != null;
            Show(modsList, play && modItems.Count > 0);
            Show(modsEmpty, !play || modItems.Count == 0);
            modsEmpty.text = !play ? "Enter Play Mode to inspect runtime modifiers."
                : (allMods.Count == 0 ? "No active modifiers." : "No modifiers match the filter.");
        }

        void RefreshResources()
        {
            resList.Clear();
            bool play = EditorApplication.isPlaying && Sheet != null;
            int count = 0;
            if (play)
            {
                foreach (var pair in Behaviour.Resources)
                {
                    count++;
                    var row = new VisualElement();
                    row.AddToClassList("ssb-resource");
                    var key = new Label(pair.Key);
                    key.AddToClassList("ssb-resource-key");
                    var r = pair.Value;
                    string state = r.IsFull ? "full" : (r.Current <= 0f ? "empty" : "");
                    var detail = new Label($"{r.Current:0.##}/{r.Max:0.##}  ({r.Normalized:0.00}) {state}  max: {LabelFor(r.MaxStat)}");
                    detail.AddToClassList("ssb-resource-detail");
                    row.Add(key);
                    row.Add(detail);
                    resList.Add(row);
                }
            }
            Show(resList, play && count > 0);
            Show(resEmpty, !play || count == 0);
            resEmpty.text = !play ? "Enter Play Mode to inspect runtime resources." : "No registered resources.";
        }

        void RefreshDebugChoices()
        {
            debugStatIds.Clear();
            var choices = new List<string>();
            for (int i = 0; i < allStats.Count; i++)
            {
                debugStatIds.Add(allStats[i]);
                choices.Add(SelectorLabel(allStats[i]));
            }
            debugStat.choices = choices;
            if (debugStat.index < 0 && choices.Count > 0) debugStat.index = 0;
            if (debugStat.index >= choices.Count) debugStat.index = choices.Count > 0 ? 0 : -1;
        }

        void UpdateStatDetail()
        {
            var sheet = Sheet;
            if (!hasSelected || sheet == null || !sheet.IsRegistered(selected))
            {
                Show(statDetail, false);
                return;
            }
            var info = GetInfo(selected);
            var snap = sheet.GetSnapshot(selected);
            statDetail.Clear();
            var title = new Label(LabelFor(selected));
            title.AddToClassList("ssb-detail-title");
            statDetail.Add(title);
            string clampText = (!float.IsNegativeInfinity(info.min) || !float.IsPositiveInfinity(info.max))
                ? $"\nclamp [{FmtBound(info, info.min)}, {FmtBound(info, info.max)}]  clamped {snap.Clamped}"
                : $"\nclamped {snap.Clamped}";
            statDetail.Add(new Label(
                $"base {FmtStat(info, snap.Base)}   final {FmtStat(info, snap.Final)}\n" +
                $"flat {FmtStat(info, snap.SumFlat)}   +% {snap.SumPercentAdd * 100f:0.##}%   x% {snap.PercentMultProduct:0.###}   " +
                $"override {(snap.HasOverride ? FmtStat(info, snap.OverrideValue) : "none")}" + clampText));
            Show(statDetail, true);
        }

        void RefreshPreview()
        {
            var preset = presetProp.objectReferenceValue as StatSheetPreset;
            warnPreset.style.display = preset == null ? DisplayStyle.Flex : DisplayStyle.None;
            previewList.Clear();
            int valid = 0;
            if (preset != null)
            {
                var entries = preset.Entries;
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    var row = new VisualElement();
                    row.AddToClassList("ssb-row");
                    row.style.height = 20;
                    var name = new Label(entry.stat == null ? "(missing stat)" : StatName(entry.stat));
                    name.AddToClassList("ssb-row-key");
                    string detailText;
                    if (entry.stat == null) detailText = "assign a definition";
                    else detailText = entry.stat.PercentDisplay ? $"base {entry.baseValue * 100f:0.##}%" : $"base {entry.baseValue:0.###}";
                    var detail = new Label(detailText);
                    detail.AddToClassList("ssb-row-values");
                    row.Add(name);
                    row.Add(detail);
                    previewList.Add(row);
                    if (entry.stat != null) valid++;
                }
                Show(previewEmpty, entries.Count == 0);
                previewEmpty.text = "No base stats configured.";
            }
            else
            {
                Show(previewEmpty, true);
                previewEmpty.text = "Assign a preset to preview stats.";
            }
        }

        void UpdateDebugPreview()
        {
            if (debugPreview == null) return;
            int index = debugStat.index;
            if (index < 0 || index >= debugStatIds.Count) { debugPreview.text = string.Empty; return; }
            var id = debugStatIds[index];
            var info = GetInfo(id);
            var op = (ModifierOperation)debugOp.value;
            float input = debugValue.value;
            bool percentInput = op == ModifierOperation.PercentAdd || op == ModifierOperation.PercentMult
                                || (info.percent && (op == ModifierOperation.Flat || op == ModifierOperation.Override));
            debugValue.label = percentInput ? "Value (%)" : "Value";
            float internalValue = ToInternal(info.percent, op, input);
            debugPreview.text = PreviewText(info.percent, op, input, internalValue);
        }

        static string PreviewText(bool percent, ModifierOperation op, float input, float internalValue)
        {
            switch (op)
            {
                case ModifierOperation.PercentAdd:
                    return $"Adds +{input:0.##}% in the PercentAdd step (internal {internalValue:0.###}).";
                case ModifierOperation.PercentMult:
                    return $"Adds +{input:0.##}% in the PercentMult step (internal {internalValue:0.###}).";
                case ModifierOperation.Flat:
                    return percent
                        ? $"Adds +{input:0.##} percentage points (internal +{internalValue:0.###})."
                        : $"Adds +{input:0.##} (internal +{internalValue:0.###}).";
                case ModifierOperation.Override:
                    return percent
                        ? $"Sets to {input:0.##}% (internal {internalValue:0.###})."
                        : $"Sets to {input:0.##}.";
                default:
                    return string.Empty;
            }
        }

        static float ToInternal(bool percentStat, ModifierOperation op, float input)
        {
            if (op == ModifierOperation.PercentAdd || op == ModifierOperation.PercentMult) return input / 100f;
            if (percentStat && (op == ModifierOperation.Flat || op == ModifierOperation.Override)) return input / 100f;
            return input;
        }

        void AddDebugModifier()
        {
            var sheet = Sheet;
            if (sheet == null) return;
            int index = debugStat.index;
            if (index < 0 || index >= debugStatIds.Count) return;
            var id = debugStatIds[index];
            var info = GetInfo(id);
            var op = (ModifierOperation)debugOp.value;
            float internalValue = ToInternal(info.percent, op, debugValue.value);
            string source = string.IsNullOrEmpty(debugSource.value) ? DebugDefault : debugSource.value;
            if (!source.StartsWith(DebugPrefix, StringComparison.Ordinal)) source = DebugPrefix + source;
            if (debugTimed.value && Behaviour.TimedModifiers != null)
                Behaviour.TimedModifiers.AddTimedModifier(sheet, id, op, internalValue, source, Mathf.Max(0.01f, debugDuration.value));
            else
                sheet.AddModifier(id, op, internalValue, source);
            RefreshAll();
        }

        void ClearDebugModifiers()
        {
            var sheet = Sheet;
            if (sheet == null) return;
            var entries = sheet.GetModifierEntries();
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e.Source is string s && s.StartsWith(DebugPrefix, StringComparison.Ordinal))
                    sheet.RemoveModifier(e.Handle);
            }
            RefreshAll();
        }

        void BuildInfoMap()
        {
            infoById.Clear();
            var preset = presetProp.objectReferenceValue as StatSheetPreset;
            if (preset == null) return;
            var entries = preset.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                var stat = entries[i].stat;
                if (stat == null) continue;
                var id = stat.ToStatId();
                if (id.IsEmpty || infoById.ContainsKey(id)) continue;
                infoById[id] = new StatInfo
                {
                    key = stat.Key,
                    display = string.IsNullOrEmpty(stat.DisplayName) ? stat.name : stat.DisplayName,
                    percent = stat.PercentDisplay,
                    min = entries[i].overrideMin ? entries[i].min : stat.Min,
                    max = entries[i].overrideMax ? entries[i].max : stat.Max
                };
            }
        }

        StatInfo GetInfo(StatId id)
        {
            if (infoById.TryGetValue(id, out var info)) return info;
            string key = Behaviour.TryGetKey(id, out var k) ? k : string.Empty;
            string text = id.ToString();
            return new StatInfo
            {
                key = key,
                display = string.IsNullOrEmpty(key) ? (text.Length > 8 ? text.Substring(0, 8) : text) : key,
                percent = false,
                min = float.NegativeInfinity,
                max = float.PositiveInfinity
            };
        }

        string LabelFor(StatId id)
        {
            var info = GetInfo(id);
            return string.IsNullOrEmpty(info.key) ? info.display : info.key;
        }

        string SelectorLabel(StatId id)
        {
            var info = GetInfo(id);
            return string.IsNullOrEmpty(info.key) ? $"No key — {info.display}" : $"{info.key} — {info.display}";
        }

        static string FmtStat(StatInfo info, float v) => info.percent ? $"{v * 100f:0.##}%" : Fmt(v);

        static string FmtBound(StatInfo info, float v)
        {
            if (float.IsNegativeInfinity(v)) return "-inf";
            if (float.IsPositiveInfinity(v)) return "+inf";
            return info.percent ? $"{v * 100f:0.##}%" : v.ToString("0.###");
        }

        static string ModValueText(ModifierOperation op, float v, bool percent)
        {
            if (op == ModifierOperation.PercentAdd || op == ModifierOperation.PercentMult) return $"+{v * 100f:0.##}%";
            if (percent && op == ModifierOperation.Flat) return $"+{v * 100f:0.##} pp";
            if (percent && op == ModifierOperation.Override) return $"={v * 100f:0.##}%";
            return Fmt(v);
        }

        static string StatName(StatDefinition stat)
        {
            if (!string.IsNullOrEmpty(stat.Key)) return stat.Key;
            return string.IsNullOrEmpty(stat.DisplayName) ? stat.name : stat.DisplayName;
        }

        static string SourceLabel(object source)
        {
            string id = StatSourceId.Of(source);
            if (!string.IsNullOrEmpty(id)) return id;
            return source != null ? source.GetType().Name : "null";
        }

        static string Fmt(float value)
        {
            if (float.IsNegativeInfinity(value)) return "-inf";
            if (float.IsPositiveInfinity(value)) return "+inf";
            return value.ToString("0.###");
        }

        static string Op(ModifierOperation op)
        {
            switch (op)
            {
                case ModifierOperation.Flat: return "flat";
                case ModifierOperation.PercentAdd: return "+%";
                case ModifierOperation.PercentMult: return "x%";
                case ModifierOperation.Override: return "override";
                default: return op.ToString();
            }
        }

        VisualElement MakeStatRow()
        {
            var row = new VisualElement();
            row.AddToClassList("ssb-row");
            var key = new Label { name = "k" };
            key.AddToClassList("ssb-row-key");
            var values = new Label { name = "v" };
            values.AddToClassList("ssb-row-values");
            var badge = new Label("CLAMP") { name = "b" };
            badge.AddToClassList("ssb-badge");
            badge.AddToClassList("ssb-badge-clamp");
            row.Add(key);
            row.Add(values);
            row.Add(badge);
            return row;
        }

        void BindStatRow(VisualElement element, int index)
        {
            if (index < 0 || index >= statItems.Count) return;
            var sheet = Sheet;
            if (sheet == null) return;
            var id = statItems[index];
            var info = GetInfo(id);
            var snap = sheet.GetSnapshot(id);
            element.Q<Label>("k").text = LabelFor(id);
            element.Q<Label>("v").text = $"{FmtStat(info, snap.Base)} → {FmtStat(info, snap.Final)}";
            Show(element.Q<Label>("b"), snap.Clamped);
        }

        VisualElement MakeModRow()
        {
            var row = new VisualElement();
            row.AddToClassList("ssb-mod-row");
            var top = new VisualElement();
            top.AddToClassList("ssb-row");
            var key = new Label { name = "k" };
            key.AddToClassList("ssb-row-key");
            var badge = new Label("TIMED") { name = "b" };
            badge.AddToClassList("ssb-badge");
            badge.AddToClassList("ssb-badge-timed");
            top.Add(key);
            top.Add(badge);
            var detail = new Label { name = "d" };
            detail.AddToClassList("ssb-row-detail");
            row.Add(top);
            row.Add(detail);
            return row;
        }

        void BindModRow(VisualElement element, int index)
        {
            if (index < 0 || index >= modItems.Count) return;
            var m = modItems[index];
            element.Q<Label>("k").text = m.label;
            string timed = m.timed ? $"   {m.remaining:0.#}s left" : string.Empty;
            element.Q<Label>("d").text = $"{m.opText} {m.valueText}   [{m.source}]{timed}";
            Show(element.Q<Label>("b"), m.timed);
        }

        static bool Match(string filter, params string[] values)
        {
            if (string.IsNullOrWhiteSpace(filter)) return true;
            for (int i = 0; i < values.Length; i++)
            {
                var value = values[i];
                if (!string.IsNullOrEmpty(value) && value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        static VisualElement Panel()
        {
            var panel = new VisualElement();
            panel.AddToClassList("ssb-panel");
            return panel;
        }

        static VisualElement Card(string header)
        {
            var card = new VisualElement();
            card.AddToClassList("stats-card");
            if (!string.IsNullOrEmpty(header))
            {
                var label = new Label(header);
                label.AddToClassList("stats-section-header");
                card.Add(label);
            }
            return card;
        }

        static VisualElement Bar()
        {
            var bar = new VisualElement();
            bar.AddToClassList("ssb-bar");
            return bar;
        }

        static Label Empty()
        {
            var label = new Label();
            label.AddToClassList("stats-empty");
            return label;
        }

        static void Show(VisualElement element, bool visible) =>
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
