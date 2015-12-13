// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementCmdData.cs
// Class for Data associated with a SettlementCmdItem.
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
    /// Class for Data associated with a SettlementCmdItem.
    /// </summary>
    public class SettlementCmdData : AUnitBaseCmdItemData { //: AUnitCmdItemData {

        private SettlementCategory _category;
        public SettlementCategory Category {
            get { return _category; }
            private set { SetProperty<SettlementCategory>(ref _category, value, "Category"); }
        }

        private int _population;
        public int Population {
            get { return _population; }
            set { SetProperty<int>(ref _population, value, "Population"); }
        }

        public int Capacity { get { return SystemData.Capacity; } } // UNCLEAR need SetProperty to properly keep isChanged updated?

        public ResourceYield Resources { get { return SystemData.Resources; } } // UNCLEAR need SetProperty to properly keep isChanged updated?

        private float _approval;
        public float Approval {
            get { return _approval; }
            set { SetProperty<float>(ref _approval, value, "Approval", ApprovalPropChangedHandler); }
        }

        public SystemData SystemData { private get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettlementCmdData" /> class
        /// with no passive countermeasures.
        /// </summary>
        /// <param name="cmdTransform">The command transform.</param>
        /// <param name="cmdStat">The stat.</param>
        /// <param name="cameraStat">The camera stat.</param>
        /// <param name="owner">The owner.</param>
        public SettlementCmdData(Transform cmdTransform, SettlementCmdStat cmdStat, CameraFocusableStat cameraStat, Player owner)
            : this(cmdTransform, cmdStat, cameraStat, owner, Enumerable.Empty<PassiveCountermeasure>()) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettlementCmdData" /> class.
        /// </summary>
        /// <param name="cmdTransform">The command transform.</param>
        /// <param name="cmdStat">The stat.</param>
        /// <param name="cameraStat">The camera stat.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="passiveCMs">The passive countermeasures.</param>
        public SettlementCmdData(Transform cmdTransform, SettlementCmdStat cmdStat, CameraFocusableStat cameraStat, Player owner, IEnumerable<PassiveCountermeasure> passiveCMs)
            : base(cmdTransform, cmdStat, owner, cameraStat, passiveCMs) {
            Population = cmdStat.Population;
        }

        public override void AddElement(AUnitElementItemData elementData) {
            base.AddElement(elementData);
            Category = GenerateCmdCategory(UnitComposition);
        }

        public override void RemoveElement(AUnitElementItemData elementData) {
            base.RemoveElement(elementData);
            Category = GenerateCmdCategory(UnitComposition);
        }

        public SettlementCategory GenerateCmdCategory(BaseComposition unitComposition) {
            int elementCount = UnitComposition.GetTotalElementsCount();
            //D.Log("{0}'s known elements count = {1}.", FullName, elementCount);
            if (elementCount >= 8) {
                return SettlementCategory.Territory;
            }
            if (elementCount >= 6) {
                return SettlementCategory.Province;
            }
            if (elementCount >= 4) {
                return SettlementCategory.CityState;
            }
            if (elementCount >= 2) {
                return SettlementCategory.City;
            }
            if (elementCount >= 1) {
                return SettlementCategory.Colony;
            }
            return SettlementCategory.None;
        }

        #region Event and Property Change Handlers

        private void ApprovalPropChangedHandler() {
            Arguments.ValidateForRange(Approval, Constants.ZeroPercent, Constants.OneHundredPercent);
        }

        #endregion

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

