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
            set {
                __ValidateUnitMaxFormationRadius();
                SetProperty<float>(ref _unitMaxFormationRadius, value, "UnitMaxFormationRadius");
            }
        }

        public abstract IEnumerable<Formation> AcceptableFormations { get; }

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

        private Formation _formation;
        public Formation Formation {
            get {
                D.AssertNotDefault((int)_formation, DebugName);
                return _formation;
            }
            set { SetProperty<Formation>(ref _formation, value, "Formation"); }
        }

        private AUnitElementData _hqElementData;
        public AUnitElementData HQElementData {
            protected get { return _hqElementData; }
            set { SetProperty<AUnitElementData>(ref _hqElementData, value, "HQElementData", HQElementDataPropChangedHandler, HQElementDataPropChangingHandler); }
        }

        // AItemData.Health, CurrentHitPts and MaxHitPts are all for this CommandData, not for the Unit as a whole.
        // This CurrentHitPts value is managed by the AUnitCommandItem.ApplyDamage() override which currently 
        // doesn't let it drop below 50% of MaxHitPts. Health is directly derived from changes in CurrentHitPts.

        private Hero _hero = TempGameValues.NoHero;
        public Hero Hero {
            get { return _hero; }
            set { SetProperty<Hero>(ref _hero, value, "Hero", HeroPropChangedHandler); }
        }

        private float _currentCmdEffectiveness;
        /// <summary>
        /// The current effectiveness of this command including contribution from a Hero if present.
        /// <remarks>9.19.17 Can be > 1.0F if hero is present.</remarks>
        /// </summary>
        public float CurrentCmdEffectiveness {  // UNDONE concept/range needs work
            get { return _currentCmdEffectiveness; }
            private set { SetProperty<float>(ref _currentCmdEffectiveness, value, "CurrentCmdEffectiveness"); }
        }

        private float _maxCmdStaffEffectiveness;
        public float MaxCmdStaffEffectiveness {  // UNDONE concept/range needs work
            get { return _maxCmdStaffEffectiveness; }
            set { SetProperty<float>(ref _maxCmdStaffEffectiveness, value, "MaxCmdStaffEffectiveness", MaxCmdStaffEffectivenessPropChangedHandler); }
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
        /// Read-only. The offensive combat strength of the entire Unit, aka the sum of all of this Unit's Elements offensive combat strength.
        /// </summary>
        public CombatStrength UnitOffensiveStrength {
            get { return _unitOffensiveStrength; }
            private set { SetProperty<CombatStrength>(ref _unitOffensiveStrength, value, "UnitOffensiveStrength"); }
        }

        private CombatStrength _unitDefensiveStrength;
        /// <summary>
        /// Read-only. The defensive combat strength of the entire Unit, aka the sum of all of this Unit's Elements defensive combat strength.
        /// </summary>
        public CombatStrength UnitDefensiveStrength {
            get { return _unitDefensiveStrength; }
            private set { SetProperty<CombatStrength>(ref _unitDefensiveStrength, value, "UnitDefensiveStrength"); }
        }

        private float _unitMaxHitPoints;
        /// <summary>
        /// Read-only. The max hit points of the entire Unit, aka the sum of all of this Unit's Elements max hit points.
        /// </summary>
        public float UnitMaxHitPoints {
            get { return _unitMaxHitPoints; }
            private set { SetProperty<float>(ref _unitMaxHitPoints, value, "UnitMaxHitPoints", UnitMaxHitPtsPropChangedHandler, UnitMaxHitPtsPropChangingHandler); }
        }

        private float _unitCurrentHitPoints;
        /// <summary>
        /// Read-only. The current hit points of the entire Unit, aka the sum of all of this Unit's Elements current hit points.
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

        private OutputsYield _unitOutputs;
        public OutputsYield UnitOutputs {
            get { return _unitOutputs; }
            private set { SetProperty<OutputsYield>(ref _unitOutputs, value, "UnitOutputs"); }
        }

        private float CmdModuleMaxHitPoints { get { return base.MaxHitPoints; } }

        [Obsolete("Use CmdModuleMaxHitPoints")]
        public new float MaxHitPoints { get { return base.MaxHitPoints; } }

        public float CmdModuleCurrentHitPoints {
            get { return base.CurrentHitPoints; }
            protected set { base.CurrentHitPoints = value; }
        }

        [Obsolete("Use CmdModuleCurrentHitPoints")]
        public new float CurrentHitPoints { get { return base.CurrentHitPoints; } }

        public float CmdModuleHealth { get { return base.Health; } }

        [Obsolete("Use CmdModuleHealth")]
        public new float Health { get { return base.Health; } }

        public IEnumerable<AUnitElementData> ElementsData { get { return _elementsData; } }

        public IEnumerable<CmdSensor> Sensors { get; private set; }

        public FtlDamper FtlDamper { get; private set; }

        public AUnitCmdModuleDesign CmdModuleDesign { get; private set; }

        public int ElementCount { get { return _elementsData.Count; } }

        protected sealed override IntelCoverage DefaultStartingIntelCoverage { get { return IntelCoverage.None; } }

        protected IDictionary<AUnitElementData, IList<IDisposable>> _elementSubscriptionsLookup;
        private IList<AUnitElementData> _elementsData;
        private IList<IDisposable> _sensorRangeSubscriptions;

        #region Initialization 

        /// <summary>
        /// Initializes a new instance of the <see cref="AUnitCmdItemData" /> class.
        /// </summary>
        /// <param name="cmd">The command.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="passiveCMs">The passive countermeasures protecting the command staff.</param>
        /// <param name="sensors">The sensors.</param>
        /// <param name="ftlDamper">The FTL damper.</param>
        /// <param name="cmdModDesign">The command module design.</param>
        public AUnitCmdData(IUnitCmd cmd, Player owner, IEnumerable<PassiveCountermeasure> passiveCMs, IEnumerable<CmdSensor> sensors,
            FtlDamper ftlDamper, AUnitCmdModuleDesign cmdModDesign)
            : base(cmd, owner, cmdModDesign.HitPoints, passiveCMs) {
            FtlDamper = ftlDamper;
            MaxCmdStaffEffectiveness = cmdModDesign.CmdModuleStat.MaxCmdStaffEffectiveness;
            CmdModuleDesign = cmdModDesign;
            // A command's UnitMaxHitPoints are constructed from the sum of the elements
            InitializeCollections();
            Initialize(sensors);
        }

        private void InitializeCollections() {
            _elementsData = new List<AUnitElementData>();
            _elementSubscriptionsLookup = new Dictionary<AUnitElementData, IList<IDisposable>>();
            _sensorRangeSubscriptions = new List<IDisposable>();
        }

        private void Initialize(IEnumerable<CmdSensor> sensors) {
            Sensors = sensors;
            SubscribeToSensorRangeChanges();
        }

        private void SubscribeToSensorRangeChanges() {
            foreach (var sensor in Sensors) {
                _sensorRangeSubscriptions.Add(sensor.SubscribeToPropertyChanged<CmdSensor, float>(s => s.RangeDistance, CmdSensorRangeDistancePropChangedHandler));
            }
        }

        #endregion

        public override void CommenceOperations() {
            base.CommenceOperations();
            RecalcPropertiesDerivedFromCombinedElements();
            // 3.30.17 Activation of FtlDamper handled by AssessFtlDamperActivation
        }

        public void ActivateCmdSensors() {
            // 5.13.17 Moved from Data.CommenceOperations to allow Cmd.CommenceOperations to call when
            // it is prepared to detect and be detected - aka after it enters Idling state.
            if (!__debugCntls.DeactivateMRSensors) {
                Sensors.Where(s => s.RangeCategory == RangeCategory.Medium).ForAll(s => s.IsActivated = true);
            }
            if (!__debugCntls.DeactivateLRSensors) {
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
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AUnitElementData, OutputsYield>(ed => ed.Outputs, ElementOutputsPropChangedHandler));
        }

        /// <summary>
        /// Replaces the existing CmdModule with the new cmdModuleDesign. Replaces the existing PassiveCMs, CmdSensors and FtlDamper
        /// with the new instances provided as these are derived from the cmdModuleDesign.
        /// <remarks>These changes do not interfere with the ongoing operations of this Cmd. They can however create momentary 
        /// changes in AlertStatus and FtlDamping before both are properly resumed.</remarks>
        /// </summary>
        /// <param name="cmdModuleDesign">The design of the new CmdModule.</param>
        /// <param name="passiveCMs">The replacement PassiveCountermeasures.</param>
        /// <param name="sensors">The replacement CmdSensors.</param>
        /// <param name="ftlDamper">The replacement FtlDamper.</param>
        public virtual void ReplaceCmdModuleWith(AUnitCmdModuleDesign cmdModuleDesign, IEnumerable<PassiveCountermeasure> passiveCMs,
            IEnumerable<CmdSensor> sensors, FtlDamper ftlDamper) {
            CmdModuleDesign = cmdModuleDesign;
            MaxCmdStaffEffectiveness = cmdModuleDesign.CmdModuleStat.MaxCmdStaffEffectiveness;
            ReplacePassiveCMs(passiveCMs);
            ReplaceSensors(sensors);
            ReplaceFtlDamper(ftlDamper);
        }

        private void ReplaceSensors(IEnumerable<CmdSensor> sensorReplacements) {
            D.Assert(Sensors.All(sensor => !sensor.IsActivated && sensor.RangeMonitor == null));
            UnsubscribeToSensorRangeChanges();
            D.Assert(sensorReplacements.All(sensor => !sensor.IsActivated && sensor.RangeMonitor != null));
            Initialize(sensorReplacements);
        }

        private void ReplaceFtlDamper(FtlDamper replacementFtlDamper) {
            D.AssertNotNull(FtlDamper);
            D.Assert(!FtlDamper.IsActivated);
            D.AssertNull(FtlDamper.RangeMonitor);

            D.Assert(!replacementFtlDamper.IsActivated);
            D.AssertNotNull(replacementFtlDamper.RangeMonitor);
            FtlDamper = replacementFtlDamper;
            AssessFtlDamperActivation();
        }

        #region Event and Property Change Handlers

        private void HeroPropChangedHandler() {
            D.AssertNotNull(Hero); // TempGameValues.NoHero but never null
            HandleHeroChanged();
        }

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

        private void MaxCmdStaffEffectivenessPropChangedHandler() {
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

        private void ElementOutputsPropChangedHandler() {
            RecalcUnitOutputs();
        }

        #endregion

        private void HandleAlertStatusChanged() {
            if (AlertStatus == AlertStatus.Red) {
                D.Log(/*ShowDebugLog, */"{0} {1} changed to {2}.", DebugName, typeof(AlertStatus).Name, AlertStatus.GetValueName());
            }
            AssessFtlDamperActivation();
            ElementsData.ForAll(eData => eData.AlertStatus = AlertStatus);
        }

        private void AssessFtlDamperActivation() {
            switch (AlertStatus) {
                case AlertStatus.Normal:
                    FtlDamper.IsActivated = false;
                    break;
                case AlertStatus.Yellow:
                    FtlDamper.IsActivated = false;
                    break;
                case AlertStatus.Red:
                    FtlDamper.IsActivated = true;
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
            // Align the IntelCoverage of this Cmd with that of its new HQ  // OPTIMIZE Most of the following is to support the D.Log
            var otherPlayers = _gameMgr.AllPlayers.Except(Owner);
            foreach (var player in otherPlayers) {
                IntelCoverage playerIntelCoverageOfOldHQ = GetIntelCoverage(player);    // Cmds coverage the same as OldHQ until changed
                IntelCoverage playerIntelCoverageOfNewHQ = HQElementData.GetIntelCoverage(player);
                IntelCoverage resultingCoverage;
                bool isPlayerIntelCoverageChgd = TryChangeIntelCoverage(player, playerIntelCoverageOfNewHQ, out resultingCoverage);
                // 2.6.17 It seems unlikely but possible that a new Facility HQ could have IntelCoverage.None when the
                // previous HQ had > None, thereby attempting to regress a BaseCmd's IntelCoverage to None from > None which won't take.
                // Same thing could happen to FleetCmd except regress to None would occur which would result in the Fleet disappearing...
                if (playerIntelCoverageOfOldHQ > IntelCoverage.None && playerIntelCoverageOfNewHQ == IntelCoverage.None) {
                    if (isPlayerIntelCoverageChgd) {
                        D.AssertEqual(GetIntelCoverage(player), IntelCoverage.None);
                        D.Assert(this is FleetCmdData);
                        // 7.29.18 This does occur, albeit rarely
                        D.Log("{0} just disappeared from {1}'s visibility because of a HQ change.", DebugName, player.DebugName);
                    }
                    else {
                        D.AssertNotEqual(GetIntelCoverage(player), IntelCoverage.None);
                        D.Assert(this is AUnitBaseCmdData);
                    }
                }

                if (isPlayerIntelCoverageChgd) {
                    D.AssertNotEqual(playerIntelCoverageOfOldHQ, resultingCoverage);    // OPTIMIZE patently obvious?
                }
            }
            HQElementData.intelCoverageChanged += HQElementIntelCoverageChangedEventHandler;

            Topography = GetTopography();
            HQElementData.topographyChanged += HQElementTopographyChangedEventHandler;
        }

        protected virtual void HandleHQElementIntelCoverageChanged(Player playerWhosCoverageChgd) {
            var playerPriorIntelCoverage = GetIntelCoverage(playerWhosCoverageChgd);
            var playerIntelCoverageOfHQElement = HQElementData.GetIntelCoverage(playerWhosCoverageChgd);
            IntelCoverage resultingCoverage;
            var isIntelCoverageChgd = TryChangeIntelCoverage(playerWhosCoverageChgd, playerIntelCoverageOfHQElement, out resultingCoverage);
            if (isIntelCoverageChgd) {
                D.AssertNotEqual(playerPriorIntelCoverage, resultingCoverage);  // OPTIMIZE patently obvious?
            }
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
            if (UnitHealth <= Constants.ZeroPercent) {
                base.CurrentHitPoints -= CmdModuleMaxHitPoints;
            }
        }

        protected override void HandleHealthChanged() {
            base.HandleHealthChanged();
            RefreshCurrentCmdEffectiveness();
        }

        protected abstract void HandleUnitWeaponsRangeChanged();

        private void HandleUnitNameChanged() {
            Name = UnitName + GameConstants.CmdNameExtension;
        }

        private void HandleCmdSensorRangeChanged() {
            RecalcUnitSensorRange();
        }

        private void HandleHeroChanged() {
            RefreshCurrentCmdEffectiveness();
        }

        private void RefreshCurrentCmdEffectiveness() {
            // concept: staff and equipment are hurt as health of the Cmd declines
            // as Health of a Cmd cannot decline below 50% due to CurrentHitPoints override, neither can CmdEffectiveness, until the Unit is destroyed
            CurrentCmdEffectiveness = (MaxCmdStaffEffectiveness * CmdModuleHealth) + Hero.CmdEffectiveness;
        }

        public virtual void AddElement(AUnitElementData elementData) {
            D.Assert(!_elementsData.Contains(elementData), elementData.DebugName);
            __ValidateOwner(elementData);
            _elementsData.Add(elementData);
            elementData.CmdData = this;

            Subscribe(elementData);
            RefreshComposition();

            if (IsOperational) {
                RecalcPropertiesDerivedFromCombinedElements();
            }
        }

        public virtual void RemoveElement(AUnitElementData elementData) {
            bool isRemoved = _elementsData.Remove(elementData);
            D.Assert(isRemoved, elementData.DebugName);

            elementData.CmdData = null;

            Unsubscribe(elementData);
            RefreshComposition();
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
            UnitOutputs = RecalcUnitOutputs();
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

        protected void RecalcUnitSensorRange() {
            var shortRangeSensors = _elementsData.SelectMany(ed => ed.Sensors).Where(s => s.IsOperational);
            var mediumRangeSensors = Sensors.Where(s => s.RangeCategory == RangeCategory.Medium && s.IsOperational);
            var longRangeSensors = Sensors.Where(s => s.RangeCategory == RangeCategory.Long && s.IsOperational);
            float shortRangeDistance = shortRangeSensors.Any() ? shortRangeSensors.First().RangeDistance : Constants.ZeroF;
            float mediumRangeDistance = mediumRangeSensors.Any() ? mediumRangeSensors.First().RangeDistance : Constants.ZeroF;
            float longRangeDistance = longRangeSensors.Any() ? longRangeSensors.First().RangeDistance : Constants.ZeroF;
            UnitSensorRange = new RangeDistance(shortRangeDistance, mediumRangeDistance, longRangeDistance);
        }

        protected abstract OutputsYield RecalcUnitOutputs();    // abstract to allow BaseData to make sure production output > 0

        #region Combat Support

        /// <summary>
        /// Applies the damage to the command module returning true as the command module will always survive the hit.
        /// </summary>
        /// <param name="damageToCmdModule">The damage sustained by the CmdModule.</param>
        /// <param name="damageSeverity">The damage severity.</param>
        /// <returns>
        ///   <c>true</c> if the command survived.
        /// </returns>
        public override bool ApplyDamage(DamageStrength damageToCmdModule, out float damageSeverity) {
            var initialDamage = damageToCmdModule.__Total;
            damageSeverity = Mathf.Clamp01(initialDamage / CmdModuleCurrentHitPoints);
            float cumCurrentHitPtReductionFromEquip = AssessDamageToEquipment(damageSeverity);
            float totalDamage = initialDamage + cumCurrentHitPtReductionFromEquip;
            var minAllowedCurrentHitPoints = 0.5F * CmdModuleMaxHitPoints;

            if (CmdModuleCurrentHitPoints - totalDamage < minAllowedCurrentHitPoints) {
                CmdModuleCurrentHitPoints = minAllowedCurrentHitPoints;
            }
            else {
                CmdModuleCurrentHitPoints -= totalDamage;
            }
            D.Assert(CmdModuleHealth > Constants.ZeroPercent, "Should never fail as CmdModules can't die directly from a hit.");
            return true;
        }

        protected override float AssessDamageToEquipment(float damageSeverity) {
            float cumCurrentHitPtReductionFromEquip = base.AssessDamageToEquipment(damageSeverity);
            var damageChance = damageSeverity;

            var undamagedDamageableSensors = Sensors.Where(s => s.IsDamageable && !s.IsDamaged);
            foreach (var s in undamagedDamageableSensors) {
                bool toDamage = RandomExtended.Chance(damageChance);
                if (toDamage) {
                    s.IsDamaged = true;
                    cumCurrentHitPtReductionFromEquip += s.HitPoints;
                    D.Log(ShowDebugLog, "{0}'s {1} has been damaged.", DebugName, s.Name);
                }
            }
            D.Assert(!FtlDamper.IsDamageable);
            return cumCurrentHitPtReductionFromEquip;
        }

        #endregion

        #region Repair

        protected override float AssessRepairToEquipment(float repairImpact) {
            float cumRprPtsFromEquip = base.AssessRepairToEquipment(repairImpact);

            var rprChance = repairImpact;

            var damagedSensors = Sensors.Where(s => s.IsDamaged);
            foreach (var s in damagedSensors) {
                bool toRpr = RandomExtended.Chance(rprChance);
                if (toRpr) {
                    s.IsDamaged = false;
                    cumRprPtsFromEquip += s.HitPoints;
                    D.Log(ShowDebugLog, "{0}'s {1} has been repaired.", DebugName, s.Name);
                }
            }
            // 11.21.17 FtlDamper is currently not damageable

            return cumRprPtsFromEquip;
        }

        #endregion

        protected override void RemoveDamageFromAllEquipment() {
            base.RemoveDamageFromAllEquipment();
            Sensors.Where(s => s.IsDamageable).ForAll(s => s.IsDamaged = false);
            // 11.21.17 FtlDamper is currently not damageable
        }

        protected sealed override void DeactivateAllEquipment() {
            base.DeactivateAllEquipment();
            Sensors.ForAll(sens => sens.IsActivated = false);
            FtlDamper.IsActivated = false;
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

            UnsubscribeToSensorRangeChanges();
        }

        private void UnsubscribeToSensorRangeChanges() {
            _sensorRangeSubscriptions.ForAll(d => d.Dispose());
            _sensorRangeSubscriptions.Clear();
        }

        #region Debug

        [System.Diagnostics.Conditional("DEBUG")]
        protected abstract void __ValidateUnitMaxFormationRadius();

        protected override void __ValidateAllEquipmentDamageRepaired() {
            base.__ValidateAllEquipmentDamageRepaired();
            Sensors.ForAll(s => D.Assert(!s.IsDamaged));
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void __ValidateOwner(AUnitElementData elementData) {
            D.AssertNotEqual(Owner, TempGameValues.NoPlayer, "Owner should be set before adding elements.");
            if (elementData.Owner == TempGameValues.NoPlayer) {
                D.Error("{0} owner should be set before adding element to {1}.", elementData.Name, DebugName);
            }
            else if (elementData.Owner != Owner) {
                D.Error("{0} owner {1} is different from {2} owner {3}.", elementData.Name, elementData.Owner.DebugName, DebugName, Owner.DebugName);
            }
        }

        #endregion

    }
}

