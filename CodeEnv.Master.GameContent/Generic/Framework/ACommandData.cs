// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ACommandData.cs
// Abstract base class for data associated with a Unit Command.
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
    /// Abstract base class for data associated with a Unit Command.
    /// </summary>
    public abstract class ACommandData : AMortalItemData, IDisposable {

        public event Action onCompositionChanged;

        protected void OnCompositionChanged() {
            var temp = onCompositionChanged;
            if (temp != null) {
                temp();
            }
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
            get {
                return base.Health;
            }
        }

        private IPlayer _owner;
        public IPlayer Owner {
            get { return _owner; }
            set { SetProperty<IPlayer>(ref _owner, value, "Owner", OnOwnerChanged); }
        }

        private int _cmdEffectiveness;
        public int CmdEffectiveness {  // TODO make use of this
            get { return _cmdEffectiveness; }
            private set { SetProperty<int>(ref _cmdEffectiveness, value, "CmdEffectiveness"); }
        }

        private float _unitWeaponsRange;
        /// <summary>
        /// The range of the longest range weapon in the Unit. IMPROVE
        /// </summary>
        public float UnitWeaponsRange {
            get { return _unitWeaponsRange; }
            set { SetProperty<float>(ref _unitWeaponsRange, value, "UnitWeaponsRange"); }
        }

        private CombatStrength _unitStrength;
        public CombatStrength UnitStrength {
            get {
                return _unitStrength;
            }
            private set {
                SetProperty<CombatStrength>(ref _unitStrength, value, "UnitStrength");
            }
        }

        private float _unitMaxHitPoints;
        public float UnitMaxHitPoints {
            get { return _unitMaxHitPoints; }
            private set { SetProperty<float>(ref _unitMaxHitPoints, value, "UnitMaxHitPoints", OnUnitMaxHitPointsChanged, OnUnitMaxHitPointsChanging); }
        }

        private float _unitCurrentHitPoints;
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
        /// <param name="cmdParentName">Name of the parent of this Command, eg. the FleetName for a FleetCommand.</param>
        /// <param name="cmdMaxHitPoints">The maximum hit points of this Command staff.</param>
        public ACommandData(string cmdParentName, float cmdMaxHitPoints)
            : base(cmdParentName + Constants.Space + CommonTerms.Command, cmdMaxHitPoints, mass: 0.1F, optionalParentName: cmdParentName) {
            // A command's UnitMaxHitPoints are constructed from the sum of the elements
            InitializeCollections();
        }

        private void InitializeCollections() {
            ElementsData = new List<AElementData>();
            _subscribers = new Dictionary<AElementData, IList<IDisposable>>();
            InitializeComposition();
        }

        protected abstract void InitializeComposition();

        private void OnHQElementDataChanged() {
            if (!ElementsData.Contains(_hqElementData)) {
                D.Error("HQ Element {0} assigned not present in Command {1}.", _hqElementData.OptionalParentName, OptionalParentName);
            }
        }

        private void OnOwnerChanged() {
            // UNDONE change all element owners?
            D.Log("{0} Owner has changed to {1}.", OptionalParentName, Owner.LeaderName);
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

        protected override void OnHealthChanged() {
            base.OnHealthChanged();
            CmdEffectiveness = Mathf.RoundToInt(100 * Health);  // concept: staff and equipment are hurt as health of the Cmd declines
            // as Health of a Cmd cannot decline below 50% due to CurrentHitPoints override, neither can CmdEffectiveness, until the Unit is destroyed
        }

        public void AddElement(AElementData elementData) {
            if (!ElementsData.Contains(elementData)) {
                ValidateOwner(elementData.Owner);
                UpdateElementParentName(elementData);
                ElementsData.Add(elementData);

                ChangeComposition(elementData, toAdd: true);
                Subscribe(elementData);
                UpdatePropertiesDerivedFromCombinedElements();
                return;
            }
            D.Warn("Attempting to add {0} {1} that is already present.", typeof(AElementData), elementData.OptionalParentName);
        }

        private void ValidateOwner(IPlayer owner) {
            if (Owner == null) {
                // first setting of owner by first Element added. Not broadcast
                _owner = owner;
            }
            D.Assert(Owner == owner, "Owners {0} and {1} are different.".Inject(Owner.LeaderName, owner.LeaderName));
        }

        private void UpdateElementParentName(AElementData elementData) {
            // TODO something more than just assigning a parent name?
            elementData.OptionalParentName = OptionalParentName;
        }

        protected abstract void ChangeComposition(AElementData elementData, bool toAdd);

        public bool RemoveElement(AElementData elementData) {
            if (ElementsData.Contains(elementData)) {
                bool isRemoved = ElementsData.Remove(elementData);

                ChangeComposition(elementData, toAdd: false);
                Unsubscribe(elementData);
                UpdatePropertiesDerivedFromCombinedElements();
                return isRemoved;
            }
            D.Warn("Attempting to remove {0} {1} that is not present.", typeof(AElementData), elementData.OptionalParentName);
            return false;
        }

        /// <summary>
        /// Recalculates any Command properties that are dependant upon the total element population.
        /// </summary>
        protected virtual void UpdatePropertiesDerivedFromCombinedElements() {
            UpdateUnitStrength();
            UpdateUnitMaxHitPoints();   // must preceed current as current uses max as a clamp
            UpdateUnitCurrentHitPoints();
            UpdateUnitWeaponsRange();
        }

        private void UpdateUnitStrength() { // IMPROVE avoid creating new each time
            CombatStrength sum = new CombatStrength();
            foreach (var eData in ElementsData) {
                sum.AddToTotal(eData.Strength);
            }
            UnitStrength = sum;
        }

        private void UpdateUnitMaxHitPoints() {
            UnitMaxHitPoints = ElementsData.Sum<AElementData>(ed => ed.MaxHitPoints);
        }

        private void UpdateUnitCurrentHitPoints() {
            UnitCurrentHitPoints = ElementsData.Sum<AElementData>(ed => ed.CurrentHitPoints);
        }

        private void UpdateUnitWeaponsRange() {
            UnitWeaponsRange = ElementsData.Count == 0 ? Constants.ZeroF : ElementsData.MaxBy<AElementData, float>(ed => ed.WeaponsRange).WeaponsRange;
        }

        #region ElementData PropertyChanged Subscription and Methods

        protected virtual void Subscribe(AElementData elementData) {
            _subscribers.Add(elementData, new List<IDisposable>());
            IList<IDisposable> anElementsSubscriptions = _subscribers[elementData];
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AElementData, float>(ed => ed.CurrentHitPoints, OnElementCurrentHitPointsChanged));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AElementData, float>(ed => ed.MaxHitPoints, OnElementMaxHitPointsChanged));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<AElementData, CombatStrength>(ed => ed.Strength, OnElementStrengthChanged));
        }

        private void OnElementStrengthChanged() {
            UpdateUnitStrength();
        }

        private void OnElementCurrentHitPointsChanged() {
            UpdateUnitCurrentHitPoints();
        }

        private void OnElementMaxHitPointsChanged() {
            UpdateUnitMaxHitPoints();
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

