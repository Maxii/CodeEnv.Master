// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IGuiHud.cs
// Interface for GuiCursorHuds so non-scripts can refer to it.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for GuiCursorHuds so non-scripts can refer to it.
    /// </summary>
    public interface IGuiHud : IHud {

        /// <summary>
        /// Populate the HUD with text from the GuiCursorHudText.
        /// </summary>
        /// <param name="guiCursorHudText">The GUI cursor hud text.</param>
        void Set(GuiHudText guiCursorHudText);

    }
}

