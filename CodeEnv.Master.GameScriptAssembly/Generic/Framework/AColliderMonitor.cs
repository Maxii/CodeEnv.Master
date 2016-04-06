// --------------------------------------------------------------------------------------------------------------------
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

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for a spherical collider GameObject whose parent is an AItem. 
/// Called a Monitor as it generally is used to take action wrt objects that enter/exit the collider.
/// </summary>
public abstract class AColliderMonitor : AMonoBase {

    /// <summary>
    /// Control for enabling/disabling the monitor's collider.
    /// Warning: When collider becomes disabled, OnTriggerExit is NOT called for items inside trigger.
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

    public virtual string Name { get { return transform.name; } }

    private float _rangeDistance;
    /// <summary>
    /// The range in units of this Monitor, aka the radius of the spherical collider.
    /// </summary>
    public float RangeDistance {
        get { return _rangeDistance; }
        protected set { SetProperty<float>(ref _rangeDistance, value, "RangeDistance", RangeDistancePropChangedHandler); }
    }

    private IItem _parentItem;
    public IItem ParentItem {
        get { return _parentItem; }
        set {
            D.Assert(_parentItem == null);   // should only happen once
            SetProperty<IItem>(ref _parentItem, value, "ParentItem", ParentItemPropSetHandler);
        }
    }

    public Player Owner { get { return ParentItem.Owner; } }

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

    protected bool ShowDebugLog { get { return ParentItem.ShowDebugLog; } }

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
        _gameMgr = References.GameManager;
        if (IsKinematicRigidbodyReqd) {
            var rigidbody = UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
        }
        else {
            var rigidbody = gameObject.GetComponent<Rigidbody>();
            D.Warn(rigidbody != null, "{0} has a rigidbody it doesn't need.", Name);
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

    protected abstract void IsOperationalPropChangedHandler();

    protected virtual void IsPausedPropChangedHandler() { }

    protected virtual void RangeDistancePropChangedHandler() {
        D.Log(ShowDebugLog, "{0} had its RangeDistance changed to {1:0.}.", Name, RangeDistance);
        _collider.radius = RangeDistance;
    }

    protected virtual void ParentItemPropSetHandler() {
        ParentItem.ownerChanging += ParentOwnerChangingEventHandler;
        ParentItem.ownerChanged += ParentOwnerChangedEventHandler;
    }

    protected virtual void ParentOwnerChangedEventHandler(object sender, EventArgs e) { }

    protected virtual void ParentOwnerChangingEventHandler(object sender, OwnerChangingEventArgs e) { }

    #endregion

    /// <summary>
    /// Resets this Monitor in preparation for reuse by the same Parent.
    /// </summary>
    protected virtual void ResetForReuse() {
        D.Log(ShowDebugLog, "{0} is being reset for potential reuse.", Name);
        IsOperational = false;
        RangeDistance = Constants.ZeroF;
        D.Assert(ParentItem != null);
    }

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

}

