using UnityEditor;
using UnityEngine;

namespace DevSuite.Hierarchy
{
    [InitializeOnLoad]
    public static class HierarchyOrganizer
    {
        static HierarchyOrganizer()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemOnGUI;
        }

        private static void OnHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (go == null) return;

            var settings = HierarchyOrganizerSettings.GetOrCreateSettings();
            if (settings == null) return;

            // 1. Draw Custom Styling
            if (settings.enableCustomStyling)
            {
                HierarchyRule matchingRule = null;
                foreach (var rule in settings.rules)
                {
                    if (rule.isActive && IsRuleMatch(go.name, rule))
                    {
                        matchingRule = rule;
                        break;
                    }
                }

                if (matchingRule != null)
                {
                    DrawCustomItem(go, matchingRule, instanceID, selectionRect);
                }
            }

            // 2. Draw Active State Toggle (drawn on the far right)
            if (settings.enableActiveToggle)
            {
                Rect toggleRect = new Rect(selectionRect.xMax - 4f, selectionRect.y, 16f, selectionRect.height);
                
                bool isActive = go.activeSelf;
                EditorGUI.BeginChangeCheck();
                bool newActive = GUI.Toggle(toggleRect, isActive, "");
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(go, "Toggle GameObject Active State");
                    go.SetActive(newActive);
                    EditorUtility.SetDirty(go);
                }
            }
        }

        private static bool IsRuleMatch(string goName, HierarchyRule rule)
        {
            if (string.IsNullOrEmpty(rule.searchString)) return false;

            switch (rule.matchType)
            {
                case MatchType.StartsWith:
                    return goName.StartsWith(rule.searchString);
                case MatchType.EndsWith:
                    return goName.EndsWith(rule.searchString);
                case MatchType.Contains:
                    return goName.Contains(rule.searchString);
                case MatchType.ExactMatch:
                    return goName == rule.searchString;
                default:
                    return false;
            }
        }

        private static void DrawCustomItem(GameObject go, HierarchyRule rule, int instanceID, Rect rect)
        {
            bool isSelected = Selection.Contains(instanceID);
            
            Color bgCol;
            if (isSelected)
            {
                bgCol = EditorGUIUtility.isProSkin ? new Color(0.17f, 0.36f, 0.7f, 1f) : new Color(0.24f, 0.48f, 0.9f, 1f);
            }
            else if (rule.drawBackground)
            {
                bgCol = rule.backgroundColor;
            }
            else
            {
                bgCol = EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f, 1f) : new Color(0.78f, 0.78f, 0.78f, 1f);
            }

            Rect bgRect = new Rect(rect);
            if (rule.isHeader)
            {
                bgRect.xMin = 32f;
                bgRect.xMax = rect.xMax + 16f;
            }

            EditorGUI.DrawRect(bgRect, bgCol);

            GUIStyle style = new GUIStyle();
            style.normal.textColor = isSelected ? Color.white : rule.textColor;
            style.fontSize = rule.fontSize;
            style.fontStyle = rule.fontStyle;
            style.alignment = rule.textAlignment;

            string displayName = go.name;
            if (rule.isHeader && rule.matchType == MatchType.StartsWith && go.name.StartsWith(rule.searchString))
            {
                displayName = go.name.Substring(rule.searchString.Length).Trim();
            }

            if (!go.activeInHierarchy && !isSelected)
            {
                Color fadeColor = style.normal.textColor;
                fadeColor.a = 0.5f;
                style.normal.textColor = fadeColor;
            }

            Rect textRect = new Rect(bgRect.xMin + 6f, bgRect.y, bgRect.width - 12f, bgRect.height);
            GUI.Label(textRect, displayName, style);
        }
    }
}
