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

    //[FormerlySerializedAs("padLeft")]
    [SerializeField]
    private int _padLeft = Constants.Zero;

    //[FormerlySerializedAs("padRight")]
    [SerializeField]
    private int _padRight = Constants.Zero;

    //[FormerlySerializedAs("padBottom")]
    [SerializeField]
    private int _padBottom = Constants.Zero;

    //[FormerlySerializedAs("padTop")]
    [SerializeField]
    private int _padTop = Constants.Zero;

    public string DebugName { get { return GetType().Name; } }

    private bool _isStarted = false;

    protected override void Start() {
        base.Start();
        _isStarted = true;
        Subscribe();
    }

    private void Subscribe() {
        UICamera.onScreenResize += Execute;
    }

    protected override void OnEnable() {
        base.OnEnable();
        if (_isStarted) { Execute(); }
    }

    [ContextMenu("Execute")]
    public void Execute() {
        if (targetRoot == transform) {
            D.ErrorContext(this, "Target Root object cannot be the same object that has Envelop Content. Make it a sibling instead.");
        }
        else if (NGUITools.IsChild(targetRoot, transform)) {
            D.ErrorContext(this, "Target Root object cannot be a parent of Envelop Content. Make it a sibling instead.");
        }
        else {
            Bounds b = NGUIMath.CalculateRelativeWidgetBounds(transform.parent, targetRoot, false, considerChildren: false);
            float x0 = b.min.x + _padLeft;
            float y0 = b.min.y + _padBottom;
            float x1 = b.max.x + _padRight;
            float y1 = b.max.y + _padTop;

            UIWidget w = GetComponent<UIWidget>();
            w.SetRect(x0, y0, x1 - x0, y1 - y0);
            BroadcastMessage("UpdateAnchors", SendMessageOptions.DontRequireReceiver);
        }
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        UICamera.onScreenResize -= Execute;
    }

    public override string ToString() {
        return DebugName;
    }

}

