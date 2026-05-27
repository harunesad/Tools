using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DevSuite.ProjectDoctor
{
    public class ProjectDoctorWindow : EditorWindow
    {
        private List<DoctorIssue> foundIssues = new List<DoctorIssue>();
        private Vector2 scrollPos;

        public enum IssueType
        {
            MissingScript,
            EmptyButtonEvent
        }

        public class DoctorIssue
        {
            public string description;
            public GameObject gameObject;
            public IssueType type;
            public int eventIndex = -1; // Specific to UI events
        }

        [MenuItem("Tools/DevSuite/Project Doctor", false, 30)]
        public static void ShowWindow()
        {
            // Open the unified Dashboard and navigate to the Doctor tab
            DevSuiteDashboard.ShowWindowAtTab(4);
        }

        private void OnGUI()
        {
            DrawHeader();

            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Scan Active Scene", GUILayout.Height(30)))
            {
                ScanScene();
            }
            if (foundIssues.Count > 0 && GUILayout.Button("Fix All Auto-Fixable", GUILayout.Height(30)))
            {
                FixAllIssues();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField($"Scan Results ({foundIssues.Count} issues found)", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            if (foundIssues.Count == 0)
            {
                EditorGUILayout.HelpBox("Clean bill of health! No critical issues found.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < foundIssues.Count; i++)
                {
                    var issue = foundIssues[i];
                    if (issue.gameObject == null) continue;

                    EditorGUILayout.BeginVertical("helpbox");
                    EditorGUILayout.BeginHorizontal();
                    
                    // Display icon or tag
                    string tag = issue.type == IssueType.MissingScript ? "[Missing Script]" : "[Empty Event]";
                    GUI.contentColor = issue.type == IssueType.MissingScript ? Color.red : Color.yellow;
                    EditorGUILayout.LabelField(tag, EditorStyles.boldLabel, GUILayout.Width(110));
                    GUI.contentColor = Color.white;

                    EditorGUILayout.LabelField($"{issue.gameObject.name}: {issue.description}", EditorStyles.wordWrappedLabel);

                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        Selection.activeGameObject = issue.gameObject;
                    }
                    if (GUILayout.Button("Auto-Fix", GUILayout.Width(70)))
                    {
                        FixIssue(issue);
                        foundIssues.RemoveAt(i);
                        i--;
                    }

                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            Rect headerRect = GUILayoutUtility.GetRect(position.width, 35);
            EditorGUI.DrawRect(headerRect, new Color(0.18f, 0.18f, 0.18f, 1f));

            var labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                normal = { textColor = new Color(0.3f, 0.85f, 1f, 1f) }
            };
            GUI.Label(headerRect, "✦ Project Doctor - Diagnostic Console ✦", labelStyle);
        }

        private void ScanScene()
        {
            foundIssues.Clear();
            GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

            foreach (var root in rootObjects)
            {
                ScanGameObjectRecursive(root);
            }
        }

        private void ScanGameObjectRecursive(GameObject go)
        {
            // 1. Check for Missing Scripts
            Component[] components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null)
                {
                    foundIssues.Add(new DoctorIssue
                    {
                        description = "GameObject contains a missing/null script component.",
                        gameObject = go,
                        type = IssueType.MissingScript
                    });
                    break; // Just report one missing script per GameObject to avoid spam
                }
            }

            // 2. Check for empty Button Click Events
            Button button = go.GetComponent<Button>();
            if (button != null)
            {
                var onClick = button.onClick;
                for (int i = 0; i < onClick.GetPersistentEventCount(); i++)
                {
                    Object target = onClick.GetPersistentTarget(i);
                    string methodName = onClick.GetPersistentMethodName(i);

                    if (target == null || string.IsNullOrEmpty(methodName))
                    {
                        foundIssues.Add(new DoctorIssue
                        {
                            description = $"UI Button contains unassigned onClick listener (Index {i}).",
                            gameObject = go,
                            type = IssueType.EmptyButtonEvent,
                            eventIndex = i
                        });
                    }
                }
            }

            // Recurse children
            for (int i = 0; i < go.transform.childCount; i++)
            {
                ScanGameObjectRecursive(go.transform.GetChild(i).gameObject);
            }
        }

        private void FixIssue(DoctorIssue issue)
        {
            if (issue.gameObject == null) return;

            Undo.RegisterCompleteObjectUndo(issue.gameObject, "Project Doctor Auto-Fix");

            if (issue.type == IssueType.MissingScript)
            {
                int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(issue.gameObject);
                Debug.Log($"[DevSuite Doctor] Removed {count} missing scripts from {issue.gameObject.name}.");
            }
            else if (issue.type == IssueType.EmptyButtonEvent)
            {
                Button btn = issue.gameObject.GetComponent<Button>();
                if (btn != null)
                {
                    SerializedObject so = new SerializedObject(btn);
                    SerializedProperty calls = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
                    if (calls != null && issue.eventIndex < calls.arraySize)
                    {
                        calls.DeleteArrayElementAtIndex(issue.eventIndex);
                        so.ApplyModifiedProperties();
                        Debug.Log($"[DevSuite Doctor] Removed empty UI Click event listener on {issue.gameObject.name}.");
                    }
                }
            }
            EditorUtility.SetDirty(issue.gameObject);
        }

        private void FixAllIssues()
        {
            // Reverse loop because index/order might change during removal
            for (int i = foundIssues.Count - 1; i >= 0; i--)
            {
                FixIssue(foundIssues[i]);
            }
            foundIssues.Clear();
            EditorUtility.DisplayDialog("DevSuite Doctor", "All fixable issues have been repaired!", "OK");
        }
    }
}
