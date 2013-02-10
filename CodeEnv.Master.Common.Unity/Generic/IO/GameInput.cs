// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameInput.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;
    using UnityEditor;

    public static class GameInput {

        /// <summary>
        /// Detects a single click down of a mouse button.
        /// </summary>
        /// <param name="mouseButton">The mouse button.</param>
        /// <returns>
        ///   <c>true</c> if the mouseButton was clicked down during this frame; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsMouseButtonClick(MouseButton mouseButton) {
            return Input.GetMouseButtonDown((int)mouseButton);
        }

        /// <summary>
        /// Detects a single click down of a mouse button.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the mouseButton was clicked down during this frame; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsLeftMouseButtonClick() {
            return IsMouseButtonClick(MouseButton.Left);
        }

        /// <summary>
        /// Detects a single click down of a mouse button.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the mouseButton was clicked down during this frame; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsRightMouseButtonClick() {
            return IsMouseButtonClick(MouseButton.Right);
        }

        /// <summary>
        /// Detects a single click down of a mouse button.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the mouseButton was clicked down during this frame; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsMiddleMouseButtonClick() {
            return IsMouseButtonClick(MouseButton.Middle);
        }



        /// <summary>
        /// Detects a single click up of a mouse button.
        /// </summary>
        /// <param name="mouseButton">The mouse button.</param>
        /// <returns>
        ///   <c>true</c>if the mouseButton was clicked up during this frame; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsMouseButtonClickReleased(MouseButton mouseButton) {
            return Input.GetMouseButtonUp((int)mouseButton);
        }

        /// <summary>
        /// Detects a single click up of a mouse button.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the mouseButton was clicked up during this frame; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsLeftMouseButtonClickReleased() {
            return IsMouseButtonClickReleased(MouseButton.Left);
        }

        /// <summary>
        /// Detects a single click up of a mouse button.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the mouseButton was clicked up during this frame; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsRightMouseButtonClickReleased() {
            return IsMouseButtonClickReleased(MouseButton.Right);
        }

        /// <summary>
        /// Detects a single click up of a mouse button.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the mouseButton was clicked up during this frame; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsMiddleMouseButtonClickReleased() {
            return IsMouseButtonClickReleased(MouseButton.Middle);
        }




        /// <summary>
        /// Detects whether a mouse button is being held down across multiple frames.
        /// </summary>
        /// <param name="mouseButton">The mouse button.</param>
        /// <returns>
        ///   <c>true</c> if the mouse button is being held down; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsMouseButtonDown(MouseButton mouseButton) {
            return Input.GetMouseButton((int)mouseButton);
        }

        /// <summary>
        /// Detects whether a mouse button is being held down across multiple frames.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the mouse button is being held down; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsLeftMouseButtonDown() {
            return IsMouseButtonDown(MouseButton.Left);
        }

        /// <summary>
        /// Detects whether a mouse button is being held down across multiple frames.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the mouse button is being held down; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsRightMouseButtonDown() {
            return IsMouseButtonDown(MouseButton.Right);
        }

        /// <summary>
        /// Detects whether a mouse button is being held down across multiple frames.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the mouse button is being held down; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsMiddleMouseButtonDown() {
            return IsMouseButtonDown(MouseButton.Middle);
        }

        /// <summary>
        /// Detects whether any mouse button is being held down across multiple frames.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if any mouse button is being held down; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAnyMouseButtonDown() {
            return IsLeftMouseButtonDown() || IsRightMouseButtonDown() || IsMiddleMouseButtonDown();
        }

        /// <summary>
        /// Detects whether any mouse button besides the one provided is being held down across multiple frames.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if any other mouse button is being held down; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAnyMouseButtonDownBesides(MouseButton mouseButton) {
            foreach (MouseButton button in Enums<MouseButton>.GetValues()) {
                if (button != mouseButton) {
                    if (IsMouseButtonDown(button)) {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool IsAnyKeyOrMouseButtonDown() {
            return Input.anyKey;
        }

        public static bool IsScrollWheelMovement(out float value) {
            value = Input.GetAxis(UnityConstants.MouseAxisName_ScrollWheel);
            return value != 0F;
        }

        public static bool IsHorizontalMouseMovement(out float value) {
            value = Input.GetAxis(UnityConstants.MouseAxisName_Horizontal);
            return value != 0F;
        }

        public static bool IsVerticalMouseMovement(out float value) {
            value = Input.GetAxis(UnityConstants.MouseAxisName_Vertical);
            return value != 0F;
        }
    }
}

