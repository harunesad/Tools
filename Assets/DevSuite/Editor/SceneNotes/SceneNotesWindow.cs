using UnityEditor;
using UnityEngine;

namespace DevSuite.SceneNotes
{
    public class SceneNotesWindow : EditorWindow
    {
        private Vector2 todoScroll;
        private Vector2 inProgressScroll;
        private Vector2 doneScroll;

        [MenuItem("Tools/DevSuite/Kanban Task Board", false, 20)]
        public static void ShowWindow()
        {
            // Open the unified Dashboard and navigate to Scene Notes tab
            DevSuiteDashboard.ShowWindowAtTab(3);
        }

        private void OnGUI()
        {
            DrawWindowHeader();

            SceneNote[] allNotes = FindObjectsOfType<SceneNote>();

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            // Column 1: TODO
            DrawColumn("TO DO", NoteStatus.Todo, allNotes, ref todoScroll);
            
            // Column 2: IN PROGRESS
            DrawColumn("IN PROGRESS", NoteStatus.InProgress, allNotes, ref inProgressScroll);
            
            // Column 3: DONE
            DrawColumn("DONE", NoteStatus.Done, allNotes, ref doneScroll);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawWindowHeader()
        {
            Rect headerRect = GUILayoutUtility.GetRect(position.width, 35);
            EditorGUI.DrawRect(headerRect, new Color(0.18f, 0.18f, 0.18f, 1f));

            var labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                normal = { textColor = new Color(0.3f, 0.85f, 1f, 1f) }
            };
            GUI.Label(headerRect, "✦ Scene Notes Kanban Board ✦", labelStyle);
        }

        private void DrawColumn(string columnName, NoteStatus targetStatus, SceneNote[] notes, ref Vector2 scrollPos)
        {
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            
            // Column Header
            var colHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField(columnName, colHeaderStyle);
            EditorGUILayout.Space(5);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            int count = 0;
            foreach (var note in notes)
            {
                if (note.status == targetStatus)
                {
                    DrawNoteCard(note);
                    count++;
                }
            }

            if (count == 0)
            {
                var emptyStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.gray }
                };
                EditorGUILayout.LabelField("Empty Column", emptyStyle);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawNoteCard(SceneNote note)
        {
            Rect cardRect = EditorGUILayout.BeginVertical("helpbox", GUILayout.Height(80));
            
            // Draw Importance color strip on the left edge
            Rect colorStrip = new Rect(cardRect.x, cardRect.y, 4f, cardRect.height);
            EditorGUI.DrawRect(colorStrip, note.GetColor());

            EditorGUI.indentLevel++;
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(note.title, EditorStyles.boldLabel);
            
            // Delete note button
            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(16)))
            {
                if (EditorUtility.DisplayDialog("Delete Note?", "Are you sure you want to delete this scene note?", "Yes", "No"))
                {
                    Undo.DestroyObjectImmediate(note.gameObject);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    EditorGUI.indentLevel--;
                    return;
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Display note type and description snippet
            EditorGUILayout.LabelField($"[{note.type.ToString().ToUpper()}] - {note.importance.ToString()} Importance", EditorStyles.miniLabel);
            EditorGUILayout.LabelField(note.description, EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(5);

            // Action Row
            EditorGUILayout.BeginHorizontal();
            
            // 1. Move Backwards
            if (note.status != NoteStatus.Todo)
            {
                if (GUILayout.Button("<", GUILayout.Width(25)))
                {
                    Undo.RecordObject(note, "Move Task Back");
                    note.status = (NoteStatus)((int)note.status - 1);
                    EditorUtility.SetDirty(note);
                }
            }
            
            // 2. Focus Camera (Teleport)
            if (GUILayout.Button("Go To", GUILayout.Height(18)))
            {
                Selection.activeGameObject = note.gameObject;
                if (SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.FrameSelected();
                }
            }

            // 3. Move Forwards
            if (note.status != NoteStatus.Done)
            {
                if (GUILayout.Button(">", GUILayout.Width(25)))
                {
                    Undo.RecordObject(note, "Move Task Forward");
                    note.status = (NoteStatus)((int)note.status + 1);
                    EditorUtility.SetDirty(note);
                }
            }

            EditorGUILayout.EndHorizontal();
            
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
    }
}
