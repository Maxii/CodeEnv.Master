// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitBaseCommandItem.cs
// Abstract base class for Base (Starbase and Settlement) Command Items.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
///  Abstract base class for Base (Starbase and Settlement) Command Items.
/// </summary>
public abstract class AUnitBaseCommandItem : AUnitCommandItem, IShipOrbitable {

    private BaseOrder _currentOrder;
    public BaseOrder CurrentOrder {
        get { return _currentOrder; }
        set { SetProperty<BaseOrder>(ref _currentOrder, value, "CurrentOrder", OnCurrentOrderChanged); }
    }

    private ICtxControl _ctxControl;

    #region Initialization

    protected override void InitializeLocalReferencesAndValues() {
        base.InitializeLocalReferencesAndValues();
        // the radius of a BaseCommand is fixed to include all of its elements
        Radius = TempGameValues.BaseRadius;
        InitializeShipOrbitSlot();
        InitializeKeepoutZone();
    }

    private void InitializeShipOrbitSlot() {
        float innerOrbitRadius = Radius * TempGameValues.KeepoutRadiusMultiplier;
        float outerOrbitRadius = innerOrbitRadius + TempGameValues.DefaultShipOrbitSlotDepth;
        ShipOrbitSlot = new ShipOrbitSlot(innerOrbitRadius, outerOrbitRadius, this);
    }

    private void InitializeKeepoutZone() {
        SphereCollider keepoutZoneCollider = gameObject.GetComponentsInImmediateChildren<SphereCollider>().Where(c => c.isTrigger).Single();
        D.Assert(keepoutZoneCollider.gameObject.layer == (int)Layers.CelestialObjectKeepout);
        keepoutZoneCollider.isTrigger = true;
        keepoutZoneCollider.radius = ShipOrbitSlot.InnerRadius;
    }

    protected override void InitializeViewMembersOnDiscernible() {
        base.InitializeViewMembersOnDiscernible();
        InitializeContextMenu(Owner);
        // revolvers control their own enabled state
    }

    private void InitializeContextMenu(IPlayer owner) {
        D.Assert(owner != TempGameValues.NoPlayer);
        if (_ctxControl != null) {
            (_ctxControl as IDisposable).Dispose();
        }
        _ctxControl = owner.IsPlayer ? new BaseCtxControl_Player(this) as ICtxControl : new BaseCtxControl_AI(this);
        //D.Log("{0} initializing {1}.", FullName, _ctxControl.GetType().Name);
    }

    #endregion

    #region Model Methods

    public override void AddElement(AUnitElementItem element) {
        base.AddElement(element);
        element.Command = this;
        if (HQElement != null) {
            _formationGenerator.RegenerateFormation();    // Bases simply regenerate the formation when adding an element
        }

        // A facility that is in Idle without being part of a unit might attempt something it is not yet prepared for
        FacilityItem facility = element as FacilityItem;
        D.Assert(facility.CurrentState != FacilityState.Idling, "{0} is adding {1} while Idling.".Inject(FullName, facility.FullName));
    }

    protected abstract void OnCurrentOrderChanged();

    protected override void OnOwnerChanging(IPlayer newOwner) {
        base.OnOwnerChanging(newOwner);
        if (_isViewMembersOnDiscernibleInitialized) {
            // _ctxControl has already been initialized
            if (Owner == TempGameValues.NoPlayer || newOwner == TempGameValues.NoPlayer || Owner.IsPlayer != newOwner.IsPlayer) {
                // Kind of owner has changed between AI and Player so generate a new ctxControl
                InitializeContextMenu(newOwner);
            }
        }
    }

    /// <summary>
    /// Kills all remaining elements of the Unit along with this Command. All Elements are ordered 
    /// to SelfDestruct (assume Dead state) which results in the Command assuming its own Dead state.
    /// </summary>
    protected void KillUnit() {
        var elementSelfDestructOrder = new FacilityOrder(FacilityDirective.SelfDestruct, OrderSource.UnitCommand);
        Elements.ForAll(e => (e as FacilityItem).CurrentOrder = elementSelfDestructOrder);
    }


    #endregion

    #region View Methods
    #endregion

    #region Mouse Events

    protected override void OnRightPress(bool isDown) {
        base.OnRightPress(isDown);
        if (!isDown && !GameInput.Instance.IsDragging) {
            // right press release while not dragging means both press and release were over this object
            _ctxControl.OnRightPressRelease();
        }
    }

    #endregion

    #region IShipOrbitable Members

    public ShipOrbitSlot ShipOrbitSlot { get; private set; }

    #endregion

}

