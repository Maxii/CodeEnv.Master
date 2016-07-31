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

        public override Index3D SectorIndex { get { return _sectorIndex; } }

        private Index3D _sectorIndex;

        public AUnitBaseCmdData(IUnitCmd cmd, Player owner, IEnumerable<PassiveCountermeasure> passiveCMs, UnitCmdStat cmdStat)
            : base(cmd, owner, passiveCMs, cmdStat) { }

        protected override void FinalInitialize() {
            base.FinalInitialize();
            // Deployment has already occurred
            _sectorIndex = InitializeSectorIndex();
        }

        private Index3D InitializeSectorIndex() {
            Index3D sectorIndex = References.SectorGrid.GetSectorIndex(Position);
            D.Assert(sectorIndex != default(Index3D));
            MarkAsChanged();
            return sectorIndex;
        }

        #region Event and Property Change Handlers

        protected override void HandleUnitWeaponsRangeChanged() {
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

