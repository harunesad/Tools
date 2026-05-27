using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DevSuite.ProjectPlus
{
    [InitializeOnLoad]
    public static class ProjectPlusRenderer
    {
        private static Dictionary<string, int> referenceCounts = new Dictionary<string, int>();
        private static bool isCacheBuilt = false;

        static ProjectPlusRenderer()
        {
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemOnGUI;
            // Delay cache build to not slow down Unity startup
            EditorApplication.delayCall += RebuildReferenceCache;
        }

        public static void RebuildReferenceCache()
        {
            referenceCounts.Clear();
            string[] allPaths = AssetDatabase.GetAllAssetPaths();
            
            // Build dependency reverse map
            foreach (var path in allPaths)
            {
                if (!path.StartsWith("Assets/")) continue;
                
                string[] dependencies = AssetDatabase.GetDependencies(path, false);
                foreach (var dep in dependencies)
                {
                    if (dep == path) continue;
                    
                    string depGuid = AssetDatabase.AssetPathToGUID(dep);
                    if (string.IsNullOrEmpty(depGuid)) continue;

                    if (referenceCounts.ContainsKey(depGuid))
                        referenceCounts[depGuid]++;
                    else
                        referenceCounts[depGuid] = 1;
                }
            }
            isCacheBuilt = true;
            EditorApplication.RepaintProjectWindow();
        }

        private static void OnProjectWindowItemOnGUI(string guid, Rect selectionRect)
        {
            var settings = ProjectPlusSettings.GetOrCreateSettings();
            if (settings == null) return;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return;

            bool isFolder = AssetDatabase.IsValidFolder(path);

            // 1. Folder Coloring
            if (isFolder && settings.enableFolderColoring)
            {
                string folderName = Path.GetFileName(path);
                FolderRule match = settings.rules.Find(r => r.isActive && r.folderName.Equals(folderName, System.StringComparison.OrdinalIgnoreCase));
                
                if (match != null)
                {
                    DrawColoredFolder(selectionRect, match.folderColor);
                }
            }

            // 2. Asset Size & Usage Badges (Only in list view, for files)
            if (!isFolder && selectionRect.height <= 20f)
            {
                float offsetRight = 0f;

                // Reference / Usage badge on the right
                if (settings.enableReferenceCount)
                {
                    int refCount = 0;
                    if (isCacheBuilt && referenceCounts.TryGetValue(guid, out refCount))
                    {
                        string badgeText = $"{refCount} ref";
                        
                        GUIStyle badgeStyle = new GUIStyle(EditorStyles.miniLabel);
                        badgeStyle.alignment = TextAnchor.MiddleCenter;
                        badgeStyle.normal.textColor = Color.white;
                        
                        // Small dark pill background
                        Rect badgeRect = new Rect(selectionRect.xMax - 65f, selectionRect.y + 2f, 40f, selectionRect.height - 4f);
                        Color pillColor = refCount > 0 ? new Color(0.2f, 0.5f, 0.2f, 0.8f) : new Color(0.6f, 0.2f, 0.2f, 0.8f);
                        EditorGUI.DrawRect(badgeRect, pillColor);
                        
                        GUI.Label(badgeRect, badgeText, badgeStyle);
                        offsetRight = 45f;
                    }
                    else if (isCacheBuilt)
                    {
                        // 0 references
                        Rect badgeRect = new Rect(selectionRect.xMax - 65f, selectionRect.y + 2f, 40f, selectionRect.height - 4f);
                        EditorGUI.DrawRect(badgeRect, new Color(0.4f, 0.4f, 0.4f, 0.4f));
                        GUIStyle badgeStyle = new GUIStyle(EditorStyles.miniLabel)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            normal = { textColor = Color.gray }
                        };
                        GUI.Label(badgeRect, "0 ref", badgeStyle);
                        offsetRight = 45f;
                    }
                }

                // File Size text
                if (settings.enableSizeVisualizer)
                {
                    FileInfo fileInfo = new FileInfo(path);
                    if (fileInfo.Exists)
                    {
                        string sizeText = FormatBytes(fileInfo.Length);
                        
                        GUIStyle sizeStyle = new GUIStyle(EditorStyles.miniLabel)
                        {
                            alignment = TextAnchor.MiddleRight,
                            normal = { textColor = new Color(0.6f, 0.6f, 0.6f, 0.7f) }
                        };
                        
                        Rect sizeRect = new Rect(selectionRect.xMax - 70f - offsetRight, selectionRect.y, 60f, selectionRect.height);
                        GUI.Label(sizeRect, sizeText, sizeStyle);
                    }
                }
            }
        }

        private static void DrawColoredFolder(Rect rect, Color tintColor)
        {
            bool isList = rect.height <= 20f;
            Rect iconRect;
            
            if (isList)
            {
                iconRect = new Rect(rect.x, rect.y, 16, 16);
            }
            else
            {
                // In grid view, icon size depends on rect size
                float iconSize = rect.height - 16f;
                iconRect = new Rect(rect.x + (rect.width - iconSize) / 2, rect.y, iconSize, iconSize);
            }

            // Draw a subtle overlay tint or draw the custom folder icon
            // Loading standard folder icon
            Texture2D folderTex = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;
            if (folderTex != null)
            {
                Color oldColor = GUI.color;
                GUI.color = tintColor;
                GUI.DrawTexture(iconRect, folderTex);
                GUI.color = oldColor;
            }
        }

        private static string FormatBytes(long bytes)
        {
            string[] Suffix = { "B", "KB", "MB", "GB" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < Suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }
            return $"{dblSByte:0.0} {Suffix[i]}";
        }
    }
}
