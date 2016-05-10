﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NguiEventLogger.cs
// Simple event logger that can be deployed on a gameObject to record an Ngui Event. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Simple event logger that can be deployed on a gameObject to record an Ngui Event. 
/// </summary>
public class NguiEventLogger : AMonoBase {

    private const string NameFormat = "{0}(from transform)";

    void OnClick() {
        LogNguiEvent();
    }

    void OnScroll(float delta) {
        LogNguiEvent(delta);
    }

    void OnHover(bool isOver) {
        LogNguiEvent(isOver);
    }

    void OnDragStart() {
        LogNguiEvent();
    }

    void OnDragEnd() {
        LogNguiEvent();
    }

    void OnPress(bool isDown) {
        LogNguiEvent(isDown);
    }

    /// <summary>
    /// Logs the method name called. WARNING:  Coroutines show up as &lt;IEnumerator.MoveNext&gt; rather than the method name
    /// </summary>
    /// <param name="parameter">The parameter.</param>
    public void LogNguiEvent(object parameter = null) {
        var stackFrame = new System.Diagnostics.StackFrame(1);
        string name = NameFormat.Inject(transform.name);
        string paramName = parameter != null ? parameter.ToString() : string.Empty;
        Debug.Log("{0}.{1}({2}) called.".Inject(name, stackFrame.GetMethod().Name, paramName));
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

