// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorData.cs
// Class for Data associated with a SectorItem.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Class for Data associated with a SectorItem.
    /// </summary>
    public class SectorData : AItemData {

        public Index3D SectorIndex { get; private set; }

        /// <summary>
        /// UNDONE
        /// The density of matter in space in this sector. Intended to be
        /// applied to pathfinding points in the sector as a 'penalty' to
        /// influence path creation. Should also increase drag on a ship
        /// in the sector to reduce its speed for a given thrust. The value
        /// should probably be a function of the OpeYield in the sector.
        /// </summary>
        [System.Obsolete]
        public float Density { get; set; }

        public sealed override Topography Topography {  // avoids CA2214
            get { return base.Topography; }
            set { base.Topography = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SectorData" /> class
        /// with the owner initialized to NoPlayer.
        /// </summary>
        /// <param name="sectorTransform">The sector transform.</param>
        /// <param name="index">The index.</param>
        public SectorData(Transform sectorTransform, Index3D index)
            : this(sectorTransform, index, TempGameValues.NoPlayer) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SectorData" /> class.
        /// </summary>
        /// <param name="sectorTransform">The sector transform.</param>
        /// <param name="index">The index.</param>
        /// <param name="owner">The owner.</param>
        public SectorData(Transform sectorTransform, Index3D index, Player owner)
            : base(sectorTransform, "Sector {0}".Inject(index), owner) {
            SectorIndex = index;
            Topography = Topography.OpenSpace;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

