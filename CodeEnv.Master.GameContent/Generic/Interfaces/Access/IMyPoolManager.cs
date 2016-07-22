// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IMyPoolManager.cs
// Interface for easy access to the MyPoolManager MonoBehaviour singleton.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for easy access to the MyPoolManager MonoBehaviour singleton.
    /// </summary>
    public interface IMyPoolManager {

        Transform OrdnanceSpawnPool { get; }

        Transform Spawn(WDVCategory ordnanceID, Vector3 location);

        Transform Spawn(WDVCategory ordnanceID, Vector3 location, Quaternion rotation);

        Transform Spawn(WDVCategory ordnanceID, Vector3 location, Quaternion rotation, Transform parent);

        void DespawnOrdnance(Transform ordnanceTransform);

        void DespawnOrdnance(Transform ordnanceTransform, Transform parent);


        Transform EffectsSpawnPool { get; }

        IEffect Spawn(EffectID effectID, Vector3 location);

        IEffect Spawn(EffectID effectID, Vector3 location, Quaternion rotation);

        IEffect Spawn(EffectID effectID, Vector3 location, Quaternion rotation, Transform parent);

        void DespawnEffect(Transform effectTransform);

        void DespawnEffect(Transform effectTransform, Transform parent);

    }
}

