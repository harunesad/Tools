using UnityEngine;

namespace SmartSave.Tests
{
    /// <summary>
    /// A test script demonstrating how to use the ISaveable interface for complex/custom types.
    /// Manually registers/unregisters and captures custom serializable class states.
    /// </summary>
    public class SmartSaveCustomTest : MonoBehaviour, ISaveable
    {
        [System.Serializable]
        public class InventoryData
        {
            public int goldAmount = 500;
            public string[] inventoryItems = new string[] { "Steel Sword", "Health Potion", "Mana Potion" };
        }

        public string SaveID => "player_inventory_system";
        public InventoryData inventory = new InventoryData();

        private void OnEnable()
        {
            SaveManager.Register(this);
        }

        private void OnDisable()
        {
            SaveManager.Unregister(this);
        }

        public object CaptureState()
        {
            return inventory;
        }

        public void RestoreState(object state)
        {
            if (state is InventoryData loadedInventory)
            {
                inventory = loadedInventory;
                Debug.Log($"[SmartSave] Restored Inventory. Gold: {inventory.goldAmount}, Items: {string.Join(", ", inventory.inventoryItems)}");
            }
        }
    }
}
