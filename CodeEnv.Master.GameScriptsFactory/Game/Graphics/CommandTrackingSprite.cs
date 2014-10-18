// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CommandTrackingSprite.cs
// Sprite resident in world space that tracks Commands.  
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
/// Sprite resident in world space that tracks Commands.  The user perceives the widget at a constant size, independent of camera distance.
/// </summary>
public class CommandTrackingSprite : ConstantSizeTrackingSprite {

    public CameraLosChangedListener CameraLosChangedListener { get; private set; }

    public UIEventListener EventListener { get; private set; }

    private Collider _collider;

    protected override void Awake() {
        base.Awake();
        NGUITools.AddWidgetCollider(WidgetTransform.gameObject);
        _collider = gameObject.GetComponentInChildren<Collider>();
        _collider.isTrigger = true;
        CameraLosChangedListener = WidgetTransform.gameObject.AddComponent<CameraLosChangedListener>();
        EventListener = WidgetTransform.gameObject.AddComponent<UIEventListener>();
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

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

