using System.Collections.Generic;

namespace Byrniee.UnityConfiguration
{
    /// <summary>
    /// Information about what config was loaded, for logging at startup.
    /// </summary>
    public interface IConfigDiagnostics
    {
        /// <summary>
        /// Gets the resolved environment name, or null if no environment is set.
        /// </summary>
        string EnvironmentName { get; }

        /// <summary>
        /// Gets the full paths of the config files that were found and merged, in the order they were applied.
        /// </summary>
        IReadOnlyList<string> LoadedFiles { get; }

        /// <summary>
        /// Gets the <c>.env</c> and process environment variable keys (such as <c>ApiSettings__ApiKey</c>)
        /// that were applied as overrides, in the order they were first applied. Values are not included,
        /// so this is safe to log.
        /// </summary>
        IReadOnlyList<string> OverriddenKeys { get; }

        /// <summary>
        /// Gets the top-level sections that were loaded but have not been requested through
        /// <see cref="IConfigFactory.Create{T}"/>. Call after startup to spot misspelt or stale sections.
        /// </summary>
        /// <returns>The unrequested section names.</returns>
        IReadOnlyCollection<string> GetUnrequestedSections();
    }
}
