using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Stats.Editor
{
    [CustomEditor(typeof(StatDefinition))]
    public sealed class StatDefinitionEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            if (!StatsEditorResources.Load("StatDefinitionEditor", root))
            {
                InspectorElement.FillDefaultInspector(root, serializedObject, this);
                return root;
            }

            var idProp = serializedObject.FindProperty("id");
            var nameProp = serializedObject.FindProperty("displayName");
            var keyProp = serializedObject.FindProperty("key");
            var hasMinProp = serializedObject.FindProperty("hasMin");
            var minProp = serializedObject.FindProperty("min");
            var hasMaxProp = serializedObject.FindProperty("hasMax");
            var maxProp = serializedObject.FindProperty("max");

            var title = root.Q<Label>("sd-title");
            var idStatus = root.Q<Label>("sd-id-status");
            var minField = root.Q<FloatField>("sd-min");
            var maxField = root.Q<FloatField>("sd-max");
            var warnMinMax = root.Q<HelpBox>("sd-warn-minmax");
            var warnId = root.Q<HelpBox>("sd-warn-id");
            var warnDup = root.Q<HelpBox>("sd-warn-dup");
            var warnKey = root.Q<HelpBox>("sd-warn-key");
            var warnKeyDup = root.Q<HelpBox>("sd-warn-keydup");
            var suggest = root.Q<Button>("sd-suggest");
            var repair = root.Q<Button>("sd-repair");
            var regen = root.Q<Button>("sd-regen");

            warnMinMax.messageType = HelpBoxMessageType.Warning;
            warnId.messageType = HelpBoxMessageType.Warning;
            warnDup.messageType = HelpBoxMessageType.Error;
            warnKeyDup.messageType = HelpBoxMessageType.Error;

            void Refresh()
            {
                var def = (StatDefinition)target;
                string label = string.IsNullOrEmpty(nameProp.stringValue) ? def.name : nameProp.stringValue;
                title.text = label;

                bool missing = string.IsNullOrEmpty(idProp.stringValue);
                bool duplicate = !missing && StatIdValidator.IsDuplicate(def);
                idStatus.RemoveFromClassList("sd-id-ok");
                idStatus.RemoveFromClassList("sd-id-bad");
                if (missing) { idStatus.text = "Missing ID"; idStatus.AddToClassList("sd-id-bad"); }
                else if (duplicate) { idStatus.text = "Duplicate ID"; idStatus.AddToClassList("sd-id-bad"); }
                else { idStatus.text = "ID Valid"; idStatus.AddToClassList("sd-id-ok"); }

                minField.SetEnabled(hasMinProp.boolValue);
                maxField.SetEnabled(hasMaxProp.boolValue);

                bool badRange = hasMinProp.boolValue && hasMaxProp.boolValue && minProp.floatValue > maxProp.floatValue;

                string key = keyProp.stringValue;
                bool keyEmpty = string.IsNullOrEmpty(key);
                bool keyInvalid = !keyEmpty && !StatKey.IsValid(key);
                bool keyDuplicate = !keyEmpty && !keyInvalid && StatKeyValidator.IsDuplicateKey(def);
                warnKey.messageType = keyEmpty ? HelpBoxMessageType.Info : HelpBoxMessageType.Warning;
                warnKey.text = keyEmpty
                    ? "No key set: this stat cannot be looked up by key."
                    : "Key must be lowercase snake_case, e.g. movement_speed.";

                Show(warnMinMax, badRange);
                Show(warnId, missing);
                Show(warnDup, duplicate);
                Show(warnKey, keyEmpty || keyInvalid);
                Show(warnKeyDup, keyDuplicate);
                Show(suggest, keyEmpty);
                Show(repair, missing);
                Show(regen, duplicate);
            }

            suggest.clicked += () =>
            {
                var def = (StatDefinition)target;
                string basis = string.IsNullOrEmpty(nameProp.stringValue) ? def.name : nameProp.stringValue;
                keyProp.stringValue = StatKey.Normalize(basis);
                serializedObject.ApplyModifiedProperties();
                Refresh();
            };
            repair.clicked += () => { StatIdValidator.AssignNewId((StatDefinition)target); serializedObject.Update(); Refresh(); };
            regen.clicked += () => { StatIdValidator.AssignNewId((StatDefinition)target); serializedObject.Update(); Refresh(); };

            root.TrackPropertyValue(nameProp, _ => Refresh());
            root.TrackPropertyValue(keyProp, _ => Refresh());
            root.TrackPropertyValue(hasMinProp, _ => Refresh());
            root.TrackPropertyValue(hasMaxProp, _ => Refresh());
            root.TrackPropertyValue(minProp, _ => Refresh());
            root.TrackPropertyValue(maxProp, _ => Refresh());
            root.TrackPropertyValue(idProp, _ => Refresh());

            Refresh();
            return root;
        }

        static void Show(VisualElement element, bool visible) =>
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
