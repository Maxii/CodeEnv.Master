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
    public abstract class ACommandData : ACombatItemData, IDisposable {

        public event Action onCompositionChanged;

        public new SpaceTopography Topography { get { return HQElementData.Topography; } }

        private Formation _unitFormation;
        public Formation UnitFormation {
            get { return _unitFormation; }
            set { SetProperty<Formation>(ref _unitFormation, value, "UnitFormation"); }
        }

        private AElementData _hqElementData;
        public AElementData HQElementData {
            get {
                return _hqElementData;
            }
            set {
                SetProperty<AElementData>(ref _hqElementData, value, "HQElementData", OnHQElementDataChanged);
            }
        }

        /// <summary>
        /// Gets or sets the current hit points of this UnitCommand.
        /// NOTE: Like Health, this value will only reach 0 when the Unit's Health reaches 0.
        /// </summary>
        public override float CurrentHitPoints {
            get {
                return base.CurrentHitPoints;
            }
            set {
                value = Mathf.Clamp(value, MaxHitPoints * 0.5F, MaxHitPoints);  // TODO externalize 0.5?
                base.CurrentHitPoints = value;
            }
        }

        /// <summary>
        /// Readonly. Indicates the health of this Unit's Command, a value between 0 and 1.
        /// NOTE: Like CurrentHitPoints, this value will only reach 0 when the Unit's overall UnitHealth reaches 0.
        /// </summary>
        public override float Health {
            get { return base.Health; }
        }

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

        /// <summary>
        /// The maximum range of all the element's weapons that are part of this unit.
        /// </summary>
        public override float MaxWeaponsRange {
            get { return base.MaxWeaponsRange; }
            set { base.MaxWeaponsRange = value; }
        }

        private CombatStrength _unitStrength;
        /// <summary>
        /// Readonly. The combat strength of the entire Unit, aka the sum of all
        /// of this Unit's Elements combat strength.
        /// </summary>
        /// <value>
        /// The unit strength.
        /// </value>
        public CombatStrength UnitStrength {
            get {
                return _unitStrength;
            }
            private set {
                SetProperty<CombatStrength>(ref _unitStrength, value, "UnitStrength");
            }
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
                //D.Log("Health {0}, CurrentHitPoints {1}, MaxHitPoints {2}.", _health, _currentHitPoints, _maxHitPoints);
                return _unitHealth;
            }
            private set {
                value = Mathf.Clamp01(value);
                SetProperty<float>(ref _unitHealth, value, "UnitHealth", OnUnitHealthChanged);
            }
        }

        // NOTE: Using new to overwrite a list of base types does not work!!
        protected IList<AElementData> ElementsData { get; private set; }
        protected IDictionary<AElementData, IList<IDisposable>> _subscribers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ACommandData" /> class.
        /// </summary>
        /// <param name="unitName">Name of this Unit, eg. the FleetName for a FleetCommand.</param>
        /// <param name="cmdMaxHitPoints">The maximum hit points of this Command staff.</param>
        public ACommandData(string unitName, float cmdMaxHitPoints)
            //: base(CommonTerms.Command, cmdMaxHitPoints, mass: 0.1F, optionalParentName: unitName) {
            : base(CommonTerms.Command, mass: 0.1F, maxHitPts: cmdMaxHitPoints) {
            ParentName = unitName;
            // A command's UnitMaxHitPoints are constructed from the sum of the elements
            InitializeCollections();
        }

        private void InitializeCollections() {
            ElementsData = new List<AElementData>();
            _subscribers = new Dictionary<AElementData, IList<IDisposable>>();
            InitializeComposition();
        }

        protected abstract void InitializeComposition();

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
            //D.Log("{0} attempting to set UnitHealth to {1}. UnitCurrentHitPoints = {2}, UnitMaxHitPoints = {3}.", Name, unitHealth, UnitCurrentHitPoints, UnitMaxHitPoints);
            UnitHealth = unitHealth;
        }

        /// <summary>
        /// Called when the Unit's health changes.
        /// NOTE: This is a workaround that sets the UnitCommand's CurrentHitPoints 
        /// and Health to 0 when UnitHealth reaches 0.. It does so by changing the UnitCommand's CurrentHitPoints by directly
        /// changing the base value, bypassing the override that holds the value to a predetermined minimum. This is not
        /// used to determine the UnitCommand's death. It is done to keep the values of a UnitCommand's CurrentHitPoints 
        /// and Health consistent with the way other Item's values are treated for any future subscribers to health changes.
        /// </summary>
        private void OnUnitHealthChanged() {
            if (UnitHealth <= Constants.ZeroF) {
                base.CurrentHitPoints -= MaxHitPoints;  // initiate destruction of Cmd item too
            }
        }

        protected void OnCompositionChanged() {
            if (onCompositionChanged != null) {
                onCompositionChanged();
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

            ChangeComposition(elementData, toAdd: true);
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

        protected abstract void ChangeComposition(AElementData elementData, bool toAdd);

        public virtual bool RemoveElement(AElementData elementData) {
            D.Assert(ElementsData.Contains(elementData), "Attempted to remove {0} {1} that is not present.".Inject(typeof(AElementData).Name, elementData.ParentName));
            bool isRemoved = ElementsData.Remove(elementData);

            ChangeComposition(elementData, toAdd: false);
            Unsubscribe(elementData);
            RecalcPropertiesDerivedFromCombinedElements();
            return isRemoved;
        }

        /// <summary>
        /// Recalculates any Command properties that are dependant upon the total element population.
        /// </summary>
        protected virtual void RecalcPropertiesDerivedFromCombinedElements() {
            RecalcUnitStrength();
            RecalcUnitMaxHitPoints();   // must preceed current as current uses max as a clamp
            RecalcUnitCurrentHitPoints();
            RecalcUnitMaxWeaponsRange();
        }

        private void RecalcUnitStrength() {
            var defaultValueIfEmpty = default(CombatStrength);
            UnitStrength = ElementsData.Select(ed => ed.Strength).Aggregate(defaultValueIfEmpty, (accum, strength) => accum + strength);
        }

        private void RecalcUnitMaxHitPoints() {
            UnitMaxHitPoints = ElementsData.Sum<AElementData>(ed => ed.MaxHitPoints);
        }

        private void RecalcUnitCurrentHitPoints() {
            UnitCurrentHitPoints = ElementsData.Sum<AElementData>(ed => ed.CurrentHitPoints);
        }

        private void RecalcUnitMaxWeaponsRange() {
            MaxWeaponsRange = ElementsData.Count == 0 ? Constants.ZeroF : ElementsData.Max<AElementData>(ed => ed.MaxWeaponsRange);
        }

        #region ElementData PropertyChanged Subscription and Methods

        protected virtual void Subscribe(AElementData elementData) {
            _subscribers.Add(elementData, new List<IDisposable>());
            IList<IDisposable> anElementsSubscriptions = _subscribers[elementData];
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AElementData, float>(ed => ed.CurrentHitPoints, OnElementCurrentHitPointsChanged));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AElementData, float>(ed => ed.MaxHitPoints, OnElementMaxHitPointsChanged));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AElementData, CombatStrength>(ed => ed.Strength, OnElementStrengthChanged));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AElementData, float>(ed => ed.MaxWeaponsRange, OnElementMaxWeaponsRangeChanged));
        }

        private void OnElementStrengthChanged() {
            RecalcUnitStrength();
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

        private void Unsubscribe(AElementData elementData) {
            _subscribers[elementData].ForAll<IDisposable>(d => d.Dispose());
            _subscribers.Remove(elementData);
        }

        #endregion

        private void Cleanup() {
            Unsubscribe();
        }

        private void Unsubscribe() {
            IList<AElementData> subscriberKeys = new List<AElementData>(_subscribers.Keys);
            // copy of key list as you can't remove keys from a list while you are iterating over the list
            foreach (AElementData eData in subscriberKeys) {
                Unsubscribe(eData);
            }
            _subscribers.Clear();
        }

        #region IDisposable
        [DoNotSerialize]
        private bool alreadyDisposed = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
        /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
        /// </summary>
        /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool isDisposing) {
            // Allows Dispose(isDisposing) to be called more than once
            if (alreadyDisposed) {
                return;
            }

            if (isDisposing) {
                // free managed resources here including unhooking events
                Cleanup();
            }
            // free unmanaged resources here

            alreadyDisposed = true;
        }

        // Example method showing check for whether the object has been disposed
        //public void ExampleMethod() {
        //    // throw Exception if called on object that is already disposed
        //    if(alreadyDisposed) {
        //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
        //    }

        //    // method content here
        //}
        #endregion

    }
}

