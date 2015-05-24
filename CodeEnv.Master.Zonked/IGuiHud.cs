﻿// --------------------------------------------------------------------------------------------------------------------
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

    using UnityEngine;

    /// <summary>
    /// Interface for GuiCursorHuds so non-scripts can refer to it.
    /// </summary>
    [System.Obsolete]
    public interface IGuiHud : IHud {

        /// <summary>
        /// Populate the HUD with text pulled from LabelText.
        /// </summary>
        /// <param name="labelText">The label text.</param>
        /// <param name="position">The position of the GameObject this HUD info represents.</param>
        void Set(ALabelText labelText, Vector3 position);

        void Set(string text, Vector3 position);


    }
}

