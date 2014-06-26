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

    public new APlanetoidData Data {
        get { return base.Data as APlanetoidData; }
        set { base.Data = value; }
    }

    protected override void InitializeRadiiComponents() {
        var meshRenderers = gameObject.GetComponentsInImmediateChildren<Renderer>();    // some planetoids have an atmosphere
        Radius = meshRenderers.First().bounds.size.x / 2F;    // half of the (length, width or height, all the same surrounding a sphere)
        (collider as SphereCollider).radius = Radius;
    }

    protected override void Initialize() {
        base.Initialize();
        CurrentState = PlanetoidState.None;
        //__CheckForOrbitingBodiesInsideOrbitDistance();
    }

    public void CommenceOperations() {
        CurrentState = PlanetoidState.Idling;
    }

    //[System.Diagnostics.Conditional("DEBUG_LOG")]
    //private void __CheckForOrbitingBodiesInsideOrbitDistance() {
    //    var moons = gameObject.GetComponentsInChildren<PlanetoidModel>().Except(this);
    //    if (!moons.IsNullOrEmpty()) {
    //        var moonsInsideKeepoutZoneRadius = moons.Where(moon => moon.transform.localPosition.magnitude + moon.Radius <= MaximumShipOrbitDistance);
    //        if (!moonsInsideKeepoutZoneRadius.IsNullOrEmpty()) {
    //            moonsInsideKeepoutZoneRadius.ForAll(moon => {
    //                D.Warn("{0} is inside {1}'s OrbitDistance of {2}.", moon.FullName, FullName, MaximumShipOrbitDistance);
    //            });
    //        }
    //    }
    //}

    protected override void OnDataChanged() {
        base.OnDataChanged();
        SetKeepoutZoneRadius();
    }

    private void SetKeepoutZoneRadius() {
        SphereCollider keepoutZoneCollider = gameObject.GetComponentInImmediateChildren<SphereCollider>();
        D.Assert(keepoutZoneCollider.gameObject.layer == (int)Layers.CelestialObjectKeepout);
        keepoutZoneCollider.radius = Data.ShipOrbitSlot.InnerRadius;
    }

    #region StateMachine - Simple Alternative

    private PlanetoidState _currentState;
    public PlanetoidState CurrentState {
        get { return _currentState; }
        private set { SetProperty<PlanetoidState>(ref _currentState, value, "CurrentState", OnCurrentStateChanged, OnCurrentStateChanging); }
    }

    private void OnCurrentStateChanging(PlanetoidState newState) {
        PlanetoidState previousState = CurrentState;
        //D.Log("{0}.CurrentState changing from {1} to {2}.", Data.Name, previousState.GetName(), newState.GetName());
        switch (previousState) {
            case PlanetoidState.None:
                IsOperational = true;
                break;
            case PlanetoidState.Idling:
                break;
            case PlanetoidState.Dead:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(previousState));
        }
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
                new Job(DelayedDestroy(3), toStart: true, onJobComplete: (wasKilled) => {
                    D.Log("{0} has been destroyed.", FullName);
                });
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

    public OrbitalSlot ShipOrbitSlot { get { return Data.ShipOrbitSlot; } }

    public void AssumeOrbit(IShipModel ship) {
        IOrbiterForShips orbiter;
        var orbiterTransform = _transform.GetTransformWithInterfaceInImmediateChildren<IOrbiterForShips>(out orbiter);
        if (orbiterTransform != null) {
            References.UnitFactory.AttachShipToOrbiter(ship, ref orbiterTransform);
        }
        else {
            References.UnitFactory.AttachShipToOrbiter(gameObject, ship, orbitedObjectIsMobile: true);
        }
    }

    public void LeaveOrbit(IShipModel orbitingShip) {
        IOrbiterForShips orbiter;
        var orbiterTransform = _transform.GetTransformWithInterfaceInImmediateChildren<IOrbiterForShips>(out orbiter);
        D.Assert(orbiterTransform != null, "{0}.{1} is not present.".Inject(FullName, typeof(IOrbiterForShips).Name));
        var ship = orbiterTransform.gameObject.GetSafeInterfacesInChildren<IShipModel>().Single(s => s == orbitingShip);
        var parentFleetTransform = ship.Command.Transform.parent;
        ship.Transform.parent = parentFleetTransform;
        // OPTIMIZE disable or remove empty orbiters?
    }

    #endregion

}

