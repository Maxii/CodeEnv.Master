// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetItem.cs
// APlanetoidItems that are Planets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using MoreLinq;
using UnityEngine;

/// <summary>
/// APlanetoidItems that are Planets.
/// </summary>
public class PlanetItem : APlanetoidItem, IPlanetItem, IShipCloseOrbitable, IShipExplorable {

    private MoonItem[] _childMoons;
    /// <summary>
    /// The moons that orbit this planet. Can be empty but never null.
    /// </summary>
    public MoonItem[] ChildMoons {
        get { return _childMoons; }
        private set { SetProperty<MoonItem[]>(ref _childMoons, value, "ChildMoons"); }
    }

    public new PlanetData Data {
        get { return base.Data as PlanetData; }
        set { base.Data = value; }
    }

    protected new PlanetDisplayManager DisplayMgr { get { return base.DisplayMgr as PlanetDisplayManager; } }

    private DetourGenerator _detourGenerator;
    private IList<IShipItem> _shipsInCloseOrbit;

    #region Initialization

    protected override void InitializeObstacleZone() {
        base.InitializeObstacleZone();
        _obstacleZoneCollider.radius = Data.CloseOrbitInnerRadius;
        InitializeObstacleDetourGenerator();
    }

    private void InitializeObstacleDetourGenerator() {
        if (IsMobile) {
            Reference<Vector3> obstacleZoneCenter = new Reference<Vector3>(() => _obstacleZoneCollider.transform.TransformPoint(_obstacleZoneCollider.center));
            _detourGenerator = new DetourGenerator(obstacleZoneCenter, ObstacleZoneRadius, Data.CloseOrbitOuterRadius);
        }
        else {
            Vector3 obstacleZoneCenter = _obstacleZoneCollider.transform.TransformPoint(_obstacleZoneCollider.center);
            _detourGenerator = new DetourGenerator(obstacleZoneCenter, ObstacleZoneRadius, Data.CloseOrbitOuterRadius);
        }
    }

    protected override ADisplayManager InitializeDisplayManager() {
        var dMgr = new PlanetDisplayManager(this, MakeIconInfo());
        SubscribeToIconEvents(dMgr.Icon);
        return dMgr;
    }

    private void SubscribeToIconEvents(IResponsiveTrackingSprite icon) {
        var iconEventListener = icon.EventListener;
        iconEventListener.onHover += HoverEventHandler;
        iconEventListener.onClick += ClickEventHandler;
        iconEventListener.onDoubleClick += DoubleClickEventHandler;
        iconEventListener.onPress += PressEventHandler;
    }

    //private ShipOrbitSlot InitializeShipOrbitSlot() {
    //    return new ShipOrbitSlot(Data.LowOrbitRadius, Data.HighOrbitRadius, this);
    //}

    protected override ICtxControl InitializeContextMenu(Player owner) {
        return new PlanetCtxControl(this);
    }

    protected override void FinalInitialize() {
        base.FinalInitialize();
        RecordAnyChildMoons();
    }

    private void RecordAnyChildMoons() {
        ChildMoons = gameObject.GetComponentsInChildren<MoonItem>();
        D.Log(ShowDebugLog && ChildMoons.Any(), "{0} recorded {1} child moons.", FullName, ChildMoons.Count());
    }

    #endregion

    internal void RemoveMoon(MoonItem moon) {
        D.Assert(!moon.IsOperational);
        D.Assert(ChildMoons.Contains(moon));
        ChildMoons = ChildMoons.Except(moon).ToArray();
    }

    private void AssessIcon() {
        if (DisplayMgr == null) { return; }

        var iconInfo = RefreshIconInfo();
        if (DisplayMgr.IconInfo != iconInfo) {    // avoid property not changed warning
            UnsubscribeToIconEvents(DisplayMgr.Icon);
            //D.Log(ShowDebugLog, "{0} changing IconInfo from {1} to {2}.", FullName, DisplayMgr.IconInfo, iconInfo);
            DisplayMgr.IconInfo = iconInfo;
            SubscribeToIconEvents(DisplayMgr.Icon);
        }
    }

    private IconInfo RefreshIconInfo() {
        return MakeIconInfo();
    }

    private IconInfo MakeIconInfo() {
        var report = GetUserReport();
        GameColor iconColor = report.Owner != null ? report.Owner.Color : GameColor.White;
        return new IconInfo("Icon02", AtlasID.Contextual, iconColor);
    }

    public override void HandleEffectFinished(EffectID effectID) {
        base.HandleEffectFinished(effectID);
        D.Log(ShowDebugLog, "{0}.HandleEffectFinished({1}) called.", FullName, effectID.GetValueName());
        switch (effectID) {
            case EffectID.Dying:
                if (ChildMoons.Any()) {
                    // planet has completed showing its death so moons need to show theirs too
                    ChildMoons.ForAll(moon => {
                        moon.effectFinished += MoonDeathEffectFinishedEventHandler; // no unsubscribing needed as all are destroyed
                        moon.HandlePlanetDying();
                    });
                }
                else {
                    DestroyMe();
                }
                break;
            case EffectID.Hit:
                break;
            case EffectID.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(effectID));
        }
    }

    private void HandleMoonDeathEffectFinished() {
        D.Log(ShowDebugLog, "{0}.HandleMoonDeathEffectFinished() called. Remaining Moons: {1}", FullName, ChildMoons.Count());
        if (ChildMoons.Count() == Constants.Zero) {
            // The last moon has shown its death effect as a result of the planet's death
            DestroyMe();
        }
    }

    #region Event and Property Change Handlers

    protected override void OwnerPropChangedHandler() {
        base.OwnerPropChangedHandler();
        AssessIcon();
    }

    private void MoonDeathEffectFinishedEventHandler(object sender, EffectEventArgs e) {
        D.Assert(e.EffectID == EffectID.Dying);
        HandleMoonDeathEffectFinished();
    }

    #endregion

    protected override void ConnectHighOrbitRigidbodyToShipOrbitJoint(FixedJoint shipOrbitJoint) {
        if (ChildMoons.Any()) {
            IMoonItem outermostMoon = ChildMoons.MaxBy(m => Vector3.SqrMagnitude(m.Position - Position));
            Rigidbody highOrbitRigidbody = outermostMoon.CelestialOrbitSimulator.OrbitRigidbody;
            shipOrbitJoint.connectedBody = highOrbitRigidbody;
        }
        else {
            base.ConnectHighOrbitRigidbodyToShipOrbitJoint(shipOrbitJoint);
        }
    }

    #region Cleanup

    protected override void Unsubscribe() {
        base.Unsubscribe();
        if (DisplayMgr != null) {
            var icon = DisplayMgr.Icon;
            if (icon != null) {
                UnsubscribeToIconEvents(icon);
            }
        }
    }

    private void UnsubscribeToIconEvents(IResponsiveTrackingSprite icon) {
        var iconEventListener = icon.EventListener;
        iconEventListener.onHover -= HoverEventHandler;
        iconEventListener.onClick -= ClickEventHandler;
        iconEventListener.onDoubleClick -= DoubleClickEventHandler;
        iconEventListener.onPress -= PressEventHandler;
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IShipCloseOrbitable Members

    private IShipCloseOrbitSimulator _closeOrbitSimulator;
    public IShipCloseOrbitSimulator CloseOrbitSimulator {
        get {
            if (_closeOrbitSimulator == null) {
                OrbitData closeOrbitData = new OrbitData(gameObject, Data.CloseOrbitInnerRadius, Data.CloseOrbitOuterRadius, IsMobile);
                _closeOrbitSimulator = GeneralFactory.Instance.MakeShipCloseOrbitSimulatorInstance(closeOrbitData);
            }
            return _closeOrbitSimulator;
        }
    }

    public void AssumeCloseOrbit(IShipItem ship, FixedJoint shipOrbitJoint) {
        if (_shipsInCloseOrbit == null) {
            _shipsInCloseOrbit = new List<IShipItem>();
        }
        shipOrbitJoint.connectedBody = CloseOrbitSimulator.OrbitRigidbody;
        _shipsInCloseOrbit.Add(ship);
    }

    public bool IsInCloseOrbit(IShipItem ship) {
        if (_shipsInCloseOrbit == null || !_shipsInCloseOrbit.Contains(ship)) {
            return false;
        }
        return true;
    }

    public bool IsCloseOrbitAllowedBy(Player player) {
        return !Owner.IsAtWarWith(player);
    }

    public IList<StationaryLocation> LocalAssemblyStations { get { return (ParentSystem as IGuardable).GuardStations; } }

    #endregion

    #region IShipOrbitable Members

    public override void HandleBrokeOrbit(IShipItem ship) {
        if (IsInCloseOrbit(ship)) {
            D.Assert(_closeOrbitSimulator != null);
            var isRemoved = _shipsInCloseOrbit.Remove(ship);
            D.Assert(isRemoved);
            D.Log("{0} has left close orbit around {1}.", ship.FullName, FullName);
            float shipDistance = Vector3.Distance(ship.Position, Position);
            float minOutsideOfOrbitCaptureRadius = Data.CloseOrbitOuterRadius - ship.CollisionDetectionZoneRadius;
            D.Warn(shipDistance > minOutsideOfOrbitCaptureRadius, "{0} is leaving orbit of {1} but is not within {2:0.0000}. Ship's current orbit distance is {3:0.0000}.",
                ship.FullName, FullName, minOutsideOfOrbitCaptureRadius, shipDistance);
            if (_shipsInCloseOrbit.Count == Constants.Zero) {
                // Choose either to deactivate the OrbitSimulator or destroy it, but not both
                CloseOrbitSimulator.IsActivated = false;
                //DestroyOrbitSimulator();
            }
        }
        else {
            base.HandleBrokeOrbit(ship);
        }
    }

    #endregion

    #region INavigableTarget Members

    public override float RadiusAroundTargetContainingKnownObstacles {
        get {
            float knownObstaclesRadius;
            if (ChildMoons.Any()) {
                MoonItem outerMoon = ChildMoons.MaxBy(moon => Vector3.SqrMagnitude(moon.Position - Position));
                float distanceToOuterMoon = Vector3.Distance(outerMoon.Position, Position);
                knownObstaclesRadius = distanceToOuterMoon + outerMoon.RadiusAroundTargetContainingKnownObstacles;
            }
            else {
                knownObstaclesRadius = base.RadiusAroundTargetContainingKnownObstacles;
            }
            return knownObstaclesRadius;
        }
    }

    public override float GetShipArrivalDistance(float shipCollisionDetectionRadius) {
        string arrivalDistanceMsg = "just outside of it's obstacle zone";
        float arrivalDistance;
        if (ChildMoons.Any()) {
            MoonItem outerMoon = ChildMoons.MaxBy(moon => Vector3.SqrMagnitude(moon.Position - Position));
            float distanceToOuterMoon = Vector3.Distance(outerMoon.Position, Position);
            arrivalDistance = distanceToOuterMoon + outerMoon.GetShipArrivalDistance(shipCollisionDetectionRadius);
            arrivalDistanceMsg = "just outside of outer moon {0}'s obstacle zone".Inject(outerMoon.FullName);
        }
        else {
            arrivalDistance = Data.CloseOrbitOuterRadius + shipCollisionDetectionRadius;
        }
        D.Log(ShowDebugLog, "{0}.GetShipArrivalDistance returned {1:0.00}, {2}.", FullName, arrivalDistance, arrivalDistanceMsg);
        return arrivalDistance;
    }

    #endregion

    #region IHighlightable Members

    public override float HoverHighlightRadius { get { return Radius * 2F; } }

    #endregion

    #region IAvoidableObstacle Members

    public override Vector3 GetDetour(Vector3 shipOrFleetPosition, RaycastHit zoneHitInfo, float fleetRadius) {
        return _detourGenerator.GenerateDetourAtObstaclePoles(shipOrFleetPosition, fleetRadius);
    }

    #endregion

    #region IShipExplorable Members

    public bool IsFullyExploredBy(Player player) {
        return GetIntelCoverage(player) == IntelCoverage.Comprehensive;
    }

    public bool IsExploringAllowedBy(Player player) {
        return !Owner.IsAtWarWith(player);
    }

    public void RecordExplorationCompletedBy(Player player) {
        SetIntelCoverage(player, IntelCoverage.Comprehensive);
        ChildMoons.ForAll(moon => moon.SetIntelCoverage(player, IntelCoverage.Comprehensive));
    }

    #endregion

}

