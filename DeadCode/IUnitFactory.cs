// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IUnitFactory.cs
// Interface allowing access to the associated Unity-compiled script. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// Interface allowing access to the associated Unity-compiled script. 
    /// Typically, a static reference to the script is established by GameManager in References.cs, providing access to the script from classes located in pre-compiled assemblies.
    /// </summary>
    [Obsolete]
    public interface IUnitFactory {

        /// <summary>
        /// Attaches the provided ship to a newly instantiated IOrbiter which is parented to the provided GameObject.
        /// </summary>
        /// <param name="parent">The parent GameObject for the new Orbiter.</param>
        /// <param name="ship">The ship.</param>
        /// <param name="orbitedObjectIsMobile">if set to <c>true</c> [orbited object is mobile].</param>
        void AttachShipToOrbiter(GameObject parent, IShipItem ship, bool orbitedObjectIsMobile);

        /// <summary>
        /// Attaches the provided ship to the provided orbiter transform.
        /// </summary>
        /// <param name="ship">The ship.</param>
        /// <param name="orbiterTransform">The orbiter transform.</param>
        void AttachShipToOrbiter(IShipItem ship, ref Transform orbiterTransform);

    }
}

