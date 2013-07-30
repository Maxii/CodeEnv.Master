// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NguiEventFallthruHandler.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// COMMENT 
/// </summary>
[Obsolete]
public class NguiEventFallthruHandler : AMonoBehaviourBase {

    void Start() {
        UICamera.fallThrough = gameObject;
    }

    void OnHover(bool isOver) {
        WriteMessage(isOver.ToString());
    }

    void OnPress(bool isDown) {
        WriteMessage(isDown.ToString());
    }

    void OnSelect(bool selected) {
        WriteMessage(selected.ToString());
    }

    void OnClick() {
        WriteMessage();
    }

    void OnDoubleClick() {
        WriteMessage();
    }

    void OnDrag(Vector2 delta) {
        WriteMessage(delta.ToString());
    }

    void OnDrop(GameObject go) {
        WriteMessage(go.name);
    }

    void OnInput(string text) {
        WriteMessage(text);
    }

    void OnTooltip(bool toShow) {
        WriteMessage(toShow.ToString());
    }

    void OnScroll(float delta) {
        WriteMessage(delta.ToString());
    }

    void OnKey(KeyCode key) {
        WriteMessage(key.ToString());
    }

    private void WriteMessage(string arg = "") {
        System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
        string touchID = UICamera.currentTouchID.ToString();
        string objectTouched = UICamera.hoveredObject.name;
        string msg = "NguiEventFallthruHandler.{0}({1}) called. TouchID = {2}, GameObject touched = {3}.".Inject(stackFrame.GetMethod().Name, arg, touchID, objectTouched);
        Logger.Log(msg);
    }





    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

