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

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Class for Data associated with a SettlementCmdItem.
    /// </summary>
    public class SettlementCmdData : AUnitBaseCmdData {

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

        public int Capacity { get { return ParentSystemData.Capacity; } } // UNCLEAR need SetProperty to properly keep isChanged updated?

        public ResourceYield Resources { get { return ParentSystemData.Resources; } } // UNCLEAR need SetProperty to properly keep isChanged updated?

        private float _approval;
        public float Approval {
            get { return _approval; }
            set { SetProperty<float>(ref _approval, value, "Approval", ApprovalPropChangedHandler); }
        }

        public SystemData ParentSystemData { get; set; }

        public new SettlementInfoAccessController InfoAccessCntlr { get { return base.InfoAccessCntlr as SettlementInfoAccessController; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettlementCmdData" /> class
        /// with no passive countermeasures.
        /// </summary>
        /// <param name="settlementCmd">The settlement command.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="cmdStat">The stat.</param>
        public SettlementCmdData(ISettlementCmd settlementCmd, Player owner, SettlementCmdStat cmdStat)
            : this(settlementCmd, owner, Enumerable.Empty<PassiveCountermeasure>(), cmdStat) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettlementCmdData" /> class.
        /// </summary>
        /// <param name="settlementCmd">The settlement command.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="passiveCMs">The passive countermeasures.</param>
        /// <param name="cmdStat">The stat.</param>
        public SettlementCmdData(ISettlementCmd settlementCmd, Player owner, IEnumerable<PassiveCountermeasure> passiveCMs, SettlementCmdStat cmdStat)
            : base(settlementCmd, owner, passiveCMs, cmdStat) {
            Population = cmdStat.Population;
        }

        protected override AInfoAccessController InitializeInfoAccessController() {
            return new SettlementInfoAccessController(this);
        }

        public override void AddElement(AUnitElementData elementData) {
            base.AddElement(elementData);
            Category = GenerateCmdCategory(UnitComposition);
        }

        public override void RemoveElement(AUnitElementData elementData) {
            base.RemoveElement(elementData);
            Category = GenerateCmdCategory(UnitComposition);
        }

        public SettlementCategory GenerateCmdCategory(BaseComposition unitComposition) {
            int elementCount = unitComposition.GetTotalElementsCount();
            //D.Log(ShowDebugLog, "{0}'s known elements count = {1}.", FullName, elementCount);
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
            Utility.ValidateForRange(Approval, Constants.ZeroPercent, Constants.OneHundredPercent);
        }

        #endregion

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

