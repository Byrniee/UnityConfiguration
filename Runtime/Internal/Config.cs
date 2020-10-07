using System;

namespace Byrniee.UnityConfiguration.Internal
{
    /// <summary>
    /// Implementation of the config abstraction.
    /// </summary>
    /// <typeparam name="T">The config type.</typeparam>
    public class Config<T> : IConfig<T>
    {
        /// <inheritdoc />
        public T Value { get; set; }
    }
}
