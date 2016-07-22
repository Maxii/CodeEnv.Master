// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitBaseCmdData.cs
// Abstract class for Data associated with an AUnitBaseCmdItem.
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
    using UnityEngine;

    /// <summary>
    /// Abstract class for Data associated with an AUnitBaseCmdItem.
    /// </summary>
    public abstract class AUnitBaseCmdData : AUnitCmdData {

        public new FacilityData HQElementData {
            protected get { return base.HQElementData as FacilityData; }
            set { base.HQElementData = value; }
        }

        private BaseComposition _unitComposition;
        public BaseComposition UnitComposition {
            get { return _unitComposition; }
            private set { SetProperty<BaseComposition>(ref _unitComposition, value, "UnitComposition"); }
        }

        public new IEnumerable<FacilityData> ElementsData { get { return base.ElementsData.Cast<FacilityData>(); } }

        public override Index3D SectorIndex { get { return References.SectorGrid.GetSectorIndex(Position); } }   // Settlements get relocated

        public AUnitBaseCmdData(IUnitCmd cmd, Player owner, CameraUnitCmdStat cameraStat, IEnumerable<PassiveCountermeasure> passiveCMs, UnitCmdStat cmdStat)
            : base(cmd, owner, cameraStat, passiveCMs, cmdStat) { }

        #region Event and Property Change Handlers

        protected override void UnitWeaponsRangePropChangedHandler() {
            D.Warn(UnitWeaponsRange.Max > TempGameValues.__MaxBaseWeaponsRangeDistance, "{0} max UnitWeaponsRange {1:0.#} > {2:0.#}.",
                FullName, UnitWeaponsRange.Max, TempGameValues.__MaxBaseWeaponsRangeDistance);
        }

        #endregion

        protected override void RefreshComposition() {
            var elementCategories = _elementsData.Cast<FacilityData>().Select(fd => fd.HullCategory);
            UnitComposition = new BaseComposition(elementCategories);
        }

    }
}

