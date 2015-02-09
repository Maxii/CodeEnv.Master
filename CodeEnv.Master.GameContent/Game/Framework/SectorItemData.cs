// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorItemData.cs
// Class for Data associated with a SectorItem.
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
    /// Class for Data associated with a SectorItem.
    /// </summary>
    public class SectorItemData : AItemData {

        public Index3D SectorIndex { get; private set; }

        /// <summary>
        /// UNDONE
        /// The density of matter in space in this sector. Intended to be
        /// applied to pathfinding points in the sector as a 'penalty' to
        /// influence path creation. Should also increase drag on a ship
        /// in the sector to reduce its speed for a given thrust. The value
        /// should probably be a function of the OpeYield in the sector.
        /// </summary>
        public float Density { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SectorItemData" /> class.
        /// </summary>
        /// <param name="sectorTransform">The sector transform.</param>
        /// <param name="index">The index.</param>
        public SectorItemData(Transform sectorTransform, Index3D index)
            : base(sectorTransform, "Sector {0}".Inject(index)) {
            SectorIndex = index;
            base.Topography = Topography.OpenSpace;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

