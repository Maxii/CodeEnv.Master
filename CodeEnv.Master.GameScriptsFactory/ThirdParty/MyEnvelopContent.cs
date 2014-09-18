// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MyEnvelopContent.cs
// Resizes the widget it's attached to in order to envelop not just one widget, but all the widgets
// within the chosen heirarchy
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
/// Resizes the widget it's attached to in order to envelop not just one widget, but all the widgets
/// within the chosen heirarchy - targetRoot. Most common use - resizing a background to envelop
/// multiple widgets such as in a custom tooltiop containing multiple elements. 
/// Derived from Ngui/Examples/Other/EnvelopContent.
/// </summary>
[RequireComponent(typeof(UIWidget))]
public class MyEnvelopContent : AMonoBase {

    public Transform targetRoot;
    public int padLeft = 0;
    public int padRight = 0;
    public int padBottom = 0;
    public int padTop = 0;

    private bool _isStarted = false;

    protected override void Start() {
        base.Start();
        _isStarted = true;
        Subscribe();
    }

    private void Subscribe() {
        UICamera.onScreenResize += Execute;
        //GameStatus.Instance.onIsRunningOneShot += Execute;
    }

    protected override void OnEnable() {
        base.OnEnable();
        if (_isStarted) { Execute(); }
    }

    public void Execute() {
        if (targetRoot == transform) {
            D.ErrorContext("Target Root object cannot be the same object that has Envelop Content. Make it a sibling instead.", this);
        }
        else if (NGUITools.IsChild(targetRoot, transform)) {
            D.ErrorContext("Target Root object should not be a parent of Envelop Content. Make it a sibling instead.", this);
        }
        else {
            Bounds b = NGUIMath.CalculateRelativeWidgetBounds(transform.parent, targetRoot, false);
            float x0 = b.min.x + padLeft;
            float y0 = b.min.y + padBottom;
            float x1 = b.max.x + padRight;
            float y1 = b.max.y + padTop;

            UIWidget w = GetComponent<UIWidget>();
            w.SetRect(x0, y0, x1 - x0, y1 - y0);
            BroadcastMessage("UpdateAnchors", SendMessageOptions.DontRequireReceiver);
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

