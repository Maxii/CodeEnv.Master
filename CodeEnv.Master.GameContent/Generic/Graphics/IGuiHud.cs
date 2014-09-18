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

using UnityEngine;
namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for GuiCursorHuds so non-scripts can refer to it.
    /// </summary>
    public interface IGuiHud : IHud {

        /// <summary>
        /// Populate the HUD with text from the GuiCursorHudText.
        /// </summary>
        /// <param name="guiCursorHudText">The GUI cursor hud text.</param>
        /// <param name="position">The position of the GameObject this HUD info represents.</param>
        void Set(GuiHudText guiCursorHudText, Vector3 position);


    }
}

