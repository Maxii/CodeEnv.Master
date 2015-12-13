// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitCmdItemData.cs
// Abstract class for Data associated with an AUnitCmdItem.
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
    ///  Abstract class for Data associated with an AUnitCmdItem.
    /// </summary>
    public abstract class AUnitCmdItemData : AMortalItemData {

        public float Radius { get { return HQElementData.HullDimensions.magnitude / 2F; } }

        private string _parentName;
        public string ParentName {
            get { return _parentName; }
            set { SetProperty<string>(ref _parentName, value, "ParentName", ParentNamePropChangedHandler); }
        }

        public override string FullName {
            get { return ParentName.IsNullOrEmpty() ? Name : ParentName + Constants.Underscore + Name; }
        }

        public override Topography Topography {
            get { return HQElementData.Topography; }
            set { throw new NotSupportedException(); }
        }

        private Formation _unitFormation;
        public Formation UnitFormation {
            get { return _unitFormation; }
            set { SetProperty<Formation>(ref _unitFormation, value, "UnitFormation"); }
        }

        private AUnitElementItemData _hqElementData;
        public AUnitElementItemData HQElementData {
            get { return _hqElementData; }
            set { SetProperty<AUnitElementItemData>(ref _hqElementData, value, "HQElementData", HQElementDataPropChangedHandler, HQElementDataPropChangingHandler); }
        }

        // AItemData.Health, CurrentHitPts and MaxHitPts are all for this CommandData, not for the Unit as a whole.
        // This CurrentHitPts value is managed by the AUnitCommandItem.ApplyDamage() override which currently 
        // doesn't let it drop below 50% of MaxHitPts. Health is directly derived from changes in CurrentHitPts.

        private int _currentCmdEffectiveness;
        public int CurrentCmdEffectiveness {  //TODO make use of this
            get { return _currentCmdEffectiveness; }
            private set { SetProperty<int>(ref _currentCmdEffectiveness, value, "CurrentCmdEffectiveness"); }
        }

        private int _maxCmdEffectiveness;
        public int MaxCmdEffectiveness {
            get { return _maxCmdEffectiveness; }
            set { SetProperty<int>(ref _maxCmdEffectiveness, value, "MaxCmdEffectiveness", MaxCmdEffectivenessPropChangedHandler); }
        }

        private RangeDistance _unitWeaponsRange;
        /// <summary>
        /// The RangeDistance profile of the weapons of this unit.
        /// </summary>
        public RangeDistance UnitWeaponsRange {
            get { return _unitWeaponsRange; }
            set { SetProperty<RangeDistance>(ref _unitWeaponsRange, value, "UnitWeaponsRange"); }
        }

        private RangeDistance _unitSensorRange;
        /// <summary>
        /// The RangeDistance profile of the sensors of this unit.
        /// </summary>
        public RangeDistance UnitSensorRange {
            get { return _unitSensorRange; }
            set { SetProperty<RangeDistance>(ref _unitSensorRange, value, "UnitSensorRange"); }
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
            private set { SetProperty<float>(ref _unitMaxHitPoints, value, "UnitMaxHitPoints", UnitMaxHitPtsPropChangedHandler, UnitMaxHitPtsPropChangingHandler); }
        }

        private float _unitCurrentHitPoints;
        /// <summary>
        /// Readonly. The current hit points of the entire Unit, aka the sum of all
        /// of this Unit's Elements current hit points.
        /// </summary>
        public float UnitCurrentHitPoints {
            get { return _unitCurrentHitPoints; }
            private set { SetProperty<float>(ref _unitCurrentHitPoints, value, "UnitCurrentHitPoints", UnitCurrentHitPtsPropChangedHandler); }
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
                SetProperty<float>(ref _unitHealth, value, "UnitHealth", UnitHealthPropChangedHandler);
            }
        }

        private float _unitScience;
        public float UnitScience {
            get { return _unitScience; }
            private set { SetProperty<float>(ref _unitScience, value, "UnitScience"); }
        }

        private float _unitCulture;
        public float UnitCulture {
            get { return _unitCulture; }
            private set { SetProperty<float>(ref _unitCulture, value, "UnitCulture"); }
        }

        private float _unitIncome;
        public float UnitIncome {
            get { return _unitIncome; }
            private set { SetProperty<float>(ref _unitIncome, value, "UnitIncome"); }
        }

        private float _unitExpense;
        public float UnitExpense {
            get { return _unitExpense; }
            private set { SetProperty<float>(ref _unitExpense, value, "UnitExpense"); }
        }

        protected IList<AUnitElementItemData> ElementsData { get; private set; }
        protected IDictionary<AUnitElementItemData, IList<IDisposable>> _subscriptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="AUnitCmdItemData" /> class.
        /// </summary>
        /// <param name="cmdTransform">The command transform.</param>
        /// <param name="unitName">Name of this Unit, eg. the FleetName for a FleetCommand.</param>
        /// <param name="cmdMaxHitPts">The command maximum hit PTS.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="cameraStat">The camera stat.</param>
        /// <param name="passiveCMs">The passive countermeasures protecting the command staff.</param>
        public AUnitCmdItemData(Transform cmdTransform, UnitCmdStat cmdStat, Player owner, CameraFocusableStat cameraStat, IEnumerable<PassiveCountermeasure> passiveCMs)
            : base(cmdTransform, CommonTerms.Command, cmdStat.MaxHitPoints, owner, cameraStat, passiveCMs) {
            ParentName = cmdStat.UnitName;
            UnitFormation = cmdStat.UnitFormation;
            MaxCmdEffectiveness = cmdStat.MaxCmdEffectiveness;
            // A command's UnitMaxHitPoints are constructed from the sum of the elements
            InitializeCollections();
        }

        private void InitializeCollections() {
            ElementsData = new List<AUnitElementItemData>();
            _subscriptions = new Dictionary<AUnitElementItemData, IList<IDisposable>>();
        }


        protected virtual void Subscribe(AUnitElementItemData elementData) {
            _subscriptions.Add(elementData, new List<IDisposable>());
            IList<IDisposable> anElementsSubscriptions = _subscriptions[elementData];
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AUnitElementItemData, float>(ed => ed.CurrentHitPoints, ElementCurrentHitPtsPropChangedHandler));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AUnitElementItemData, float>(ed => ed.MaxHitPoints, ElementMaxHitPtsPropChangedHandler));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AUnitElementItemData, CombatStrength>(ed => ed.DefensiveStrength, ElementDefensiveStrengthPropChangedHandler));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AUnitElementItemData, CombatStrength>(ed => ed.OffensiveStrength, ElementOffensiveStrengthPropChangedHandler));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AUnitElementItemData, RangeDistance>(ed => ed.WeaponsRange, ElementWeaponsRangePropChangedHandler));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AUnitElementItemData, RangeDistance>(ed => ed.SensorRange, ElementSensorRangePropChangedHandler));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AUnitElementItemData, float>(ed => ed.Science, ElementSciencePropChangedHandler));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AUnitElementItemData, float>(ed => ed.Culture, ElementCulturePropChangedHandler));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AUnitElementItemData, float>(ed => ed.Income, ElementIncomePropChangedHandler));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AUnitElementItemData, float>(ed => ed.Expense, ElementExpensePropChangedHandler));
        }

        #region Event and Property Change Handlers

        protected virtual void HQElementDataPropChangingHandler(AUnitElementItemData newHQElementData) {
            var previousHQElementData = HQElementData;
            if (previousHQElementData != null) {
                previousHQElementData.intelCoverageChanged -= HQElementIntelCoverageChangedEventHandler;
            }
        }

        protected virtual void HQElementDataPropChangedHandler() {
            D.Assert(ElementsData.Contains(HQElementData), "HQ Element {0} assigned not present in {1}.".Inject(_hqElementData.FullName, FullName));
            HQElementData.intelCoverageChanged += HQElementIntelCoverageChangedEventHandler;
        }
        private void HQElementIntelCoverageChangedEventHandler(object sender, IntelEventArgs e) {
            var player = e.Player;
            var playerIntelCoverageOfHQElement = HQElementData.GetIntelCoverage(player);
            var isIntelCoverageSet = SetIntelCoverage(player, playerIntelCoverageOfHQElement);
            D.Assert(isIntelCoverageSet);
            D.Log("{0}.HQElement's IntelCoverage for {1} has changed to {2}. {0} has assumed the same value.", FullName, player.LeaderName, playerIntelCoverageOfHQElement.GetValueName());
        }

        private void UnitMaxHitPtsPropChangingHandler(float newMaxHitPoints) {
            if (newMaxHitPoints < UnitMaxHitPoints) {
                // reduction in max hit points so reduce current hit points to match
                UnitCurrentHitPoints = Mathf.Clamp(UnitCurrentHitPoints, Constants.ZeroF, newMaxHitPoints);
                // FIXME changing CurrentHitPoints here sends out a temporary erroneous health change event. The accurate health change event follows shortly
            }
        }

        private void UnitMaxHitPtsPropChangedHandler() {
            UnitHealth = UnitMaxHitPoints > Constants.ZeroF ? UnitCurrentHitPoints / UnitMaxHitPoints : Constants.ZeroF;
        }

        private void UnitCurrentHitPtsPropChangedHandler() {
            var unitHealth = UnitMaxHitPoints > Constants.ZeroF ? UnitCurrentHitPoints / UnitMaxHitPoints : Constants.ZeroF;
            UnitHealth = unitHealth;
        }

        /// <summary>
        /// Called when the Unit's health changes.
        /// NOTE: This sets the UnitCommand's CurrentHitPoints (and Health) to 0 when UnitHealth reaches 0. 
        /// This is not done to initiate the UnitCommand's death, but to keep the values of a UnitCommand's CurrentHitPoints 
        /// and Health consistent with the way other Item's values are treated for any future subscribers to health changes.
        /// </summary>
        private void UnitHealthPropChangedHandler() {
            //D.Log("{0}: UnitHealth {1}, UnitCurrentHitPoints {2}, UnitMaxHitPoints {3}.", FullName, _unitHealth, UnitCurrentHitPoints, UnitMaxHitPoints);
            if (UnitHealth <= Constants.ZeroF) {
                CurrentHitPoints -= MaxHitPoints;
            }
        }

        protected override void HealthPropChangedHandler() {
            base.HealthPropChangedHandler();
            RefreshCurrentCmdEffectiveness();
        }

        private void MaxCmdEffectivenessPropChangedHandler() {
            RefreshCurrentCmdEffectiveness();
        }

        private void ParentNamePropChangedHandler() {
            // the parent name of a command is the unit name
            if (!ElementsData.IsNullOrEmpty()) {
                ElementsData.ForAll(eData => eData.ParentName = ParentName);
            }
        }

        private void ElementOffensiveStrengthPropChangedHandler() {
            RecalcUnitOffensiveStrength();
        }

        private void ElementDefensiveStrengthPropChangedHandler() {
            RecalcUnitDefensiveStrength();
        }

        private void ElementCurrentHitPtsPropChangedHandler() {
            RecalcUnitCurrentHitPoints();
        }

        private void ElementMaxHitPtsPropChangedHandler() {
            RecalcUnitMaxHitPoints();
        }

        private void ElementWeaponsRangePropChangedHandler() {
            RecalcUnitWeaponsRange();
        }

        private void ElementSensorRangePropChangedHandler() {
            RecalcUnitSensorRange();
        }

        private void ElementSciencePropChangedHandler() {
            RecalcUnitScience();
        }

        private void ElementCulturePropChangedHandler() {
            RecalcUnitCulture();
        }

        private void ElementIncomePropChangedHandler() {
            RecalcUnitIncome();
        }

        private void ElementExpensePropChangedHandler() {
            RecalcUnitExpense();
        }
        #endregion

        private void RefreshCurrentCmdEffectiveness() {
            CurrentCmdEffectiveness = Mathf.RoundToInt(MaxCmdEffectiveness * Health);
            // concept: staff and equipment are hurt as health of the Cmd declines
            // as Health of a Cmd cannot decline below 50% due to CurrentHitPoints override, neither can CmdEffectiveness, until the Unit is destroyed
        }

        public virtual void AddElement(AUnitElementItemData elementData) {
            D.Assert(!ElementsData.Contains(elementData), "Attempted to add {0} {1} that is already present.".Inject(typeof(AUnitElementItemData).Name, elementData.ParentName));
            VerifyOwner(elementData);
            UpdateElementParentName(elementData);
            ElementsData.Add(elementData);

            RefreshComposition();
            Subscribe(elementData);
            RecalcPropertiesDerivedFromCombinedElements();
        }

        private void VerifyOwner(AUnitElementItemData elementData) {
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

        private void UpdateElementParentName(AUnitElementItemData elementData) {
            //TODO something more than just assigning a parent name?
            //D.Log("{0}.ParentName changing to {1}.", elementData.Name, ParentName);
            elementData.ParentName = ParentName;    // the name of the fleet, not the command
        }


        public virtual void RemoveElement(AUnitElementItemData elementData) {
            bool isRemoved = ElementsData.Remove(elementData);
            D.Assert(isRemoved, "Attempted to remove {0} {1} that is not present.".Inject(typeof(AUnitElementItemData).Name, elementData.ParentName));

            RefreshComposition();
            Unsubscribe(elementData);
            RecalcPropertiesDerivedFromCombinedElements();
        }

        // OPTIMIZE avoid creating new Composition at startup for every element.add transaction
        protected abstract void RefreshComposition();

        /// <summary>
        /// Recalculates any Command properties that are dependant upon the total element population.
        /// </summary>
        protected virtual void RecalcPropertiesDerivedFromCombinedElements() {
            RecalcUnitDefensiveStrength();
            RecalcUnitOffensiveStrength();
            RecalcUnitMaxHitPoints();   // must preceed current as current uses max as a clamp
            RecalcUnitCurrentHitPoints();
            RecalcUnitWeaponsRange();
            RecalcUnitSensorRange();
            RecalcUnitScience();
            RecalcUnitCulture();
            RecalcUnitIncome();
            RecalcUnitExpense();
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
            UnitMaxHitPoints = ElementsData.Sum(ed => ed.MaxHitPoints);
        }

        private void RecalcUnitCurrentHitPoints() {
            UnitCurrentHitPoints = ElementsData.Sum(ed => ed.CurrentHitPoints);
        }

        private void RecalcUnitWeaponsRange() {
            var allUnitWeapons = ElementsData.SelectMany(ed => ed.Weapons);
            var undamagedUnitWeapons = allUnitWeapons.Where(w => !w.IsDamaged);
            var shortRangeWeapons = undamagedUnitWeapons.Where(w => w.RangeCategory == RangeCategory.Short);
            var mediumRangeWeapons = undamagedUnitWeapons.Where(w => w.RangeCategory == RangeCategory.Medium);
            var longRangeWeapons = undamagedUnitWeapons.Where(w => w.RangeCategory == RangeCategory.Long);
            float shortRangeDistance = shortRangeWeapons.Any() ? shortRangeWeapons.First().RangeDistance : Constants.ZeroF;
            float mediumRangeDistance = mediumRangeWeapons.Any() ? mediumRangeWeapons.First().RangeDistance : Constants.ZeroF;
            float longRangeDistance = longRangeWeapons.Any() ? longRangeWeapons.First().RangeDistance : Constants.ZeroF;
            UnitWeaponsRange = new RangeDistance(shortRangeDistance, mediumRangeDistance, longRangeDistance);
        }

        private void RecalcUnitSensorRange() {
            var allUnitSensors = ElementsData.SelectMany(ed => ed.Sensors);
            var shortRangeSensors = allUnitSensors.Where(s => s.RangeCategory == RangeCategory.Short);
            var mediumRangeSensors = allUnitSensors.Where(s => s.RangeCategory == RangeCategory.Medium);
            var longRangeSensors = allUnitSensors.Where(s => s.RangeCategory == RangeCategory.Long);
            float shortRangeDistance = shortRangeSensors.CalcSensorRangeDistance();
            float mediumRangeDistance = mediumRangeSensors.CalcSensorRangeDistance();
            float longRangeDistance = longRangeSensors.CalcSensorRangeDistance();
            UnitSensorRange = new RangeDistance(shortRangeDistance, mediumRangeDistance, longRangeDistance);
        }

        private void RecalcUnitScience() {
            UnitScience = ElementsData.Sum(ed => ed.Science);
        }

        private void RecalcUnitCulture() {
            UnitCulture = ElementsData.Sum(ed => ed.Culture);
        }

        private void RecalcUnitIncome() {
            UnitIncome = ElementsData.Sum(ed => ed.Income);
        }

        private void RecalcUnitExpense() {
            UnitExpense = ElementsData.Sum(ed => ed.Expense);
        }

        private void Unsubscribe(AUnitElementItemData elementData) {
            _subscriptions[elementData].ForAll(d => d.Dispose());
            _subscriptions.Remove(elementData);

            D.Assert(HQElementData != null);    // UNCLEAR when HQElementData gets nulled when elementData == HQElementData
            if (elementData == HQElementData) {
                //HQElementData.onIntelCoverageChanged -= OnHQElementIntelCoverageChanged;
                HQElementData.intelCoverageChanged -= HQElementIntelCoverageChangedEventHandler;
            }
        }

        protected override void Unsubscribe() {
            base.Unsubscribe();
            IList<AUnitElementItemData> subscriberKeys = new List<AUnitElementItemData>(_subscriptions.Keys);
            // copy of key list as you can't remove keys from a list while you are iterating over the list
            foreach (AUnitElementItemData eData in subscriberKeys) {
                Unsubscribe(eData);
            }
            _subscriptions.Clear();
        }

    }
}

