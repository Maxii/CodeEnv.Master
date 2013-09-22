// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementData.cs
// All the data associated with a particular Settlement in a System.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// All the data associated with a particular Settlement in a System.
    /// </summary>
    public class SettlementData : APropertyChangeTracking {

        private SettlementSize _settlementSize;
        public SettlementSize SettlementSize {
            get { return _settlementSize; }
            set {
                SetProperty<SettlementSize>(ref _settlementSize, value, "SettlementSize");
            }
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

        private IPlayer _owner;
        public IPlayer Owner {
            get { return _owner; }
            set {
                SetProperty<IPlayer>(ref _owner, value, "Owner");
            }
        }

        private float _health;
        public float Health {
            get { return _health; }
            set {
                SetProperty<float>(ref _health, value, "Health");
            }
        }

        private float _maxHitPoints;
        public float MaxHitPoints {
            get { return _maxHitPoints; }
            set {
                SetProperty<float>(ref _maxHitPoints, value, "MaxHitPoints");
            }
        }

        private CombatStrength _strength;
        public CombatStrength Strength {
            get { return _strength; }
            set {
                SetProperty<CombatStrength>(ref _strength, value, "Strength");
            }
        }

        public SettlementData() { }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

