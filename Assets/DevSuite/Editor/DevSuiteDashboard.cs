using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DevSuite
{
    public class DevSuiteDashboard : EditorWindow
    {
        private int activeTab = 0;
        private readonly string[] tabs = { "Overview", "Project Window+", "Hierarchy Organizer", "Scene Notes", "Project Doctor", "Command Palette" };
        private Vector2 scrollPos;

        // Scene Notes Kanban variables
        private Vector2 todoScroll;
        private Vector2 inProgressScroll;
        private Vector2 doneScroll;

        // Project Doctor variables
        private List<ProjectDoctor.ProjectDoctorWindow.DoctorIssue> foundIssues = new List<ProjectDoctor.ProjectDoctorWindow.DoctorIssue>();
        private Vector2 doctorScroll;

        [MenuItem("Tools/DevSuite/Dashboard", false, 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<DevSuiteDashboard>("DevSuite Dashboard");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        [MenuItem("Tools/DevSuite/Project Window+", false, 10)]
        public static void ShowProjectWindowPlus()
        {
            ShowWindowAtTab(1);
        }

        [MenuItem("Tools/DevSuite/Hierarchy Organizer", false, 11)]
        public static void ShowHierarchyOrganizer()
        {
            ShowWindowAtTab(2);
        }

        public static void ShowWindowAtTab(int tabIndex)
        {
            var window = GetWindow<DevSuiteDashboard>("DevSuite Dashboard");
            window.minSize = new Vector2(500, 600);
            window.activeTab = tabIndex;
            window.Show();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawTabBar();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            EditorGUILayout.Space(10);

            switch (activeTab)
            {
                case 0:
                    DrawOverview();
                    break;
                case 1:
                    DrawProjectPlusOverview();
                    break;
                case 2:
                    DrawHierarchyOrganizerOverview();
                    break;
                case 3:
                    DrawSceneNotesOverview();
                    break;
                case 4:
                    DrawProjectDoctorOverview();
                    break;
                case 5:
                    DrawCommandPaletteOverview();
                    break;
            }

            EditorGUILayout.EndScrollView();
            DrawFooter();
        }

        private void DrawHeader()
        {
            // Sleek dark title bar area
            Rect headerRect = GUILayoutUtility.GetRect(position.width, 60);
            EditorGUI.DrawRect(headerRect, new Color(0.15f, 0.15f, 0.15f, 1f));

            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.3f, 0.85f, 1f, 1f) }
            };

            var subStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.gray }
            };

            GUI.Label(new Rect(headerRect.x + 15, headerRect.y + 10, headerRect.width - 30, 25), "✦ DevSuite Dashboard", titleStyle);
            GUI.Label(new Rect(headerRect.x + 15, headerRect.y + 35, headerRect.width - 30, 20), "v1.0.0 - Ultimate Unity Workflow Toolbox", subStyle);
        }

        private void DrawTabBar()
        {
            activeTab = GUILayout.Toolbar(activeTab, tabs, GUILayout.Height(25));
        }

        private void DrawOverview()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Welcome to DevSuite!", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("DevSuite is a unified package of 5 high-productivity tools designed to streamline your Unity development workflow.", MessageType.Info);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("Module Status", EditorStyles.boldLabel);

            DrawModuleCard("Project Window+", "Customizes folders, visualizes file sizes and reference counts directly in Project View.", true);
            DrawModuleCard("Hierarchy Organizer", "Customize colors, fonts, backgrounds, and active toggles for GameObjects in the Hierarchy view.", true);
            DrawModuleCard("Scene Notes & Kanban", "Leave 3D sticky notes in Scene View and organize them in a visual Kanban Board.", true);
            DrawModuleCard("Project Doctor", "Finds missing references, unassigned UI events, and provides one-click Auto-Fixes.", true);
            DrawModuleCard("Command Palette", "Global search & macro execution overlay triggered via Ctrl+Shift+P.", true);
        }

        private void DrawModuleCard(string moduleName, string description, bool isActive)
        {
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.BeginHorizontal();
            
            var statusStyle = new GUIStyle(EditorStyles.boldLabel);
            statusStyle.normal.textColor = isActive ? Color.green : Color.red;
            
            EditorGUILayout.LabelField(moduleName, EditorStyles.boldLabel, GUILayout.Width(180));
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(isActive ? "● Active" : "○ Inactive", statusStyle, GUILayout.Width(70));
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(description, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawProjectPlusOverview()
        {
            var settings = ProjectPlus.ProjectPlusSettings.GetOrCreateSettings();
            if (settings == null) return;

            EditorGUILayout.LabelField("Project Window+ Configurations", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Customize folder colors, show sizes, and count asset usage references.", MessageType.Info);
            
            EditorGUILayout.Space(5);
            EditorGUI.BeginChangeCheck();

            settings.enableFolderColoring = EditorGUILayout.Toggle("Enable Folder Coloring", settings.enableFolderColoring);
            settings.enableSizeVisualizer = EditorGUILayout.Toggle("Enable Size Visualizer", settings.enableSizeVisualizer);
            settings.enableReferenceCount = EditorGUILayout.Toggle("Enable Reference Badges", settings.enableReferenceCount);

            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Folder Color Rules", EditorStyles.boldLabel);
            if (GUILayout.Button("Add Rule", GUILayout.Width(80)))
            {
                settings.rules.Add(new ProjectPlus.FolderRule());
                EditorUtility.SetDirty(settings);
            }
            if (GUILayout.Button("Force Rebuild Ref Cache", GUILayout.Width(150)))
            {
                ProjectPlus.ProjectPlusRenderer.RebuildReferenceCache();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            for (int i = 0; i < settings.rules.Count; i++)
            {
                var rule = settings.rules[i];
                EditorGUILayout.BeginVertical("helpbox");
                
                EditorGUILayout.BeginHorizontal();
                rule.isActive = EditorGUILayout.Toggle(rule.isActive, GUILayout.Width(20));
                rule.ruleName = EditorGUILayout.TextField(rule.ruleName, EditorStyles.boldLabel);
                
                GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                if (GUILayout.Button("Delete", GUILayout.Width(50)))
                {
                    settings.rules.RemoveAt(i);
                    EditorUtility.SetDirty(settings);
                    i--;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    continue;
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                if (rule.isActive)
                {
                    rule.folderName = EditorGUILayout.TextField("Folder Name", rule.folderName);
                    rule.folderColor = EditorGUILayout.ColorField("Folder Icon Color", rule.folderColor);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(settings);
                EditorApplication.RepaintProjectWindow();
            }

            EditorGUILayout.Space(15);
            if (GUILayout.Button("Clean Empty Folders Now", GUILayout.Height(30)))
            {
                ProjectPlus.ProjectPlusUtility.CleanEmptyFolders();
            }
        }

        private void DrawSceneNotesOverview()
        {
            EditorGUILayout.LabelField("Scene Notes & Tasks Kanban Board", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Press [Shift + N] in the Scene View to drop a 3D sticky note. Manage them below:", MessageType.Info);
            
            EditorGUILayout.Space(5);

            SceneNote[] allNotes = FindObjectsOfType<SceneNote>();
            
            EditorGUILayout.BeginHorizontal();
            DrawKanbanColumn("TO DO", NoteStatus.Todo, allNotes, ref todoScroll);
            DrawKanbanColumn("IN PROGRESS", NoteStatus.InProgress, allNotes, ref inProgressScroll);
            DrawKanbanColumn("DONE", NoteStatus.Done, allNotes, ref doneScroll);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawKanbanColumn(string name, NoteStatus status, SceneNote[] notes, ref Vector2 scroll)
        {
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.MinHeight(300));
            EditorGUILayout.LabelField(name, EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(250));
            int count = 0;
            foreach (var note in notes)
            {
                if (note.status == status)
                {
                    DrawKanbanCard(note);
                    count++;
                }
            }
            if (count == 0)
            {
                GUI.contentColor = Color.gray;
                EditorGUILayout.LabelField("No Tasks", EditorStyles.miniLabel);
                GUI.contentColor = Color.white;
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawKanbanCard(SceneNote note)
        {
            Rect cardRect = EditorGUILayout.BeginVertical("helpbox", GUILayout.Height(85));
            Rect colorStrip = new Rect(cardRect.x, cardRect.y, 4f, cardRect.height);
            EditorGUI.DrawRect(colorStrip, note.GetColor());

            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(note.title, EditorStyles.boldLabel);
            if (GUILayout.Button("X", GUILayout.Width(18), GUILayout.Height(15)))
            {
                if (EditorUtility.DisplayDialog("Delete Note?", "Delete this scene note?", "Yes", "No"))
                {
                    Undo.DestroyObjectImmediate(note.gameObject);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"[{note.type}] - {note.importance}", EditorStyles.miniLabel);
            
            string desc = note.description.Length > 40 ? note.description.Substring(0, 37) + "..." : note.description;
            EditorGUILayout.LabelField(desc, EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            if (note.status != NoteStatus.Todo)
            {
                if (GUILayout.Button("<", GUILayout.Width(20)))
                {
                    Undo.RecordObject(note, "Move Task Back");
                    note.status = (NoteStatus)((int)note.status - 1);
                    EditorUtility.SetDirty(note);
                }
            }
            if (GUILayout.Button("Go To", GUILayout.Height(16)))
            {
                Selection.activeGameObject = note.gameObject;
                if (SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.FrameSelected();
                }
            }
            if (note.status != NoteStatus.Done)
            {
                if (GUILayout.Button(">", GUILayout.Width(20)))
                {
                    Undo.RecordObject(note, "Move Task Forward");
                    note.status = (NoteStatus)((int)note.status + 1);
                    EditorUtility.SetDirty(note);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        private void DrawProjectDoctorOverview()
        {
            EditorGUILayout.LabelField("Project Doctor Diagnostics", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Automatically scans for Missing script references and unassigned Button onClick events.", MessageType.Info);
            
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Scan Scene Now", GUILayout.Height(30)))
            {
                ScanDoctorScene();
            }
            if (foundIssues.Count > 0 && GUILayout.Button("Fix All Auto-Fixable", GUILayout.Height(30)))
            {
                FixAllDoctorIssues();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField($"Scan Results ({foundIssues.Count} issues found)", EditorStyles.boldLabel);

            doctorScroll = EditorGUILayout.BeginScrollView(doctorScroll, GUILayout.MinHeight(250));
            if (foundIssues.Count == 0)
            {
                EditorGUILayout.HelpBox("No issues found. Clean bill of health!", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < foundIssues.Count; i++)
                {
                    var issue = foundIssues[i];
                    if (issue.gameObject == null) continue;

                    EditorGUILayout.BeginVertical("helpbox");
                    EditorGUILayout.BeginHorizontal();
                    
                    string tag = issue.type == ProjectDoctor.ProjectDoctorWindow.IssueType.MissingScript ? "[Missing Script]" : "[Empty Event]";
                    GUI.contentColor = issue.type == ProjectDoctor.ProjectDoctorWindow.IssueType.MissingScript ? Color.red : Color.yellow;
                    EditorGUILayout.LabelField(tag, EditorStyles.boldLabel, GUILayout.Width(110));
                    GUI.contentColor = Color.white;

                    EditorGUILayout.LabelField($"{issue.gameObject.name}: {issue.description}", EditorStyles.wordWrappedLabel);

                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        Selection.activeGameObject = issue.gameObject;
                    }
                    if (GUILayout.Button("Fix", GUILayout.Width(50)))
                    {
                        FixDoctorIssue(issue);
                        foundIssues.RemoveAt(i);
                        i--;
                    }

                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void ScanDoctorScene()
        {
            foundIssues.Clear();
            GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in rootObjects)
            {
                ScanDoctorGameObjectRecursive(root);
            }
        }

        private void ScanDoctorGameObjectRecursive(GameObject go)
        {
            Component[] components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null)
                {
                    foundIssues.Add(new ProjectDoctor.ProjectDoctorWindow.DoctorIssue
                    {
                        description = "GameObject contains a missing/null script component.",
                        gameObject = go,
                        type = ProjectDoctor.ProjectDoctorWindow.IssueType.MissingScript
                    });
                    break;
                }
            }

            UnityEngine.UI.Button button = go.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                var onClick = button.onClick;
                for (int i = 0; i < onClick.GetPersistentEventCount(); i++)
                {
                    Object target = onClick.GetPersistentTarget(i);
                    string methodName = onClick.GetPersistentMethodName(i);
                    if (target == null || string.IsNullOrEmpty(methodName))
                    {
                        foundIssues.Add(new ProjectDoctor.ProjectDoctorWindow.DoctorIssue
                        {
                            description = $"UI Button contains unassigned onClick listener (Index {i}).",
                            gameObject = go,
                            type = ProjectDoctor.ProjectDoctorWindow.IssueType.EmptyButtonEvent,
                            eventIndex = i
                        });
                    }
                }
            }

            for (int i = 0; i < go.transform.childCount; i++)
            {
                ScanDoctorGameObjectRecursive(go.transform.GetChild(i).gameObject);
            }
        }

        private void FixDoctorIssue(ProjectDoctor.ProjectDoctorWindow.DoctorIssue issue)
        {
            if (issue.gameObject == null) return;
            Undo.RegisterCompleteObjectUndo(issue.gameObject, "Project Doctor Auto-Fix");

            if (issue.type == ProjectDoctor.ProjectDoctorWindow.IssueType.MissingScript)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(issue.gameObject);
            }
            else if (issue.type == ProjectDoctor.ProjectDoctorWindow.IssueType.EmptyButtonEvent)
            {
                var btn = issue.gameObject.GetComponent<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    SerializedObject so = new SerializedObject(btn);
                    SerializedProperty calls = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
                    if (calls != null && issue.eventIndex < calls.arraySize)
                    {
                        calls.DeleteArrayElementAtIndex(issue.eventIndex);
                        so.ApplyModifiedProperties();
                    }
                }
            }
            EditorUtility.SetDirty(issue.gameObject);
        }

        private void FixAllDoctorIssues()
        {
            for (int i = foundIssues.Count - 1; i >= 0; i--)
            {
                FixDoctorIssue(foundIssues[i]);
            }
            foundIssues.Clear();
        }

        private void DrawHierarchyOrganizerOverview()
        {
            var settings = Hierarchy.HierarchyOrganizerSettings.GetOrCreateSettings();
            if (settings == null) return;

            EditorGUILayout.LabelField("Hierarchy Organizer Configurations", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Customize fonts, colors, full-width headers and active toggles for hierarchy items.", MessageType.Info);
            
            EditorGUILayout.Space(5);
            EditorGUI.BeginChangeCheck();

            settings.enableCustomStyling = EditorGUILayout.Toggle("Enable Custom Styling", settings.enableCustomStyling);
            settings.enableActiveToggle = EditorGUILayout.Toggle("Enable Active State Toggle", settings.enableActiveToggle);

            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Hierarchy Styling Rules", EditorStyles.boldLabel);
            if (GUILayout.Button("Add Rule", GUILayout.Width(80)))
            {
                settings.rules.Add(new Hierarchy.HierarchyRule());
                EditorUtility.SetDirty(settings);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            for (int i = 0; i < settings.rules.Count; i++)
            {
                var rule = settings.rules[i];
                EditorGUILayout.BeginVertical("helpbox");
                
                EditorGUILayout.BeginHorizontal();
                rule.isActive = EditorGUILayout.Toggle(rule.isActive, GUILayout.Width(20));
                rule.name = EditorGUILayout.TextField(rule.name, EditorStyles.boldLabel);
                
                GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                if (GUILayout.Button("Delete", GUILayout.Width(50)))
                {
                    settings.rules.RemoveAt(i);
                    EditorUtility.SetDirty(settings);
                    i--;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    continue;
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                if (rule.isActive)
                {
                    EditorGUI.indentLevel++;
                    
                    EditorGUILayout.BeginHorizontal();
                    rule.matchType = (Hierarchy.MatchType)EditorGUILayout.EnumPopup("Match Type", rule.matchType);
                    rule.searchString = EditorGUILayout.TextField("Search Term", rule.searchString);
                    EditorGUILayout.EndHorizontal();

                    rule.isHeader = EditorGUILayout.Toggle("Is Full Width Header", rule.isHeader);

                    EditorGUILayout.LabelField("Text Style", EditorStyles.miniBoldLabel);
                    EditorGUILayout.BeginHorizontal();
                    rule.textColor = EditorGUILayout.ColorField("Color", rule.textColor);
                    rule.fontSize = EditorGUILayout.IntField("Size", rule.fontSize);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    rule.fontStyle = (FontStyle)EditorGUILayout.EnumPopup("Font Style", rule.fontStyle);
                    rule.textAlignment = (TextAnchor)EditorGUILayout.EnumPopup("Alignment", rule.textAlignment);
                    EditorGUILayout.EndHorizontal();

                    rule.drawBackground = EditorGUILayout.Toggle("Draw Background", rule.drawBackground);
                    if (rule.drawBackground)
                    {
                        rule.backgroundColor = EditorGUILayout.ColorField("Bg Color", rule.backgroundColor);
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(settings);
                EditorApplication.RepaintHierarchyWindow();
            }
        }

        private void DrawCommandPaletteOverview()
        {
            EditorGUILayout.LabelField("Command Palette & Shortcuts", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Trigger the floating command bar with the global shortcut:\n[Ctrl + Shift + Alt + P]", MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("Open Command Palette Popup", GUILayout.Height(40)))
            {
                CommandPalette.CommandPaletteWindow.ShowPalette();
            }
        }

        private void DrawFooter()
        {
            GUILayout.FlexibleSpace();
            Rect footerRect = GUILayoutUtility.GetRect(position.width, 25);
            EditorGUI.DrawRect(footerRect, new Color(0.12f, 0.12f, 0.12f, 1f));
            
            var footerStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.gray }
            };
            GUI.Label(footerRect, "Developed for Unity Developers with ❤", footerStyle);
        }
    }
}
