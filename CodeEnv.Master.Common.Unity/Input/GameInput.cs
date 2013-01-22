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
    using CodeEnv.Master.Common.Resources;
    using UnityEngine;
    using UnityEditor;

    public static class GameInput {

        public static bool IsMouseButtonDown(MouseButton mouseButton) {
            return Input.GetMouseButton((int)mouseButton);
        }

        public static bool IsLeftMouseButtonDown() {
            return IsMouseButtonDown(MouseButton.Left);
        }

        public static bool IsRightMouseButtonDown() {
            return IsMouseButtonDown(MouseButton.Right);
        }

        public static bool IsMiddleMouseButtonDown() {
            return IsMouseButtonDown(MouseButton.Middle);
        }

        public static bool IsAnyMouseButtonDown() {
            return IsLeftMouseButtonDown() || IsRightMouseButtonDown() || IsMiddleMouseButtonDown();
        }

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

