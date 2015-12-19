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

using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// APlanetoidItems that are Planets.
/// </summary>
public class PlanetItem : APlanetoidItem, IShipOrbitable {

    public new PlanetData Data {
        get { return base.Data as PlanetData; }
        set { base.Data = value; }
    }

    protected new PlanetDisplayManager DisplayMgr { get { return base.DisplayMgr as PlanetDisplayManager; } }

    #region Initialization

    protected override void InitializeOnData() {
        base.InitializeOnData();
        InitializeShipOrbitSlot();
    }

    private void InitializeShipOrbitSlot() {
        ShipOrbitSlot = new ShipOrbitSlot(Data.LowOrbitRadius, Data.HighOrbitRadius, this);
    }

    protected override ADisplayManager InitializeDisplayManager() {
        var dMgr = new PlanetDisplayManager(this, MakeIconInfo());
        SubscribeToIconEvents(dMgr.Icon);
        return dMgr;
    }

    private void SubscribeToIconEvents(IResponsiveTrackingSprite icon) {
        var iconEventListener = icon.EventListener;
        iconEventListener.onHover += (go, isOver) => HoverEventHandler(isOver);
        iconEventListener.onClick += (go) => ClickEventHandler();
        iconEventListener.onDoubleClick += (go) => DoubleClickEventHandler();
        iconEventListener.onPress += (go, isDown) => PressEventHandler(isDown);
    }

    #endregion

    protected override void PrepareForOnDeathNotification() {
        base.PrepareForOnDeathNotification();
        var moons = transform.GetComponentsInChildren<MoonItem>();
        if (moons.Any()) {
            // since the planet is on its way to destruction, the moons need to show their destruction too
            moons.ForAll(moon => moon.HandlePlanetDying());
        }
        //TODO consider destroying the orbiter object and separating it from the OrbitSlot
    }

    private void AssessIcon() {
        if (DisplayMgr == null) { return; }

        var iconInfo = RefreshIconInfo();
        if (DisplayMgr.IconInfo != iconInfo) {    // avoid property not changed warning
            UnsubscribeToIconEvents(DisplayMgr.Icon);
            //D.Log("{0} changing IconInfo from {1} to {2}.", FullName, DisplayMgr.IconInfo, iconInfo);
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

    #region Event and Property Change Handlers

    protected override void OwnerPropChangedHandler() {
        base.OwnerPropChangedHandler();
        AssessIcon();
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
        iconEventListener.onHover -= (go, isOver) => HoverEventHandler(isOver);
        iconEventListener.onClick -= (go) => ClickEventHandler();
        iconEventListener.onDoubleClick -= (go) => DoubleClickEventHandler();
        iconEventListener.onPress -= (go, isDown) => PressEventHandler(isDown);
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IShipOrbitable Members

    public ShipOrbitSlot ShipOrbitSlot { get; private set; }

    #endregion

    #region IShipTransitBanned Members

    public override float ShipTransitBanRadius { get { return Data.HighOrbitRadius; } }

    #endregion

}

