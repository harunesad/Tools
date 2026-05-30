using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SmartSave.Editor
{
    public class SmartSaveDebuggerWindow : EditorWindow
    {
        private int selectedTab = 0;
        private readonly string[] tabLabels = { " Live Debugger", " Profile Manager", " Settings" };

        // Scroll positions
        private Vector2 liveScroll = Vector2.zero;
        private Vector2 profilesScroll = Vector2.zero;

        // Custom Styles
        private GUIStyle headerStyle;
        private GUIStyle cardStyle;
        private GUIStyle boldLabelStyle;
        private GUIStyle badgeStyle;
        private GUIStyle greenBadgeStyle;
        private GUIStyle tabButtonStyle;

        // Settings variables
        private bool encryptionEnabled;
        private string encryptionPassword;
        private bool autoSaveEnabled;
        private float autoSaveInterval;

        [MenuItem("Tools/SmartSave Debugger", false, 10)]
        public static void ShowWindow()
        {
            var window = GetWindow<SmartSaveDebuggerWindow>("SmartSave Debugger");
            window.minSize = new Vector2(450, 500);
            window.Show();
        }

        private void OnEnable()
        {
            encryptionEnabled = SaveManager.EncryptionEnabled;
            encryptionPassword = SaveManager.EncryptionPassword;
            autoSaveEnabled = SaveManager.AutoSaveEnabled;
            autoSaveInterval = SaveManager.AutoSaveInterval;
        }

        private void OnGUI()
        {
            InitializeStyles();

            // Draw Header
            DrawHeader();

            // Draw Tabs
            DrawTabs();

            // Draw Content
            EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(10, 10, 10, 10) });
            switch (selectedTab)
            {
                case 0:
                    DrawLiveDebugger();
                    break;
                case 1:
                    DrawProfileManager();
                    break;
                case 2:
                    DrawSettings();
                    break;
            }
            EditorGUILayout.EndVertical();
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

            badgeStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { textColor = Color.gray },
                fontSize = 9,
                padding = new RectOffset(4, 4, 2, 2)
            };

            greenBadgeStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { textColor = new Color(0.2f, 0.8f, 0.2f) },
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(4, 4, 2, 2)
            };

            tabButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                fixedHeight = 30
            };
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label("💾 SmartSave", headerStyle);
            GUILayout.FlexibleSpace();
            
            // Quick Save / Load Action Buttons
            if (GUILayout.Button("Save Now", GUILayout.Width(80), GUILayout.Height(30)))
            {
                SaveManager.Save();
                ShowNotification(new GUIContent("Game Saved!"));
            }
            if (GUILayout.Button("Load Now", GUILayout.Width(80), GUILayout.Height(30)))
            {
                SaveManager.Load();
                ShowNotification(new GUIContent("Game Loaded!"));
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTabs()
        {
            selectedTab = GUILayout.Toolbar(selectedTab, tabLabels, GUILayout.Height(25));
        }

        private void DrawLiveDebugger()
        {
            GUILayout.Label("Active Saveables", boldLabelStyle);
            
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Play Mode is not active. Start the game to view, inspect, and modify live save data.", MessageType.Info);
                return;
            }

            if (GUILayout.Button("🔄 Scan Active Scene Elements", GUILayout.Height(25)))
            {
                SaveManager.ScanSceneForSaveables();
            }

            liveScroll = EditorGUILayout.BeginScrollView(liveScroll);

            var registeredBehaviours = SaveManager.GetRegisteredBehaviours();
            var registeredSaveables = SaveManager.GetRegisteredSaveables();

            if (registeredBehaviours.Count == 0 && registeredSaveables.Count == 0)
            {
                EditorGUILayout.HelpBox("No active saveables found in the scene. Register components using SaveManager.Register() or tag fields/properties with [Saveable].", MessageType.Warning);
            }

            // 1. Render custom ISaveables
            foreach (var saveable in registeredSaveables)
            {
                if (saveable == null) continue;
                EditorGUILayout.BeginVertical(cardStyle);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"🔑 {saveable.SaveID}", boldLabelStyle);
                GUILayout.FlexibleSpace();
                GUILayout.Label("ISaveable Interface", greenBadgeStyle);
                EditorGUILayout.EndHorizontal();

                object state = saveable.CaptureState();
                if (state != null)
                {
                    string json = JsonUtility.ToJson(state);
                    EditorGUILayout.LabelField("State JSON", json, EditorStyles.wordWrappedLabel);
                }
                EditorGUILayout.EndVertical();
            }

            // 2. Render Attribute based saveables
            foreach (var behaviour in registeredBehaviours)
            {
                if (behaviour == null) continue;
                EditorGUILayout.BeginVertical(cardStyle);
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"📦 {behaviour.name} ({behaviour.GetType().Name})", boldLabelStyle);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Ping", GUILayout.Width(45)))
                {
                    EditorGUIUtility.PingObject(behaviour.gameObject);
                }
                EditorGUILayout.EndHorizontal();

                Type type = behaviour.GetType();
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                EditorGUI.indentLevel++;

                foreach (var field in fields)
                {
                    var attr = field.GetCustomAttribute<SaveableAttribute>();
                    if (attr != null)
                    {
                        string label = attr.CustomKey ?? field.Name;
                        object currentVal = field.GetValue(behaviour);
                        object newVal = DrawFieldValue(label, currentVal, field.FieldType);
                        if (newVal != currentVal && newVal != null)
                        {
                            field.SetValue(behaviour, newVal);
                        }
                    }
                }

                foreach (var prop in properties)
                {
                    if (!prop.CanRead || !prop.CanWrite) continue;
                    var attr = prop.GetCustomAttribute<SaveableAttribute>();
                    if (attr != null)
                    {
                        string label = attr.CustomKey ?? prop.Name;
                        object currentVal = prop.GetValue(behaviour);
                        object newVal = DrawFieldValue(label, currentVal, prop.PropertyType);
                        if (newVal != currentVal && newVal != null)
                        {
                            prop.SetValue(behaviour, newVal);
                        }
                    }
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }

        private object DrawFieldValue(string label, object value, Type type)
        {
            if (value == null) return null;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(150));

            object result = value;

            if (type == typeof(int))
            {
                result = EditorGUILayout.IntField((int)value);
            }
            else if (type == typeof(float))
            {
                result = EditorGUILayout.FloatField((float)value);
            }
            else if (type == typeof(bool))
            {
                result = EditorGUILayout.Toggle((bool)value);
            }
            else if (type == typeof(string))
            {
                result = EditorGUILayout.TextField((string)value);
            }
            else if (type == typeof(Vector3))
            {
                result = EditorGUILayout.Vector3Field("", (Vector3)value);
            }
            else if (type == typeof(Vector2))
            {
                result = EditorGUILayout.Vector2Field("", (Vector2)value);
            }
            else if (type == typeof(Color))
            {
                result = EditorGUILayout.ColorField((Color)value);
            }
            else
            {
                string json = JsonUtility.ToJson(value);
                EditorGUILayout.LabelField(json, EditorStyles.wordWrappedLabel);
            }

            EditorGUILayout.EndHorizontal();
            return result;
        }

        private void DrawProfileManager()
        {
            GUILayout.Label("Save Profile Slots", boldLabelStyle);

            profilesScroll = EditorGUILayout.BeginScrollView(profilesScroll);

            for (int i = 1; i <= 5; i++)
            {
                string path = SaveManager.GetSavePath(i);
                bool exists = File.Exists(path);
                bool isActiveSlot = (SaveManager.ActiveSlot == i);

                EditorGUILayout.BeginVertical(cardStyle);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"Slot {i} " + (isActiveSlot ? "★ (Active)" : ""), boldLabelStyle);
                GUILayout.FlexibleSpace();

                if (isActiveSlot)
                {
                    GUILayout.Label("Selected Profile", greenBadgeStyle);
                }
                else
                {
                    if (GUILayout.Button("Select Slot", GUILayout.Width(80)))
                    {
                        SaveManager.SetActiveSlot(i);
                    }
                }
                EditorGUILayout.EndHorizontal();

                if (exists)
                {
                    DateTime writeTime = File.GetLastWriteTime(path);
                    long fileSizeBytes = new FileInfo(path).Length;
                    float fileSizeKb = fileSizeBytes / 1024f;

                    EditorGUILayout.LabelField("Save File Path:", path, EditorStyles.miniLabel);
                    EditorGUILayout.LabelField("Last Updated:", writeTime.ToString("yyyy-MM-dd HH:mm:ss"));
                    EditorGUILayout.LabelField("Size:", $"{fileSizeKb:F2} KB ({fileSizeBytes} bytes)");

                    EditorGUILayout.BeginHorizontal();
                    
                    if (GUILayout.Button("🔓 Decrypt & Inspect File", GUILayout.Width(150)))
                    {
                        InspectSaveFile(path);
                    }

                    if (GUILayout.Button("Delete Save File", GUILayout.Width(120)))
                    {
                        if (EditorUtility.DisplayDialog("Delete Save File?", $"Are you sure you want to delete the save file for Slot {i}?", "Yes", "No"))
                        {
                            File.Delete(path);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.LabelField("No save data found for this profile slot.", EditorStyles.miniLabel);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSettings()
        {
            GUILayout.Label("Configuration Settings", boldLabelStyle);

            EditorGUILayout.BeginVertical(cardStyle);

            encryptionEnabled = EditorGUILayout.Toggle("Enable AES Encryption", encryptionEnabled);
            SaveManager.EncryptionEnabled = encryptionEnabled;

            if (encryptionEnabled)
            {
                encryptionPassword = EditorGUILayout.PasswordField("Encryption Password", encryptionPassword);
                SaveManager.EncryptionPassword = encryptionPassword;
            }

            EditorGUILayout.Space();

            autoSaveEnabled = EditorGUILayout.Toggle("Enable Auto-Save", autoSaveEnabled);
            if (autoSaveEnabled != SaveManager.AutoSaveEnabled)
            {
                SaveManager.AutoSaveEnabled = autoSaveEnabled;
                SaveManager.StartAutoSaveLoop();
            }

            if (autoSaveEnabled)
            {
                autoSaveInterval = EditorGUILayout.Slider("Auto-Save Interval (Sec)", autoSaveInterval, 10f, 600f);
                if (Math.Abs(autoSaveInterval - SaveManager.AutoSaveInterval) > 0.1f)
                {
                    SaveManager.AutoSaveInterval = autoSaveInterval;
                    SaveManager.StartAutoSaveLoop();
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.HelpBox("Save files are stored in the user's persistent data directory: \n" + Application.persistentDataPath, MessageType.Info);
        }

        private void InspectSaveFile(string path)
        {
            if (!File.Exists(path)) return;

            string content = File.ReadAllText(path);
            if (encryptionEnabled)
            {
                content = SaveEncryptor.Decrypt(content, encryptionPassword);
            }

            SaveInspectorPopup.ShowPopup(content);
        }
    }

    public class SaveInspectorPopup : EditorWindow
    {
        private string jsonContent;
        private Vector2 scrollPos;

        public static void ShowPopup(string json)
        {
            var window = CreateInstance<SaveInspectorPopup>();
            window.titleContent = new GUIContent("Save File Inspector");
            window.jsonContent = json;
            window.minSize = new Vector2(400, 450);
            window.ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(10, 10, 10, 10) });
            GUILayout.Label("Decrypted Save File Data (JSON)", EditorStyles.boldLabel);
            
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            EditorGUILayout.TextArea(jsonContent, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Close", GUILayout.Height(30)))
            {
                Close();
            }
            EditorGUILayout.EndVertical();
        }
    }
}
