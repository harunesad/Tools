using UnityEngine;

namespace SmartSave.Templates
{
    /// <summary>
    /// Out-of-the-box templates for RPG/Action games to save player status, level, gold, and health.
    /// Just attach to your Player prefab.
    /// </summary>
    [AddComponentMenu("SmartSave/Templates/Player Progression")]
    public class PlayerProgression : SaveableBehaviour
    {
        [Header("Character Identity")]
        [Saveable] public string playerName = "New Hero";

        [Header("Character Stats")]
        [Saveable] public int level = 1;
        [Saveable] public int currentXP = 0;
        [Saveable] public int requiredXP = 100;

        [Header("Currencies")]
        [Saveable] public int gold = 100;
        [Saveable] public int premiumCurrency = 0;

        [Header("Vitals")]
        [Saveable] public float maxHealth = 100f;
        [Saveable] public float currentHealth = 100f;
        [Saveable] public float maxMana = 50f;
        [Saveable] public float currentMana = 50f;

        public void AddXP(int amount)
        {
            currentXP += amount;
            if (currentXP >= requiredXP)
            {
                currentXP -= requiredXP;
                level++;
                requiredXP = Mathf.RoundToInt(requiredXP * 1.5f);
                Debug.Log($"[SmartSave Template] Level Up! Level: {level}");
            }
        }

        public void AddGold(int amount)
        {
            gold += amount;
        }

        public bool SpendGold(int amount)
        {
            if (gold >= amount)
            {
                gold -= amount;
                return true;
            }
            return false;
        }

        public void Heal(float amount)
        {
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        }

        public void TakeDamage(float amount)
        {
            currentHealth = Mathf.Max(currentHealth - amount, 0f);
        }
    }
}
