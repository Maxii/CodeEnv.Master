// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitBaseCmdItemData.cs
// Abstract class for Data associated with an AUnitBaseCmdItem.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract class for Data associated with an AUnitBaseCmdItem.
    /// </summary>
    public abstract class AUnitBaseCmdItemData : AUnitCmdItemData {

        public float LowOrbitRadius { get; private set; }

        public float HighOrbitRadius { get { return LowOrbitRadius + TempGameValues.ShipOrbitSlotDepth; } }

        public new FacilityData HQElementData {
            get { return base.HQElementData as FacilityData; }
            set { base.HQElementData = value; }
        }

        public new CameraFocusableStat CameraStat { get { return base.CameraStat as CameraFocusableStat; } }

        private BaseComposition _unitComposition;
        public BaseComposition UnitComposition {
            get { return _unitComposition; }
            set { SetProperty<BaseComposition>(ref _unitComposition, value, "UnitComposition"); }
        }

        public override Index3D SectorIndex { get { return References.SectorGrid.GetSectorIndex(Position); } }   // Settlements get relocated

        public AUnitBaseCmdItemData(Transform cmdTransform, UnitBaseCmdStat cmdStat, Player owner, CameraFocusableStat cameraStat, IEnumerable<PassiveCountermeasure> passiveCMs)
            : base(cmdTransform, cmdStat, owner, cameraStat, passiveCMs) {
            LowOrbitRadius = cmdStat.LowOrbitRadius;
        }

        protected override void RefreshComposition() {
            var elementCategories = ElementsData.Cast<FacilityData>().Select(fd => fd.HullCategory);
            UnitComposition = new BaseComposition(elementCategories);
        }

    }
}

