﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AColliderMonitor.cs
// Abstract base class for a spherical collider GameObject whose parent is an AItem. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Abstract base class for a spherical collider GameObject whose parent is an AItem. 
/// Called a Monitor as it generally is used to take action on objects that enter/exit the collider.
/// </summary>
public abstract class AColliderMonitor : AMonoBase, IColliderMonitor {

    private const string DebugNameFormat = "{0}.{1}";

    /// <summary>
    /// Control for enabling/disabling the monitor's collider.
    /// <remarks>Property not subscribable.</remarks>
    /// <remarks> Warning: When collider becomes disabled, OnTriggerExit is NOT called for items inside trigger.</remarks>
    /// </summary>
    public bool IsOperational {
        get { return _collider.enabled; }
        set {
            if (_collider.enabled != value) {
                _collider.enabled = value;
                IsOperationalPropChangedHandler();
            }
        }
    }

    public virtual string DebugName {
        get { return DebugNameFormat.Inject(ParentItem != null ? ParentItem.DebugName : transform.name, GetType().Name); }
    }

    private float _rangeDistance;
    /// <summary>
    /// The range in units of this Monitor, aka the radius of the spherical collider.
    /// </summary>
    public float RangeDistance {
        get { return _rangeDistance; }
        protected set { SetProperty<float>(ref _rangeDistance, value, "RangeDistance", RangeDistancePropChangedHandler); }
    }

    private IOwnerItem _parentItem;
    public IOwnerItem ParentItem {
        get { return _parentItem; }
        set {
            D.AssertNull(_parentItem);   // should only happen once
            SetProperty<IOwnerItem>(ref _parentItem, value, "ParentItem", ParentItemPropSetHandler);
        }
    }

    public Player Owner { get { return ParentItem.Owner; } }

    public bool ShowDebugLog { get { return ParentItem != null ? ParentItem.ShowDebugLog : true; } }

    /// <summary>
    /// Flag indicating whether a Kinematic Rigidbody is required by the monitor. 
    /// <remarks>A Kinematic Rigidbody is required if the collider is a regular collider and its parent has a regular collider. 
    /// This would form a compound collider which is not what is desired in a Monitor. It can also be required if the monitor
    /// is a trigger collider and the monitor must detect static colliders like Planetoids, Stars and UCenter, as trigger
    /// events aren't generated between 2 static colliders.
    /// </remarks>
    /// </summary>
    protected abstract bool IsKinematicRigidbodyReqd { get; }

    /// <summary>
    /// Flag indicating whether the collider used should be a trigger or normal collider.
    /// </summary>
    protected abstract bool IsTriggerCollider { get; }

    protected bool _isResetting;
    protected SphereCollider _collider;
    protected IGameManager _gameMgr;

    private IList<IDisposable> _subscriptons;

    protected sealed override void Awake() {
        base.Awake();
        InitializeValuesAndReferences();
        Subscribe();
        IsOperational = false;
    }

    protected virtual void InitializeValuesAndReferences() {
        _gameMgr = GameReferences.GameManager;
        if (IsKinematicRigidbodyReqd) {
            var rigidbody = UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
        }
        else {

            Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
            var rigidbody = gameObject.GetComponent<Rigidbody>();
            Profiler.EndSample();

            if (rigidbody != null) {
                D.Warn("{0} has a rigidbody it doesn't need.", DebugName);
            }
        }
        _collider = UnityUtility.ValidateComponentPresence<SphereCollider>(gameObject);
        _collider.isTrigger = IsTriggerCollider;
        _collider.radius = Constants.ZeroF;
        _collider.enabled = false;
    }

    private void Subscribe() {
        _subscriptons = new List<IDisposable>();
        _subscriptons.Add(_gameMgr.SubscribeToPropertyChanged<IGameManager, bool>(gm => gm.IsPaused, IsPausedPropChangedHandler));
    }

    #region Event and Property Change Handlers

    private void IsOperationalPropChangedHandler() {
        HandleIsOperationalChanged();
    }

    private void IsPausedPropChangedHandler() {
        HandleIsPausedChanged();
    }

    private void RangeDistancePropChangedHandler() {
        HandleRangeDistanceChanged();
    }

    private void ParentItemPropSetHandler() {
        HandleParentItemSet();
    }

    private void ParentOwnerChangingEventHandler(object sender, OwnerChangingEventArgs e) {
        HandleParentItemOwnerChanging(e.IncomingOwner);
    }

    private void ParentOwnerChangedEventHandler(object sender, EventArgs e) {
        HandleParentItemOwnerChanged();
    }

    #endregion

    protected virtual void HandleParentItemSet() {
        ParentItem.ownerChanging += ParentOwnerChangingEventHandler;
        ParentItem.ownerChanged += ParentOwnerChangedEventHandler;
    }

    protected virtual void HandleParentItemOwnerChanging(Player incomingOwner) { }
    protected virtual void HandleParentItemOwnerChanged() { }
    protected abstract void HandleIsOperationalChanged();
    protected virtual void HandleIsPausedChanged() { }
    protected virtual void HandleRangeDistanceChanged() {
        //D.Log(ShowDebugLog, "{0} had its RangeDistance changed from {1:0.#} to {2:0.#}.", DebugName, _collider.radius, RangeDistance);
        _collider.radius = RangeDistance;
    }

    /// <summary>
    /// Resets this Monitor in preparation for reuse by the same Parent.
    /// <remarks>_isResetting will be true while reseting.</remarks>
    /// </summary>
    protected void ResetForReuse() {
        _isResetting = true;
        IsOperational = false;
        RangeDistance = Constants.ZeroF;
        D.AssertNotNull(ParentItem);
        CompleteResetForReuse();
        _isResetting = false;
    }

    /// <summary>
    /// Hook that allows derived classes to reset for reuse.
    /// </summary>
    protected virtual void CompleteResetForReuse() { }

    protected override void Cleanup() {
        Unsubscribe();
    }

    protected virtual void Unsubscribe() {
        _subscriptons.ForAll(s => s.Dispose());
        _subscriptons.Clear();
        if (ParentItem != null) {
            ParentItem.ownerChanging -= ParentOwnerChangingEventHandler;
            ParentItem.ownerChanged -= ParentOwnerChangedEventHandler;
        }
    }

    public sealed override string ToString() {
        return DebugName;
    }

}

