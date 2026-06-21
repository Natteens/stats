using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Stats.Editor
{
    [CustomPropertyDrawer(typeof(StatModifierData))]
    public sealed class StatModifierDataDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            if (!StatsEditorResources.Load("StatModifierDataDrawer", root))
                return new PropertyField(property);

            var statProp = property.FindPropertyRelative("stat");
            var opProp = property.FindPropertyRelative("operation");
            var valueProp = property.FindPropertyRelative("value");

            var valueField = root.Q<FloatField>("smd-value");
            var percent = root.Q<Label>("smd-percent");
            var warn = root.Q<Label>("smd-warn");
            warn.tooltip = "A Stat Definition is required.";

            void RefreshValue()
            {
                var op = CurrentOp(opProp);
                valueField.SetValueWithoutNotify(PercentConversion.ToDisplay(valueProp.floatValue, op));
                percent.style.display = PercentConversion.IsPercent(op) ? DisplayStyle.Flex : DisplayStyle.None;
            }

            void RefreshWarn() =>
                warn.style.display = statProp.objectReferenceValue == null ? DisplayStyle.Flex : DisplayStyle.None;

            valueField.RegisterValueChangedCallback(evt =>
            {
                valueProp.floatValue = PercentConversion.ToStored(evt.newValue, CurrentOp(opProp));
                property.serializedObject.ApplyModifiedProperties();
            });

            root.TrackPropertyValue(opProp, _ => RefreshValue());
            root.TrackPropertyValue(valueProp, _ => RefreshValue());
            root.TrackPropertyValue(statProp, _ => RefreshWarn());

            RefreshValue();
            RefreshWarn();
            return root;
        }

        static ModifierOperation CurrentOp(SerializedProperty opProp)
        {
            var values = (ModifierOperation[])Enum.GetValues(typeof(ModifierOperation));
            int index = opProp.enumValueIndex;
            return index >= 0 && index < values.Length ? values[index] : ModifierOperation.Flat;
        }
    }
}
