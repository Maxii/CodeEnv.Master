// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetItem.cs
// Class for APlanetoidItems that are Planets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Linq;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Class for APlanetoidItems that are Planets.
/// </summary>
public class PlanetItem : APlanetoidItem {

    protected new PlanetDisplayManager DisplayMgr { get { return base.DisplayMgr as PlanetDisplayManager; } }


    #region Initialization

    protected override ADisplayManager InitializeDisplayManager() {
        var displayMgr = new PlanetDisplayManager(gameObject);
        displayMgr.Icon = InitializeIcon();
        return displayMgr;
    }

    private ResponsiveTrackingSprite InitializeIcon() {
        var icon = TrackingWidgetFactory.Instance.CreateResponsiveTrackingSprite(this, TrackingWidgetFactory.IconAtlasID.Contextual,
            new Vector2(12, 12), WidgetPlacement.Over);
        icon.Set("Icon02"); // TODO planet icon should vary depending on whether it has moons or not
        icon.Color = Owner.Color;
        var iconEventListener = icon.EventListener;
        iconEventListener.onHover += (iconGo, isOver) => OnHover(isOver);
        iconEventListener.onClick += (iconGo) => OnClick();
        iconEventListener.onDoubleClick += (iconGo) => OnDoubleClick();
        iconEventListener.onPress += (iconGo, isDown) => OnPress(isDown);
        return icon;
    }

    #endregion

    #region Model Methods

    protected override void OnOwnerChanged() {
        base.OnOwnerChanged();
        if (DisplayMgr != null && DisplayMgr.Icon != null) {
            DisplayMgr.Icon.Color = Owner.Color;
        }
    }


    protected override void OnDeath() {
        base.OnDeath();
        var moons = _transform.GetComponentsInChildren<MoonItem>();
        if (moons.Any()) {
            // since the planet is on its way to destruction, the moons need to show their destruction too
            moons.ForAll(moon => moon.OnPlanetDying());
        }
        // TODO consider destroying the orbiter object and separating it from the OrbitSlot
    }

    #endregion

    #region View Methods
    #endregion

    #region Mouse Events
    #endregion

    #region Cleanup

    protected override void Unsubscribe() {
        base.Unsubscribe();
        if (DisplayMgr != null && DisplayMgr.Icon != null) {
            var iconEventListener = DisplayMgr.Icon.EventListener;
            iconEventListener.onHover -= (iconGo, isOver) => OnHover(isOver);
            iconEventListener.onClick -= (iconGo) => OnClick();
            iconEventListener.onDoubleClick -= (iconGo) => OnDoubleClick();
            iconEventListener.onPress -= (iconGo, isDown) => OnPress(isDown);
        }
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

