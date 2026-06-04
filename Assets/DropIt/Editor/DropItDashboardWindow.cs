using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DropIt.Editor
{
    public class DropItDashboardWindow : EditorWindow
    {
        private int selectedTab = 0;
        private readonly string[] tabLabels = { " Paint Brush", " Physics Drop", " Help & Guide" };

        private Vector2 scrollPos = Vector2.zero;

        // Custom Styles
        private GUIStyle headerStyle;
        private GUIStyle cardStyle;
        private GUIStyle boldLabelStyle;
        private GUIStyle neonButtonStyle;
        private GUIStyle dragDropAreaStyle;

        // Physics Settings
        private float gravityMultiplier = 2f;
        private float bounciness = 0.2f;
        private int maxSimulationSteps = 500;

        [MenuItem("Tools/DropIt/Scatter Dashboard", false, 10)]
        public static void ShowWindow()
        {
            var window = GetWindow<DropItDashboardWindow>("DropIt Dashboard");
            window.minSize = new Vector2(460, 560);
            window.Show();
        }

        private void OnEnable()
        {
            DropItBrushEngine.RegisterSceneCallbacks();
        }

        private void OnDisable()
        {
            DropItBrushEngine.UnregisterSceneCallbacks();
            DropItBrushEngine.IsBrushActive = false;
        }

        private void OnGUI()
        {
            InitializeStyles();

            // Draw Header
            DrawHeader();

            // Draw Palette
            DrawPrefabPalette();

            // Draw Tab Menu
            selectedTab = GUILayout.Toolbar(selectedTab, tabLabels, GUILayout.Height(28));

            // Draw Tab Contents
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(10, 10, 10, 10) });

            switch (selectedTab)
            {
                case 0:
                    DrawBrushSettings();
                    break;
                case 1:
                    DrawPhysicsSettings();
                    break;
                case 2:
                    DrawHelpGuide();
                    break;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void InitializeStyles()
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(0, 0, 10, 10)
            };

            cardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(0, 0, 0, 10)
            };

            boldLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12
            };

            neonButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                fixedHeight = 35
            };

            dragDropAreaStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 11,
                normal = { textColor = Color.gray }
            };
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label("🪂 DropIt! Level Designer", headerStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPrefabPalette()
        {
            EditorGUILayout.BeginVertical(cardStyle);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("🎨 Active Brush Palette", boldLabelStyle);
            GUILayout.FlexibleSpace();
            
            // Randomize selection toggle
            DropItBrushEngine.RandomizePaletteSelection = EditorGUILayout.ToggleLeft("Randomize Selection", DropItBrushEngine.RandomizePaletteSelection, GUILayout.Width(140));

            if (GUILayout.Button("Clear Palette", GUILayout.Width(90)))
            {
                DropItBrushEngine.PrefabPalette.Clear();
                DropItBrushEngine.SelectedPrefabIndex = 0;
            }
            EditorGUILayout.EndHorizontal();

            // Drag and Drop area for prefabs
            Rect dragRect = GUILayoutUtility.GetRect(0, 45, GUILayout.ExpandWidth(true));
            GUI.Box(dragRect, "Drag & Drop Prefabs Here to Add to Palette", dragDropAreaStyle);
            HandleDragAndDrop(dragRect);

            // Draw grid of items currently in palette
            var palette = DropItBrushEngine.PrefabPalette;
            if (palette.Count > 0)
            {
                EditorGUILayout.Space(5);
                
                int columns = Mathf.Max(1, (int)(position.width - 30) / 90);
                int rows = Mathf.CeilToInt((float)palette.Count / columns);

                for (int r = 0; r < rows; r++)
                {
                    EditorGUILayout.BeginHorizontal();
                    for (int c = 0; c < columns; c++)
                    {
                        int index = r * columns + c;
                        if (index < palette.Count)
                        {
                            var prefab = palette[index];
                            if (prefab == null) continue;

                            bool isSelected = (DropItBrushEngine.SelectedPrefabIndex == index) && !DropItBrushEngine.RandomizePaletteSelection;

                            // Custom style to show blue highlighting for selected item
                            GUIStyle itemBoxStyle = new GUIStyle(GUI.skin.box);
                            if (isSelected)
                            {
                                // Setup a custom highlighted look
                                itemBoxStyle.normal.background = Texture2D.whiteTexture;
                                GUI.color = new Color(0.2f, 0.6f, 1f, 0.25f); // Light blue background
                            }

                            EditorGUILayout.BeginVertical(itemBoxStyle, GUILayout.Width(80), GUILayout.Height(85));
                            GUI.color = Color.white; // Reset GUI color

                            // Clickable texture area to set selection
                            Texture2D thumb = AssetPreview.GetAssetPreview(prefab);
                            if (GUILayout.Button(thumb != null ? (Texture)thumb : (Texture)Texture2D.blackTexture, GUI.skin.label, GUILayout.Width(50), GUILayout.Height(50)))
                            {
                                DropItBrushEngine.SelectedPrefabIndex = index;
                                Repaint();
                                SceneView.RepaintAll();
                            }

                            // Show short name and remove button
                            EditorGUILayout.BeginHorizontal();
                            if (GUILayout.Button(prefab.name.Length > 8 ? prefab.name.Substring(0, 6) + ".." : prefab.name, EditorStyles.miniLabel))
                            {
                                DropItBrushEngine.SelectedPrefabIndex = index;
                                Repaint();
                                SceneView.RepaintAll();
                            }

                            if (GUILayout.Button("X", GUILayout.Width(18), GUILayout.Height(15)))
                            {
                                palette.RemoveAt(index);
                                if (DropItBrushEngine.SelectedPrefabIndex >= palette.Count)
                                {
                                    DropItBrushEngine.SelectedPrefabIndex = Mathf.Max(0, palette.Count - 1);
                                }
                                Repaint();
                                SceneView.RepaintAll();
                                break; // Break out of inner loop because collection modified
                            }
                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.EndVertical();
                        }
                        else
                        {
                            GUILayout.Space(84);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Drag and drop some prefabs to start painting or scattering objects.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void HandleDragAndDrop(Rect rect)
        {
            Event currentEvent = Event.current;
            if (!rect.Contains(currentEvent.mousePosition)) return;

            if (currentEvent.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                {
                    if (draggedObject is GameObject go && PrefabUtility.IsPartOfPrefabAsset(go))
                    {
                        if (!DropItBrushEngine.PrefabPalette.Contains(go))
                        {
                            DropItBrushEngine.PrefabPalette.Add(go);
                        }
                    }
                }
                currentEvent.Use();
                Repaint();
            }
        }

        private void DrawBrushSettings()
        {
            GUILayout.Label("Scatter Brush Controls", boldLabelStyle);

            EditorGUILayout.BeginVertical(cardStyle);

            // Brush Active Toggle
            bool brushActive = EditorGUILayout.Toggle("Activate Paint Brush", DropItBrushEngine.IsBrushActive);
            if (brushActive != DropItBrushEngine.IsBrushActive)
            {
                DropItBrushEngine.IsBrushActive = brushActive;
                SceneView.RepaintAll();
            }

            // Brush Mode Selection
            DropItBrushEngine.CurrentMode = (BrushMode)EditorGUILayout.EnumPopup("Brush Mode", DropItBrushEngine.CurrentMode);

            // Spawn Parent Field
            DropItBrushEngine.SpawnParent = (Transform)EditorGUILayout.ObjectField("Spawn Parent", DropItBrushEngine.SpawnParent, typeof(Transform), true);

            if (DropItBrushEngine.IsBrushActive)
            {
                switch (DropItBrushEngine.CurrentMode)
                {
                    case BrushMode.Paint:
                        EditorGUILayout.HelpBox("Hold LMB and drag to paint items on static colliders.", MessageType.Info);
                        break;
                    case BrushMode.Eraser:
                        EditorGUILayout.HelpBox("Hold LMB and drag to erase painted items inside the brush circle.", MessageType.Warning);
                        break;
                    case BrushMode.PhysicsPaint:
                        EditorGUILayout.HelpBox("Hold LMB and drag to drop items with active physics simulation live!", MessageType.Info);
                        break;
                }
            }

            DropItBrushEngine.BrushRadius = EditorGUILayout.Slider("Brush Size (Radius)", DropItBrushEngine.BrushRadius, 0.1f, 20f);
            
            if (DropItBrushEngine.CurrentMode != BrushMode.Eraser)
            {
                DropItBrushEngine.BrushDensity = EditorGUILayout.IntSlider("Density (Per Click)", DropItBrushEngine.BrushDensity, 1, 20);
            }

            // Layer Mask Filter
            SerializedObject tagManager = new SerializedObject(this); // Just placeholder to render layer mask picker beautifully
            DropItBrushEngine.LayerFilter = LayerMaskField("Layer Filter", DropItBrushEngine.LayerFilter);

            EditorGUILayout.Space();

            if (DropItBrushEngine.CurrentMode != BrushMode.Eraser)
            {
                // Grid Snapping Config
                DropItBrushEngine.EnableGridSnapping = EditorGUILayout.BeginToggleGroup("Enable Grid Snapping", DropItBrushEngine.EnableGridSnapping);
                DropItBrushEngine.GridSize = EditorGUILayout.Slider("Grid Size (m)", DropItBrushEngine.GridSize, 0.1f, 10f);
                EditorGUILayout.EndToggleGroup();

                EditorGUILayout.Space();

                // Random Rotation
                DropItBrushEngine.RandomizeRotation = EditorGUILayout.BeginToggleGroup("Randomize Rotation", DropItBrushEngine.RandomizeRotation);
                DropItBrushEngine.MinRotation = EditorGUILayout.Vector3Field("Min Euler Offset", DropItBrushEngine.MinRotation);
                DropItBrushEngine.MaxRotation = EditorGUILayout.Vector3Field("Max Euler Offset", DropItBrushEngine.MaxRotation);
                EditorGUILayout.EndToggleGroup();

                EditorGUILayout.Space();

                // Random Scale
                DropItBrushEngine.RandomizeScale = EditorGUILayout.BeginToggleGroup("Randomize Scale", DropItBrushEngine.RandomizeScale);
                DropItBrushEngine.UniformScale = EditorGUILayout.Toggle("Uniform Proportions", DropItBrushEngine.UniformScale);
                DropItBrushEngine.MinScale = EditorGUILayout.Slider("Min Scale Bound", DropItBrushEngine.MinScale, 0.1f, 5f);
                DropItBrushEngine.MaxScale = EditorGUILayout.Slider("Max Scale Bound", DropItBrushEngine.MaxScale, 0.1f, 5f);
                EditorGUILayout.EndToggleGroup();

                EditorGUILayout.Space();

                // Align and Prevention settings
                DropItBrushEngine.AlignToSlope = EditorGUILayout.Toggle("Align to Surface Slope", DropItBrushEngine.AlignToSlope);
                
                // Pivot Alignment Presets
                DropItBrushEngine.SelectedPivotPreset = (PivotPreset)EditorGUILayout.EnumPopup("Pivot Alignment", DropItBrushEngine.SelectedPivotPreset);
                DropItBrushEngine.ManualPivotOffset = EditorGUILayout.Vector3Field("Manual Offset (XYZ)", DropItBrushEngine.ManualPivotOffset);

                DropItBrushEngine.PreventClipping = EditorGUILayout.Toggle("Prevent Overlap (Clipping)", DropItBrushEngine.PreventClipping);
                
                if (DropItBrushEngine.PreventClipping)
                {
                    DropItBrushEngine.MinDistanceBetweenObjects = EditorGUILayout.Slider("Min Distance Check", DropItBrushEngine.MinDistanceBetweenObjects, 0.1f, 10f);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPhysicsSettings()
        {
            GUILayout.Label("Editor Physics Simulation", boldLabelStyle);

            EditorGUILayout.BeginVertical(cardStyle);

            gravityMultiplier = EditorGUILayout.Slider("Gravity Multiplier", gravityMultiplier, 0.5f, 10f);
            bounciness = EditorGUILayout.Slider("Physical Bounciness", bounciness, 0f, 1f);
            maxSimulationSteps = EditorGUILayout.IntSlider("Max Sim Steps", maxSimulationSteps, 100, 1000);

            EditorGUILayout.Space();

            GUI.enabled = Selection.gameObjects.Length > 0;

            if (GUILayout.Button("🪂 Simulate Physics Drop on Selection", neonButtonStyle))
            {
                DropItPhysicsSimulator.SimulateDrop(Selection.gameObjects, gravityMultiplier, bounciness, maxSimulationSteps);
            }

            if (GUILayout.Button("Snap Selected Directly to Ground", GUI.skin.button))
            {
                DropItPhysicsSimulator.SnapToGround(Selection.gameObjects);
            }

            GUI.enabled = true;

            if (Selection.gameObjects.Length == 0)
            {
                EditorGUILayout.HelpBox("Select one or more GameObjects in the scene hierarchy to simulate physics drop or snap them down.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawHelpGuide()
        {
            GUILayout.Label("Quick User Guide", boldLabelStyle);

            EditorGUILayout.BeginVertical(cardStyle);
            EditorGUILayout.LabelField("1. Brush Modes:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("- Paint: Standard scattering and placement.\n" +
                                       "- Eraser: Drag over objects to erase them cleanly.\n" +
                                       "- Physics Paint: Drop objects with physical gravity simulation *as you paint them*.", EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("2. Grid Snapping:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Enable 'Grid Snapping' to lock modular floor/wall prefabs to exact coordinates, perfect for modular grid systems.", EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("3. Dynamic Physics Stack:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Select floating items in Scene view and click 'Simulate Physics Drop' to make them stack naturally in Editor Mode.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();
        }

        // Helper to draw layer mask picker properly
        private static LayerMask LayerMaskField(string label, LayerMask layerMask)
        {
            List<string> layers = new List<string>();
            List<int> layerNumbers = new List<int>();

            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(layerName))
                {
                    layers.Add(layerName);
                    layerNumbers.Add(i);
                }
            }

            int maskWithoutEmpty = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if (((1 << layerNumbers[i]) & layerMask.value) > 0)
                {
                    maskWithoutEmpty |= (1 << i);
                }
            }

            maskWithoutEmpty = EditorGUILayout.MaskField(label, maskWithoutEmpty, layers.ToArray());

            int mask = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if ((maskWithoutEmpty & (1 << i)) > 0)
                {
                    mask |= (1 << layerNumbers[i]);
                }
            }

            return mask;
        }
    }
}
