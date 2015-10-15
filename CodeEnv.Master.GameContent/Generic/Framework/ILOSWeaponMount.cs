// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ILOSWeaponMount.cs
// Interface for a weapon mount on a hull used for line of sight weapons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for a weapon mount on a hull used for line of sight weapons.
    /// </summary>
    public interface ILOSWeaponMount : IWeaponMount {

        /// <summary>
        /// Traverses the mount to point at the location indicated by the firing solution.
        /// </summary>
        /// <param name="firingSolution">The firing solution.</param>
        void TraverseTo(LosWeaponFiringSolution firingSolution);

        /// <summary>
        /// The Muzzle GameObject, used to hold the BeamOrdnance gameObject while being fired.
        /// </summary>
        GameObject Muzzle { get; }
    }
}

