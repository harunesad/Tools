using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using BehaviorFlow.Runtime;

namespace BehaviorFlow.Editor
{
    public static class BehaviorFlowDemoBuilder
    {
        [MenuItem("Tools/BehaviorFlow/Rebuild Playable Demo Scene")]
        public static void RebuildDemoScene()
        {
            // 1. Create a new scene
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Find existing Camera and Light to configure them
            var mainCam = GameObject.Find("Main Camera");
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(0f, 10f, -15f);
                mainCam.transform.rotation = Quaternion.Euler(35f, 0f, 0f);
            }

            // 2. Create Ground Plane
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "GroundPlane";
            plane.transform.position = Vector3.zero;
            plane.transform.localScale = new Vector3(3f, 1f, 3f);
            
            // Set Static for NavMesh baking
            GameObjectUtility.SetStaticEditorFlags(plane, StaticEditorFlags.NavigationStatic);

            // Give it a dark material
            var planeRenderer = plane.GetComponent<Renderer>();
            if (planeRenderer != null)
            {
                planeRenderer.material = GetOrCreateMaterial("GroundMat", new Color(0.12f, 0.12f, 0.12f));
            }

            // 3. Create Player Sphere
            var player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            player.name = "PlayerSphere";
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 1f, -4f);
            
            var playerRb = player.AddComponent<Rigidbody>();
            playerRb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            var playerAgent = player.AddComponent<DemoPlayerAgent>();
            
            // Give it a vibrant blue material
            var playerRenderer = player.GetComponent<Renderer>();
            if (playerRenderer != null)
            {
                playerRenderer.material = GetOrCreateMaterial("PlayerMat", new Color(0.1f, 0.6f, 1f));
            }

            // 4. Create Enemy Capsule
            var enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = "EnemyAI";
            enemy.transform.position = new Vector3(6f, 1f, 6f);

            var nma = enemy.AddComponent<NavMeshAgent>();
            nma.speed = 3.5f;
            nma.stoppingDistance = 1.2f;

            var enemyAgent = enemy.AddComponent<DemoEnemyAgent>();
            enemyAgent.TargetPlayer = player;

            // Give it a vibrant red material
            var enemyRenderer = enemy.GetComponent<Renderer>();
            if (enemyRenderer != null)
            {
                enemyRenderer.material = GetOrCreateMaterial("EnemyMat", new Color(1f, 0.2f, 0.2f));
            }

            // 5. Create Collectible/Interactive Cube
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "InteractiveScoreCube";
            cube.transform.position = new Vector3(-4f, 1f, 4f);
            
            var boxCollider = cube.GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                boxCollider.isTrigger = true;
            }

            cube.AddComponent<ScoreSystem>();
            cube.AddComponent<TriggerScoreOnContact>();

            // Give it a vibrant yellow/gold material
            var cubeRenderer = cube.GetComponent<Renderer>();
            if (cubeRenderer != null)
            {
                cubeRenderer.material = GetOrCreateMaterial("CollectibleMat", new Color(1f, 0.85f, 0f));
            }

            // 6. Bake NavMesh
            #pragma warning disable CS0618
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
            #pragma warning restore CS0618

            // 7. Save Scene to standard path
            string sceneDirectory = "Assets/BehaviorFlow/Scenes";
            if (!Directory.Exists(sceneDirectory))
            {
                Directory.CreateDirectory(sceneDirectory);
            }
            
            string scenePath = sceneDirectory + "/BehaviorFlowDemo.unity";
            EditorSceneManager.SaveScene(newScene, scenePath);
            AssetDatabase.Refresh();

            Debug.Log($"[BehaviorFlow] Playable Demo Scene rebuilt and saved at: {scenePath}");
            EditorUtility.DisplayDialog("Success", "Playable Demo Scene has been rebuilt successfully!\n\nPress PLAY to test WASD movement, Space to jump, and let the red Enemy chase you into the gold Score Cube!", "Awesome");
        }

        private static Material GetOrCreateMaterial(string matName, Color color)
        {
            string dir = "Assets/BehaviorFlow/Scenes/Materials";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string path = $"{dir}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }
                if (shader == null)
                {
                    shader = Shader.Find("Sprites/Default");
                }

                mat = new Material(shader);
                mat.color = color;
                if (shader.name.Contains("Universal Render Pipeline"))
                {
                    mat.SetFloat("_Smoothness", 0.7f);
                }
                else
                {
                    mat.SetFloat("_Glossiness", 0.8f);
                }

                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.color = color;
                EditorUtility.SetDirty(mat);
            }

            return mat;
        }
    }
}
