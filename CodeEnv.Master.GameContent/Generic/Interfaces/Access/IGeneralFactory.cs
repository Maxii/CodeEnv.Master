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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface allowing access to the associated Unity-compiled script. 
    /// Typically, a static reference to the script is established by GameManager in References.cs, providing access to the script from classes located in pre-compiled assemblies.
    /// </summary>
    public interface IGeneralFactory {

        /// <summary>
        /// Makes and returns an instance of IShipOrbitSimulator for this ShipOrbitSlot.
        /// </summary>
        /// <param name="orbitData">The orbit slot.</param>
        /// <returns></returns>
        IShipCloseOrbitSimulator MakeShipCloseOrbitSimulatorInstance(OrbitData orbitData);

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

