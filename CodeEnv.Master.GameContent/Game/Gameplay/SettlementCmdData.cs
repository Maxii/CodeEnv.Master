// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementCmdData.cs
// All the data associated with a particular Settlement in a System.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// All the data associated with a particular Settlement in a System.
    /// </summary>
    public class SettlementCmdData : ACommandData {

        public SettlementCategory Category { get; set; }

        private int _population;
        public int Population {
            get { return _population; }
            set {
                SetProperty<int>(ref _population, value, "Population");
            }
        }

        private int _capacityUsed;
        public int CapacityUsed {
            get { return _capacityUsed; }
            set {
                SetProperty<int>(ref _capacityUsed, value, "CapacityUsed");
            }
        }

        private OpeYield _resourcesUsed;
        public OpeYield ResourcesUsed {
            get { return _resourcesUsed; }
            set {
                SetProperty<OpeYield>(ref _resourcesUsed, value, "ResourcesUsed");
            }
        }

        private XYield _specialResourcesUsed;
        public XYield SpecialResourcesUsed {
            get { return _specialResourcesUsed; }
            set {
                SetProperty<XYield>(ref _specialResourcesUsed, value, "SpecialResourcesUsed");
            }
        }

        public new FacilityData HQElementData {
            get { return base.HQElementData as FacilityData; }
            set { base.HQElementData = value; }
        }

        private BaseComposition _composition;
        public BaseComposition Composition {
            get { return _composition; }
            private set { SetProperty<BaseComposition>(ref _composition, value, "Composition"); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StarbaseCmdData" /> class.
        /// </summary>
        /// <param name="settlementName">Name of the settlement.</param>
        /// <param name="cmdMaxHitPoints">The command maximum hit points.</param>
        public SettlementCmdData(string settlementName, float cmdMaxHitPoints) : base(settlementName, cmdMaxHitPoints) { }

        protected override void InitializeComposition() {
            Composition = new BaseComposition();
        }

        protected override void ChangeComposition(AElementData elementData, bool toAdd) {
            bool isChanged = false;
            if (toAdd) {
                isChanged = Composition.Add(elementData as FacilityData);
            }
            else {
                isChanged = Composition.Remove(elementData as FacilityData);
            }

            if (isChanged) {
                Composition = new BaseComposition(Composition);
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

