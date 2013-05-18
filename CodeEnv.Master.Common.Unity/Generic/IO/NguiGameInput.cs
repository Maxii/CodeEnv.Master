// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NguiGameInput.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

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

