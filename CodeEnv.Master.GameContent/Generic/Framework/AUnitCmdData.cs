// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitCmdData.cs
// Abstract class for Data associated with an AUnitCmdItem.
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
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    ///  Abstract class for Data associated with an AUnitCmdItem.
    /// </summary>
    public abstract class AUnitCmdData : AMortalItemData {

        private float _unitMaxFormationRadius;
        /// <summary>
        /// The maximum radius of this Unit's current formation, independent of the number of elements currently assigned a
        /// station in the formation or whether the Unit's elements are located on their formation station. 
        /// Value encompasses each element's "KeepoutZone" (Facility: AvoidableObstacleZone, Ship: CollisionDetectionZone) 
        /// when the element is OnStation. 
        /// <remarks>Needed in Data to allow reports access to this value when Intel is high enough. The
        /// value is useful when combined with Position and MaxWeaponsRange in determining arrival standoff distance to avoid
        /// initial enemy fire.</remarks>
        /// </summary>
        public float UnitMaxFormationRadius {
            get { return _unitMaxFormationRadius; }
            set { SetProperty<float>(ref _unitMaxFormationRadius, value, "UnitMaxFormationRadius"); }
        }

        /// <summary>
        /// The radius of the Command, aka the radius of the HQElement.
        /// </summary>
        public float Radius { get { return HQElementData.HullDimensions.magnitude / 2F; } }

        private string _parentName;
        public string ParentName {
            get { return _parentName; }
            set { SetProperty<string>(ref _parentName, value, "ParentName", ParentNamePropChangedHandler); }
        }

        public override string FullName {
            get { return ParentName.IsNullOrEmpty() ? Name : ParentName + Constants.Underscore + Name; }
        }

        private Formation _unitFormation;
        public Formation UnitFormation {
            get {
                D.Assert(_unitFormation != Formation.None, "{0}.{1} not yet set.", FullName, typeof(Formation).Name);
                return _unitFormation;
            }
            set { SetProperty<Formation>(ref _unitFormation, value, "UnitFormation"); }
        }

        private AUnitElementData _hqElementData;
        public AUnitElementData HQElementData {
            protected get { return _hqElementData; }
            set { SetProperty<AUnitElementData>(ref _hqElementData, value, "HQElementData", HQElementDataPropChangedHandler, HQElementDataPropChangingHandler); }
        }

        // AItemData.Health, CurrentHitPts and MaxHitPts are all for this CommandData, not for the Unit as a whole.
        // This CurrentHitPts value is managed by the AUnitCommandItem.ApplyDamage() override which currently 
        // doesn't let it drop below 50% of MaxHitPts. Health is directly derived from changes in CurrentHitPts.

        private float _currentCmdEffectiveness;
        public float CurrentCmdEffectiveness {  // UNDONE concept/range needs work
            get { return _currentCmdEffectiveness; }
            private set { SetProperty<float>(ref _currentCmdEffectiveness, value, "CurrentCmdEffectiveness"); }
        }

        private float _maxCmdEffectiveness;
        public float MaxCmdEffectiveness {  // UNDONE concept/range needs work
            get { return _maxCmdEffectiveness; }
            set { SetProperty<float>(ref _maxCmdEffectiveness, value, "MaxCmdEffectiveness", MaxCmdEffectivenessPropChangedHandler); }
        }

        private RangeDistance _unitWeaponsRange;
        /// <summary>
        /// The RangeDistance profile of the weapons of this unit.
        /// </summary>
        public RangeDistance UnitWeaponsRange {
            get { return _unitWeaponsRange; }
            private set { SetProperty<RangeDistance>(ref _unitWeaponsRange, value, "UnitWeaponsRange", UnitWeaponsRangePropChangedHandler); }
        }

        private RangeDistance _unitSensorRange;
        /// <summary>
        /// The RangeDistance profile of the sensors of this unit.
        /// </summary>
        public RangeDistance UnitSensorRange {
            get { return _unitSensorRange; }
            private set { SetProperty<RangeDistance>(ref _unitSensorRange, value, "UnitSensorRange"); }
        }

        private CombatStrength _unitOffensiveStrength;
        /// <summary>
        /// Read-only. The offensive combat strength of the entire Unit, aka the sum of all
        /// of this Unit's Elements offensive combat strength.
        /// </summary>
        public CombatStrength UnitOffensiveStrength {
            get { return _unitOffensiveStrength; }
            private set { SetProperty<CombatStrength>(ref _unitOffensiveStrength, value, "UnitOffensiveStrength"); }
        }

        private CombatStrength _unitDefensiveStrength;
        /// <summary>
        /// Read-only. The defensive combat strength of the entire Unit, aka the sum of all
        /// of this Unit's Elements defensive combat strength.
        /// </summary>
        public CombatStrength UnitDefensiveStrength {
            get { return _unitDefensiveStrength; }
            private set { SetProperty<CombatStrength>(ref _unitDefensiveStrength, value, "UnitDefensiveStrength"); }
        }

        private float _unitMaxHitPoints;
        /// <summary>
        /// Read-only. The max hit points of the entire Unit, aka the sum of all
        /// of this Unit's Elements max hit points.
        /// </summary>
        public float UnitMaxHitPoints {
            get { return _unitMaxHitPoints; }
            private set { SetProperty<float>(ref _unitMaxHitPoints, value, "UnitMaxHitPoints", UnitMaxHitPtsPropChangedHandler, UnitMaxHitPtsPropChangingHandler); }
        }

        private float _unitCurrentHitPoints;
        /// <summary>
        /// Read-only. The current hit points of the entire Unit, aka the sum of all
        /// of this Unit's Elements current hit points.
        /// </summary>
        public float UnitCurrentHitPoints {
            get { return _unitCurrentHitPoints; }
            private set { SetProperty<float>(ref _unitCurrentHitPoints, value, "UnitCurrentHitPoints", UnitCurrentHitPtsPropChangedHandler); }
        }

        private float _unitHealth;
        /// <summary>
        /// Read-only. Indicates the health of the entire Unit, a value between 0 and 1.
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

        public IEnumerable<AUnitElementData> ElementsData { get { return _elementsData; } }

        protected IList<AUnitElementData> _elementsData;
        protected IDictionary<AUnitElementData, IList<IDisposable>> _subscriptions;

        #region Initialization 

        /// <summary>
        /// Initializes a new instance of the <see cref="AUnitCmdItemData" /> class.
        /// </summary>
        /// <param name="cmd">The command.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="passiveCMs">The passive countermeasures protecting the command staff.</param>
        /// <param name="cmdStat">The command stat.</param>
        public AUnitCmdData(IUnitCmd cmd, Player owner, IEnumerable<PassiveCountermeasure> passiveCMs, UnitCmdStat cmdStat)
            : base(cmd, owner, cmdStat.MaxHitPoints, passiveCMs) {
            ParentName = cmdStat.UnitName;
            UnitFormation = cmdStat.UnitFormation;
            MaxCmdEffectiveness = cmdStat.MaxCmdEffectiveness;
            // A command's UnitMaxHitPoints are constructed from the sum of the elements
            InitializeCollections();
        }

        private void InitializeCollections() {
            _elementsData = new List<AUnitElementData>();
            _subscriptions = new Dictionary<AUnitElementData, IList<IDisposable>>();
        }

        #endregion

        protected virtual void Subscribe(AUnitElementData elementData) {
            _subscriptions.Add(elementData, new List<IDisposable>());
            IList<IDisposable> anElementsSubscriptions = _subscriptions[elementData];
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AUnitElementData, float>(ed => ed.CurrentHitPoints, ElementCurrentHitPtsPropChangedHandler));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AUnitElementData, float>(ed => ed.MaxHitPoints, ElementMaxHitPtsPropChangedHandler));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AUnitElementData, CombatStrength>(ed => ed.DefensiveStrength, ElementDefensiveStrengthPropChangedHandler));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AUnitElementData, CombatStrength>(ed => ed.OffensiveStrength, ElementOffensiveStrengthPropChangedHandler));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AUnitElementData, RangeDistance>(ed => ed.WeaponsRange, ElementWeaponsRangePropChangedHandler));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AUnitElementData, RangeDistance>(ed => ed.SensorRange, ElementSensorRangePropChangedHandler));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AUnitElementData, float>(ed => ed.Science, ElementSciencePropChangedHandler));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AUnitElementData, float>(ed => ed.Culture, ElementCulturePropChangedHandler));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AUnitElementData, float>(ed => ed.Income, ElementIncomePropChangedHandler));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AUnitElementData, float>(ed => ed.Expense, ElementExpensePropChangedHandler));
        }

        #region Event and Property Change Handlers

        protected override void HandleOwnerChanged() {
            base.HandleOwnerChanged();
            // Only Cmds can be 'taken over'
            PropagateOwnerChanged();
        }

        private void HQElementDataPropChangingHandler(AUnitElementData newHQElementData) {
            HandleHQElementDataChanging(newHQElementData);
        }

        protected virtual void HandleHQElementDataChanging(AUnitElementData newHQElementData) {
            //D.Log(ShowDebugLog, "{0}.HQElementData is about to change.", FullName);
            var previousHQElementData = HQElementData;
            if (previousHQElementData != null) {
                previousHQElementData.intelCoverageChanged -= HQElementIntelCoverageChangedEventHandler;
                previousHQElementData.topographyChanged -= HQElementTopographyChangedEventHandler;
            }
        }

        private void HQElementDataPropChangedHandler() {
            HandleHQElementDataChanged();
        }

        protected virtual void HandleHQElementDataChanged() {
            D.Assert(_elementsData.Contains(HQElementData), "HQ Element {0} assigned not present in {1}.".Inject(_hqElementData.FullName, FullName));
            HQElementData.intelCoverageChanged += HQElementIntelCoverageChangedEventHandler;
            Topography = GetTopography();
            HQElementData.topographyChanged += HQElementTopographyChangedEventHandler;
        }

        private void HQElementTopographyChangedEventHandler(object sender, EventArgs e) {
            Topography = HQElementData.Topography;
        }

        private void HQElementIntelCoverageChangedEventHandler(object sender, IntelCoverageChangedEventArgs e) {
            HandleHQElementIntelCoverageChanged(e.Player);
        }

        private void HandleHQElementIntelCoverageChanged(Player playerWhosCoverageChgd) {
            var playerIntelCoverageOfHQElement = HQElementData.GetIntelCoverage(playerWhosCoverageChgd);
            var isIntelCoverageSet = SetIntelCoverage(playerWhosCoverageChgd, playerIntelCoverageOfHQElement);
            D.Assert(isIntelCoverageSet);
            //D.Log(ShowDebugLog, "{0}.HQElement's IntelCoverage for {1} has changed to {2}. {0} has assumed the same value.", 
            //    FullName, playerWhosCoverageChgd.LeaderName, playerIntelCoverageOfHQElement.GetValueName());
        }

        private void UnitMaxHitPtsPropChangingHandler(float newMaxHitPts) {
            HandleUnitMaxHitPtsChanging(newMaxHitPts);
        }

        private void HandleUnitMaxHitPtsChanging(float newMaxHitPts) {
            if (newMaxHitPts < UnitMaxHitPoints) {
                // reduction in max hit points so reduce current hit points to match
                UnitCurrentHitPoints = Mathf.Clamp(UnitCurrentHitPoints, Constants.ZeroF, newMaxHitPts);
                // FIXME changing CurrentHitPoints here sends out a temporary erroneous health change event. The accurate health change event follows shortly
            }
        }

        private void UnitMaxHitPtsPropChangedHandler() {
            UnitHealth = UnitMaxHitPoints > Constants.ZeroF ? UnitCurrentHitPoints / UnitMaxHitPoints : Constants.ZeroF;
        }

        private void UnitCurrentHitPtsPropChangedHandler() {
            UnitHealth = UnitMaxHitPoints > Constants.ZeroF ? UnitCurrentHitPoints / UnitMaxHitPoints : Constants.ZeroF;
        }

        private void UnitHealthPropChangedHandler() {
            HandleUnitHealthChanged();
        }

        /// <summary>
        /// Called when the Unit's health changes.
        /// NOTE: This sets the UnitCommand's CurrentHitPoints (and Health) to 0 when UnitHealth reaches 0. 
        /// This is not done to initiate the UnitCommand's death, but to keep the values of a UnitCommand's CurrentHitPoints 
        /// and Health consistent with the way other Item's values are treated for any future subscribers to health changes.
        /// </summary>
        private void HandleUnitHealthChanged() {
            //D.Log(ShowDebugLog, "{0}: UnitHealth {1}, UnitCurrentHitPoints {2}, UnitMaxHitPoints {3}.", FullName, _unitHealth, UnitCurrentHitPoints, UnitMaxHitPoints);
            if (UnitHealth <= Constants.ZeroF) {
                CurrentHitPoints -= MaxHitPoints;
            }
        }

        protected override void HandleHealthChanged() {
            base.HandleHealthChanged();
            RefreshCurrentCmdEffectiveness();
        }

        private void UnitWeaponsRangePropChangedHandler() {
            HandleUnitWeaponsRangeChanged();
        }

        protected abstract void HandleUnitWeaponsRangeChanged();

        private void MaxCmdEffectivenessPropChangedHandler() {
            RefreshCurrentCmdEffectiveness();
        }

        private void ParentNamePropChangedHandler() {
            HandleParentNameChanged();
        }

        private void HandleParentNameChanged() {
            // the parent name of a command is the unit name
            if (!_elementsData.IsNullOrEmpty()) {
                _elementsData.ForAll(eData => eData.ParentName = ParentName);
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

        private void PropagateOwnerChanged() {
            _elementsData.ForAll(eData => eData.Owner = Owner);
        }

        private void RefreshCurrentCmdEffectiveness() {
            CurrentCmdEffectiveness = MaxCmdEffectiveness * Health;
            // concept: staff and equipment are hurt as health of the Cmd declines
            // as Health of a Cmd cannot decline below 50% due to CurrentHitPoints override, neither can CmdEffectiveness, until the Unit is destroyed
        }

        public virtual void AddElement(AUnitElementData elementData) {
            D.Assert(!_elementsData.Contains(elementData), "Attempted to add {0} {1} that is already present.".Inject(typeof(AUnitElementData).Name, elementData.ParentName));
            __ValidateOwner(elementData);
            UpdateElementParentName(elementData);
            _elementsData.Add(elementData);

            RefreshComposition();
            Subscribe(elementData);
            RecalcPropertiesDerivedFromCombinedElements();
        }

        private void __ValidateOwner(AUnitElementData elementData) {
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

        private void UpdateElementParentName(AUnitElementData elementData) {
            //TODO something more than just assigning a parent name?
            //D.Log(ShowDebugLog, "{0}.ParentName changing to {1}.", elementData.Name, ParentName);
            elementData.ParentName = ParentName;    // the name of the fleet, not the command
        }

        public virtual void RemoveElement(AUnitElementData elementData) {
            bool isRemoved = _elementsData.Remove(elementData);
            D.Assert(isRemoved, "Attempted to remove {0} {1} that is not present.".Inject(typeof(AUnitElementData).Name, elementData.ParentName));

            RefreshComposition();
            Unsubscribe(elementData);
            RecalcPropertiesDerivedFromCombinedElements();
        }

        /// <summary>
        /// Called when the HQElement changes, this method returns the Topography for this UnitCmd. 
        /// Default returns the HQElement's Topography.
        /// <remarks>FleetCmds should override this method as their Flagship does not yet know their
        /// Topography when the Flagship is first assigned as the HQElement during construction.</remarks>
        /// </summary>
        /// <returns></returns>
        protected virtual Topography GetTopography() { return HQElementData.Topography; }

        // OPTIMIZE avoid creating new Composition at startup for every element.add transaction
        protected abstract void RefreshComposition();

        /// <summary>
        /// Recalculates any Command properties that are dependent upon the total element population.
        /// </summary>
        protected virtual void RecalcPropertiesDerivedFromCombinedElements() {
            RecalcUnitDefensiveStrength();
            RecalcUnitOffensiveStrength();
            RecalcUnitMaxHitPoints();   // must precede current as current uses max as a clamp
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
            UnitOffensiveStrength = _elementsData.Select(ed => ed.OffensiveStrength).Aggregate(defaultValueIfEmpty, (accum, strength) => accum + strength);
        }

        private void RecalcUnitDefensiveStrength() {
            var defaultValueIfEmpty = default(CombatStrength);
            UnitDefensiveStrength = _elementsData.Select(ed => ed.DefensiveStrength).Aggregate(defaultValueIfEmpty, (accum, strength) => accum + strength);
        }

        private void RecalcUnitMaxHitPoints() {
            UnitMaxHitPoints = _elementsData.Sum(ed => ed.MaxHitPoints);
        }

        private void RecalcUnitCurrentHitPoints() {
            UnitCurrentHitPoints = _elementsData.Sum(ed => ed.CurrentHitPoints);
        }

        private void RecalcUnitWeaponsRange() {
            var allUnitWeapons = _elementsData.SelectMany(ed => ed.Weapons);
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
            var allUnitSensors = _elementsData.SelectMany(ed => ed.Sensors);
            var undamagedSensors = allUnitSensors.Where(s => s.IsOperational);
            var shortRangeSensors = undamagedSensors.Where(s => s.RangeCategory == RangeCategory.Short);
            var mediumRangeSensors = undamagedSensors.Where(s => s.RangeCategory == RangeCategory.Medium);
            var longRangeSensors = undamagedSensors.Where(s => s.RangeCategory == RangeCategory.Long);
            float shortRangeDistance = shortRangeSensors.Any() ? shortRangeSensors.First().RangeDistance : Constants.ZeroF; //shortRangeSensors.CalcSensorRangeDistance();
            float mediumRangeDistance = mediumRangeSensors.Any() ? mediumRangeSensors.First().RangeDistance : Constants.ZeroF;    //mediumRangeSensors.CalcSensorRangeDistance();
            float longRangeDistance = longRangeSensors.Any() ? longRangeSensors.First().RangeDistance : Constants.ZeroF;  //longRangeSensors.CalcSensorRangeDistance();
            UnitSensorRange = new RangeDistance(shortRangeDistance, mediumRangeDistance, longRangeDistance);
        }

        private void RecalcUnitScience() {
            UnitScience = _elementsData.Sum(ed => ed.Science);
        }

        private void RecalcUnitCulture() {
            UnitCulture = _elementsData.Sum(ed => ed.Culture);
        }

        private void RecalcUnitIncome() {
            UnitIncome = _elementsData.Sum(ed => ed.Income);
        }

        private void RecalcUnitExpense() {
            UnitExpense = _elementsData.Sum(ed => ed.Expense);
        }

        private void Unsubscribe(AUnitElementData elementData) {
            _subscriptions[elementData].ForAll(d => d.Dispose());
            _subscriptions.Remove(elementData);

            D.Assert(HQElementData != null);    // UNCLEAR when HQElementData gets nulled when elementData == HQElementData
            if (elementData == HQElementData) {
                HQElementData.intelCoverageChanged -= HQElementIntelCoverageChangedEventHandler;
            }
        }

        protected override void Unsubscribe() {
            base.Unsubscribe();
            IList<AUnitElementData> subscriberKeys = new List<AUnitElementData>(_subscriptions.Keys);
            // copy of key list as you can't remove keys from a list while you are iterating over the list
            foreach (AUnitElementData eData in subscriberKeys) {
                Unsubscribe(eData);
            }
            _subscriptions.Clear();
        }

    }
}

