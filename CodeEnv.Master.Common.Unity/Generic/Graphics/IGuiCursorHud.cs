// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IGuiCursorHud.cs
// Interface for GuiCursorHuds so non-scripts can refer to it.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common.Unity {

    /// <summary>
    /// Interface for GuiCursorHuds so non-scripts can refer to it.
    /// </summary>
    public interface IGuiCursorHud : IGuiHud {

        /// <summary>
        /// Populate the HUD with text from the GuiCursorHudText.
        /// </summary>
        /// <param name="guiCursorHudText">The GUI cursor hud text.</param>
        void Set(GuiCursorHudText guiCursorHudText);

    }
}

