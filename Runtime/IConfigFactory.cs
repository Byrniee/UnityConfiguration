using System;

namespace Byrniee.UnityConfiguration
{
    /// <summary>
    /// Factory abstraction class for creating the config classes.
    /// </summary>
    public interface IConfigFactory
    {
        /// <summary>
        /// Reads the config.json file next to the exe and returns the 
        /// config class. 
        /// </summary>
        /// <param name="index">The config index.</param>
        /// <typeparam name="T">The config type.</typeparam>
        /// <returns>The config class.</returns>
        IConfig<T> Create<T>(string index);
    }
}
