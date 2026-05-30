using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartSave.Templates
{
    [Serializable]
    public class ActiveQuest
    {
        public string questID;
        public int currentObjectiveIndex;
        public int objectiveProgress; // e.g. 3 of 5 kills

        public ActiveQuest() { }
        public ActiveQuest(string id, int objIndex, int progress)
        {
            questID = id;
            currentObjectiveIndex = objIndex;
            objectiveProgress = progress;
        }
    }

    /// <summary>
    /// A ready-to-use Quest Save System template component.
    /// Tracks active quests, current objective indexes, and completed quests.
    /// </summary>
    [AddComponentMenu("SmartSave/Templates/Saveable Quest State")]
    public class SaveableQuestState : MonoBehaviour, ISaveable
    {
        [Header("Unique Save ID")]
        [SerializeField] private string customSaveID = "player_quests";

        [Header("Quest States")]
        [SerializeField] private List<ActiveQuest> activeQuests = new List<ActiveQuest>();
        [SerializeField] private List<string> completedQuestIDs = new List<string>();

        public string SaveID => customSaveID;

        private void OnEnable()
        {
            SaveManager.Register(this);
        }

        private void OnDisable()
        {
            SaveManager.Unregister(this);
        }

        public List<ActiveQuest> GetActiveQuests() => activeQuests;
        public List<string> GetCompletedQuests() => completedQuestIDs;

        public void StartQuest(string questID)
        {
            if (!completedQuestIDs.Contains(questID) && !activeQuests.Exists(q => q.questID == questID))
            {
                activeQuests.Add(new ActiveQuest(questID, 0, 0));
            }
        }

        public void UpdateQuestProgress(string questID, int objectiveIndex, int progress)
        {
            var quest = activeQuests.Find(q => q.questID == questID);
            if (quest != null)
            {
                quest.currentObjectiveIndex = objectiveIndex;
                quest.objectiveProgress = progress;
            }
        }

        public void CompleteQuest(string questID)
        {
            var quest = activeQuests.Find(q => q.questID == questID);
            if (quest != null)
            {
                activeQuests.Remove(quest);
            }
            if (!completedQuestIDs.Contains(questID))
            {
                completedQuestIDs.Add(questID);
            }
        }

        // Capture State for ISaveable
        public object CaptureState()
        {
            SerializableQuestData data = new SerializableQuestData();
            data.savedActiveQuests = new List<ActiveQuest>(activeQuests);
            data.savedCompletedQuests = new List<string>(completedQuestIDs);
            return data;
        }

        // Restore State for ISaveable
        public void RestoreState(object state)
        {
            if (state is SerializableQuestData data)
            {
                activeQuests = new List<ActiveQuest>(data.savedActiveQuests);
                completedQuestIDs = new List<string>(data.savedCompletedQuests);
                Debug.Log($"[SmartSave Template] Quests loaded. Active: {activeQuests.Count}, Completed: {completedQuestIDs.Count}");
            }
        }

        [Serializable]
        private class SerializableQuestData
        {
            public List<ActiveQuest> savedActiveQuests;
            public List<string> savedCompletedQuests;
        }
    }
}
