using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SmartSave
{
    [System.Serializable]
    public class SaveEntry
    {
        public string Key;
        public string Value; // JSON string of the serialized data
    }

    [System.Serializable]
    public class SaveData
    {
        public string ProfileName;
        public string SavedSceneName;
        public string SavedTime;
        public float PlayTime;
        public List<SaveEntry> Entries = new List<SaveEntry>();
    }

    public class SaveManager : MonoBehaviour
    {
        private static SaveManager instance;
        public static SaveManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("SmartSaveManager");
                    instance = go.AddComponent<SaveManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        // Configuration Settings
        public static int ActiveSlot { get; private set; } = 1;
        public static bool EncryptionEnabled { get; set; } = true;
        public static string EncryptionPassword { get; set; } = "SmartSavePass123!";
        public static bool AutoSaveEnabled { get; set; } = false;
        public static float AutoSaveInterval { get; set; } = 300f; // 5 minutes

        private static List<MonoBehaviour> registeredBehaviours = new List<MonoBehaviour>();
        private static List<ISaveable> registeredSaveables = new List<ISaveable>();

        private float playTimeSessionStart;
        private float accumulatedPlayTime = 0f;
        private Coroutine autoSaveCoroutine;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            playTimeSessionStart = Time.time;
        }

        private void Start()
        {
            StartAutoSaveLoop();
        }

        public static void SetActiveSlot(int slot)
        {
            ActiveSlot = slot;
        }

        public static void Register(MonoBehaviour behaviour)
        {
            if (behaviour == null) return;

            if (behaviour is ISaveable saveable)
            {
                if (!registeredSaveables.Contains(saveable))
                    registeredSaveables.Add(saveable);
            }
            else
            {
                if (!registeredBehaviours.Contains(behaviour))
                    registeredBehaviours.Add(behaviour);
            }
        }

        public static void Unregister(MonoBehaviour behaviour)
        {
            if (behaviour == null) return;

            if (behaviour is ISaveable saveable)
            {
                registeredSaveables.Remove(saveable);
            }
            else
            {
                registeredBehaviours.Remove(behaviour);
            }
        }

        public static string GetSavePath(int slot)
        {
            return Path.Combine(Application.persistentDataPath, $"save_slot_{slot}.json");
        }

        public static void StartAutoSaveLoop()
        {
            if (Instance.autoSaveCoroutine != null)
            {
                Instance.StopCoroutine(Instance.autoSaveCoroutine);
            }

            if (AutoSaveEnabled)
            {
                Instance.autoSaveCoroutine = Instance.StartCoroutine(Instance.AutoSaveLoop());
            }
        }

        private IEnumerator AutoSaveLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(AutoSaveInterval);
                Save();
                Debug.Log("[SmartSave] Auto-saved successfully.");
            }
        }

        /// <summary>
        /// Saves all registered saveable components in the active scene to the active slot.
        /// </summary>
        public static void Save()
        {
            if (registeredBehaviours.Count == 0 && registeredSaveables.Count == 0)
            {
                ScanSceneForSaveables();
            }

            SaveData saveData = new SaveData();
            saveData.ProfileName = $"Slot {ActiveSlot}";
            saveData.SavedSceneName = SceneManager.GetActiveScene().name;
            saveData.SavedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            saveData.PlayTime = Instance.accumulatedPlayTime + (Time.time - Instance.playTimeSessionStart);

            // 1. Process custom ISaveable components
            foreach (var saveable in registeredSaveables)
            {
                if (saveable == null) continue;
                string saveID = saveable.SaveID;
                if (string.IsNullOrEmpty(saveID)) continue;

                object state = saveable.CaptureState();
                if (state != null)
                {
                    string jsonState = SerializeValue(state);
                    saveData.Entries.Add(new SaveEntry { Key = $"ISaveable_{saveID}", Value = jsonState });
                }
            }

            // 2. Process [Saveable] attributes via reflection
            foreach (var behaviour in registeredBehaviours)
            {
                if (behaviour == null) continue;
                string baseKey = GetUniqueIdentifier(behaviour);

                Type type = behaviour.GetType();
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (var field in fields)
                {
                    var attr = field.GetCustomAttribute<SaveableAttribute>();
                    if (attr != null)
                    {
                        string customKey = attr.CustomKey ?? field.Name;
                        string key = $"{baseKey}_{customKey}";
                        object val = field.GetValue(behaviour);
                        string serializedVal = SerializeValue(val);
                        saveData.Entries.Add(new SaveEntry { Key = key, Value = serializedVal });
                    }
                }

                foreach (var prop in properties)
                {
                    if (!prop.CanRead || !prop.CanWrite) continue;
                    var attr = prop.GetCustomAttribute<SaveableAttribute>();
                    if (attr != null)
                    {
                        string customKey = attr.CustomKey ?? prop.Name;
                        string key = $"{baseKey}_{customKey}";
                        object val = prop.GetValue(behaviour);
                        string serializedVal = SerializeValue(val);
                        saveData.Entries.Add(new SaveEntry { Key = key, Value = serializedVal });
                    }
                }
            }

            // Serialize profile data
            string fullJson = JsonUtility.ToJson(saveData, true);

            // Encrypt if enabled
            if (EncryptionEnabled)
            {
                fullJson = SaveEncryptor.Encrypt(fullJson, EncryptionPassword);
            }

            // Write file
            string path = GetSavePath(ActiveSlot);
            File.WriteAllText(path, fullJson);
        }

        /// <summary>
        /// Loads save data from the active slot and restores all states in the scene.
        /// </summary>
        public static void Load()
        {
            string path = GetSavePath(ActiveSlot);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[SmartSave] Save file not found at path: {path}");
                return;
            }

            if (registeredBehaviours.Count == 0 && registeredSaveables.Count == 0)
            {
                ScanSceneForSaveables();
            }

            string fileContent = File.ReadAllText(path);

            // Decrypt if enabled
            if (EncryptionEnabled)
            {
                fileContent = SaveEncryptor.Decrypt(fileContent, EncryptionPassword);
            }

            SaveData saveData;
            try
            {
                saveData = JsonUtility.FromJson<SaveData>(fileContent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SmartSave] Failed to parse save file. It may be corrupted or encrypted with a different key. Error: {ex.Message}");
                return;
            }

            if (saveData == null) return;

            Instance.accumulatedPlayTime = saveData.PlayTime;
            Instance.playTimeSessionStart = Time.time;

            // Map entries to a helper dictionary for O(1) lookups
            Dictionary<string, string> entryMap = new Dictionary<string, string>();
            foreach (var entry in saveData.Entries)
            {
                entryMap[entry.Key] = entry.Value;
            }

            // 1. Restore Custom ISaveable components
            foreach (var saveable in registeredSaveables)
            {
                if (saveable == null) continue;
                string saveID = saveable.SaveID;
                if (string.IsNullOrEmpty(saveID)) continue;

                string key = $"ISaveable_{saveID}";
                if (entryMap.TryGetValue(key, out string jsonState))
                {
                    object dummy = saveable.CaptureState();
                    if (dummy != null)
                    {
                        object deserialized = DeserializeValue(jsonState, dummy.GetType());
                        saveable.RestoreState(deserialized);
                    }
                }
            }

            // 2. Restore [Saveable] attributes
            foreach (var behaviour in registeredBehaviours)
            {
                if (behaviour == null) continue;
                string baseKey = GetUniqueIdentifier(behaviour);

                Type type = behaviour.GetType();
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (var field in fields)
                {
                    var attr = field.GetCustomAttribute<SaveableAttribute>();
                    if (attr != null)
                    {
                        string customKey = attr.CustomKey ?? field.Name;
                        string key = $"{baseKey}_{customKey}";
                        if (entryMap.TryGetValue(key, out string serializedVal))
                        {
                            object val = DeserializeValue(serializedVal, field.FieldType);
                            field.SetValue(behaviour, val);
                        }
                    }
                }

                foreach (var prop in properties)
                {
                    if (!prop.CanRead || !prop.CanWrite) continue;
                    var attr = prop.GetCustomAttribute<SaveableAttribute>();
                    if (attr != null)
                    {
                        string customKey = attr.CustomKey ?? prop.Name;
                        string key = $"{baseKey}_{customKey}";
                        if (entryMap.TryGetValue(key, out string serializedVal))
                        {
                            object val = DeserializeValue(serializedVal, prop.PropertyType);
                            prop.SetValue(behaviour, val);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Reads the save file to find the saved scene name and loads it asynchronously.
        /// </summary>
        public static void LoadSavedScene()
        {
            string path = GetSavePath(ActiveSlot);
            if (!File.Exists(path)) return;

            string fileContent = File.ReadAllText(path);
            if (EncryptionEnabled)
            {
                fileContent = SaveEncryptor.Decrypt(fileContent, EncryptionPassword);
            }

            SaveData saveData = JsonUtility.FromJson<SaveData>(fileContent);
            if (saveData != null && !string.IsNullOrEmpty(saveData.SavedSceneName))
            {
                SceneManager.LoadScene(saveData.SavedSceneName);
            }
        }

        /// <summary>
        /// Scan the current scene to automatically find [Saveable] objects and ISaveables.
        /// </summary>
        public static void ScanSceneForSaveables()
        {
            registeredBehaviours.Clear();
            registeredSaveables.Clear();

            MonoBehaviour[] allBehaviours = FindObjectsOfType<MonoBehaviour>();
            foreach (var b in allBehaviours)
            {
                if (b == null) continue;

                if (b is ISaveable)
                {
                    Register(b);
                }
                else
                {
                    Type type = b.GetType();
                    var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    bool hasAttribute = false;
                    foreach (var f in fields)
                    {
                        if (f.GetCustomAttribute<SaveableAttribute>() != null) { hasAttribute = true; break; }
                    }
                    if (!hasAttribute)
                    {
                        foreach (var p in properties)
                        {
                            if (p.GetCustomAttribute<SaveableAttribute>() != null) { hasAttribute = true; break; }
                        }
                    }

                    if (hasAttribute)
                    {
                        Register(b);
                    }
                }
            }
        }

        public static List<MonoBehaviour> GetRegisteredBehaviours() => registeredBehaviours;
        public static List<ISaveable> GetRegisteredSaveables() => registeredSaveables;

        #region Helper Methods

        private static string GetUniqueIdentifier(MonoBehaviour behaviour)
        {
            return $"{behaviour.gameObject.scene.name}/{GetGameObjectPath(behaviour.gameObject)}/{behaviour.GetType().Name}";
        }

        private static string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = obj.name + "/" + path;
            }
            return path;
        }

        public static string SerializeValue(object val)
        {
            if (val == null) return string.Empty;

            Type type = val.GetType();

            if (type.IsPrimitive || val is string)
            {
                return val.ToString();
            }
            if (val is Vector3 v3)
            {
                return JsonUtility.ToJson(new SerializableVector3(v3));
            }
            if (val is Vector2 v2)
            {
                return JsonUtility.ToJson(new SerializableVector2(v2));
            }
            if (val is Quaternion q)
            {
                return JsonUtility.ToJson(new SerializableQuaternion(q));
            }
            if (val is Color c)
            {
                return JsonUtility.ToJson(new SerializableColor(c));
            }
            if (val is DateTime dt)
            {
                return dt.ToString("o");
            }
            if (val is Dictionary<string, string> dictStr)
            {
                return JsonUtility.ToJson(new SerializableDictionary<string, string>(dictStr));
            }
            if (val is Dictionary<string, int> dictInt)
            {
                return JsonUtility.ToJson(new SerializableDictionary<string, int>(dictInt));
            }
            if (val is Dictionary<string, float> dictFloat)
            {
                return JsonUtility.ToJson(new SerializableDictionary<string, float>(dictFloat));
            }

            return JsonUtility.ToJson(val);
        }

        public static object DeserializeValue(string serialized, Type targetType)
        {
            if (string.IsNullOrEmpty(serialized)) return null;

            try
            {
                if (targetType == typeof(string)) return serialized;
                if (targetType == typeof(int)) return int.Parse(serialized);
                if (targetType == typeof(float)) return float.Parse(serialized);
                if (targetType == typeof(bool)) return bool.Parse(serialized);
                if (targetType == typeof(double)) return double.Parse(serialized);
                if (targetType.IsEnum) return Enum.Parse(targetType, serialized);

                if (targetType == typeof(Vector3))
                {
                    return JsonUtility.FromJson<SerializableVector3>(serialized).ToVector3();
                }
                if (targetType == typeof(Vector2))
                {
                    return JsonUtility.FromJson<SerializableVector2>(serialized).ToVector2();
                }
                if (targetType == typeof(Quaternion))
                {
                    return JsonUtility.FromJson<SerializableQuaternion>(serialized).ToQuaternion();
                }
                if (targetType == typeof(Color))
                {
                    return JsonUtility.FromJson<SerializableColor>(serialized).ToColor();
                }
                if (targetType == typeof(DateTime))
                {
                    return DateTime.Parse(serialized);
                }
                if (targetType == typeof(Dictionary<string, string>))
                {
                    return JsonUtility.FromJson<SerializableDictionary<string, string>>(serialized).ToDictionary();
                }
                if (targetType == typeof(Dictionary<string, int>))
                {
                    return JsonUtility.FromJson<SerializableDictionary<string, int>>(serialized).ToDictionary();
                }
                if (targetType == typeof(Dictionary<string, float>))
                {
                    return JsonUtility.FromJson<SerializableDictionary<string, float>>(serialized).ToDictionary();
                }

                return JsonUtility.FromJson(serialized, targetType);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SmartSave] Failed to deserialize value: {serialized} to type {targetType}. Error: {ex.Message}");
                return null;
            }
        }

        #endregion
    }
}
