// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IGamePoolManager.cs
// Interface for easy access to the GamePoolManager MonoBehaviour singleton.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for easy access to the GamePoolManager MonoBehaviour singleton.
    /// </summary>
    public interface IGamePoolManager {

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


        Transform HighlightsSpawnPool { get; }

        ISphericalHighlight SpawnHighlight(Vector3 location);

        ISphericalHighlight SpawnHighlight(Vector3 location, Quaternion rotation);

        ISphericalHighlight SpawnHighlight(Vector3 location, Quaternion rotation, Transform parent);

        void DespawnHighlight(Transform highlightTransform);

        void DespawnHighlight(Transform highlightTransform, Transform parent);


    }
}

