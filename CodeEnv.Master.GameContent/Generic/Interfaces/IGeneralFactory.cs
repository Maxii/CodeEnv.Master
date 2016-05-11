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
        /// Installs the provided orbitingObject into orbit around the OrbitedObject held by orbitSlot.
        /// </summary>
        /// <param name="orbitingObject">The orbiting object.</param>
        /// <param name="orbitData">The orbit slot.</param>
        void InstallCelestialItemInOrbit(GameObject orbitingObject, OrbitData orbitData);

        /// <summary>
        /// Makes and returns an instance of IShipOrbitSimulator for this ShipOrbitSlot.
        /// </summary>
        /// <param name="orbitData">The orbit slot.</param>
        /// <returns></returns>
        IShipCloseOrbitSimulator MakeShipCloseOrbitSimulatorInstance(OrbitData orbitData);

        /// <summary>
        /// Makes an instance of an explosion, scaled to work with the item it is being applied too.
        /// Parented to the DynamicObjectsFolder. Destroys itself when completed.
        /// </summary>
        /// <param name="itemRadius">The item radius.</param>
        /// <param name="itemPosition">The item position.</param>
        /// <returns></returns>
        ParticleSystem MakeAutoDestructExplosionInstance(float itemRadius, Vector3 itemPosition);

        IExplosion_Pooled SpawnExplosionInstance(Vector3 itemPosition);


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

