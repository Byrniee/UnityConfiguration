namespace Byrniee.UnityConfiguration
{
    /// <summary>
    /// Abstraction interface for a config.
    /// </summary>
    /// <typeparam name="T">The config type.</typeparam>
    public interface IConfig<T>
    {
        /// <summary>
        /// Gets the config.
        /// </summary>
        T Value { get; }
    }
}