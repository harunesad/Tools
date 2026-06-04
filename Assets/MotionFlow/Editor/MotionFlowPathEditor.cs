using UnityEditor;
using UnityEngine;
using MotionFlow.Runtime;

namespace MotionFlow.Editor
{
    [CustomEditor(typeof(MotionFlowPath))]
    public class MotionFlowPathEditor : UnityEditor.Editor
    {
        private MotionFlowPath _path;

        private void OnEnable()
        {
            _path = (MotionFlowPath)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            GUILayout.Space(10);
            if (GUILayout.Button("Add Waypoint Node", GUILayout.Height(30)))
            {
                Undo.RecordObject(_path, "Add Path Waypoint");
                Vector3 newPos = Vector3.zero;
                if (_path.Waypoints.Count > 0)
                {
                    newPos = _path.Waypoints[_path.Waypoints.Count - 1].Position + new Vector3(3, 0, 3);
                }
                _path.Waypoints.Add(new Waypoint(newPos));
                EditorUtility.SetDirty(_path);
            }

            if (_path.Waypoints.Count > 0 && GUILayout.Button("Clear All Nodes"))
            {
                if (EditorUtility.DisplayDialog("Clear Nodes", "Delete all waypoints in this spline?", "Yes", "Cancel"))
                {
                    Undo.RecordObject(_path, "Clear Spline Nodes");
                    _path.Waypoints.Clear();
                    EditorUtility.SetDirty(_path);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            if (_path == null || _path.Waypoints == null) return;

            for (int i = 0; i < _path.Waypoints.Count; i++)
            {
                var wp = _path.Waypoints[i];
                Vector3 worldPos = _path.transform.TransformPoint(wp.Position);

                // Draw node label
                Handles.Label(worldPos + Vector3.up * 0.4f, $"Node {i}", EditorStyles.boldLabel);

                // Point handle
                EditorGUI.BeginChangeCheck();
                Vector3 newWorldPos = Handles.PositionHandle(worldPos, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_path, $"Move Path Node {i}");
                    wp.Position = _path.transform.InverseTransformPoint(newWorldPos);
                    EditorUtility.SetDirty(_path);
                }

                // Tangent out handle
                Vector3 tOutWorld = _path.transform.TransformPoint(wp.Position + wp.TangentOut);
                Handles.color = Color.cyan;
                Handles.DrawLine(worldPos, tOutWorld);
                EditorGUI.BeginChangeCheck();
                Vector3 newTOut = Handles.FreeMoveHandle(tOutWorld, 0.08f, Vector3.zero, Handles.SphereHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_path, $"Modify Tangent Out {i}");
                    wp.TangentOut = _path.transform.InverseTransformPoint(newTOut) - wp.Position;
                    // Mirror Inward Tangent to maintain continuity
                    wp.TangentIn = -wp.TangentOut;
                    EditorUtility.SetDirty(_path);
                }

                // Tangent in handle
                Vector3 tInWorld = _path.transform.TransformPoint(wp.Position + wp.TangentIn);
                Handles.color = Color.red;
                Handles.DrawLine(worldPos, tInWorld);
                EditorGUI.BeginChangeCheck();
                Vector3 newTIn = Handles.FreeMoveHandle(tInWorld, 0.08f, Vector3.zero, Handles.SphereHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_path, $"Modify Tangent In {i}");
                    wp.TangentIn = _path.transform.InverseTransformPoint(newTIn) - wp.Position;
                    // Mirror Outward Tangent
                    wp.TangentOut = -wp.TangentIn;
                    EditorUtility.SetDirty(_path);
                }
            }
        }
    }
}
