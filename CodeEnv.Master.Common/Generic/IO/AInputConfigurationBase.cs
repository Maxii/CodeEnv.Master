// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AInputConfigurationBase.cs
// Abstract base class for CameraControl and PlayerViews input configuration classes.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;

    /// <summary>
    /// Abstract base class for CameraControl and PlayerViews input 
    /// configuration classes.
    /// </summary>
    [Serializable]
    public abstract class AInputConfigurationBase {

        public bool activate;
        public KeyModifiers modifiers = new KeyModifiers();
        public float sensitivity = 1.0F;
        public virtual bool IsActivated() {
            return activate && modifiers.confirmModifierKeyState();
        }

    }
}

