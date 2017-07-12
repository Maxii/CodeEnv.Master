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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

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

        private const string DebugNameFormat = "{0}'s {1}"; //"{0}'s {1}.{2}";

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

        public string DesignName { get; private set; }

        /// <summary>
        /// The radius of the Command, aka the radius of the HQElement.
        /// </summary>
        public float Radius { get { return HQElementData.HullDimensions.magnitude / 2F; } }

        private string _unitName;
        public string UnitName {
            get { return _unitName; }
            set { SetProperty<string>(ref _unitName, value, "UnitName", UnitNamePropChangedHandler); }
        }

        public override string DebugName {
            get {
                // 7.10.17 UnitName is already embedded in Cmd's Name
                if (Owner != null) {
                    return DebugNameFormat.Inject(Owner.DebugName, Name);
                }
                return DebugNameFormat.Inject(Constants.Empty, Name);
            }
        }

        private AlertStatus _alertStatus;
        public AlertStatus AlertStatus {
            get { return _alertStatus; }
            set { SetProperty<AlertStatus>(ref _alertStatus, value, "AlertStatus", AlertStatusPropChangedHandler); }
        }

        private Formation _unitFormation;
        public Formation UnitFormation {
            get {
                D.AssertNotDefault((int)_unitFormation, DebugName);
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

        public IEnumerable<CmdSensor> Sensors { get; private set; }

        public FtlDampener FtlDampener { get; private set; }

        protected IList<AUnitElementData> _elementsData;
        protected IDictionary<AUnitElementData, IList<IDisposable>> _elementSubscriptionsLookup;
        private IList<IDisposable> _subscriptions;

        #region Initialization 

        /// <summary>
        /// Initializes a new instance of the <see cref="AUnitCmdItemData" /> class.
        /// </summary>
        /// <param name="cmd">The command.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="passiveCMs">The passive countermeasures protecting the command staff.</param>
        /// <param name="sensors">The sensors.</param>
        /// <param name="ftlDampener">The FTL dampener.</param>
        /// <param name="cmdStat">The command stat.</param>
        /// <param name="designName">Name of the design.</param>
        public AUnitCmdData(IUnitCmd cmd, Player owner, IEnumerable<PassiveCountermeasure> passiveCMs, IEnumerable<CmdSensor> sensors,
            FtlDampener ftlDampener, ACmdModuleStat cmdStat, string designName)
            : base(cmd, owner, cmdStat.MaxHitPoints, passiveCMs) {
            FtlDampener = ftlDampener;
            Sensors = sensors;
            MaxCmdEffectiveness = cmdStat.MaxCmdEffectiveness;
            DesignName = designName;
            // A command's UnitMaxHitPoints are constructed from the sum of the elements
            InitializeCollections();
            Subscribe();
        }

        private void InitializeCollections() {
            _elementsData = new List<AUnitElementData>();
            _elementSubscriptionsLookup = new Dictionary<AUnitElementData, IList<IDisposable>>();
        }

        private void Subscribe() {
            _subscriptions = new List<IDisposable>();
            foreach (var sensor in Sensors) {
                _subscriptions.Add(sensor.SubscribeToPropertyChanged<CmdSensor, float>(s => s.RangeDistance, CmdSensorRangeDistancePropChangedHandler));
            }
        }

        #endregion

        public override void CommenceOperations() {
            base.CommenceOperations();
            RecalcPropertiesDerivedFromCombinedElements();
            // 3.30.17 Activation of FtlDampener handled by HandleAlertStatusChanged
        }

        public void ActivateSensors() {
            // 5.13.17 Moved from Data.CommenceOperations to allow Cmd.CommenceOperations to call when
            // it is prepared to detect and be detected - aka after it enters Idling state.
            if (!_debugCntls.DeactivateMRSensors) {
                Sensors.Where(s => s.RangeCategory == RangeCategory.Medium).ForAll(s => s.IsActivated = true);
            }
            if (!_debugCntls.DeactivateLRSensors) {
                Sensors.Where(s => s.RangeCategory == RangeCategory.Long).ForAll(s => s.IsActivated = true);
            }
            RecalcUnitSensorRange();
        }

        protected virtual void Subscribe(AUnitElementData elementData) {
            _elementSubscriptionsLookup.Add(elementData, new List<IDisposable>());
            IList<IDisposable> anElementsSubscriptions = _elementSubscriptionsLookup[elementData];
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

        private void AlertStatusPropChangedHandler() {
            HandleAlertStatusChanged();
        }

        private void HQElementDataPropChangingHandler(AUnitElementData newHQElementData) {
            HandleHQElementDataChanging(newHQElementData);
        }

        private void HQElementDataPropChangedHandler() {
            HandleHQElementDataChanged();
        }

        private void HQElementTopographyChangedEventHandler(object sender, EventArgs e) {
            Topography = HQElementData.Topography;
        }

        private void HQElementIntelCoverageChangedEventHandler(object sender, IntelCoverageChangedEventArgs e) {
            HandleHQElementIntelCoverageChanged(e.Player);
        }

        private void UnitMaxHitPtsPropChangingHandler(float newMaxHitPts) {
            HandleUnitMaxHitPtsChanging(newMaxHitPts);
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

        private void UnitWeaponsRangePropChangedHandler() {
            HandleUnitWeaponsRangeChanged();
        }

        private void MaxCmdEffectivenessPropChangedHandler() {
            RefreshCurrentCmdEffectiveness();
        }

        private void UnitNamePropChangedHandler() {
            HandleUnitNameChanged();
        }

        private void CmdSensorRangeDistancePropChangedHandler() {
            HandleCmdSensorRangeChanged();
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

        private void HandleAlertStatusChanged() {
            ElementsData.ForAll(eData => eData.AlertStatus = AlertStatus);
            switch (AlertStatus) {
                case AlertStatus.Normal:
                    D.Log(ShowDebugLog, "{0} {1} changed to {2}.", DebugName, typeof(AlertStatus).Name, AlertStatus.GetValueName());
                    FtlDampener.IsActivated = false;
                    break;
                case AlertStatus.Yellow:
                    D.Log(ShowDebugLog, "{0} {1} changed to {2}.", DebugName, typeof(AlertStatus).Name, AlertStatus.GetValueName());
                    FtlDampener.IsActivated = false;
                    break;
                case AlertStatus.Red:
                    D.Log(/*ShowDebugLog, */"{0} {1} changed to {2}.", DebugName, typeof(AlertStatus).Name, AlertStatus.GetValueName());
                    FtlDampener.IsActivated = true;
                    break;
                case AlertStatus.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(AlertStatus));
            }
        }

        protected virtual void HandleHQElementDataChanging(AUnitElementData newHQElementData) {
            //D.Log(ShowDebugLog, "{0}.HQElementData is about to change.", DebugName);
            var previousHQElementData = HQElementData;
            if (previousHQElementData != null) {
                previousHQElementData.intelCoverageChanged -= HQElementIntelCoverageChangedEventHandler;
                previousHQElementData.topographyChanged -= HQElementTopographyChangedEventHandler;
            }
        }

        protected virtual void HandleHQElementDataChanged() {
            D.Assert(_elementsData.Contains(HQElementData), HQElementData.DebugName);
            // Align the IntelCoverage of this Cmd with that of its new HQ
            var otherPlayers = _gameMgr.AllPlayers.Except(Owner);
            foreach (var player in otherPlayers) {
                IntelCoverage playerIntelCoverageOfNewHQ = HQElementData.GetIntelCoverage(player);
                IntelCoverage resultingCoverage;
                bool isPlayerIntelCoverageAccepted = TrySetIntelCoverage(player, playerIntelCoverageOfNewHQ, out resultingCoverage);
                // 2.6.17 FIXME It seems unlikely but possible that a new Facility HQ could have IntelCoverage.None when the
                // previous HQ had > None, thereby attempting to illegally regress a BaseCmd's IntelCoverage to None from > None.
                // Same thing could happen to FleetCmd except regress to None wouldn't be illegal. It WOULD result in the Fleet disappearing...
                // Possible fixes: if illegal, force change of new HQ IntelCoverage to what old HQ was?
                D.Assert(isPlayerIntelCoverageAccepted);
            }
            HQElementData.intelCoverageChanged += HQElementIntelCoverageChangedEventHandler;

            Topography = GetTopography();
            HQElementData.topographyChanged += HQElementTopographyChangedEventHandler;
        }

        private void HandleHQElementIntelCoverageChanged(Player playerWhosCoverageChgd) {
            if (HQElementData.IsOwnerChangeUnderway && _elementsData.Count > Constants.One) {
                // 5.17.17 HQElement is changing owner and I'm not going to go with it to that owner
                // so don't follow its IntelCoverage change. I'll pick up my IntelCoverage as soon as
                // I get my newly assigned HQElement. Following its change when not going with it results
                // in telling others of my very temp change which will throw errors when the chg doesn't make sense
                // - e.g. tracking its change to IntelCoverage.None with an ally.
                // 5.20.17 This combination of criteria can never occur for a BaseCmd as an element owner change
                // is only possible when it is the only element.
                return;
            }

            var playerIntelCoverageOfHQElement = HQElementData.GetIntelCoverage(playerWhosCoverageChgd);
            IntelCoverage resultingCoverage;
            var isIntelCoverageAccepted = TrySetIntelCoverage(playerWhosCoverageChgd, playerIntelCoverageOfHQElement, out resultingCoverage);
            D.Assert(isIntelCoverageAccepted);
            //D.Log(ShowDebugLog, "{0}.HQElement's IntelCoverage for {1} has changed to {2}. {0} has assumed the same value.", 
            //    DebugName, playerWhosCoverageChgd.LeaderName, playerIntelCoverageOfHQElement.GetValueName());
        }

        private void HandleUnitMaxHitPtsChanging(float newMaxHitPts) {
            if (newMaxHitPts < UnitMaxHitPoints) {
                // reduction in max hit points so reduce current hit points to match
                UnitCurrentHitPoints = Mathf.Clamp(UnitCurrentHitPoints, Constants.ZeroF, newMaxHitPts);
                // FIXME changing CurrentHitPoints here sends out a temporary erroneous health change event. The accurate health change event follows shortly
            }
        }

        /// <summary>
        /// Called when the Unit's health changes.
        /// NOTE: This sets the UnitCommand's CurrentHitPoints (and Health) to 0 when UnitHealth reaches 0. 
        /// This is not done to initiate the UnitCommand's death, but to keep the values of a UnitCommand's CurrentHitPoints 
        /// and Health consistent with the way other Item's values are treated for any future subscribers to health changes.
        /// </summary>
        private void HandleUnitHealthChanged() {
            //D.Log(ShowDebugLog, "{0}: UnitHealth {1}, UnitCurrentHitPoints {2}, UnitMaxHitPoints {3}.", DebugName, _unitHealth, UnitCurrentHitPoints, UnitMaxHitPoints);
            if (UnitHealth <= Constants.ZeroF) {
                CurrentHitPoints -= MaxHitPoints;
            }
        }

        protected override void HandleHealthChanged() {
            base.HandleHealthChanged();
            RefreshCurrentCmdEffectiveness();
        }

        protected abstract void HandleUnitWeaponsRangeChanged();

        private void HandleUnitNameChanged() {
            Name = UnitName + GameConstants.CmdNameExtension;
            if (!_elementsData.IsNullOrEmpty()) {
                _elementsData.ForAll(eData => eData.UnitName = UnitName);
            }
        }

        private void HandleCmdSensorRangeChanged() {
            RecalcUnitSensorRange();
        }

        private void RefreshCurrentCmdEffectiveness() {
            CurrentCmdEffectiveness = MaxCmdEffectiveness * Health;
            // concept: staff and equipment are hurt as health of the Cmd declines
            // as Health of a Cmd cannot decline below 50% due to CurrentHitPoints override, neither can CmdEffectiveness, until the Unit is destroyed
        }

        public virtual void AddElement(AUnitElementData elementData) {
            D.Assert(!_elementsData.Contains(elementData), elementData.DebugName);
            __ValidateOwner(elementData);
            UpdateElementUnitName(elementData);
            _elementsData.Add(elementData);
            Subscribe(elementData);
            RefreshComposition();

            if (IsOperational) {
                RecalcPropertiesDerivedFromCombinedElements();
            }
        }

        private void __ValidateOwner(AUnitElementData elementData) {
            D.AssertNotEqual(Owner, TempGameValues.NoPlayer, "Owner should be set before adding elements.");
            if (elementData.Owner == TempGameValues.NoPlayer) {
                D.Warn("{0} owner should be set before adding element to {1}.", elementData.Name, DebugName);
                elementData.Owner = Owner;
            }
            else if (elementData.Owner != Owner) {
                D.Warn("{0} owner {1} is different from {2} owner {3}.", elementData.Name, elementData.Owner, DebugName, Owner);
                elementData.Owner = Owner;
            }
        }

        private void UpdateElementUnitName(AUnitElementData elementData) {
            //D.Log(ShowDebugLog, "{0}.ParentName changing to {1}.", elementData.Name, UnitName);
            elementData.UnitName = UnitName;    // the name of the fleet, not the command
        }

        public virtual void RemoveElement(AUnitElementData elementData) {
            bool isRemoved = _elementsData.Remove(elementData);
            D.Assert(isRemoved, elementData.DebugName);

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
            D.Assert(IsOperational);
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
            var shortRangeSensors = _elementsData.SelectMany(ed => ed.Sensors).Where(s => s.IsOperational);
            var mediumRangeSensors = Sensors.Where(s => s.RangeCategory == RangeCategory.Medium && s.IsOperational);
            var longRangeSensors = Sensors.Where(s => s.RangeCategory == RangeCategory.Long && s.IsOperational);
            float shortRangeDistance = shortRangeSensors.First().RangeDistance;
            float mediumRangeDistance = mediumRangeSensors.Any() ? mediumRangeSensors.First().RangeDistance : Constants.ZeroF;
            float longRangeDistance = longRangeSensors.Any() ? longRangeSensors.First().RangeDistance : Constants.ZeroF;
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

        protected override void HandleDeath() {
            base.HandleDeath();
            Sensors.ForAll(sens => sens.IsActivated = false);
            FtlDampener.IsActivated = false;
        }

        private void Unsubscribe(AUnitElementData elementData) {
            _elementSubscriptionsLookup[elementData].ForAll(d => d.Dispose());
            _elementSubscriptionsLookup.Remove(elementData);

            D.AssertNotNull(HQElementData);    // UNCLEAR when HQElementData gets nulled when elementData == HQElementData
            if (elementData == HQElementData) {
                HQElementData.intelCoverageChanged -= HQElementIntelCoverageChangedEventHandler;
            }
        }

        protected override void Unsubscribe() {
            base.Unsubscribe();
            IList<AUnitElementData> subscriberKeys = new List<AUnitElementData>(_elementSubscriptionsLookup.Keys);
            // copy of key list as you can't remove keys from a list while you are iterating over the list
            foreach (AUnitElementData eData in subscriberKeys) {
                Unsubscribe(eData);
            }
            _elementSubscriptionsLookup.Clear();

            _subscriptions.ForAll(d => d.Dispose());
            _subscriptions.Clear();
        }


    }
}

