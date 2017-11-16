// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IOrdnance.cs
// Interface for easy access to Ordnance of all types.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using Common;
    using UnityEngine;

    /// <summary>
    /// Interface for easy access to Ordnance MonoBehaviours of all types.
    /// </summary>
    public interface IOrdnance : IDebugable {

        /// <summary>
        /// Occurs when an ordnance terminates.
        /// <remarks>Beams only self terminate either by order from its weapon or when its duration has run its course.</remarks>
        /// <remarks>AProjectile ordnance also self terminate either by order from its weapon or when it reaches its max range.
        /// They can also be intercepted which can result in their self termination.</remarks>
        /// </summary>
        event EventHandler terminationOneShot;

        Vector3 CurrentHeading { get; }

        /// <summary>
        /// The owner of this ordnance. 
        /// <remarks>No reason to hinder access to this 'owner' as it will
        /// always be close enough to know who fired it.</remarks>
        /// </summary>
        Player Owner { get; }

        IElementAttackable Target { get; }


    }
}

