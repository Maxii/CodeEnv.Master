// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseData.cs
// All the data associated with a Starbase.
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
    /// All the data associated with a Starbase.
    /// </summary>
    public class StarbaseData : ACommandData<FacilityCategory, FacilityData, StarbaseComposition> {
        //public class StarbaseData : AMortalData, IDisposable {

        public StarbaseCategory Category { get; set; }

        //private FacilityData _hqItemData;
        //public FacilityData HQItemData {
        //    get {
        //        return _hqItemData;
        //    }
        //    set {
        //        SetProperty<FacilityData>(ref _hqItemData, value, "HQItemData", OnHQItemDataChanged);
        //    }
        //}

        //private CombatStrength _strength;
        //public CombatStrength Strength {
        //    get {
        //        return _strength;
        //    }
        //    private set {
        //        SetProperty<CombatStrength>(ref _strength, value, "Strength");
        //    }
        //}

        //private StarbaseComposition _composition;
        //public StarbaseComposition Composition {
        //    get { return _composition; }
        //    private set { SetProperty<StarbaseComposition>(ref _composition, value, "Composition"); }
        //}

        //private IPlayer _owner;
        //public IPlayer Owner {
        //    get { return _owner; }
        //    set { SetProperty<IPlayer>(ref _owner, value, "Owner", OnOwnerChanged); }
        //}

        //private IList<FacilityData> _itemsData;
        //private IDictionary<FacilityData, IList<IDisposable>> _subscribers;

        ///// <summary>
        ///// Initializes a new instance of the <see cref="StarbaseData"/> class.
        ///// </summary>
        ///// <param name="starbaseName">Name of the starbase.</param>
        //public StarbaseData(string starbaseName)
        //    : base(starbaseName + Constants.Space + CommonTerms.Command, maxHitPoints: Constants.ZeroF, optionalParentName: starbaseName) {
        //    // MaxHitPoints are constructed from the sum of the items in the composition
        //    InitializeCollections();
        //}

        public StarbaseData(string starbaseName) : base(starbaseName) { }

        //private void InitializeCollections() {
        //    _itemsData = new List<FacilityData>();
        //    Composition = new StarbaseComposition();
        //    _subscribers = new Dictionary<FacilityData, IList<IDisposable>>();
        //}

        //private void OnOwnerChanged() {
        //    // UNDONE change all facility owners?
        //    D.Log("{0} Owner has changed to {1}.", Name, Owner.LeaderName);
        //}

        //private void OnHQItemDataChanged() {
        //    if (!_itemsData.Contains(_hqItemData)) {
        //        D.Error("HQ {0} assigned not contained in {1}.", _hqItemData.OptionalParentName, OptionalParentName);
        //    }
        //}

        //public void Add(FacilityData itemData) {
        //    if (!_itemsData.Contains(itemData)) {
        //        ValidateOwner(itemData.Owner);
        //        AssignParentName(itemData);
        //        _itemsData.Add(itemData);

        //        ChangeComposition(itemData, toAdd: true);
        //        Subscribe(itemData);
        //        UpdatePropertiesDerivedFromTotalCompositionItems();
        //        return;
        //    }
        //    D.Warn("Attempting to add {0} {1} that is already present.", typeof(FacilityData), itemData.OptionalParentName);
        //}

        //private void ValidateOwner(IPlayer owner) {
        //    if (Owner == null) {
        //        // first setting of owner by first ship added. Not broadcast
        //        _owner = owner;
        //    }
        //    D.Assert(Owner == owner, "Owners {0} and {1} are different.".Inject(Owner.LeaderName, owner.LeaderName));
        //}

        //private void AssignParentName(FacilityData itemData) {
        //    itemData.OptionalParentName = Name;
        //}

        ///// <summary>
        ///// Adds or removes <see cref="FacilityData" /> from the Composition.
        ///// </summary>
        ///// <param name="itemData">The data for the Facility.</param>
        ///// <param name="toAdd">if set to <c>true</c> add the data, otherwise remove it.</param>
        //private void ChangeComposition(FacilityData itemData, bool toAdd) {
        //    bool isChanged = false;
        //    if (toAdd) {
        //        isChanged = Composition.Add(itemData);
        //    }
        //    else {
        //        isChanged = Composition.Remove(itemData);
        //    }

        //    if (isChanged) {
        //        Composition = new StarbaseComposition(Composition);
        //    }
        //}

        //public bool Remove(FacilityData itemData) {
        //    if (_itemsData.Contains(itemData)) {
        //        bool isRemoved = _itemsData.Remove(itemData);

        //        ChangeComposition(itemData, toAdd: false);
        //        Unsubscribe(itemData);
        //        UpdatePropertiesDerivedFromTotalCompositionItems();
        //        return isRemoved;
        //    }
        //    D.Warn("Attempting to remove {0} {1} that is not present.", typeof(FacilityData), itemData.OptionalParentName);
        //    return false;
        //}

        ///// <summary>
        ///// Recalculates any properties that are dependant upon the total population
        ///// of the composition.
        ///// </summary>
        //private void UpdatePropertiesDerivedFromTotalCompositionItems() {
        //    UpdateStrength();
        //    UpdateMaxHitPoints();   // must preceed current as current uses max as a clamp
        //    UpdateCurrentHitPoints();
        //}

        //private void UpdateStrength() {
        //    CombatStrength sum = new CombatStrength();
        //    foreach (var data in _itemsData) {
        //        sum.AddToTotal(data.Strength);
        //    }
        //    Strength = sum;
        //}

        //private void UpdateCurrentHitPoints() {
        //    CurrentHitPoints = _itemsData.Sum<FacilityData>(data => data.CurrentHitPoints);
        //}

        //private void UpdateMaxHitPoints() {
        //    MaxHitPoints = _itemsData.Sum<FacilityData>(data => data.MaxHitPoints);
        //}

        //#region Data PropertyChanged Subscription and Methods

        //private void Subscribe(FacilityData itemData) {
        //    _subscribers.Add(itemData, new List<IDisposable>());
        //    IList<IDisposable> shipSubscriptions = _subscribers[itemData];
        //    shipSubscriptions.Add(itemData.SubscribeToPropertyChanged<FacilityData, float>(sd => sd.CurrentHitPoints, OnItemCurrentHitPointsChanged));
        //    shipSubscriptions.Add(itemData.SubscribeToPropertyChanged<FacilityData, float>(sd => sd.MaxHitPoints, OnItemMaxHitPointsChanged));
        //    shipSubscriptions.Add(itemData.SubscribeToPropertyChanged<FacilityData, CombatStrength>(sd => sd.Strength, OnItemStrengthChanged));
        //}

        //private void OnItemStrengthChanged() {
        //    UpdateStrength();
        //}

        //private void OnItemCurrentHitPointsChanged() {
        //    UpdateCurrentHitPoints();
        //}

        //private void OnItemMaxHitPointsChanged() {
        //    UpdateMaxHitPoints();
        //}

        //private void Unsubscribe(FacilityData itemData) {
        //    _subscribers[itemData].ForAll<IDisposable>(d => d.Dispose());
        //    _subscribers.Remove(itemData);
        //}

        //#endregion

        //private void Cleanup() {
        //    Unsubscribe();
        //}

        //private void Unsubscribe() {
        //    IList<FacilityData> subscriberKeys = new List<FacilityData>(_subscribers.Keys);
        //    // copy of key list as you can't remove keys from a list while you are iterating over the list
        //    foreach (FacilityData shipData in subscriberKeys) {
        //        Unsubscribe(shipData);
        //    }
        //    _subscribers.Clear();
        //}

        //public override string ToString() {
        //    return new ObjectAnalyzer().ToString(this);
        //}

        //#region IDisposable
        //[DoNotSerialize]
        //private bool alreadyDisposed = false;

        ///// <summary>
        ///// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        ///// </summary>
        //public void Dispose() {
        //    Dispose(true);
        //    GC.SuppressFinalize(this);
        //}

        ///// <summary>
        ///// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
        ///// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
        ///// </summary>
        ///// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        //protected virtual void Dispose(bool isDisposing) {
        //    // Allows Dispose(isDisposing) to be called more than once
        //    if (alreadyDisposed) {
        //        return;
        //    }

        //    if (isDisposing) {
        //        // free managed resources here including unhooking events
        //        Cleanup();
        //    }
        //    // free unmanaged resources here

        //    alreadyDisposed = true;
        //}

        //// Example method showing check for whether the object has been disposed
        ////public void ExampleMethod() {
        ////    // throw Exception if called on object that is already disposed
        ////    if(alreadyDisposed) {
        ////        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
        ////    }

        ////    // method content here
        ////}
        //#endregion

    }
}

