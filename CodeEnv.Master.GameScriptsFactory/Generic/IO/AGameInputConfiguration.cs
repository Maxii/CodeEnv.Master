// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGameInputConfiguration.cs
// Singleton. Abstract base class holding core GameInput Configuration nested classes.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;

/// <summary>
/// Singleton. Abstract base class holding core GameInput Configuration nested classes.
/// </summary>
[Serializable]
public abstract class AGameInputConfiguration<T> : AMonoBehaviourBaseSingletonInstanceIdentity<T> where T : AMonoBehaviourBase {

    public static GameInput gameInput;

    protected override void Awake() {
        base.Awake();
        gameInput = GameInput.Instance;
    }

    #region Nested Classes

    // KeyModifiers class, once moved here, had to be moved out to Common to show in inspector

    [Serializable]
    public abstract class ConfigurationBase {
        public bool activate;
        public KeyModifiers modifiers = new KeyModifiers();
        public float sensitivity = 1.0F;
        public virtual bool IsActivated() {
            return activate && modifiers.confirmModifierKeyState();
        }
    }

    #endregion

}

