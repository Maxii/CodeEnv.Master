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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

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
public class PlanetItem : APlanetoidItem, IPlanet, IPlanet_Ltd, IShipExplorable {

    private static readonly Vector2 IconSize = new Vector2(20F, 20F);

    public new PlanetData Data {
        get { return base.Data as PlanetData; }
        set { base.Data = value; }
    }

    public override float ClearanceRadius {
        get {
            float baseClearance = Data.CloseOrbitOuterRadius;
            if (ChildMoons.Any()) {
                MoonItem outerMoon = ChildMoons.MaxBy(moon => Vector3.SqrMagnitude(moon.Position - Position));
                float distanceToOuterMoon = Vector3.Distance(outerMoon.Position, Position);
                baseClearance = distanceToOuterMoon + outerMoon.ClearanceRadius;
            }
            return baseClearance * 2F;  // HACK
        }
    }

    private IList<MoonItem> _childMoons;
    /// <summary>
    /// The moons that orbit this planet, if any.
    /// </summary>
    public IList<MoonItem> ChildMoons {
        get {
            _childMoons = _childMoons ?? GetComponentsInChildren<MoonItem>().ToList();
            return _childMoons;
        }
    }

    protected override float ObstacleClearanceDistance { get { return Data.CloseOrbitOuterRadius; } }

    protected new PlanetDisplayManager DisplayMgr { get { return base.DisplayMgr as PlanetDisplayManager; } }

    private IList<IShip_Ltd> _shipsInCloseOrbit;

    #region Initialization

    protected override ADisplayManager MakeDisplayManagerInstance() {
        return new PlanetDisplayManager(this, __DetermineCullingLayer());
    }

    protected override void InitializeDisplayManager() {
        base.InitializeDisplayManager();
        InitializeIcon();
    }

    protected override ICtxControl InitializeContextMenu(Player owner) {
        return new PlanetCtxControl(this);
    }

    protected override HoverHighlightManager InitializeHoverHighlightMgr() {
        float highlightRadius = Radius * 2F;
        return new HoverHighlightManager(this, highlightRadius);
    }

    protected override float InitializeObstacleZoneRadius() {
        return Data.CloseOrbitInnerRadius;
    }

    public override void FinalInitialize() {
        base.FinalInitialize();
        IsOperational = true;
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        CelestialOrbitSimulator.IsActivated = true; // planet orbit in system always activated as not just eye candy
    }

    internal void RemoveMoon(MoonItem moon) {
        D.Assert(!moon.IsOperational);
        bool isRemoved = ChildMoons.Remove(moon);
        D.Assert(isRemoved);
    }

    public override void HandleEffectSequenceFinished(EffectSequenceID effectID) {
        base.HandleEffectSequenceFinished(effectID);
        //D.Log(ShowDebugLog, "{0}.HandleEffectFinished({1}) called.", DebugName, effectID.GetValueName());
        switch (effectID) {
            case EffectSequenceID.Dying:
                if (ChildMoons.Any()) {
                    // planet has completed showing its death so moons need to show theirs too
                    ChildMoons.ForAll(moon => {
                        moon.effectSeqFinished += MoonDeathEffectFinishedEventHandler; // no unsubscribing needed as all are destroyed
                        moon.HandlePlanetDying();
                    });
                }
                else {
                    DestroyMe();
                }
                break;
            case EffectSequenceID.Hit:
                break;
            case EffectSequenceID.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(effectID));
        }
    }

    #region Event and Property Change Handlers

    protected override void HandleOwnerChanged() {
        base.HandleOwnerChanged();
        AssessIcon();
    }

    private void MoonDeathEffectFinishedEventHandler(object sender, EffectSeqEventArgs e) {
        D.AssertEqual(EffectSequenceID.Dying, e.EffectSeqID);
        HandleMoonDeathEffectFinished();
    }

    private void HandleMoonDeathEffectFinished() {
        //D.Log(ShowDebugLog, "{0}.HandleMoonDeathEffectFinished() called. Remaining Moons: {1}", DebugName, ChildMoons.Count);
        if (ChildMoons.Count() == Constants.Zero) {
            // The last moon has shown its death effect as a result of the planet's death
            DestroyMe();
        }
    }

    #endregion

    protected override void ConnectHighOrbitRigidbodyToShipOrbitJoint(FixedJoint shipOrbitJoint) {
        if (ChildMoons.Any()) {
            IMoon outermostMoon = ChildMoons.MaxBy(m => Vector3.SqrMagnitude(m.Position - Position));
            Rigidbody highOrbitRigidbody = outermostMoon.CelestialOrbitSimulator.OrbitRigidbody;
            shipOrbitJoint.connectedBody = highOrbitRigidbody;
        }
        else {
            base.ConnectHighOrbitRigidbodyToShipOrbitJoint(shipOrbitJoint);
        }
    }

    private Layers __DetermineCullingLayer() {
        switch (Data.Category) {
            case PlanetoidCategory.GasGiant:    // radius 5
            case PlanetoidCategory.Ice:         // radius 2 with rings
                return TempGameValues.LargerPlanetMeshCullLayer;
            case PlanetoidCategory.Terrestrial: // radius 2
            case PlanetoidCategory.Volcanic:    // radius 1
                return TempGameValues.SmallerPlanetMeshCullLayer;
            case PlanetoidCategory.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(Data.Category));
        }
    }

    #region Show Icon

    private void InitializeIcon() {
        DebugControls debugControls = DebugControls.Instance;
        debugControls.showPlanetIcons += ShowPlanetIconsChangedEventHandler;
        if (debugControls.ShowPlanetIcons) {
            EnableIcon(true);
        }
    }

    private void EnableIcon(bool toEnable) {
        if (toEnable) {
            if (DisplayMgr.Icon == null) {
                DisplayMgr.IconInfo = MakeIconInfo();
            }
        }
        else {
            if (DisplayMgr.Icon != null) {
                DisplayMgr.IconInfo = default(IconInfo);
            }
        }
    }

    private void AssessIcon() {
        if (DisplayMgr != null) {
            if (DisplayMgr.Icon != null) {
                var iconInfo = RefreshIconInfo();
                if (DisplayMgr.IconInfo != iconInfo) {    // avoid property not changed warning
                    //D.Log(ShowDebugLog, "{0} changing IconInfo from {1} to {2}.", DebugName, DisplayMgr.IconInfo, iconInfo);
                    DisplayMgr.IconInfo = iconInfo;
                }
            }
            else {
                D.Assert(!DebugControls.Instance.ShowPlanetIcons);
            }
        }
    }

    private IconInfo RefreshIconInfo() {
        return MakeIconInfo();
    }

    private IconInfo MakeIconInfo() {
        var report = UserReport;
        GameColor iconColor = report.Owner != null ? report.Owner.Color : GameColor.White;
        return new IconInfo("Icon02", AtlasID.Contextual, iconColor, IconSize, WidgetPlacement.Over, TempGameValues.PlanetIconCullLayer);
    }

    private void ShowPlanetIconsChangedEventHandler(object sender, EventArgs e) {
        EnableIcon(DebugControls.Instance.ShowPlanetIcons);
    }

    /// <summary>
    /// Cleans up any icon subscriptions.
    /// <remarks>The icon itself will be cleaned up when DisplayMgr.Dispose() is called.</remarks>
    /// </summary>
    private void CleanupIconSubscriptions() {
        var debugControls = DebugControls.Instance;
        if (debugControls != null) {
            debugControls.showPlanetIcons -= ShowPlanetIconsChangedEventHandler;
        }
    }

    #region Planet Icon Archive

    // 1.16.17 TEMP Replaced fixed use of Icons with easily accessible DebugControls setting

    //protected override void InitializeDisplayManager() {
    //    base.InitializeDisplayManager();
    //    DisplayMgr.IconInfo = MakeIconInfo();
    //}

    //private void AssessIcon() {
    //    if (DisplayMgr != null) {
    //        var iconInfo = RefreshIconInfo();
    //        if (DisplayMgr.IconInfo != iconInfo) {    // avoid property not changed warning
    //            //D.Log(ShowDebugLog, "{0} changing IconInfo from {1} to {2}.", DebugName, DisplayMgr.IconInfo, iconInfo);
    //            DisplayMgr.IconInfo = iconInfo;
    //        }
    //    }
    //}

    #endregion

    #endregion

    #region Cleanup

    protected override void Unsubscribe() {
        base.Unsubscribe();
        CleanupIconSubscriptions();
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

    public void AssumeCloseOrbit(IShip_Ltd ship, FixedJoint shipOrbitJoint) {
        if (_shipsInCloseOrbit == null) {
            _shipsInCloseOrbit = new List<IShip_Ltd>();
        }
        shipOrbitJoint.connectedBody = CloseOrbitSimulator.OrbitRigidbody;
        _shipsInCloseOrbit.Add(ship);
    }

    public bool IsInCloseOrbit(IShip_Ltd ship) {
        if (_shipsInCloseOrbit == null || !_shipsInCloseOrbit.Contains(ship)) {
            return false;
        }
        return true;
    }

    public bool IsCloseOrbitAllowedBy(Player player) {
        if (!InfoAccessCntlr.HasAccessToInfo(player, ItemInfoID.Owner)) {
            return true;
        }
        return !Owner.IsAtWarWith(player);
    }

    public IList<StationaryLocation> LocalAssemblyStations { get { return ParentSystem.LocalAssemblyStations; } }

    #endregion

    #region IShipOrbitable Members

    public override void HandleBrokeOrbit(IShip_Ltd ship) {
        if (IsInCloseOrbit(ship)) {
            D.AssertNotNull(_closeOrbitSimulator);
            var isRemoved = _shipsInCloseOrbit.Remove(ship);
            D.Assert(isRemoved);
            D.Log(ShowDebugLog, "{0} has left close orbit around {1}.", ship.DebugName, DebugName);
            float shipDistance = Vector3.Distance(ship.Position, Position);
            float insideOrbitSlotThreshold = Data.CloseOrbitOuterRadius - ship.CollisionDetectionZoneRadius_Debug;
            if (shipDistance > insideOrbitSlotThreshold) {
                D.Log(ShowDebugLog, "{0} is leaving orbit of {1} but collision detection zone is poking outside of orbit slot by {2:0.0000} units.",
                    ship.DebugName, DebugName, shipDistance - insideOrbitSlotThreshold);
                float halfOutsideOrbitSlotThreshold = Data.CloseOrbitOuterRadius;
                if (shipDistance > halfOutsideOrbitSlotThreshold) {
                    D.Warn("{0} is leaving orbit of {1} but collision detection zone is half outside of orbit slot.", ship.DebugName, DebugName);
                }
            }
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

    #region IShipNavigable Members

    public override AutoPilotDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, Vector3 shipPosition) {
        float innerShellRadius;
        float outerShellRadius;
        if (ChildMoons.Any()) {
            MoonItem outerMoon = ChildMoons.MaxBy(moon => Vector3.SqrMagnitude(moon.Position - Position));
            float distanceToOuterMoon = Vector3.Distance(outerMoon.Position, Position);
            innerShellRadius = distanceToOuterMoon + outerMoon.GetApMoveTgtProxy(tgtOffset, tgtStandoffDistance, shipPosition).InnerRadius;
            outerShellRadius = innerShellRadius + 1F;   // HACK depth of arrival shell is 1
        }
        else {
            innerShellRadius = Data.CloseOrbitOuterRadius + tgtStandoffDistance;   // closest arrival keeps CDZone outside of close orbit
            outerShellRadius = innerShellRadius + 1F;   // HACK depth of arrival shell is 1
        }
        return new AutoPilotDestinationProxy(this, tgtOffset, innerShellRadius, outerShellRadius);
    }

    #endregion

    #region IShipExplorable Members

    public bool IsFullyExploredBy(Player player) {
        return GetIntelCoverage(player) == IntelCoverage.Comprehensive;
    }

    public bool IsExploringAllowedBy(Player player) {
        if (!InfoAccessCntlr.HasAccessToInfo(player, ItemInfoID.Owner)) {
            return true;
        }
        return !Owner.IsAtWarWith(player);
    }

    public void RecordExplorationCompletedBy(Player player) {
        SetIntelCoverage(player, IntelCoverage.Comprehensive);
        ChildMoons.ForAll(moon => moon.SetIntelCoverage(player, IntelCoverage.Comprehensive));
    }

    #endregion

}

