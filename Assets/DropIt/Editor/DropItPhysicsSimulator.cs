using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DropIt.Editor
{
    public static class DropItPhysicsSimulator
    {
        private class TempComponentData
        {
            public GameObject gameObject;
            public bool addedRigidbody;
            public bool addedCollider;
            public Rigidbody rigidbody;
            public Collider collider;
            public Vector3 initialPosition;
            public Quaternion initialRotation;
        }

        private static List<TempComponentData> tempObjects = new List<TempComponentData>();

        /// <summary>
        /// Simulates gravity and collisions for selected GameObjects in the Editor.
        /// </summary>
        public static void SimulateDrop(GameObject[] objects, float gravityMultiplier, float bounciness, int maxSimulationSteps)
        {
            if (objects == null || objects.Length == 0)
            {
                Debug.LogWarning("[DropIt!] No objects selected for physics simulation.");
                return;
            }

            // Set up undo group
            Undo.RegisterCompleteObjectUndo(objects, "DropIt Physics Simulate");

            tempObjects.Clear();
            Vector3 originalGravity = Physics.gravity;
            Physics.gravity = originalGravity * gravityMultiplier;

            // 1. Prepare temporary rigidbodies and colliders
            foreach (var obj in objects)
            {
                if (obj == null) continue;

                var data = new TempComponentData
                {
                    gameObject = obj,
                    initialPosition = obj.transform.position,
                    initialRotation = obj.transform.rotation
                };

                // Rigidbody management
                var rb = obj.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = obj.AddComponent<Rigidbody>();
                    rb.mass = 1f;
                    rb.interpolation = RigidbodyInterpolation.None;
                    data.addedRigidbody = true;
                }
                rb.isKinematic = false;
                rb.useGravity = true;
                data.rigidbody = rb;

                // Collider management
                var col = obj.GetComponent<Collider>();
                if (col == null)
                {
                    // Add a box collider matching the object's renderer bounds
                    var renderer = obj.GetComponentInChildren<Renderer>();
                    if (renderer != null)
                    {
                        var boxCol = obj.AddComponent<BoxCollider>();
                        boxCol.size = renderer.bounds.size;
                        data.collider = boxCol;
                    }
                    else
                    {
                        // Fallback simple box collider
                        var boxCol = obj.AddComponent<BoxCollider>();
                        boxCol.size = Vector3.one;
                        data.collider = boxCol;
                    }
                    data.addedCollider = true;
                }
                else
                {
                    data.collider = col;
                }

                // Apply custom material for bounciness if requested
                if (bounciness > 0.01f)
                {
                    var physMat = new PhysicsMaterial("DropItTempMaterial")
                    {
                        bounciness = bounciness,
                        bounceCombine = PhysicsMaterialCombine.Maximum
                    };
                    data.collider.sharedMaterial = physMat;
                }

                tempObjects.Add(data);
            }

            // 2. Perform editor physics simulation loop
            float stepTime = Time.fixedDeltaTime;
            
            // Auto-simulation must be disabled temporarily to step manually
            bool originalAutoSimulation = Physics.autoSimulation;
            Physics.autoSimulation = false;

            try
            {
                for (int i = 0; i < maxSimulationSteps; i++)
                {
                    Physics.Simulate(stepTime);

                    // Check if all rigidbodies have settled (gone to sleep)
                    bool allSleeping = true;
                    foreach (var temp in tempObjects)
                    {
                        if (temp.rigidbody != null && !temp.rigidbody.IsSleeping() && temp.rigidbody.linearVelocity.sqrMagnitude > 0.001f)
                        {
                            allSleeping = false;
                            break;
                        }
                    }

                    if (allSleeping)
                    {
                        break;
                    }
                }
            }
            finally
            {
                // Restore settings
                Physics.autoSimulation = originalAutoSimulation;
                Physics.gravity = originalGravity;
            }

            // 3. Clean up temporary components and bake final positions
            BakeAndCleanup();
        }

        private static void BakeAndCleanup()
        {
            foreach (var temp in tempObjects)
            {
                if (temp.gameObject == null) continue;

                // Remove temporary Rigidbody
                if (temp.addedRigidbody && temp.rigidbody != null)
                {
                    Object.DestroyImmediate(temp.rigidbody);
                }
                else if (temp.rigidbody != null)
                {
                    temp.rigidbody.isKinematic = true;
                }

                // Remove temporary Collider
                if (temp.addedCollider && temp.collider != null)
                {
                    Object.DestroyImmediate(temp.collider);
                }
                else if (temp.collider != null)
                {
                    if (temp.collider.sharedMaterial != null && temp.collider.sharedMaterial.name == "DropItTempMaterial")
                    {
                        Object.DestroyImmediate(temp.collider.sharedMaterial);
                        temp.collider.sharedMaterial = null;
                    }
                }
            }

            tempObjects.Clear();
            Debug.Log("[DropIt!] Physics simulated and baked successfully.");
        }

        /// <summary>
        /// Snap objects directly down to the nearest collider surface.
        /// </summary>
        public static void SnapToGround(GameObject[] objects)
        {
            if (objects == null || objects.Length == 0) return;

            Undo.RegisterCompleteObjectUndo(objects, "DropIt Snap To Ground");

            foreach (var obj in objects)
            {
                if (obj == null) continue;

                // Raycast downwards
                Ray ray = new Ray(obj.transform.position + Vector3.up * 0.1f, Vector3.down);
                
                // Exclude the object itself from the raycast to prevent snapping to itself
                var colliders = obj.GetComponentsInChildren<Collider>();
                foreach (var c in colliders) c.enabled = false;

                if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
                {
                    obj.transform.position = hit.point;
                }

                foreach (var c in colliders) c.enabled = true;
            }
        }
    }
}
