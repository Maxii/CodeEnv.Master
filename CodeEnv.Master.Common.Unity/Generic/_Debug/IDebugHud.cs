// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IDebugHud.cs
// Interface for DebugHuds so non-scripts can refer to it.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common.Unity {

    /// <summary>
    /// Interface for DebugHuds so non-scripts can refer to it.
    /// </summary>
    public interface IDebugHud : IGuiHud {

        DebugHudText DebugHudText { get; }

        /// <summary>
        /// Populate the HUD with text from the DebugHudText.
        /// </summary>
        /// <param name="debugHudText">The DebugHudText.</param>
        void Set(DebugHudText debugHudText);

    }
}

