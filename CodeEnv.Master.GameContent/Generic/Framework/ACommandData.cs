// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ACommandData.cs
// Abstract generic base class for data associated with Command Items that have an ElementType under command.
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
    /// Abstract generic base class for data associated with Command Items that have an ElementType under command.
    /// </summary>
    /// <typeparam name="ElementCategoryType">The Type that defines the possible sub-categories of an Element, eg. a ShipItem can be sub-categorized as a Frigate which is defined within the ShipCategory Type.</typeparam>
    /// <typeparam name="ElementDataType">The type of Data associated with the ElementType used under this Command.</typeparam>
    /// <typeparam name="CommandCompositionType">The Type of the Composition component of this Command.</typeparam>
    public abstract class ACommandData<ElementCategoryType, ElementDataType, CommandCompositionType> : AMortalData, IDisposable
        where ElementCategoryType : struct
        where ElementDataType : AElementData<ElementCategoryType>
        where CommandCompositionType : AComposition<ElementCategoryType, ElementDataType> {

        private ElementDataType _hqElementData;
        public ElementDataType HQElementData {
            get {
                return _hqElementData;
            }
            set {
                SetProperty<ElementDataType>(ref _hqElementData, value, "HQElementData", OnHQElementDataChanged);
            }
        }

        private void OnHQElementDataChanged() {
            if (!_elementsData.Contains(_hqElementData)) {
                D.Error("HQ Element {0} assigned not present in Command {1}.", _hqElementData.OptionalParentName, OptionalParentName);
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

        private CommandCompositionType _composition;
        public CommandCompositionType Composition {
            get { return _composition; }
            private set { SetProperty<CommandCompositionType>(ref _composition, value, "Composition"); }
        }

        private IPlayer _owner;
        public IPlayer Owner {
            get { return _owner; }
            set { SetProperty<IPlayer>(ref _owner, value, "Owner", OnOwnerChanged); }
        }

        protected IList<ElementDataType> _elementsData;
        protected IDictionary<ElementDataType, IList<IDisposable>> _subscribers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ACommandData{ElementCategoryType, ElementDataType, CommandCompositionType}"/> class.
        /// </summary>
        /// <param name="cmdParentName">Name of the parent of this Command, eg. the FleetName for a FleetCommand.</param>
        public ACommandData(string cmdParentName)
            : base(cmdParentName + Constants.Space + CommonTerms.Command, maxHitPoints: Constants.ZeroF, optionalParentName: cmdParentName) {
            // A command's maxHitPoints are constructed from the sum of the elements
            InitializeCollections();
        }

        private void InitializeCollections() {
            _elementsData = new List<ElementDataType>();
            Composition = Activator.CreateInstance<CommandCompositionType>();
            _subscribers = new Dictionary<ElementDataType, IList<IDisposable>>();
        }

        private void OnOwnerChanged() {
            // UNDONE change all element owners?
            D.Log("{0} Owner has changed to {1}.", OptionalParentName, Owner.LeaderName);
        }

        public void AddElement(ElementDataType elementData) {
            if (!_elementsData.Contains(elementData)) {
                ValidateOwner(elementData.Owner);
                UpdateElementParentName(elementData);
                _elementsData.Add(elementData);

                ChangeComposition(elementData, toAdd: true);
                Subscribe(elementData);
                UpdatePropertiesDerivedFromCombinedElements();
                return;
            }
            D.Warn("Attempting to add {0} {1} that is already present.", typeof(ElementDataType), elementData.OptionalParentName);
        }

        private void ValidateOwner(IPlayer owner) {
            if (Owner == null) {
                // first setting of owner by first Element added. Not broadcast
                _owner = owner;
            }
            D.Assert(Owner == owner, "Owners {0} and {1} are different.".Inject(Owner.LeaderName, owner.LeaderName));
        }

        private void UpdateElementParentName(ElementDataType elementData) {
            // TODO something more than just assigning a parent name?
            elementData.OptionalParentName = OptionalParentName;
        }

        /// <summary>
        /// Adds or removes Element Data from the Composition.
        /// </summary>
        /// <param name="elementData">The data.</param>
        /// <param name="toAdd">if set to <c>true</c> add the element, otherwise remove it.</param>
        private void ChangeComposition(ElementDataType elementData, bool toAdd) {
            bool isChanged = false;
            if (toAdd) {
                isChanged = Composition.Add(elementData);
            }
            else {
                isChanged = Composition.Remove(elementData);
            }

            if (isChanged) {
                Composition = Activator.CreateInstance(typeof(CommandCompositionType), Composition) as CommandCompositionType;
            }
        }

        public bool RemoveElement(ElementDataType elementData) {
            if (_elementsData.Contains(elementData)) {
                bool isRemoved = _elementsData.Remove(elementData);

                ChangeComposition(elementData, toAdd: false);
                Unsubscribe(elementData);
                UpdatePropertiesDerivedFromCombinedElements();
                return isRemoved;
            }
            D.Warn("Attempting to remove {0} {1} that is not present.", typeof(ElementDataType), elementData.OptionalParentName);
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
            foreach (var eData in _elementsData) {
                sum.AddToTotal(eData.Strength);
            }
            Strength = sum;
        }

        private void UpdateCurrentHitPoints() {
            CurrentHitPoints = _elementsData.Sum<ElementDataType>(ed => ed.CurrentHitPoints);
        }

        private void UpdateMaxHitPoints() {
            MaxHitPoints = _elementsData.Sum<ElementDataType>(ed => ed.MaxHitPoints);
        }

        #region ElementData PropertyChanged Subscription and Methods

        protected virtual void Subscribe(ElementDataType elementData) {
            _subscribers.Add(elementData, new List<IDisposable>());
            IList<IDisposable> anElementsSubscriptions = _subscribers[elementData];
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<ElementDataType, float>(ed => ed.CurrentHitPoints, OnElementCurrentHitPointsChanged));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<ElementDataType, float>(ed => ed.MaxHitPoints, OnElementMaxHitPointsChanged));
            anElementsSubscriptions.Add(elementData.SubscribeToPropertyChanged<ElementDataType, CombatStrength>(ed => ed.Strength, OnElementStrengthChanged));
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

        private void Unsubscribe(ElementDataType elementData) {
            _subscribers[elementData].ForAll<IDisposable>(d => d.Dispose());
            _subscribers.Remove(elementData);
        }

        #endregion

        private void Cleanup() {
            Unsubscribe();
        }

        private void Unsubscribe() {
            IList<ElementDataType> subscriberKeys = new List<ElementDataType>(_subscribers.Keys);
            // copy of key list as you can't remove keys from a list while you are iterating over the list
            foreach (ElementDataType eData in subscriberKeys) {
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

