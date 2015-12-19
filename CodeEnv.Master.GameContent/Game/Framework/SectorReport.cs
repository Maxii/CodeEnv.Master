// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorReport.cs
// Immutable report on a sector.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    ///  Immutable report on a sector.
    /// </summary>
    public class SectorReport : AItemReport {

        public Index3D SectorIndex { get; private set; }

        [System.Obsolete]
        public float Density { get; private set; }

        public SectorReport(SectorData data, Player player, ISectorItem item)
            : base(player, item) {
            AssignValues(data);
        }

        private void AssignValues(AItemData data) {
            var sData = data as SectorData;
            Name = sData.Name;
            Owner = sData.Owner;
            Position = sData.Position;
            SectorIndex = sData.SectorIndex;
            //Density = sData.Density;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

