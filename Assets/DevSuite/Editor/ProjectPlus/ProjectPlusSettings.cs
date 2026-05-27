using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DevSuite.ProjectPlus
{
    [Serializable]
    public class FolderRule
    {
        public string ruleName = "New Rule";
        public bool isActive = true;
        public string folderName = "Scripts";
        public Color folderColor = new Color(0.3f, 0.6f, 0.9f, 1f);
        public bool customIcon = false;
        public string iconGuid = ""; // For customized textures if any
    }

    public class ProjectPlusSettings : ScriptableObject
    {
        public bool enableFolderColoring = true;
        public bool enableSizeVisualizer = true;
        public bool enableReferenceCount = true;

        public List<FolderRule> rules = new List<FolderRule>();

        private static ProjectPlusSettings instance;

        public static ProjectPlusSettings GetOrCreateSettings()
        {
            if (instance != null) return instance;

            string[] guids = AssetDatabase.FindAssets("t:ProjectPlusSettings");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                instance = AssetDatabase.LoadAssetAtPath<ProjectPlusSettings>(path);
            }

            if (instance == null)
            {
                instance = CreateInstance<ProjectPlusSettings>();

                // Add default folder coloring rules
                instance.rules.Add(new FolderRule
                {
                    ruleName = "Scripts Folder",
                    folderName = "Scripts",
                    folderColor = new Color(0.2f, 0.65f, 0.9f, 1f) // Azure
                });
                instance.rules.Add(new FolderRule
                {
                    ruleName = "Prefabs Folder",
                    folderName = "Prefabs",
                    folderColor = new Color(0.25f, 0.75f, 0.45f, 1f) // Teal/Green
                });
                instance.rules.Add(new FolderRule
                {
                    ruleName = "Scenes Folder",
                    folderName = "Scenes",
                    folderColor = new Color(0.9f, 0.35f, 0.35f, 1f) // Salmon/Red
                });
                instance.rules.Add(new FolderRule
                {
                    ruleName = "Materials & Textures Folder",
                    folderName = "Materials",
                    folderColor = new Color(0.9f, 0.65f, 0.2f, 1f) // Orange
                });

                // Create folder structures if missing
                if (!AssetDatabase.IsValidFolder("Assets/DevSuite"))
                {
                    AssetDatabase.CreateFolder("Assets", "DevSuite");
                }
                if (!AssetDatabase.IsValidFolder("Assets/DevSuite/Editor"))
                {
                    AssetDatabase.CreateFolder("Assets/DevSuite", "Editor");
                }
                if (!AssetDatabase.IsValidFolder("Assets/DevSuite/Editor/ProjectPlus"))
                {
                    AssetDatabase.CreateFolder("Assets/DevSuite/Editor", "ProjectPlus");
                }

                AssetDatabase.CreateAsset(instance, "Assets/DevSuite/Editor/ProjectPlus/ProjectPlusSettings.asset");
                AssetDatabase.SaveAssets();
            }

            return instance;
        }
    }
}
