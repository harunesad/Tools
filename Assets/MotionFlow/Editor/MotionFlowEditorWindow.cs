using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using MotionFlow.Runtime;

namespace MotionFlow.Editor
{
    public class MotionFlowEditorWindow : EditorWindow
    {
        private MotionFlowAsset _selectedAsset;
        private MotionFlowSequence _selectedSequence;
        private int _selectedSequenceIndex = -1;

        private Vector2 _sidebarScroll = Vector2.zero;
        private Vector2 _mainScroll = Vector2.zero;

        private double _lastUpdateTime;

        [MenuItem("Tools/MotionFlow/Animation Sequencer")]
        public static void OpenWindow()
        {
            var window = GetWindow<MotionFlowEditorWindow>("MotionFlow Sequencer");
            window.minSize = new Vector2(950, 550);
            window.Show();
        }

        private void OnEnable()
        {
            _lastUpdateTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += EditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= EditorUpdate;
        }

        private void EditorUpdate()
        {
            double currentTime = EditorApplication.timeSinceStartup;
            float deltaTime = (float)(currentTime - _lastUpdateTime);
            _lastUpdateTime = currentTime;

            if (MotionFlowEngine.IsTweening)
            {
                MotionFlowEngine.UpdateTweens(deltaTime);
                SceneView.RepaintAll();
                Repaint();
            }
            else
            {
                MotionFlowEngine.UpdateTweens(deltaTime);
            }
        }

        private void OnGUI()
        {
            DrawHeader();

            if (_selectedAsset == null)
            {
                DrawAssetSelectionNotice();
                return;
            }

            EditorGUILayout.BeginHorizontal();

            // 1. Sidebar (Sequences list)
            DrawSidebar();

            DrawVerticalDivider();

            // 2. Main Workspace (Tracks, steps, and timeline tracks)
            DrawMainWorkspace();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(30));
            GUILayout.Label("MotionFlow Design Workspace", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            _selectedAsset = (MotionFlowAsset)EditorGUILayout.ObjectField(
                "", 
                _selectedAsset, 
                typeof(MotionFlowAsset), 
                false, 
                GUILayout.Width(250)
            );

            if (_selectedAsset == null)
            {
                if (GUILayout.Button("Create New Asset", EditorStyles.toolbarButton, GUILayout.Width(120)))
                {
                    CreateNewAsset();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawAssetSelectionNotice()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginVertical("box", GUILayout.Width(500));
            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            GUILayout.Label("Welcome to MotionFlow!", titleStyle);
            GUILayout.Space(10);
            GUILayout.Label("Design fluid movements for Transforms, UI, Lights, Materials, and Audio. Edit paths inside SceneView, sequence complex timing, and compile directly into C# updates.", EditorStyles.wordWrappedLabel);
            GUILayout.Space(20);

            if (GUILayout.Button("Create Animation Asset Database", GUILayout.Height(35)))
            {
                CreateNewAsset();
            }
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        private void DrawSidebar()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(250), GUILayout.ExpandHeight(true));
            
            EditorGUILayout.BeginHorizontal("box");
            GUILayout.Label("Sequences", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ Add Sequence", EditorStyles.miniButton, GUILayout.Width(90)))
            {
                AddNewSequence();
            }
            EditorGUILayout.EndHorizontal();

            _sidebarScroll = EditorGUILayout.BeginScrollView(_sidebarScroll);

            if (_selectedAsset.Sequences == null || _selectedAsset.Sequences.Count == 0)
            {
                GUILayout.Label("No sequences created.", EditorStyles.miniLabel);
            }
            else
            {
                for (int i = 0; i < _selectedAsset.Sequences.Count; i++)
                {
                    var seq = _selectedAsset.Sequences[i];
                    bool isSelected = (_selectedSequenceIndex == i);
                    var style = new GUIStyle(GUI.skin.button)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal
                    };

                    if (isSelected) GUI.backgroundColor = new Color(0.12f, 0.58f, 0.95f, 1f);

                    if (GUILayout.Button($"🎬 {seq.SequenceName}", style, GUILayout.Height(28)))
                    {
                        _selectedSequenceIndex = i;
                        _selectedSequence = seq;
                        GUI.FocusControl(null);
                    }

                    GUI.backgroundColor = Color.white;
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawMainWorkspace()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (_selectedSequence == null)
            {
                GUILayout.FlexibleSpace();
                var centerStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Italic };
                GUILayout.Label("Select a sequence in the left sidebar to start designing.", centerStyle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUI.BeginChangeCheck();

            _mainScroll = EditorGUILayout.BeginScrollView(_mainScroll);

            // Section 1: Sequence Name
            EditorGUILayout.BeginHorizontal("box");
            _selectedSequence.SequenceName = EditorGUILayout.TextField("Sequence Name", _selectedSequence.SequenceName);
            GUILayout.FlexibleSpace();
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("▶ Test Play In Editor", GUILayout.Width(150), GUILayout.Height(25)))
            {
                // Force save asset before previewing to ensure it runs with latest values
                EditorUtility.SetDirty(_selectedAsset);
                AssetDatabase.SaveAssets();
                PlaySequencePreview();
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("⚙ Compile to C#", GUILayout.Width(130), GUILayout.Height(25)))
            {
                ExportToCSharp();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(15);

            // Section 2: Tracks Editor
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Animation Tracks Timeline", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ Add Animation Track", EditorStyles.miniButton, GUILayout.Width(140)))
            {
                AddNewTrack();
            }
            EditorGUILayout.EndHorizontal();

            int trackToDelete = -1;
            int trackIndexForStepDelete = -1;
            int stepToDelete = -1;

            if (_selectedSequence.Tracks.Count == 0)
            {
                GUILayout.Label("This sequence contains no animation tracks. Add a track to targets an object.", EditorStyles.wordWrappedLabel);
            }
            else
            {
                for (int i = 0; i < _selectedSequence.Tracks.Count; i++)
                {
                    DrawTrackBox(i, ref trackToDelete, ref trackIndexForStepDelete, ref stepToDelete);
                }
            }

            if (trackToDelete != -1)
            {
                _selectedSequence.Tracks.RemoveAt(trackToDelete);
                GUI.changed = true;
            }
            else if (trackIndexForStepDelete != -1 && stepToDelete != -1)
            {
                _selectedSequence.Tracks[trackIndexForStepDelete].Steps.RemoveAt(stepToDelete);
                GUI.changed = true;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck() || GUI.changed)
            {
                EditorUtility.SetDirty(_selectedAsset);
                AssetDatabase.SaveAssets();
            }
        }

        private void DrawTrackBox(int trackIndex, ref int trackToDelete, ref int trackIndexForStepDelete, ref int stepToDelete)
        {
            var track = _selectedSequence.Tracks[trackIndex];

            EditorGUILayout.BeginVertical("box");
            
            // Header row
            EditorGUILayout.BeginHorizontal();
            
            // Track Delete button
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                trackToDelete = trackIndex;
            }

            track.TrackName = EditorGUILayout.TextField("", track.TrackName, GUILayout.Width(150));
            track.TargetType = (MotionTargetType)EditorGUILayout.EnumPopup("", track.TargetType, GUILayout.Width(110));
            track.Resolver = (TargetResolverMode)EditorGUILayout.EnumPopup("", track.Resolver, GUILayout.Width(120));
            
            track.ResolverQuery = EditorGUILayout.TextField("Target Match Query", track.ResolverQuery);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("+ Add Step", EditorStyles.miniButton, GUILayout.Width(70)))
            {
                track.Steps.Add(new MotionFlowStep());
            }

            EditorGUILayout.EndHorizontal();

            // Draw steps within this track
            if (track.Steps.Count > 0)
            {
                GUILayout.Space(5);
                EditorGUILayout.BeginVertical("helpBox");
                for (int s = 0; s < track.Steps.Count; s++)
                {
                    DrawStepRow(track, trackIndex, s, ref trackIndexForStepDelete, ref stepToDelete);
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
        }

        private void DrawStepRow(MotionFlowTrack track, int trackIndex, int stepIndex, ref int trackIndexForStepDelete, ref int stepToDelete)
        {
            var step = track.Steps[stepIndex];

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                trackIndexForStepDelete = trackIndex;
                stepToDelete = stepIndex;
            }

            // Path spline movement checkbox
            step.IsPathMovement = EditorGUILayout.ToggleLeft("Path Spline", step.IsPathMovement, GUILayout.Width(90));

            if (step.IsPathMovement)
            {
                step.TargetPathName = EditorGUILayout.TextField("Path Spline Name", step.TargetPathName, GUILayout.Width(220));
                step.Easing = (MotionFlowEase)EditorGUILayout.EnumPopup(step.Easing, GUILayout.Width(100));
                GUILayout.Label("Dur", GUILayout.Width(25));
                step.Duration = EditorGUILayout.FloatField(step.Duration, GUILayout.Width(40));
                GUILayout.Label("Dly", GUILayout.Width(25));
                step.Delay = EditorGUILayout.FloatField(step.Delay, GUILayout.Width(40));
            }
            else
            {
                // Property selector
                step.Property = (MotionProperty)EditorGUILayout.EnumPopup(step.Property, GUILayout.Width(100));

                // Value Types mapping
                EditorGUILayout.LabelField("Start/End", GUILayout.Width(60));
                if (step.Property == MotionProperty.Position || step.Property == MotionProperty.LocalPosition || step.Property == MotionProperty.Rotation || step.Property == MotionProperty.LocalRotation || step.Property == MotionProperty.Scale || step.Property == MotionProperty.AnchoredPosition || step.Property == MotionProperty.SizeDelta)
                {
                    step.IsVector = true; step.IsColor = false; step.IsFloat = false;
                    step.StartVec = EditorGUILayout.Vector3Field("", step.StartVec, GUILayout.Width(120));
                    step.EndVec = EditorGUILayout.Vector3Field("", step.EndVec, GUILayout.Width(120));
                }
                else if (step.Property == MotionProperty.Color || step.Property == MotionProperty.UIBackgroundColor)
                {
                    step.IsVector = false; step.IsColor = true; step.IsFloat = false;
                    step.StartColor = EditorGUILayout.ColorField(step.StartColor, GUILayout.Width(60));
                    step.EndColor = EditorGUILayout.ColorField(step.EndColor, GUILayout.Width(60));
                    step.ShaderVarName = EditorGUILayout.TextField("Shader Property", step.ShaderVarName, GUILayout.Width(100));
                }
                else
                {
                    step.IsVector = false; step.IsColor = false; step.IsFloat = true;
                    step.StartFloat = EditorGUILayout.FloatField(step.StartFloat, GUILayout.Width(50));
                    step.EndFloat = EditorGUILayout.FloatField(step.EndFloat, GUILayout.Width(50));
                    if (step.Property == MotionProperty.FloatValue)
                    {
                        step.ShaderVarName = EditorGUILayout.TextField("Shader Property", step.ShaderVarName, GUILayout.Width(100));
                    }
                }

                step.Easing = (MotionFlowEase)EditorGUILayout.EnumPopup(step.Easing, GUILayout.Width(100));
                
                if (step.Easing == MotionFlowEase.CustomCurve)
                {
                    step.CustomCurve = EditorGUILayout.CurveField(step.CustomCurve, GUILayout.Width(60));
                }

                GUILayout.Label("Dur", GUILayout.Width(25));
                step.Duration = EditorGUILayout.FloatField(step.Duration, GUILayout.Width(40));
                GUILayout.Label("Dly", GUILayout.Width(25));
                step.Delay = EditorGUILayout.FloatField(step.Delay, GUILayout.Width(40));
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawVerticalDivider()
        {
            var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(1), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));
        }

        private void CreateNewAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create MotionFlow Sequence Asset",
                "NewMotionFlowAsset",
                "asset",
                "Create a new MotionFlow sequence settings file."
            );

            if (!string.IsNullOrEmpty(path))
            {
                var newAsset = CreateInstance<MotionFlowAsset>();
                AssetDatabase.CreateAsset(newAsset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                _selectedAsset = newAsset;
                _selectedSequenceIndex = -1;
                _selectedSequence = null;
            }
        }

        private void AddNewSequence()
        {
            if (_selectedAsset == null) return;
            var seq = new MotionFlowSequence { SequenceName = $"Sequence {_selectedAsset.Sequences.Count + 1}" };
            _selectedAsset.Sequences.Add(seq);
            _selectedSequenceIndex = _selectedAsset.Sequences.Count - 1;
            _selectedSequence = seq;
            EditorUtility.SetDirty(_selectedAsset);
            AssetDatabase.SaveAssets();
        }

        private void AddNewTrack()
        {
            if (_selectedSequence == null) return;
            _selectedSequence.Tracks.Add(new MotionFlowTrack());
            EditorUtility.SetDirty(_selectedAsset);
            AssetDatabase.SaveAssets();
        }

        private void PlaySequencePreview()
        {
            if (_selectedSequence == null) return;

            // Force save asset before previewing to ensure it runs with latest values
            EditorUtility.SetDirty(_selectedAsset);
            AssetDatabase.SaveAssets();

            Debug.Log($"[MotionFlow Preview] Starting visual preview for sequence: {_selectedSequence.SequenceName}");

            foreach (var track in _selectedSequence.Tracks)
            {
                var targets = new List<object>();
                if (track.Resolver == TargetResolverMode.DirectReference || track.Resolver == TargetResolverMode.ByName)
                {
                    var go = GameObject.Find(track.ResolverQuery);
                    var resolved = ResolveComponentForPreview(go, track.TargetType);
                    if (resolved != null) targets.Add(resolved);
                }
                else if (track.Resolver == TargetResolverMode.ByTag)
                {
                    var tagObjects = GameObject.FindGameObjectsWithTag(track.ResolverQuery);
                    foreach (var go in tagObjects)
                    {
                        var resolved = ResolveComponentForPreview(go, track.TargetType);
                        if (resolved != null) targets.Add(resolved);
                    }
                }

                if (targets.Count == 0)
                {
                    Debug.LogWarning($"[MotionFlow Preview] Track '{track.TrackName}' could not resolve target using '{track.Resolver}' with query '{track.ResolverQuery}'");
                    continue;
                }

                for (int i = 0; i < targets.Count; i++)
                {
                    var target = targets[i];
                    MotionFlowEngine.CancelAllTweens(target);
                    float accumulatedTime = track.EnableStagger ? (i * track.StaggerDelay) : 0f;
                    
                    foreach (var step in track.Steps)
                    {
                        accumulatedTime += step.Delay;
                        float totalDelay = accumulatedTime;

                        if (step.IsPathMovement)
                        {
                            var pathGo = GameObject.Find(step.TargetPathName);
                            if (pathGo != null)
                            {
                                var path = pathGo.GetComponent<MotionFlowPath>();
                                if (path != null && target is Transform tr)
                                {
                                    MotionFlowEngine.StartFloatTween(
                                        tr,
                                        MotionTargetType.Transform,
                                        MotionProperty.Position,
                                        0f,
                                        1f,
                                        step.Duration,
                                        totalDelay,
                                        step.Easing,
                                        step.CustomCurve,
                                        step.TargetPathName,
                                        null
                                    );
                                }
                            }
                        }
                        else if (step.IsVector)
                        {
                            MotionFlowEngine.StartVectorTween(
                                target,
                                track.TargetType,
                                step.Property,
                                step.StartVec,
                                step.EndVec,
                                step.Duration,
                                totalDelay,
                                step.Easing,
                                step.CustomCurve,
                                null
                            );
                        }
                        else if (step.IsColor)
                        {
                            MotionFlowEngine.StartColorTween(
                                target,
                                track.TargetType,
                                step.Property,
                                step.StartColor,
                                step.EndColor,
                                step.Duration,
                                totalDelay,
                                step.Easing,
                                step.CustomCurve,
                                step.ShaderVarName,
                                null
                            );
                        }
                        else if (step.IsFloat)
                        {
                            MotionFlowEngine.StartFloatTween(
                                target,
                                track.TargetType,
                                step.Property,
                                step.StartFloat,
                                step.EndFloat,
                                step.Duration,
                                totalDelay,
                                step.Easing,
                                step.CustomCurve,
                                step.ShaderVarName,
                                null
                            );
                        }

                        accumulatedTime += step.Duration;
                    }
                }
            }

            SceneView.RepaintAll();
        }

        private object ResolveComponentForPreview(GameObject go, MotionTargetType type)
        {
            if (go == null) return null;
            switch (type)
            {
                case MotionTargetType.Transform:
                    return go.transform;
                case MotionTargetType.RectTransform:
                    return go.GetComponent<RectTransform>();
                case MotionTargetType.Material:
                    var renderer = go.GetComponent<Renderer>();
                    return renderer != null ? renderer.sharedMaterial : null;
                case MotionTargetType.Light:
                    return go.GetComponent<Light>();
                case MotionTargetType.AudioSource:
                    return go.GetComponent<AudioSource>();
            }
            return null;
        }

        private void ExportToCSharp()
        {
            if (_selectedSequence == null) return;

            string path = EditorUtility.SaveFolderPanel("Select Output Directory for Script", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                string relativePath = path;
                if (path.StartsWith(Application.dataPath))
                {
                    relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                }

                string className = MakeValidClassName(_selectedSequence.SequenceName);
                MotionFlowCodeGenerator.GenerateScript(_selectedSequence, className, relativePath);
                
                EditorUtility.DisplayDialog(
                    "Export Successful",
                    $"Exported sequence as C# Class '{className}' inside directory: {relativePath}",
                    "OK"
                );
            }
        }

        private string MakeValidClassName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "CompiledSequence";
            var clean = new System.Text.StringBuilder();
            if (!char.IsLetter(name[0])) clean.Append("MF");
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c)) clean.Append(c);
            }
            return clean.ToString();
        }
    }
}
