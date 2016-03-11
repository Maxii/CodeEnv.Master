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
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// APlanetoidItems that are Planets.
/// </summary>
public class PlanetItem : APlanetoidItem, IPlanetItem, IShipOrbitable, IShipExplorable {

    /// <summary>
    /// The moons that orbit this planet. Can be empty but never null.
    /// </summary>
    public MoonItem[] ChildMoons { get; private set; }

    public new PlanetData Data {
        get { return base.Data as PlanetData; }
        set { base.Data = value; }
    }

    protected new PlanetDisplayManager DisplayMgr { get { return base.DisplayMgr as PlanetDisplayManager; } }

    private DetourGenerator _detourGenerator;

    #region Initialization

    protected override void InitializeObstacleZone() {
        base.InitializeObstacleZone();
        _obstacleZoneCollider.radius = Data.LowOrbitRadius;
        Vector3 obstacleZoneCenter = Position + _obstacleZoneCollider.center;
        _detourGenerator = new DetourGenerator(obstacleZoneCenter, _obstacleZoneCollider.radius, Data.HighOrbitRadius);
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

    private ShipOrbitSlot InitializeShipOrbitSlot() {
        return new ShipOrbitSlot(Data.LowOrbitRadius, Data.HighOrbitRadius, this);
    }

    protected override void FinalInitialize() {
        base.FinalInitialize();
        RecordAnyChildMoons();
    }

    private void RecordAnyChildMoons() {
        ChildMoons = gameObject.GetComponentsInChildren<MoonItem>();
        //D.Log(showDebugLog && ChildMoons.Any(), "{0} recorded {1} child moons.", FullName, ChildMoons.Count());
    }

    #endregion

    private void AssessIcon() {
        if (DisplayMgr == null) { return; }

        var iconInfo = RefreshIconInfo();
        if (DisplayMgr.IconInfo != iconInfo) {    // avoid property not changed warning
            UnsubscribeToIconEvents(DisplayMgr.Icon);
            //D.Log(toShowDLog, "{0} changing IconInfo from {1} to {2}.", FullName, DisplayMgr.IconInfo, iconInfo);
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
        //D.Log(showDebugLog, "{0}.HandleEffectFinished({1}) called.", FullName, effectID.GetValueName());
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

    private int _deadMoonCount;
    private void HandleMoonDeathEffectFinished() {
        _deadMoonCount++;
        if (_deadMoonCount == ChildMoons.Count()) {
            // this is the last moon to die
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

    #region IShipOrbitable Members

    private ShipOrbitSlot _shipOrbitSlot;
    public ShipOrbitSlot ShipOrbitSlot {
        get {
            if (_shipOrbitSlot == null) { _shipOrbitSlot = InitializeShipOrbitSlot(); }
            return _shipOrbitSlot;
        }
    }

    public bool IsOrbitAllowedBy(Player player) {
        return !Owner.IsAtWarWith(player);
    }

    #endregion

    #region INavigableTarget Members

    public override float GetShipArrivalDistance(float shipCollisionDetectionRadius) {
        return Data.HighOrbitRadius + shipCollisionDetectionRadius; // OPTIMIZE want shipRadius value as AvoidableObstacleZone ends at LowOrbitRadius?
    }

    #endregion

    #region IHighlightable Members

    public override float HoverHighlightRadius { get { return Radius * 2F; } }

    #endregion

    #region IAvoidableObstacle Members

    public override Vector3 GetDetour(Vector3 shipOrFleetPosition, RaycastHit zoneHitInfo, float fleetRadius, Vector3 formationOffset) {
        return _detourGenerator.GenerateDetourAtObstaclePoles(shipOrFleetPosition, fleetRadius, formationOffset);
    }

    #endregion

    #region IShipExplorable Members

    public bool IsFullyExploredBy(Player player) {
        return GetIntelCoverage(player) == IntelCoverage.Comprehensive;
    }

    public bool IsExplorationAllowedBy(Player player) {
        return !Owner.IsAtWarWith(player);
    }

    public void RecordExplorationCompletedBy(Player player) {
        SetIntelCoverage(player, IntelCoverage.Comprehensive);
        ChildMoons.ForAll(moon => moon.SetIntelCoverage(player, IntelCoverage.Comprehensive));
    }

    #endregion

}

