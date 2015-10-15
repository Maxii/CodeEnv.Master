// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IGeneralFactory.cs
// Interface allowing access to the associated Unity-compiled script. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface allowing access to the associated Unity-compiled script. 
    /// Typically, a static reference to the script is established by GameManager in References.cs, providing access to the script from classes located in pre-compiled assemblies.
    /// </summary>
    public interface IGeneralFactory {

        /// <summary>
        /// Makes the appropriate instance of IOrbitSimulator parented to <c>parent</c> and not yet enabled.
        /// </summary>
        /// <param name="parent">The GameObject the IOrbitSimulator should be parented too.</param>
        /// <param name="isParentMobile">if set to <c>true</c> [is parent mobile].</param>
        /// <param name="isForShips">if set to <c>true</c> [is for ships].</param>
        /// <param name="orbitPeriod">The orbit period.</param>
        /// <param name="name">Name to be applied to the OrbitSimulator gameObject.</param>
        /// <returns></returns>
        IOrbitSimulator MakeOrbitSimulatorInstance(GameObject parent, bool isParentMobile, bool isForShips, GameTimeDuration orbitPeriod, string name = "");

        /// <summary>
        /// Makes an instance of an explosion, scaled to work with the item it is being applied too.
        /// </summary>
        /// <param name="itemRadius">The item radius.</param>
        /// <param name="itemPosition">The item position.</param>
        /// <returns></returns>
        ParticleSystem MakeExplosionInstance(float itemRadius, Vector3 itemPosition);

        /// <summary>
        /// Makes a GameObject that will auto destruct when its AudioSource (added by client) finishes playing. The position
        /// is important as the AudioSFX playing is 3D. Being too far away from the AudioListener on the MainCamera
        /// will result in no audio. Parented to the DynamicObjectsFolder.
        /// </summary>
        /// <param name="name">The name to apply to the gameObject.</param>
        /// <param name="position">The position to locate the gameObject.</param>
        /// <returns></returns>
        GameObject MakeAutoDestruct3DAudioSFXInstance(string name, Vector3 position);


    }
}

