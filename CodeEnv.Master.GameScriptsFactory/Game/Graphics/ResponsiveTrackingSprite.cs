// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ResponsiveTrackingSprite.cs
// Sprite resident in world space that can respond to the mouse. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Sprite resident in world space that can respond to the mouse. 
/// The user perceives the widget at a constant size, independent of camera distance.
/// </summary>
public class ResponsiveTrackingSprite : ConstantSizeTrackingSprite, IResponsiveTrackingSprite {

    public ICameraLosChangedListener CameraLosChangedListener { get; private set; }

    public IMyNguiEventListener EventListener { get; private set; }

    private IconInfo _iconInfo;
    public IconInfo IconInfo {
        get { return _iconInfo; }
        set { SetProperty<IconInfo>(ref _iconInfo, value, "IconInfo", IconInfoPropChangedHandler); }
    }

    private Collider _collider;

    protected override void Awake() {
        base.Awake();
        NGUITools.AddWidgetCollider(WidgetTransform.gameObject);
        _collider = gameObject.GetComponentInChildren<Collider>();
        _collider.isTrigger = true;
        CameraLosChangedListener = WidgetTransform.gameObject.AddComponent<CameraLosChangedListener>();
        EventListener = WidgetTransform.gameObject.AddComponent<MyNguiEventListener>();
    }

    public override void __SetDimensions(int width, int height) {
        base.__SetDimensions(width, height);
        NGUITools.UpdateWidgetCollider(WidgetTransform.gameObject);
    }

    protected override void Show() {
        base.Show();
        _collider.enabled = true;
    }

    protected override void Hide() {
        base.Hide();
        _collider.enabled = false;
        // Note: donot disable CameraLosChangedListener, as disabling it will also eliminate OnBecameVisible() events
    }

    #region Event and Property Change Handlers

    private void IconInfoPropChangedHandler() {
        AtlasID = IconInfo.AtlasID;
        Set(IconInfo.Filename);
        Color = IconInfo.Color;
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

