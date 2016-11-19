// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IEffectsMgrClient.cs
// Interface for Items that are clients of EffectsManagers.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for Items that are clients of EffectsManagers.
    /// </summary>
    public interface IEffectsMgrClient : IDebugable {

        Vector3 Position { get; }

        float Radius { get; }

        void HandleEffectSequenceFinished(EffectSequenceID effectID);

    }
}

