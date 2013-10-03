// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: KeyModifiers.cs
// Wrapper class around modifier keys used in GameInput Configurations.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using UnityEngine;

    /// <summary>
    /// Wrapper class around modifier keys used in GameInput Configurations. Originally a nested class
    /// in CameraControl, but wouldn't show in inspector once it was pushed to a base class.
    /// </summary>
    [Serializable]
    public class KeyModifiers {

        public bool altKeyReqd;
        public bool ctrlKeyReqd;
        public bool shiftKeyReqd;
        public bool appleKeyReqd;

        public bool confirmModifierKeyState() { // ^ = Exclusive OR
            return (!altKeyReqd ^ (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))) &&
                (!ctrlKeyReqd ^ (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) &&
                (!shiftKeyReqd ^ (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) &&
                (!appleKeyReqd ^ (Input.GetKey(KeyCode.LeftApple) || Input.GetKey(KeyCode.RightApple)));
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

