using System;

namespace AssetImportRules.Editor.Core
{
    /// <summary>
    /// Explicit opt-in override wrapper so import rules modify only configured properties
    /// and leave all other artist settings untouched.
    /// </summary>
    [Serializable]
    public struct RuleOverride<T>
    {
        public bool enabled;
        public T value;

        public RuleOverride(bool enabled, T value)
        {
            this.enabled = enabled;
            this.value = value;
        }

        public static RuleOverride<T> On(T value) => new RuleOverride<T>(true, value);
        public static RuleOverride<T> Off(T defaultValue = default) => new RuleOverride<T>(false, defaultValue);
    }
}
