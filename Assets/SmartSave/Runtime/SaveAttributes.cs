using System;

namespace SmartSave
{
    /// <summary>
    /// Mark fields or properties with this attribute to automatically include them in the save system.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SaveableAttribute : Attribute
    {
        public string CustomKey { get; }

        public SaveableAttribute(string customKey = null)
        {
            CustomKey = customKey;
        }
    }
}
