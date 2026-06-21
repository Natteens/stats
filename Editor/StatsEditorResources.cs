using UnityEditor;
using UnityEngine.UIElements;

namespace Stats.Editor
{
    static class StatsEditorResources
    {
        const string UiPath = "Packages/com.natteens.stats/Editor/UI/";

        public static bool Load(string name, VisualElement root)
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPath + name + ".uxml");
            if (tree == null) return false;
            tree.CloneTree(root);
            var common = AssetDatabase.LoadAssetAtPath<StyleSheet>(UiPath + "StatsEditorCommon.uss");
            if (common != null) root.styleSheets.Add(common);
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(UiPath + name + ".uss");
            if (sheet != null) root.styleSheets.Add(sheet);
            return true;
        }
    }
}
