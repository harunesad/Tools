using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DevSuite.Hierarchy
{
    public enum MatchType
    {
        StartsWith,
        EndsWith,
        Contains,
        ExactMatch
    }

    [Serializable]
    public class HierarchyRule
    {
        public string name = "New Rule";
        public bool isActive = true;
        public MatchType matchType = MatchType.StartsWith;
        public string searchString = "---";
        
        [Header("Text Styling")]
        public Color textColor = Color.white;
        public int fontSize = 12;
        public FontStyle fontStyle = FontStyle.Normal;
        public TextAnchor textAlignment = TextAnchor.MiddleLeft;

        [Header("Background Styling")]
        public bool drawBackground = true;
        public Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);

        [Header("Header Settings")]
        public bool isHeader = false;
    }

    public class HierarchyOrganizerSettings : ScriptableObject
    {
        public bool enableCustomStyling = true;
        public bool enableActiveToggle = true;
        
        public List<HierarchyRule> rules = new List<HierarchyRule>();

        private static HierarchyOrganizerSettings instance;

        public static HierarchyOrganizerSettings GetOrCreateSettings()
        {
            if (instance != null) return instance;

            string[] guids = AssetDatabase.FindAssets("t:HierarchyOrganizerSettings");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                instance = AssetDatabase.LoadAssetAtPath<HierarchyOrganizerSettings>(path);
            }

            if (instance == null)
            {
                instance = CreateInstance<HierarchyOrganizerSettings>();
                
                instance.rules.Add(new HierarchyRule
                {
                    name = "Section Headers",
                    matchType = MatchType.StartsWith,
                    searchString = "---",
                    textColor = new Color(0.9f, 0.9f, 0.9f, 1f),
                    fontStyle = FontStyle.Bold,
                    textAlignment = TextAnchor.MiddleCenter,
                    backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f),
                    isHeader = true
                });

                instance.rules.Add(new HierarchyRule
                {
                    name = "Main Camera",
                    matchType = MatchType.ExactMatch,
                    searchString = "Main Camera",
                    textColor = new Color(0.4f, 0.7f, 1f, 1f),
                    fontStyle = FontStyle.Bold,
                    drawBackground = false
                });

                // Ensure the folder exists
                if (!AssetDatabase.IsValidFolder("Assets/DevSuite/Editor/Hierarchy"))
                {
                    if (!AssetDatabase.IsValidFolder("Assets/DevSuite/Editor"))
                    {
                        AssetDatabase.CreateFolder("Assets/DevSuite", "Editor");
                    }
                    AssetDatabase.CreateFolder("Assets/DevSuite/Editor", "Hierarchy");
                }

                AssetDatabase.CreateAsset(instance, "Assets/DevSuite/Editor/Hierarchy/HierarchyOrganizerSettings.asset");
                AssetDatabase.SaveAssets();
            }

            return instance;
        }
    }
}
