// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IEffect.cs
// Interface for easy access to Effect MonoBehaviours.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Interface for easy access to Effect MonoBehaviours.
    /// </summary>
    public interface IEffect {

        event EventHandler effectFinishedOneShot;

        bool IsPaused { get; set; }

        bool IsPlaying { get; }

        void Play(float effectRadius);
    }
}

