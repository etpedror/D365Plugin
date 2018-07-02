using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasePlugin
{
    /// <summary>
    /// An enum with the modes that the plugin might be running
    /// </summary>
    public enum PluginMode
    {
        /// <summary>
        /// The plugin is running Asynchronously
        /// </summary>
        Asynchronous = 1,
        /// <summary>
        /// The plugin is running Synchronously
        /// </summary>
        Synchronous = 0,
    }
}
