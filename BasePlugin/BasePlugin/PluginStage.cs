using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasePlugin
{
    // <summary>
    /// An enum with the options for the stage that the plugin is run
    /// </summary>
    public enum PluginStage
    {
        /// <summary>
        /// The plugin is running after the operation stage
        /// </summary>
        Postoperation = 40,

        /// <summary>
        /// The plugin is running before the operation stage
        /// </summary>
        Preoperation = 20,

        /// <summary>
        /// The plugin is running before the validation stage
        /// </summary>
        Prevalidation = 10,
    }
}
