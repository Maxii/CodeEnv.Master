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
    protected bool IsOperational {
        get { return _collider.enabled; }
        set {
            if (_collider.enabled != value) {
                _collider.enabled = value;
                OnIsOperationalChanged();
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
        protected set { SetProperty<float>(ref _rangeDistance, value, "RangeDistance", OnRangeDistanceChanged); }
    }

    private AItem _parentItem;
    public AItem ParentItem {
        get { return _parentItem; }
        set {
            D.Assert(_parentItem == null);   // should only happen once
            SetProperty<AItem>(ref _parentItem, value, "ParentItem", OnParentItemChanged);
        }
    }

    public Player Owner { get { return ParentItem.Owner; } }

    /// <summary>
    /// Flag indicating whether a Kinematic Rigidbody is required by the monitor to keep any
    /// parent rigidbody from forming a compound collider.
    protected virtual bool IsKinematicRigidbodyReqd { get { return true; } }

    /// <summary>
    /// Flag indicating whether the collider used should be a trigger or normal collider.
    /// </summary>
    protected virtual bool IsTriggerCollider { get { return true; } }

    protected SphereCollider _collider;

    protected sealed override void Awake() {
        base.Awake();
        InitializeValuesAndReferences();
        IsOperational = false;
    }

    protected virtual void InitializeValuesAndReferences() {
        if (IsKinematicRigidbodyReqd) {
            var rigidbody = UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
            rigidbody.isKinematic = true;
        }
        _collider = UnityUtility.ValidateComponentPresence<SphereCollider>(gameObject);
        _collider.isTrigger = IsTriggerCollider;
        _collider.radius = Constants.ZeroF;
        _collider.enabled = false;
    }

    protected abstract void OnIsOperationalChanged();

    protected virtual void OnRangeDistanceChanged() {
        D.Log("{0} had its RangeDistance changed to {1:0.}.", Name, RangeDistance);
        _collider.radius = RangeDistance;
    }

    protected virtual void OnParentItemChanged() {
        ParentItem.onOwnerChanging += OnParentOwnerChanging;
        ParentItem.onOwnerChanged += OnParentOwnerChanged;
    }

    protected virtual void OnParentOwnerChanging(IItem parentItem, Player newOwner) { }

    protected virtual void OnParentOwnerChanged(IItem parentItem) { }

    /// <summary>
    /// Resets this Monitor in preparation for reuse by the same Parent.
    /// </summary>
    protected virtual void ResetForReuse() {
        D.Log("{0} is being reset for potential reuse.", Name);
        IsOperational = false;
        RangeDistance = Constants.ZeroF;
        D.Assert(ParentItem != null);
    }

    protected override void Cleanup() {
        if (ParentItem != null) {
            ParentItem.onOwnerChanging -= OnParentOwnerChanging;
            ParentItem.onOwnerChanged -= OnParentOwnerChanged;
        }
    }

}

