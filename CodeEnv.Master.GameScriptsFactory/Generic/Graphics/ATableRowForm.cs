// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ATableRowForm.cs
//  Abstract base class for ReportForms that are TableRows. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for ReportForms that are TableRows. 
/// </summary>
public abstract class ATableRowForm : AReportForm {

    public event Action<ICameraFocusable> onFocusOnItem;

    protected override void InitializeNameGuiElement(AGuiElement e) {
        base.InitializeNameGuiElement(e);
        MyNguiEventListener.Get(e.gameObject).onDoubleClick += OnFocusOnItem;
    }

    protected void OnFocusOnItem(GameObject go) {
        if (onFocusOnItem != null) {
            onFocusOnItem(Report.Item as ICameraFocusable);
        }
    }

}

