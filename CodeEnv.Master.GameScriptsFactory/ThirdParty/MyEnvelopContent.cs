// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MyEnvelopContent.cs
// Resizes the widget it's attached to in order to envelop not just one widget, but all the widgets
// within the chosen hierarchy
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Resizes the widget it's attached to in order to envelop not just one widget, but all the widgets
/// within the chosen hierarchy - targetRoot. Most common use - resizing a background to envelop
/// multiple widgets such as in a custom tooltip containing multiple elements. 
/// Derived from Ngui/Examples/Other/EnvelopContent.
/// </summary>
[RequireComponent(typeof(UIWidget))]
public class MyEnvelopContent : AMonoBase {

    [Tooltip("The root object of the hierarchy of widgets you wish to envelop")]
    public Transform targetRoot;

    [SerializeField]
    private int _padLeft = Constants.Zero;
    [SerializeField]
    private int _padRight = Constants.Zero;
    [SerializeField]
    private int _padBottom = Constants.Zero;
    [SerializeField]
    private int _padTop = Constants.Zero;

    public string DebugName { get { return GetType().Name; } }

    private bool _isStarted = false;

    protected override void Start() {
        base.Start();
        _isStarted = true;
        Subscribe();
        __Validate();
    }

    private void Subscribe() {
        UICamera.onScreenResize += ScreenResizeEventHandler;
    }

    protected override void OnEnable() {
        base.OnEnable();
        if (_isStarted) { Execute(); }
    }

    [ContextMenu("Execute")]
    public void Execute() {
        __Validate();
        //D.Log("{0} of {1} is Executing.", DebugName, targetRoot.name);
        // 6.19.17 considerChildren: false is OK. It only refers to children of a parent with a widget on it. If targetRoot
        // has a widget on it, only that widget will be encompassed as its children will not be evaluated. If targetRoot 
        // is an empty folder holding widgets as children, those children will be encompassed.
        Bounds b = NGUIMath.CalculateRelativeWidgetBounds(transform.parent, targetRoot, false, considerChildren: false);
        float x0 = b.min.x + _padLeft;
        float y0 = b.min.y + _padBottom;
        float x1 = b.max.x + _padRight;
        float y1 = b.max.y + _padTop;

        UIWidget w = GetComponent<UIWidget>();
        w.SetRect(x0, y0, x1 - x0, y1 - y0);
        BroadcastMessage("UpdateAnchors", SendMessageOptions.DontRequireReceiver);
    }

    #region Event and Property Change Handlers

    private void ScreenResizeEventHandler() {
        //D.Log("{0}.ScreenResizeEventHandler called.", DebugName);
        Execute();
    }

    #endregion

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        UICamera.onScreenResize -= ScreenResizeEventHandler;
    }

    public override string ToString() {
        return DebugName;
    }

    #region Debug

    private void __Validate() {
        if (targetRoot == null) {
            D.ErrorContext(this, "Target Root object cannot be null.");
        }
        else if (targetRoot == transform) {
            D.ErrorContext(this, "Target Root object cannot be the same object that has Envelop Content. Make it a sibling instead.");
        }
        else if (NGUITools.IsChild(targetRoot, transform)) {
            D.ErrorContext(this, "Target Root object cannot be a parent of Envelop Content. Make it a sibling instead.");
        }
    }

    #endregion

}

