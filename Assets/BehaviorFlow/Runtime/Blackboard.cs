using System;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviorFlow.Runtime
{
    [Serializable]
    public class BlackboardEntry
    {
        public string key;
        public enum ValueType { Float, Int, Bool, String, Vector3, GameObject }
        public ValueType type;

        public float floatVal;
        public int intVal;
        public bool boolVal;
        public string stringVal;
        public Vector3 vectorVal;
        public GameObject gameObjectVal;
    }

    [Serializable]
    public class Blackboard
    {
        public List<BlackboardEntry> entries = new List<BlackboardEntry>();

        public T GetValue<T>(string key)
        {
            var entry = entries.Find(e => e.key == key);
            if (entry == null) return default;

            if (typeof(T) == typeof(float)) return (T)(object)entry.floatVal;
            if (typeof(T) == typeof(int)) return (T)(object)entry.intVal;
            if (typeof(T) == typeof(bool)) return (T)(object)entry.boolVal;
            if (typeof(T) == typeof(string)) return (T)(object)entry.stringVal;
            if (typeof(T) == typeof(Vector3)) return (T)(object)entry.vectorVal;
            if (typeof(T) == typeof(GameObject)) return (T)(object)entry.gameObjectVal;

            return default;
        }

        public void SetValue<T>(string key, T val)
        {
            var entry = entries.Find(e => e.key == key);
            if (entry == null)
            {
                entry = new BlackboardEntry { key = key };
                entries.Add(entry);
            }

            if (val is float f) { entry.type = BlackboardEntry.ValueType.Float; entry.floatVal = f; }
            else if (val is int i) { entry.type = BlackboardEntry.ValueType.Int; entry.intVal = i; }
            else if (val is bool b) { entry.type = BlackboardEntry.ValueType.Bool; entry.boolVal = b; }
            else if (val is string s) { entry.type = BlackboardEntry.ValueType.String; entry.stringVal = s; }
            else if (val is Vector3 v) { entry.type = BlackboardEntry.ValueType.Vector3; entry.vectorVal = v; }
            else if (val is GameObject g) { entry.type = BlackboardEntry.ValueType.GameObject; entry.gameObjectVal = g; }
        }
        public bool HasKey(string key)
        {
            return entries.Exists(e => e.key == key);
        }
    }
}

