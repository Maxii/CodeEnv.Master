// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: APlanetoidModel.cs
// Abstract base class for Planet and Moon Models.  
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for Planet and Moon Models.  
/// </summary>
public abstract class APlanetoidModel : AMortalItemModel, IShipOrbitable {

    public PlanetoidCategory category;

    public new APlanetoidData Data {
        get { return base.Data as APlanetoidData; }
        set { base.Data = value; }
    }

    protected override void InitializeRadiiComponents() {
        var meshRenderers = gameObject.GetComponentsInImmediateChildren<Renderer>();    // some planetoids have an atmosphere
        Radius = meshRenderers.First().bounds.size.x / 2F;    // half of the (length, width or height, all the same surrounding a sphere)
        (collider as SphereCollider).radius = Radius;
        collider.isTrigger = false;

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

    protected override void Initialize() {
        D.Assert(category == Data.Category);
        CurrentState = PlanetoidState.None;
    }

    public override void CommenceOperations() {
        base.CommenceOperations();
        CurrentState = PlanetoidState.Idling;
    }

    protected override void OnDeath() {
        base.OnDeath();
        collider.enabled = false;
        DisableParentOrbiter();
    }

    private void DisableParentOrbiter() {
        _transform.parent.GetInterface<IOrbiter>().enabled = false;
    }

    #region StateMachine - Simple Alternative

    private PlanetoidState _currentState;
    public PlanetoidState CurrentState {
        get { return _currentState; }
        protected set { SetProperty<PlanetoidState>(ref _currentState, value, "CurrentState", OnCurrentStateChanged); }
    }

    private void OnCurrentStateChanged() {
        //D.Log("{0}.CurrentState changed to {1}.", Data.Name, CurrentState.GetName());
        switch (CurrentState) {
            case PlanetoidState.Idling:
                break;
            case PlanetoidState.Dead:
                OnDeath();
                OnShowAnimation(MortalAnimations.Dying);
                break;
            case PlanetoidState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(CurrentState));
        }
    }

    public override void OnShowCompletion() {
        switch (CurrentState) {
            case PlanetoidState.Dead:
                DestroyMortalItem(3F);
                break;
            case PlanetoidState.Idling:
                // do nothing
                break;
            case PlanetoidState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(CurrentState));
        }
    }

    #endregion

    #region IMortalTarget Members

    public override void TakeHit(CombatStrength attackerWeaponStrength) {
        if (CurrentState == PlanetoidState.Dead) {
            return;
        }
        LogEvent();
        float damage = Data.Strength - attackerWeaponStrength;
        if (damage == Constants.ZeroF) {
            return;
        }
        bool isAlive = ApplyDamage(damage);
        if (!isAlive) {
            CurrentState = PlanetoidState.Dead;
            return;
        }
        OnShowAnimation(MortalAnimations.Hit);
    }

    #endregion

    #region IDestinationTarget Members

    public override bool IsMobile { get { return true; } }

    #endregion

    #region IShipOrbitable Members

    public ShipOrbitSlot ShipOrbitSlot { get; private set; }

    #endregion

}

