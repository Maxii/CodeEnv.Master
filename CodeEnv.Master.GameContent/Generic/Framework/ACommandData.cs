﻿// --------------------------------------------------------------------------------------------------------------------
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

    /// <summary>
    /// Abstract base class for data associated with a Unit Command.
    /// </summary>
    public abstract class ACommandData : AMortalItemData, IDisposable {

        private AElementData _hqElementData;
        public AElementData HQElementData {
            get {
                return _hqElementData;
            }
            set {
                SetProperty<AElementData>(ref _hqElementData, value, "HQElementData", OnHQElementDataChanged);
            }
        }

        private CombatStrength _strength;
        public CombatStrength Strength {
            get {
                return _strength;
            }
            private set {
                SetProperty<CombatStrength>(ref _strength, value, "Strength");
            }
        }

        private IPlayer _owner;
        public IPlayer Owner {
            get { return _owner; }
            set { SetProperty<IPlayer>(ref _owner, value, "Owner", OnOwnerChanged); }
        }

        // NOTE: Using new to overwrite a list of base types does not work!!
        protected IList<AElementData> ElementsData { get; private set; }
        protected IDictionary<AElementData, IList<IDisposable>> _subscribers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ACommandData"/> class.
        /// </summary>
        /// <param name="cmdParentName">Name of the parent of this Command, eg. the FleetName for a FleetCommand.</param>
        public ACommandData(string cmdParentName)
            : base(cmdParentName + Constants.Space + CommonTerms.Command, maxHitPoints: Constants.ZeroF, optionalParentName: cmdParentName) {
            // A command's maxHitPoints are constructed from the sum of the elements
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
            UpdateStrength();
            UpdateMaxHitPoints();   // must preceed current as current uses max as a clamp
            UpdateCurrentHitPoints();
        }

        private void UpdateStrength() {
            CombatStrength sum = new CombatStrength();
            foreach (var eData in ElementsData) {
                sum.AddToTotal(eData.Strength);
            }
            Strength = sum;
        }

        private void UpdateCurrentHitPoints() {
            CurrentHitPoints = ElementsData.Sum<AElementData>(ed => ed.CurrentHitPoints);
        }

        private void UpdateMaxHitPoints() {
            MaxHitPoints = ElementsData.Sum<AElementData>(ed => ed.MaxHitPoints);
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
            UpdateStrength();
        }

        private void OnElementCurrentHitPointsChanged() {
            UpdateCurrentHitPoints();
        }

        private void OnElementMaxHitPointsChanged() {
            UpdateMaxHitPoints();
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

