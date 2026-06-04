using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DropIt.Editor
{
    public enum BrushMode { Paint, Eraser, PhysicsPaint }
    public enum RotationAxis { X, Y, Z }
    public enum PivotPreset { Center, Bottom, Top }

    public static class DropItBrushEngine
    {
        public static bool IsBrushActive { get; set; } = false;
        public static BrushMode CurrentMode { get; set; } = BrushMode.Paint;
        public static float BrushRadius { get; set; } = 0.5f;
        public static int BrushDensity { get; set; } = 1;
        
        // Randomization Settings
        public static bool RandomizeRotation { get; set; } = false;
        public static Vector3 MinRotation { get; set; } = Vector3.zero;
        public static Vector3 MaxRotation { get; set; } = new Vector3(0, 360, 0);

        public static bool RandomizeScale { get; set; } = false;
        public static bool UniformScale { get; set; } = true;
        public static float MinScale { get; set; } = 0.8f;
        public static float MaxScale { get; set; } = 1.3f;

        public static bool AlignToSlope { get; set; } = true;
        public static bool PreventClipping { get; set; } = true;
        public static float MinDistanceBetweenObjects { get; set; } = 0.5f;

        // Grid Snapping Settings
        public static bool EnableGridSnapping { get; set; } = false;
        public static float GridSize { get; set; } = 1.0f;

        // Layer Filter
        public static LayerMask LayerFilter { get; set; } = ~0;

        // Parent Object Setting
        public static Transform SpawnParent { get; set; }

        // Palette Settings
        public static List<GameObject> PrefabPalette { get; } = new List<GameObject>();
        public static int SelectedPrefabIndex { get; set; } = 0;
        public static bool RandomizePaletteSelection { get; set; } = false;

        // Pivot Alignment Settings
        public static PivotPreset SelectedPivotPreset { get; set; } = PivotPreset.Bottom;
        public static Vector3 ManualPivotOffset { get; set; } = Vector3.zero;

        // Rotation Manipulation State
        public static RotationAxis ActiveRotationAxis { get; set; } = RotationAxis.Y;
        private static float manualRotationX = 0f;
        private static float manualRotationY = 0f;
        private static float manualRotationZ = 0f;

        private static Vector3 brushPosition = Vector3.zero;
        private static Vector3 brushNormal = Vector3.up;
        private static bool hasHitSurface = false;

        // Ghost Preview State
        private static GameObject ghostObject;
        private static GameObject cachedGhostPrefab;

        public static void RegisterSceneCallbacks()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        public static void UnregisterSceneCallbacks()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            DestroyGhost();
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!IsBrushActive)
            {
                DestroyGhost();
                return;
            }

            Event e = Event.current;
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            // 1. Raycast from mouse to find surface in SceneView
            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag || e.type == EventType.MouseDown || e.type == EventType.Layout)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                int originalLayer = 0;
                if (ghostObject != null)
                {
                    originalLayer = ghostObject.layer;
                    ghostObject.layer = 2; // Ignore Raycast Layer
                    foreach (Transform child in ghostObject.transform) child.gameObject.layer = 2;
                }

                if (Physics.Raycast(ray, out RaycastHit hit, 1000f, LayerFilter))
                {
                    brushPosition = hit.point;
                    brushNormal = hit.normal;
                    hasHitSurface = true;
                }
                else
                {
                    hasHitSurface = false;
                }

                if (ghostObject != null)
                {
                    ghostObject.layer = originalLayer;
                    foreach (Transform child in ghostObject.transform) child.gameObject.layer = originalLayer;
                }
            }

            // 2. Handle Scroll Wheel for Rotation (Mouse Wheel) on active axis
            if (e.type == EventType.ScrollWheel && hasHitSurface && CurrentMode != BrushMode.Eraser)
            {
                float scrollAmount = e.delta.y * 5f;
                switch (ActiveRotationAxis)
                {
                    case RotationAxis.X:
                        manualRotationX += scrollAmount;
                        break;
                    case RotationAxis.Y:
                        manualRotationY += scrollAmount;
                        break;
                    case RotationAxis.Z:
                        manualRotationZ += scrollAmount;
                        break;
                }
                e.Use(); // Consume event to prevent Scene Zooming
            }

            if (!hasHitSurface)
            {
                if (ghostObject != null) ghostObject.SetActive(false);
                return;
            }

            // 3. Render Ghost Preview, Brush circle and UI Overlay on Repaint
            if (e.type == EventType.Repaint)
            {
                Color brushColor = new Color(0.2f, 1f, 0.2f, 1f);
                Color brushFillColor = new Color(0.2f, 1f, 0.2f, 0.08f);

                if (CurrentMode == BrushMode.Eraser)
                {
                    brushColor = new Color(1f, 0.2f, 0.2f, 1f);
                    brushFillColor = new Color(1f, 0.2f, 0.2f, 0.08f);
                    DestroyGhost();
                }
                else
                {
                    UpdateGhostPreview();
                    DrawUIOverlay();
                }

                Handles.color = brushFillColor;
                Handles.DrawSolidDisc(brushPosition, brushNormal, BrushRadius);
                
                Handles.color = brushColor;
                Handles.DrawWireDisc(brushPosition, brushNormal, BrushRadius);
            }

            // 4. Mouse Input Handling
            if (e.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(controlID);
            }

            if (!e.alt)
            {
                // Right Click to toggle rotation axis (button = 1)
                if (e.type == EventType.MouseDown && e.button == 1)
                {
                    ToggleRotationAxis();
                    e.Use();
                }

                // Left Click to Paint
                if (e.button == 0)
                {
                    if (e.type == EventType.MouseDown)
                    {
                        GUIUtility.hotControl = controlID;
                        ApplyBrushAction();
                        e.Use();
                    }
                    else if (e.type == EventType.MouseDrag && GUIUtility.hotControl == controlID)
                    {
                        ApplyBrushAction();
                        e.Use();
                    }
                    else if (e.type == EventType.MouseUp && GUIUtility.hotControl == controlID)
                    {
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }
                }
            }
        }

        private static void ToggleRotationAxis()
        {
            switch (ActiveRotationAxis)
            {
                case RotationAxis.Y:
                    ActiveRotationAxis = RotationAxis.X;
                    break;
                case RotationAxis.X:
                    ActiveRotationAxis = RotationAxis.Z;
                    break;
                case RotationAxis.Z:
                    ActiveRotationAxis = RotationAxis.Y;
                    break;
            }
            Debug.Log($"[DropIt!] Active Rotation Axis changed to: {ActiveRotationAxis}");
        }

        private static void DrawUIOverlay()
        {
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(4, 4, 4, 4),
            };

            Color axisColor = Color.green;
            if (ActiveRotationAxis == RotationAxis.X) axisColor = Color.red;
            else if (ActiveRotationAxis == RotationAxis.Z) axisColor = new Color(0.2f, 0.6f, 1f);

            style.normal.textColor = axisColor;

            string infoText = $"Rotation Axis: [{ActiveRotationAxis}]\n" +
                              $"Pivot Align: [{SelectedPivotPreset}]\n" +
                              $"(Right-click to Toggle Axis)\n" +
                              $"(Scroll wheel to Rotate)\n" +
                              $"Angle: X:{manualRotationX:F0} Y:{manualRotationY:F0} Z:{manualRotationZ:F0}";

            Handles.Label(brushPosition + brushNormal * (BrushRadius + 0.5f), infoText, style);
        }

        private static void UpdateGhostPreview()
        {
            GameObject activePrefab = GetActivePrefab();
            if (activePrefab == null)
            {
                DestroyGhost();
                return;
            }

            if (ghostObject == null || cachedGhostPrefab != activePrefab)
            {
                DestroyGhost();
                cachedGhostPrefab = activePrefab;
                ghostObject = Object.Instantiate(activePrefab);
                ghostObject.name = "DropIt_Ghost_Preview";
                ghostObject.hideFlags = HideFlags.HideAndDontSave;

                var renderers = ghostObject.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    foreach (var mat in r.sharedMaterials)
                    {
                        if (mat == null) continue;
                        mat.SetFloat("_Mode", 3);
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.SetInt("_ZWrite", 0);
                        mat.DisableKeyword("_ALPHATEST_ON");
                        mat.EnableKeyword("_ALPHABLEND_ON");
                        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        mat.renderQueue = 3000;
                        if (mat.HasProperty("_Color"))
                        {
                            Color col = mat.color;
                            col.a = 0.4f;
                            mat.color = col;
                        }
                    }
                }

                var colliders = ghostObject.GetComponentsInChildren<Collider>();
                foreach (var c in colliders) c.enabled = false;
            }

            ghostObject.SetActive(true);
            
            // 1. Calculate base Raycast hit Position
            Vector3 finalPos = brushPosition;
            if (EnableGridSnapping)
            {
                finalPos.x = Mathf.Round(finalPos.x / GridSize) * GridSize;
                finalPos.y = Mathf.Round(finalPos.y / GridSize) * GridSize;
                finalPos.z = Mathf.Round(finalPos.z / GridSize) * GridSize;
            }

            // Apply manual slider offset
            finalPos += ManualPivotOffset;

            ghostObject.transform.position = finalPos;
            Quaternion rot = activePrefab.transform.localRotation;
            if (AlignToSlope)
            {
                rot = Quaternion.FromToRotation(Vector3.up, brushNormal) * rot;
            }
            rot = rot * Quaternion.Euler(manualRotationX, manualRotationY, manualRotationZ);
            ghostObject.transform.rotation = rot;

            // Apply Pivot Presets (Bottom, Top, Center) using bounds calculations
            ApplyPivotOffset(ghostObject, finalPos);
        }

        private static void ApplyPivotOffset(GameObject obj, Vector3 basePos)
        {
            var renderer = obj.GetComponentInChildren<Renderer>();
            if (renderer == null) return;

            Vector3 pivotPos = obj.transform.position;
            Vector3 center = renderer.bounds.center;
            Vector3 extents = renderer.bounds.extents;

            Vector3 offset = Vector3.zero;

            if (SelectedPivotPreset == PivotPreset.Bottom)
            {
                Vector3 boundsBottom = center - new Vector3(0, extents.y, 0);
                offset = pivotPos - boundsBottom;
            }
            else if (SelectedPivotPreset == PivotPreset.Top)
            {
                Vector3 boundsTop = center + new Vector3(0, extents.y, 0);
                offset = pivotPos - boundsTop;
            }

            obj.transform.position = basePos + offset;
        }

        private static void DestroyGhost()
        {
            if (ghostObject != null)
            {
                Object.DestroyImmediate(ghostObject);
                ghostObject = null;
            }
        }

        private static GameObject GetActivePrefab()
        {
            if (PrefabPalette.Count == 0) return null;
            
            if (RandomizePaletteSelection)
            {
                return PrefabPalette[Random.Range(0, PrefabPalette.Count)];
            }

            if (SelectedPrefabIndex >= 0 && SelectedPrefabIndex < PrefabPalette.Count)
            {
                return PrefabPalette[SelectedPrefabIndex];
            }

            return PrefabPalette[0];
        }

        private static void ApplyBrushAction()
        {
            if (CurrentMode == BrushMode.Eraser)
            {
                EraseObjects();
            }
            else
            {
                PaintStroke();
            }
        }

        private static void PaintStroke()
        {
            GameObject activePrefab = GetActivePrefab();
            if (activePrefab == null) return;

            List<GameObject> spawnedObjects = new List<GameObject>();

            for (int i = 0; i < BrushDensity; i++)
            {
                Vector3 targetPos = brushPosition;

                if (BrushRadius > 0.1f && BrushDensity > 1)
                {
                    Vector2 randomPoint = Random.insideUnitCircle * BrushRadius;
                    Vector3 spawnOffset = new Vector3(randomPoint.x, 0f, randomPoint.y);

                    Quaternion alignRot = Quaternion.FromToRotation(Vector3.up, brushNormal);
                    spawnOffset = alignRot * spawnOffset;

                    targetPos = brushPosition + spawnOffset;
                }

                if (EnableGridSnapping)
                {
                    targetPos.x = Mathf.Round(targetPos.x / GridSize) * GridSize;
                    targetPos.y = Mathf.Round(targetPos.y / GridSize) * GridSize;
                    targetPos.z = Mathf.Round(targetPos.z / GridSize) * GridSize;
                }

                // Apply manual slider offset
                targetPos += ManualPivotOffset;

                Ray ray = new Ray(targetPos + brushNormal * 5f, -brushNormal);
                if (Physics.Raycast(ray, out RaycastHit hit, 20f, LayerFilter))
                {
                    Vector3 finalPos = hit.point;

                    GameObject targetPrefab = RandomizePaletteSelection 
                        ? PrefabPalette[UnityEngine.Random.Range(0, PrefabPalette.Count)] 
                        : activePrefab;

                    if (targetPrefab == null) continue;

                    Vector3 finalScale = targetPrefab.transform.localScale;
                    if (RandomizeScale)
                    {
                        if (UniformScale)
                        {
                            float randScale = UnityEngine.Random.Range(MinScale, MaxScale);
                            finalScale = targetPrefab.transform.localScale * randScale;
                        }
                        else
                        {
                            finalScale = new Vector3(
                                targetPrefab.transform.localScale.x * UnityEngine.Random.Range(MinScale, MaxScale),
                                targetPrefab.transform.localScale.y * UnityEngine.Random.Range(MinScale, MaxScale),
                                targetPrefab.transform.localScale.z * UnityEngine.Random.Range(MinScale, MaxScale)
                            );
                        }
                    }

                    // Dynamically calculate check radius based on prefab bounds
                    float checkRadius = MinDistanceBetweenObjects;
                    var renderer = targetPrefab.GetComponentInChildren<Renderer>();
                    if (renderer != null)
                    {
                        checkRadius = Mathf.Max(renderer.bounds.extents.x, renderer.bounds.extents.z) * finalScale.x;
                        checkRadius = Mathf.Max(checkRadius, MinDistanceBetweenObjects);
                    }

                    if (PreventClipping && CheckCollision(finalPos, checkRadius, hit.collider))
                    {
                        continue;
                    }

                    GameObject spawned = PrefabUtility.InstantiatePrefab(targetPrefab) as GameObject;
                    if (spawned == null) continue;

                    if (SpawnParent != null)
                    {
                        spawned.transform.SetParent(SpawnParent, true);
                    }

                    spawned.transform.position = finalPos;

                    // Apply Rotation
                    Quaternion finalRot = targetPrefab.transform.localRotation;
                    if (AlignToSlope)
                    {
                        finalRot = Quaternion.FromToRotation(Vector3.up, hit.normal) * finalRot;
                    }
                    
                    finalRot = finalRot * Quaternion.Euler(manualRotationX, manualRotationY, manualRotationZ);

                    if (RandomizeRotation)
                    {
                        Vector3 randEuler = new Vector3(
                            UnityEngine.Random.Range(MinRotation.x, MaxRotation.x),
                            UnityEngine.Random.Range(MinRotation.y, MaxRotation.y),
                            UnityEngine.Random.Range(MinRotation.z, MaxRotation.z)
                        );
                        finalRot = finalRot * Quaternion.Euler(randEuler);
                    }
                    spawned.transform.rotation = finalRot;
                    spawned.transform.localScale = finalScale;

                    // Apply Pivot Alignment offset to spawned object
                    ApplyPivotOffset(spawned, finalPos);

                    Undo.RegisterCreatedObjectUndo(spawned, "DropIt Paint Object");
                    spawnedObjects.Add(spawned);
                }
            }

            if (CurrentMode == BrushMode.PhysicsPaint && spawnedObjects.Count > 0)
            {
                foreach (var obj in spawnedObjects)
                {
                    obj.transform.position += brushNormal * 1.5f;
                }
                DropItPhysicsSimulator.SimulateDrop(spawnedObjects.ToArray(), 2.0f, 0.2f, 300);
            }
        }

        private static void EraseObjects()
        {
            Collider[] colliders = Physics.OverlapSphere(brushPosition, BrushRadius, LayerFilter);
            HashSet<GameObject> objectsToDestroy = new HashSet<GameObject>();

            foreach (var col in colliders)
            {
                if (col.gameObject.name == "DropIt_Ghost_Preview") continue;

                GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(col.gameObject);
                if (root != null)
                {
                    objectsToDestroy.Add(root);
                }
                else if (col.gameObject.transform.parent != null)
                {
                    objectsToDestroy.Add(col.gameObject);
                }
            }

            foreach (var obj in objectsToDestroy)
            {
                Undo.DestroyObjectImmediate(obj);
            }
        }

        private static bool CheckCollision(Vector3 position, float radius, Collider groundCollider)
        {
            Collider[] colliders = Physics.OverlapSphere(position, radius, LayerFilter);
            foreach (var col in colliders)
            {
                if (col.gameObject.name == "DropIt_Ghost_Preview") continue;
                if (col != groundCollider)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
