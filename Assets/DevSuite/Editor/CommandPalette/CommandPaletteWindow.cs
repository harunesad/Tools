using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DevSuite.CommandPalette
{
    public class CommandPaletteWindow : EditorWindow
    {
        private string searchQuery = "";
        private List<PaletteCommand> allCommands = new List<PaletteCommand>();
        private List<PaletteCommand> filteredCommands = new List<PaletteCommand>();
        private int selectedIndex = 0;

        private struct PaletteCommand
        {
            public string name;
            public string description;
            public System.Action action;
        }

        [MenuItem("Tools/DevSuite/Command Palette %&#p")] // Ctrl+Shift+Alt+P (or Ctrl+Shift+P depending on mapping)
        public static void ShowPalette()
        {
            var window = GetWindow<CommandPaletteWindow>(true, "DevSuite Command Palette", true);
            window.minSize = new Vector2(400, 350);
            window.maxSize = new Vector2(400, 350);
            window.CenterOnScreen();
            window.ShowPopup();
        }

        private void CenterOnScreen()
        {
            var main = ResolutionCurrent();
            position = new Rect(main.x + (main.width - 400) / 2, main.y + (main.height - 350) / 2, 400, 350);
        }

        private Rect ResolutionCurrent()
        {
            return new Rect(0, 0, Screen.currentResolution.width, Screen.currentResolution.height);
        }

        private void OnEnable()
        {
            InitializeCommands();
            FilterCommands();
        }

        private void InitializeCommands()
        {
            allCommands.Clear();
            allCommands.Add(new PaletteCommand
            {
                name = "> Group Selected GameObjects",
                description = "Groups all selected GameObjects under a new parent.",
                action = GroupSelected
            });
            allCommands.Add(new PaletteCommand
            {
                name = "> Snap to Floor (Align Down)",
                description = "Snaps selected GameObjects to the surface directly below them.",
                action = SnapSelectedToFloor
            });
            allCommands.Add(new PaletteCommand
            {
                name = "> Batch Rename Selection",
                description = "Sequentially renames all selected GameObjects.",
                action = BatchRenameDialog
            });
            allCommands.Add(new PaletteCommand
            {
                name = "> Clean Empty Folders",
                description = "Scans project folders and deletes empty ones.",
                action = () => ProjectPlus.ProjectPlusUtility.CleanEmptyFolders()
            });
            allCommands.Add(new PaletteCommand
            {
                name = "> Open Project Doctor Diagnostics",
                description = "Runs the diagnostic linter for missing scripts and null events.",
                action = () => ProjectDoctor.ProjectDoctorWindow.ShowWindow()
            });
            allCommands.Add(new PaletteCommand
            {
                name = "> Open Kanban Task Board",
                description = "Opens the scene-wide sticky notes and tasks manager.",
                action = () => SceneNotes.SceneNotesWindow.ShowWindow()
            });
        }

        private void FilterCommands()
        {
            filteredCommands.Clear();
            foreach (var cmd in allCommands)
            {
                if (string.IsNullOrEmpty(searchQuery) || 
                    cmd.name.ToLower().Contains(searchQuery.ToLower()) || 
                    cmd.description.ToLower().Contains(searchQuery.ToLower()))
                {
                    filteredCommands.Add(cmd);
                }
            }
            selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, filteredCommands.Count - 1));
        }

        private void OnGUI()
        {
            // Styling & Header
            Rect bgRect = new Rect(0, 0, position.width, position.height);
            EditorGUI.DrawRect(bgRect, new Color(0.15f, 0.15f, 0.15f, 1f));

            GUILayout.Space(10);
            
            // Search Input
            GUI.SetNextControlName("SearchField");
            searchQuery = EditorGUILayout.TextField("", searchQuery, GUILayout.Height(30));
            GUI.FocusControl("SearchField");

            if (GUI.changed)
            {
                FilterCommands();
            }

            // Keyboard navigation
            Event e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.DownArrow)
                {
                    selectedIndex = (selectedIndex + 1) % Mathf.Max(1, filteredCommands.Count);
                    e.Use();
                }
                else if (e.keyCode == KeyCode.UpArrow)
                {
                    selectedIndex = (selectedIndex - 1 + filteredCommands.Count) % Mathf.Max(1, filteredCommands.Count);
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Return)
                {
                    ExecuteSelected();
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Escape)
                {
                    Close();
                }
            }

            EditorGUILayout.Space(10);

            // Draw filtered commands
            for (int i = 0; i < filteredCommands.Count; i++)
            {
                var cmd = filteredCommands[i];
                bool isSelected = (i == selectedIndex);

                Rect itemRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(40));
                
                // Highlight color
                Color itemBg = isSelected ? new Color(0.2f, 0.45f, 0.7f, 1f) : new Color(0.2f, 0.2f, 0.2f, 0.5f);
                EditorGUI.DrawRect(itemRect, itemBg);

                var titleStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.white } };
                var descStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = isSelected ? Color.white : Color.gray } };

                GUI.Label(new Rect(itemRect.x + 10, itemRect.y + 3, itemRect.width - 20, 18), cmd.name, titleStyle);
                GUI.Label(new Rect(itemRect.x + 10, itemRect.y + 20, itemRect.width - 20, 18), cmd.description, descStyle);

                // Mouse click selection
                if (e.type == EventType.MouseDown && itemRect.Contains(e.mousePosition))
                {
                    selectedIndex = i;
                    ExecuteSelected();
                    e.Use();
                }
            }
        }

        private void ExecuteSelected()
        {
            if (filteredCommands.Count > 0 && selectedIndex < filteredCommands.Count)
            {
                var action = filteredCommands[selectedIndex].action;
                Close();
                action?.Invoke();
            }
        }

        // --- COMMAND MACROS IMPLEMENTATIONS ---

        private void GroupSelected()
        {
            GameObject[] selected = Selection.gameObjects;
            if (selected.Length == 0) return;

            GameObject parent = new GameObject("Group_Parent");
            Undo.RegisterCreatedObjectUndo(parent, "Group GameObjects");

            // Position parent at center of selection
            Vector3 center = Vector3.zero;
            foreach (var go in selected)
            {
                center += go.transform.position;
            }
            parent.transform.position = center / selected.Length;

            foreach (var go in selected)
            {
                Undo.SetTransformParent(go.transform, parent.transform, "Group GameObjects");
            }

            Selection.activeGameObject = parent;
            Debug.Log($"[DevSuite] Grouped {selected.Length} objects under parent.");
        }

        private void SnapSelectedToFloor()
        {
            GameObject[] selected = Selection.gameObjects;
            int snapCount = 0;

            foreach (var go in selected)
            {
                Ray ray = new Ray(go.transform.position + Vector3.up * 0.1f, Vector3.down);
                // Raycast down but ignore own collider if any
                RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
                foreach (var hit in hits)
                {
                    if (hit.transform != go.transform && !hit.transform.IsChildOf(go.transform))
                    {
                        Undo.RecordObject(go.transform, "Snap to Floor");
                        go.transform.position = hit.point;
                        snapCount++;
                        break;
                    }
                }
            }
            Debug.Log($"[DevSuite] Snapped {snapCount} GameObjects to floor.");
        }

        private void BatchRenameDialog()
        {
            // Simple prompt dialog
            // Note: Since standard Unity doesn't have an EditorGUI prompt dialog, we can make a tiny quick rename window
            BatchRenameWindow.ShowWindow();
        }
    }

    public class BatchRenameWindow : EditorWindow
    {
        private string prefix = "ObjectName";
        private int startNumber = 1;

        public static void ShowWindow()
        {
            var window = GetWindow<BatchRenameWindow>(true, "Batch Rename Selection");
            window.minSize = new Vector2(300, 120);
            window.maxSize = new Vector2(300, 120);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            prefix = EditorGUILayout.TextField("Name Prefix", prefix);
            startNumber = EditorGUILayout.IntField("Start Counter", startNumber);
            EditorGUILayout.Space(10);

            if (GUILayout.Button("Rename Selection", GUILayout.Height(30)))
            {
                GameObject[] selected = Selection.gameObjects;
                if (selected.Length > 0)
                {
                    // Sort by hierarchy order or position so sorting is natural
                    System.Array.Sort(selected, (a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
                    
                    int current = startNumber;
                    foreach (var go in selected)
                    {
                        Undo.RecordObject(go, "Batch Rename");
                        go.name = $"{prefix}_{current:D2}";
                        current++;
                    }
                    Debug.Log($"[DevSuite] Renamed {selected.Length} objects with pattern {prefix}_xx.");
                }
                Close();
            }
        }
    }
}
