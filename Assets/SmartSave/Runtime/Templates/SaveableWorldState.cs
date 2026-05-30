using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartSave.Templates
{
    [Serializable]
    public class WorldEvent
    {
        public string eventID; // e.g. "bridge_lowered", "boss_defeated_01"
        public bool isTriggered;
        public string timestamp;

        public WorldEvent() { }
        public WorldEvent(string id, bool state)
        {
            eventID = id;
            isTriggered = state;
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    /// <summary>
    /// A ready-to-use World State manager. Saves triggered world events,
    /// boss kills, door/lever positions, and level progression details.
    /// </summary>
    [AddComponentMenu("SmartSave/Templates/Saveable World State")]
    public class SaveableWorldState : MonoBehaviour, ISaveable
    {
        [Header("Unique Save ID")]
        [SerializeField] private string customSaveID = "world_state_manager";

        [Header("Tracked World Events")]
        [SerializeField] private List<WorldEvent> worldEvents = new List<WorldEvent>();

        public string SaveID => customSaveID;

        private void OnEnable()
        {
            SaveManager.Register(this);
        }

        private void OnDisable()
        {
            SaveManager.Unregister(this);
        }

        public bool IsEventTriggered(string eventID)
        {
            var ev = worldEvents.Find(e => e.eventID == eventID);
            return ev != null && ev.isTriggered;
        }

        public void SetEventState(string eventID, bool isTriggered)
        {
            var ev = worldEvents.Find(e => e.eventID == eventID);
            if (ev != null)
            {
                ev.isTriggered = isTriggered;
                ev.timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                worldEvents.Add(new WorldEvent(eventID, isTriggered));
            }
        }

        public void ClearEvents()
        {
            worldEvents.Clear();
        }

        // Capture State for ISaveable
        public object CaptureState()
        {
            SerializableWorldData data = new SerializableWorldData();
            data.savedEvents = new List<WorldEvent>(worldEvents);
            return data;
        }

        // Restore State for ISaveable
        public void RestoreState(object state)
        {
            if (state is SerializableWorldData data)
            {
                worldEvents = new List<WorldEvent>(data.savedEvents);
                Debug.Log($"[SmartSave Template] World state loaded. Total tracked events: {worldEvents.Count}");
            }
        }

        [Serializable]
        private class SerializableWorldData
        {
            public List<WorldEvent> savedEvents;
        }
    }
}
