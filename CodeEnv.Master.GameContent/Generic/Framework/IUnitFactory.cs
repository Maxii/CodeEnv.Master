// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IUnitFactory.cs
// Interface allowing access to UnitFactory methods and properties.
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
    /// Interface allowing access to UnitFactory methods and properties.
    /// </summary>
    public interface IUnitFactory {

        /// <summary>
        /// Attaches the provided ship to a newly instantiated IOrbiter which is parented to the provided GameObject.
        /// </summary>
        /// <param name="parent">The parent GameObject for the new Orbiter.</param>
        /// <param name="ship">The ship.</param>
        /// <param name="orbitedObjectIsMobile">if set to <c>true</c> [orbited object is mobile].</param>
        void AttachShipToOrbiter(GameObject parent, IShipModel ship, bool orbitedObjectIsMobile);

        /// <summary>
        /// Attaches the provided ship to the provided orbiter transform.
        /// </summary>
        /// <param name="ship">The ship.</param>
        /// <param name="orbiterTransform">The orbiter transform.</param>
        void AttachShipToOrbiter(IShipModel ship, ref Transform orbiterTransform);

    }
}

