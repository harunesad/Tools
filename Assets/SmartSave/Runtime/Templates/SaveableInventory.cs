using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartSave.Templates
{
    [Serializable]
    public class InventoryItem
    {
        public string itemID;
        public int quantity;
        public int slotIndex;

        public InventoryItem() { }
        public InventoryItem(string id, int qty, int slot)
        {
            itemID = id;
            quantity = qty;
            slotIndex = slot;
        }
    }

    /// <summary>
    /// A ready-to-use Saveable Inventory component. 
    /// Manages items, quantities, slots, and saves/loads automatically.
    /// </summary>
    [AddComponentMenu("SmartSave/Templates/Saveable Inventory")]
    public class SaveableInventory : MonoBehaviour, ISaveable
    {
        [Header("Unique Save ID")]
        [SerializeField] private string customSaveID = "player_inventory";

        [Header("Inventory Settings")]
        public int maxSlots = 20;

        [Header("Current Items")]
        [SerializeField] private List<InventoryItem> items = new List<InventoryItem>();

        public string SaveID => customSaveID;

        private void OnEnable()
        {
            SaveManager.Register(this);
        }

        private void OnDisable()
        {
            SaveManager.Unregister(this);
        }

        public List<InventoryItem> GetItems() => items;

        public bool AddItem(string itemID, int quantity, int slotIndex)
        {
            // Check if slot is already occupied
            var existing = items.Find(i => i.slotIndex == slotIndex);
            if (existing != null)
            {
                if (existing.itemID == itemID)
                {
                    existing.quantity += quantity;
                    return true;
                }
                return false; // Slot occupied by another item
            }

            if (items.Count >= maxSlots) return false;

            items.Add(new InventoryItem(itemID, quantity, slotIndex));
            return true;
        }

        public void RemoveItem(int slotIndex, int quantity)
        {
            var item = items.Find(i => i.slotIndex == slotIndex);
            if (item != null)
            {
                item.quantity -= quantity;
                if (item.quantity <= 0)
                {
                    items.Remove(item);
                }
            }
        }

        public void ClearInventory()
        {
            items.Clear();
        }

        // Capture State for ISaveable
        public object CaptureState()
        {
            // Wrap in a serializable wrapper since Unity JsonUtility doesn't serialize root lists directly
            SerializableInventoryData data = new SerializableInventoryData();
            data.savedItems = new List<InventoryItem>(items);
            return data;
        }

        // Restore State for ISaveable
        public void RestoreState(object state)
        {
            if (state is SerializableInventoryData data)
            {
                items = new List<InventoryItem>(data.savedItems);
                Debug.Log($"[SmartSave Template] Inventory loaded. Total unique item slots: {items.Count}");
            }
        }

        // Helper wrapper class for serialization
        [Serializable]
        private class SerializableInventoryData
        {
            public List<InventoryItem> savedItems;
        }
    }
}
