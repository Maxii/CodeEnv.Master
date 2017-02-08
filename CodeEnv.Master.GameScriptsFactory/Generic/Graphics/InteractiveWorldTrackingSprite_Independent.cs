﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: InteractiveWorldTrackingSprite_Independent.cs
/// Sprite that can respond to the mouse, resident in world space that tracks an object as its child. 
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
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Sprite that can respond to the mouse, resident in world space that tracks an object as its child. 
/// The user perceives the widget at a constant size, independent of camera distance.
/// </summary>
public class InteractiveWorldTrackingSprite_Independent : WorldTrackingSprite_Independent, IInteractiveWorldTrackingSprite_Independent {

    public IMyEventListener EventListener { get; private set; }

    private Collider _collider;

    protected override void Awake() {
        base.Awake();
        GameObject widgetGo = WidgetTransform.gameObject;
        NGUITools.AddWidgetCollider(widgetGo);
        _collider = widgetGo.GetComponent<Collider>();
        _collider.isTrigger = false;    // Ngui 3.11.0 events now ignore trigger colliders when Ngui's EventType is World_3D so the
                                        // collider can no longer be a trigger. As the whole GameObject is on Layers.TransparentFX and 
                                        // has no allowed collisions (ProjectSettings.Physics), it doesn't need to be a trigger.
        _collider.enabled = false;

        Profiler.BeginSample("Proper AddComponent allocation", gameObject);
        EventListener = widgetGo.AddComponent<MyEventListener>();
        Profiler.EndSample();
    }

    protected override void Show() {
        base.Show();
        _collider.enabled = true;
    }

    protected override void Hide() {
        base.Hide();
        _collider.enabled = false;
        // Note: do not disable CameraLosChangedListener, as disabling it will also eliminate OnBecameVisible() events
    }

    #region Event and Property Change Handlers

    #endregion

    protected override void SetDimensions(Vector2 size) {
        base.SetDimensions(size);
        NGUITools.UpdateWidgetCollider(WidgetTransform.gameObject);
    }

    protected override void AlignWidgetOtherTo(WidgetPlacement placement) {
        base.AlignWidgetOtherTo(placement);
        // reposition the collider to account for the widget's pivot generated by the placement value
        NGUITools.UpdateWidgetCollider(WidgetTransform.gameObject);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }


}
