// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GraphicsOptionSettings.cs
// Data wrapper class carrying all the settings available from the GraphicsOptionsMenu.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Data wrapper class carrying all the settings available from the GraphicsOptionsMenu.
    /// </summary>
    public class GraphicsOptionSettings {

        public string DebugName { get { return GetType().Name; } }

        public string QualitySetting { get; set; }

        // 1.15.17 TEMP removed to allow addition of DebugControls.IsElementIconsEnabled
        //public bool IsElementIconsEnabled { get; set; }

        public GraphicsOptionSettings() { }

        public override string ToString() {
            return DebugName;
        }

    }
}

