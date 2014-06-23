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

    using System;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// All the data associated with a particular Settlement in a System.
    /// </summary>
    public class SettlementCmdData : ACommandData {

        private OrbitalSlot _shipOrbitSlot;
        public OrbitalSlot ShipOrbitSlot {
            get { return _shipOrbitSlot; }
            set { SetProperty<OrbitalSlot>(ref _shipOrbitSlot, value, "ShipOrbitSlot"); }
        }

        private SettlementCategory _category;
        public SettlementCategory Category {
            get { return _category; }
            private set { SetProperty<SettlementCategory>(ref _category, value, "Category"); }
        }

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

        public BaseComposition Composition { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettlementCmdData"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        public SettlementCmdData(SettlementCmdStat stat)
            : base(stat.Name, stat.MaxHitPoints) {
            MaxCmdEffectiveness = stat.MaxCmdEffectiveness;
            Population = stat.Population;
            UnitFormation = stat.UnitFormation;
            Strength = stat.Strength;
        }

        protected override void InitializeComposition() {
            Composition = new BaseComposition();
        }

        protected override void ChangeComposition(AElementData elementData, bool toAdd) {
            bool isChanged = toAdd ? Composition.Add(elementData as FacilityData) : Composition.Remove(elementData as FacilityData);
            if (isChanged) {
                AssessCommandCategory();
                OnCompositionChanged();
            }
        }

        private void AssessCommandCategory() {
            int elementCount = Composition.ElementCount;
            switch (elementCount) {
                case 1:
                    Category = SettlementCategory.Colony;
                    break;
                case 2:
                case 3:
                    Category = SettlementCategory.City;
                    break;
                case 4:
                case 5:
                    Category = SettlementCategory.CityState;
                    break;
                case 6:
                case 7:
                    Category = SettlementCategory.Province;
                    break;
                case 8:
                case 9:
                    Category = SettlementCategory.Territory;
                    break;
                case 0:
                    // element count of 0 = dead, so don't generate a change to be handled
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(elementCount));
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

