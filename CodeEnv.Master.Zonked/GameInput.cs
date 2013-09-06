// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameInput.cs
// Static helper class for determining the state of Mouse controls
// using Unity's default mouse input values.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR


namespace CodeEnv.Master.Common.Unity {

    using CodeEnv.Master.Common;
    using UnityEngine;


    /// <summary>
    /// Static helper class for determining the state of Mouse controls
    /// using Unity's default mouse input values.
    /// </summary>
    public static class GameInput {

        /// <summary>
        /// Detects a single click down of a mouse button.
        /// </summary>
        /// <param name="mouseButton">The mouse button.</param>
        /// <returns>
        ///   <c>true</c> if the mouseButton was clicked down during this frame; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsMouseButtonClick(UnityMouseButton mouseButton) {
            return Input.GetMouseButtonDown((int)mouseButton);
        }

        /// <summary>
        /// Detects a single click down of a mouse button.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the mouseButton was clicked down during this frame; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsLeftMouseButtonClick() {
            return IsMouseButtonClick(UnityMouseButton.Left);
        }

        /// <summary>
        /// Detects a single click down of a mouse button.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the mouseButton was clicked down during this frame; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsRightMouseButtonClick() {
            return IsMouseButtonClick(UnityMouseButton.Right);
        }

        /// <summary>
        /// Detects a single click down of a mouse button.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the mouseButton was clicked down during this frame; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsMiddleMouseButtonClick() {
            return IsMouseButtonClick(UnityMouseButton.Middle);
        }



        /// <summary>
        /// Detects a single click up of a mouse button.
        /// </summary>
        /// <param name="mouseButton">The mouse button.</param>
        /// <returns>
        ///   <c>true</c>if the mouseButton was clicked up during this frame; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsMouseButtonClickReleased(UnityMouseButton mouseButton) {
            return Input.GetMouseButtonUp((int)mouseButton);
        }

        /// <summary>
        /// Detects a single click up of a mouse button.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the mouseButton was clicked up during this frame; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsLeftMouseButtonClickReleased() {
            return IsMouseButtonClickReleased(UnityMouseButton.Left);
        }

        /// <summary>
        /// Detects a single click up of a mouse button.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the mouseButton was clicked up during this frame; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsRightMouseButtonClickReleased() {
            return IsMouseButtonClickReleased(UnityMouseButton.Right);
        }

        /// <summary>
        /// Detects a single click up of a mouse button.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the mouseButton was clicked up during this frame; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsMiddleMouseButtonClickReleased() {
            return IsMouseButtonClickReleased(UnityMouseButton.Middle);
        }

        /// <summary>
        /// Detects whether a mouse button is being held down across multiple frames.
        /// </summary>
        /// <param name="mouseButton">The mouse button.</param>
        /// <returns>
        ///   <c>true</c> if the mouse button is being held down; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsMouseButtonDown(UnityMouseButton mouseButton) {
            return Input.GetMouseButton((int)mouseButton);
        }

        /// <summary>
        /// Detects whether a mouse button is being held down across multiple frames.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the mouse button is being held down; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsLeftMouseButtonDown() {
            return IsMouseButtonDown(UnityMouseButton.Left);
        }

        /// <summary>
        /// Detects whether a mouse button is being held down across multiple frames.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the mouse button is being held down; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsRightMouseButtonDown() {
            return IsMouseButtonDown(UnityMouseButton.Right);
        }

        /// <summary>
        /// Detects whether a mouse button is being held down across multiple frames.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the mouse button is being held down; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsMiddleMouseButtonDown() {
            return IsMouseButtonDown(UnityMouseButton.Middle);
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
        public static bool IsAnyMouseButtonDownBesides(UnityMouseButton mouseButton) {
            foreach (UnityMouseButton button in Enums<UnityMouseButton>.GetValues()) {
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
            delta = Input.GetAxis(UnityConstants.MouseAxisName_ScrollWheel);
            D.Log("Mouse ScrollWheel value = {0:0.0000}.", delta);
            return delta != 0F; // No floating point equality issues as value is smoothed by Unity
        }

        public static bool IsHorizontalMouseMovement(out float value) {
            value = Input.GetAxis(UnityConstants.MouseAxisName_Horizontal);
            D.Log("Mouse Horizontal Movement value = {0:0.0000}.", value);
            return value != 0F; // No floating point equality issues as value is smoothed by Unity
        }

        public static bool IsVerticalMouseMovement(out float value) {
            value = Input.GetAxis(UnityConstants.MouseAxisName_Vertical);
            D.Log("Mouse Vertical Movement value = {0:0.0000}.", value);
            return value != 0F; // No floating point equality issues as value is smoothed by Unity
        }
    }
}

