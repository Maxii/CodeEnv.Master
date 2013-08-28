﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NguiGameInput.cs
// Static helper class for determining the state of Mouse controls
// using Ngui's default mouse input values. These input values are 
// different than Unitys.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Static helper class for determining the state of Mouse controls
    /// using Ngui's default mouse input values. These input values are 
    /// different than Unitys.
    /// </summary>
    public static class NguiGameInput {

        /// <summary>
        /// Detects a single click down of a mouse button.
        /// </summary>
        /// <param name="mouseButton">The mouse button.</param>
        /// <returns>
        ///   <c>true</c> if the mouseButton was clicked down during this frame; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsMouseButtonClick(NguiMouseButton mouseButton) {
            return UICamera.currentTouchID == (int)mouseButton;
        }

        /// <summary>
        /// Detects a single click down of a mouse button.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the mouseButton was clicked down during this frame; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsLeftMouseButtonClick() {
            return IsMouseButtonClick(NguiMouseButton.Left);
        }

        /// <summary>
        /// Detects a single click down of a mouse button.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the mouseButton was clicked down during this frame; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsRightMouseButtonClick() {
            return IsMouseButtonClick(NguiMouseButton.Right);
        }

        /// <summary>
        /// Detects a single click down of a mouse button.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the mouseButton was clicked down during this frame; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsMiddleMouseButtonClick() {
            return IsMouseButtonClick(NguiMouseButton.Middle);
        }



    }
}

