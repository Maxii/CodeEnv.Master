// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IEffectsClient.cs
//  Interface for the Item that is the client of this EffectsManager.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for the Item that is the client of this EffectsManager.
    /// </summary>
    public interface IEffectsClient {

        string FullName { get; }

        ADisplayManager DisplayMgr { get; }

        Vector3 Position { get; }

        float Radius { get; }

        void HandleEffectFinished(EffectID effectID);

    }
}

