using System;

namespace AssetImportRules.Editor.Core
{
    /// <summary>
    /// Explicit opt-in override wrapper so import rules modify only configured properties
    /// and leave all other artist settings untouched.
    /// </summary>
    /// <typeparam name="T">Importer property value type controlled by this override.</typeparam>
    [Serializable]
    public struct RuleOverride<T>
    {
        /// <summary>When <c>true</c>, <see cref="value"/> is checked during audits and enforced during apply.</summary>
        public bool enabled;
        /// <summary>Target importer property value used when <see cref="enabled"/> is <c>true</c>.</summary>
        public T value;

        /// <summary>
        /// Initializes a new <see cref="RuleOverride{T}"/> with an explicit enable flag and value.
        /// </summary>
        /// <param name="enabled">Whether this property override is active.</param>
        /// <param name="value">Property value to enforce when enabled.</param>
        public RuleOverride(bool enabled, T value)
        {
            this.enabled = enabled;
            this.value = value;
        }

        /// <summary>
        /// Creates an enabled override that enforces <paramref name="value"/>.
        /// </summary>
        /// <param name="value">Target value to audit and apply.</param>
        /// <returns>An enabled <see cref="RuleOverride{T}"/> instance.</returns>
        public static RuleOverride<T> On(T value) => new RuleOverride<T>(true, value);

        /// <summary>
        /// Creates a disabled override that leaves the importer property untouched.
        /// </summary>
        /// <param name="defaultValue">Default value shown in the Inspector while disabled.</param>
        /// <returns>A disabled <see cref="RuleOverride{T}"/> instance.</returns>
        public static RuleOverride<T> Off(T defaultValue = default) => new RuleOverride<T>(false, defaultValue);
    }
}
