// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ACommandData.cs
// Abstract base class that holds data for Items that are a unit command.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Abstract base class that holds data for Items that are a unit command.
    /// </summary>
    public abstract class ACommandData : AMortalItemData {

        public new Topography Topography { get { return HQElementData.Topography; } }

        private Formation _unitFormation;
        public Formation UnitFormation {
            get { return _unitFormation; }
            set { SetProperty<Formation>(ref _unitFormation, value, "UnitFormation"); }
        }

        private AUnitElementItemData _hqElementData;
        public AUnitElementItemData HQElementData {
            get { return _hqElementData; }
            set { SetProperty<AUnitElementItemData>(ref _hqElementData, value, "HQElementData", OnHQElementDataChanged); }
        }

        // AItemData.Health, CurrentHitPts and MaxHitPts are all for this CommandData, not for the Unit as a whole.
        // This CurrentHitPts value is managed by the AUnitCommandItem.ApplyDamage() override which currently 
        // doesn't let it drop below 50% of MaxHitPts. Health is directly derived from changes in CurrentHitPts.


        private int _currentCmdEffectiveness;
        public int CurrentCmdEffectiveness {  // TODO make use of this
            get { return _currentCmdEffectiveness; }
            private set { SetProperty<int>(ref _currentCmdEffectiveness, value, "CurrentCmdEffectiveness"); }
        }

        private int _maxCmdEffectiveness;
        public int MaxCmdEffectiveness {
            get { return _maxCmdEffectiveness; }
            set { SetProperty<int>(ref _maxCmdEffectiveness, value, "MaxCmdEffectiveness", OnMaxCmdEffectivenessChanged); }
        }

        private float _unitMaxWeaponsRange;
        /// <summary>
        /// The maximum range of all the element's weapons that are part of this unit.
        /// </summary>
        public float UnitMaxWeaponsRange {
            get { return _unitMaxWeaponsRange; }
            private set { SetProperty<float>(ref _unitMaxWeaponsRange, value, "UnitMaxWeaponsRange"); }
        }

        private float _unitMaxSensorRange;
        public float UnitMaxSensorRange {
            get { return _unitMaxSensorRange; }
            set { SetProperty<float>(ref _unitMaxSensorRange, value, "UnitMaxSensorRange"); }
        }

        private CombatStrength _unitOffensiveStrength;
        /// <summary>
        /// Readonly. The offensive combat strength of the entire Unit, aka the sum of all
        /// of this Unit's Elements offensive combat strength.
        /// </summary>
        public CombatStrength UnitOffensiveStrength {
            get { return _unitOffensiveStrength; }
            private set { SetProperty<CombatStrength>(ref _unitOffensiveStrength, value, "UnitOffensiveStrength"); }
        }

        private CombatStrength _unitDefensiveStrength;
        /// <summary>
        /// Readonly. The defensive combat strength of the entire Unit, aka the sum of all
        /// of this Unit's Elements defensive combat strength.
        /// </summary>
        public CombatStrength UnitDefensiveStrength {
            get { return _unitDefensiveStrength; }
            private set { SetProperty<CombatStrength>(ref _unitDefensiveStrength, value, "UnitDefensiveStrength"); }
        }

        private float _unitMaxHitPoints;
        /// <summary>
        /// Readonly. The max hit points of the entire Unit, aka the sum of all
        /// of this Unit's Elements max hit points.
        /// </summary>
        public float UnitMaxHitPoints {
            get { return _unitMaxHitPoints; }
            private set { SetProperty<float>(ref _unitMaxHitPoints, value, "UnitMaxHitPoints", OnUnitMaxHitPointsChanged, OnUnitMaxHitPointsChanging); }
        }

        private float _unitCurrentHitPoints;
        /// <summary>
        /// Readonly. The current hit points of the entire Unit, aka the sum of all
        /// of this Unit's Elements current hit points.
        /// </summary>
        public float UnitCurrentHitPoints {
            get { return _unitCurrentHitPoints; }
            private set { SetProperty<float>(ref _unitCurrentHitPoints, value, "UnitCurrentHitPoints", OnUnitCurrentHitPointsChanged); }
        }

        private float _unitHealth;
        /// <summary>
        /// Readonly. Indicates the health of the entire Unit, a value between 0 and 1.
        /// </summary>
        public float UnitHealth {
            get {
                return _unitHealth;
            }
            private set {
                value = Mathf.Clamp01(value);
                SetProperty<float>(ref _unitHealth, value, "UnitHealth", OnUnitHealthChanged);
            }
        }

        protected IList<AUnitElementItemData> ElementsData { get; private set; }
        protected IDictionary<AUnitElementItemData, IList<IDisposable>> _subscribers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ACommandItemData" /> class.
        /// </summary>
        /// <param name="unitName">Name of this Unit, eg. the FleetName for a FleetCommand.</param>
        /// <param name="cmdMaxHitPoints">The maximum hit points of this Command staff.</param>
        public ACommandItemData(string unitName, float cmdMaxHitPoints)
            : base(CommonTerms.Command, mass: 0.1F, maxHitPts: cmdMaxHitPoints) {
            ParentName = unitName;
            // A command's UnitMaxHitPoints are constructed from the sum of the elements
            InitializeCollections();
        }

        private void InitializeCollections() {
            ElementsData = new List<AElementData>();
            _subscribers = new Dictionary<AElementData, IList<IDisposable>>();
        }

        protected virtual void OnHQElementDataChanged() {
            D.Assert(ElementsData.Contains(HQElementData),
                "HQ Element {0} assigned not present in {1}.".Inject(_hqElementData.FullName, FullName));
        }

        private void OnUnitMaxHitPointsChanging(float newMaxHitPoints) {
            if (newMaxHitPoints < UnitMaxHitPoints) {
                // reduction in max hit points so reduce current hit points to match
                UnitCurrentHitPoints = Mathf.Clamp(UnitCurrentHitPoints, Constants.ZeroF, newMaxHitPoints);
                // FIXME changing CurrentHitPoints here sends out a temporary erroneous health change event. The accurate health change event follows shortly
            }
        }

        private void OnUnitMaxHitPointsChanged() {
            UnitHealth = UnitMaxHitPoints > Constants.ZeroF ? UnitCurrentHitPoints / UnitMaxHitPoints : Constants.ZeroF;
        }

        private void OnUnitCurrentHitPointsChanged() {
            var unitHealth = UnitMaxHitPoints > Constants.ZeroF ? UnitCurrentHitPoints / UnitMaxHitPoints : Constants.ZeroF;
            UnitHealth = unitHealth;
        }

        /// <summary>
        /// Called when the Unit's health changes.
        /// NOTE: This sets the UnitCommand's CurrentHitPoints (and Health) to 0 when UnitHealth reaches 0. 
        /// This is not done to initiate the UnitCommand's death, but to keep the values of a UnitCommand's CurrentHitPoints 
        /// and Health consistent with the way other Item's values are treated for any future subscribers to health changes.
        /// </summary>
        private void OnUnitHealthChanged() {
            //D.Log("{0}: UnitHealth {1}, UnitCurrentHitPoints {2}, UnitMaxHitPoints {3}.", FullName, _unitHealth, UnitCurrentHitPoints, UnitMaxHitPoints);
            if (UnitHealth <= Constants.ZeroF) {
                CurrentHitPoints -= MaxHitPoints;
            }
        }

        protected override void OnHealthChanged() {
            base.OnHealthChanged();
            RefreshCurrentCmdEffectiveness();
        }

        private void OnMaxCmdEffectivenessChanged() {
            RefreshCurrentCmdEffectiveness();
        }

        protected override void OnParentNameChanged() {
            base.OnParentNameChanged();
            // the parent name of a command is the unit name
            if (!ElementsData.IsNullOrEmpty()) {
                ElementsData.ForAll(eData => eData.ParentName = ParentName);
            }
        }

        private void RefreshCurrentCmdEffectiveness() {
            CurrentCmdEffectiveness = Mathf.RoundToInt(MaxCmdEffectiveness * Health);
            // concept: staff and equipment are hurt as health of the Cmd declines
            // as Health of a Cmd cannot decline below 50% due to CurrentHitPoints override, neither can CmdEffectiveness, until the Unit is destroyed
        }

        public virtual void AddElement(AElementData elementData) {
            D.Assert(!ElementsData.Contains(elementData), "Attempted to add {0} {1} that is already present.".Inject(typeof(AElementData).Name, elementData.ParentName));
            VerifyOwner(elementData);
            UpdateElementParentName(elementData);
            ElementsData.Add(elementData);

            UpdateComposition();
            Subscribe(elementData);
            RecalcPropertiesDerivedFromCombinedElements();
        }

        private void VerifyOwner(AElementData elementData) {
            D.Assert(Owner != TempGameValues.NoPlayer, "{0} owner should be set before adding elements.".Inject(Name));
            if (elementData.Owner == TempGameValues.NoPlayer) {
                D.Warn("{0} owner should be set before adding element to {1}.", elementData.Name, Name);
                elementData.Owner = Owner;
            }
            else if (elementData.Owner != Owner) {
                D.Warn("{0} owner {1} is different from {2} owner {3}.", elementData.Name, elementData.Owner.LeaderName, Name, Owner.LeaderName);
                elementData.Owner = Owner;
            }
        }

        private void UpdateElementParentName(AElementData elementData) {
            // TODO something more than just assigning a parent name?
            D.Log("{0} OptionalParentName changing to {1}.", elementData.Name, ParentName);
            elementData.ParentName = ParentName;    // the name of the fleet, not the command
        }


        public virtual bool RemoveElement(AElementData elementData) {
            D.Assert(ElementsData.Contains(elementData), "Attempted to remove {0} {1} that is not present.".Inject(typeof(AElementData).Name, elementData.ParentName));
            bool isRemoved = ElementsData.Remove(elementData);

            UpdateComposition();
            Unsubscribe(elementData);
            RecalcPropertiesDerivedFromCombinedElements();
            return isRemoved;
        }

        // OPTIMIZE avoid creating new Composition at startup for every element.add transaction
        protected abstract void UpdateComposition();

        /// <summary>
        /// Recalculates any Command properties that are dependant upon the total element population.
        /// </summary>
        protected virtual void RecalcPropertiesDerivedFromCombinedElements() {
            RecalcUnitDefensiveStrength();
            RecalcUnitOffensiveStrength();
            RecalcUnitMaxHitPoints();   // must preceed current as current uses max as a clamp
            RecalcUnitCurrentHitPoints();
            RecalcUnitMaxWeaponsRange();
            RecalcUnitMaxSensorRange();
        }

        private void RecalcUnitOffensiveStrength() {
            var defaultValueIfEmpty = default(CombatStrength);
            UnitOffensiveStrength = ElementsData.Select(ed => ed.OffensiveStrength).Aggregate(defaultValueIfEmpty, (accum, strength) => accum + strength);
        }

        private void RecalcUnitDefensiveStrength() {
            var defaultValueIfEmpty = default(CombatStrength);
            UnitDefensiveStrength = ElementsData.Select(ed => ed.DefensiveStrength).Aggregate(defaultValueIfEmpty, (accum, strength) => accum + strength);
        }

        private void RecalcUnitMaxHitPoints() {
            UnitMaxHitPoints = ElementsData.Sum<AElementData>(ed => ed.MaxHitPoints);
        }

        private void RecalcUnitCurrentHitPoints() {
            UnitCurrentHitPoints = ElementsData.Sum<AElementData>(ed => ed.CurrentHitPoints);
        }

        private void RecalcUnitMaxWeaponsRange() {
            UnitMaxWeaponsRange = ElementsData.Count == Constants.Zero ? Constants.ZeroF : ElementsData.Max(ed => ed.MaxWeaponsRange);
        }

        private void RecalcUnitMaxSensorRange() {
            UnitMaxSensorRange = ElementsData.Count == Constants.Zero ? Constants.ZeroF : ElementsData.Max(ed => ed.MaxSensorRange);
        }

        #region ElementData PropertyChanged Subscription and Methods

        protected virtual void Subscribe(AElementData elementData) {
            _subscribers.Add(elementData, new List<IDisposable>());
            IList<IDisposable> anElementsSubscriptions = _subscribers[elementData];
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AElementData, float>(ed => ed.CurrentHitPoints, OnElementCurrentHitPointsChanged));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AElementData, float>(ed => ed.MaxHitPoints, OnElementMaxHitPointsChanged));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AElementData, CombatStrength>(ed => ed.DefensiveStrength, OnElementDefensiveStrengthChanged));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AElementData, CombatStrength>(ed => ed.OffensiveStrength, OnElementOffensiveStrengthChanged));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AElementData, float>(ed => ed.MaxWeaponsRange, OnElementMaxWeaponsRangeChanged));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AElementData, float>(ed => ed.MaxSensorRange, OnElementMaxSensorRangeChanged));
        }

        private void OnElementOffensiveStrengthChanged() {
            RecalcUnitOffensiveStrength();
        }

        private void OnElementDefensiveStrengthChanged() {
            RecalcUnitDefensiveStrength();
        }

        private void OnElementCurrentHitPointsChanged() {
            RecalcUnitCurrentHitPoints();
        }

        private void OnElementMaxHitPointsChanged() {
            RecalcUnitMaxHitPoints();
        }

        private void OnElementMaxWeaponsRangeChanged() {
            RecalcUnitMaxWeaponsRange();
        }

        private void OnElementMaxSensorRangeChanged() {
            RecalcUnitMaxSensorRange();
        }

        private void Unsubscribe(AElementData elementData) {
            _subscribers[elementData].ForAll(d => d.Dispose());
            _subscribers.Remove(elementData);
        }

        #endregion

        protected override void Unsubscribe() {
            base.Unsubscribe();
            IList<AElementData> subscriberKeys = new List<AElementData>(_subscribers.Keys);
            // copy of key list as you can't remove keys from a list while you are iterating over the list
            foreach (AElementData eData in subscriberKeys) {
                Unsubscribe(eData);
            }
            _subscribers.Clear();
        }

    }
}

