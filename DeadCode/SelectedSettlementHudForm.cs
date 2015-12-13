// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SelectedSettlementHudForm.cs
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
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// COMMENT 
/// </summary>
[System.Obsolete]
public class SelectedSettlementHudForm : SelectedItemHudForm {

    private ResourcesGuiElement _resourcesElement;

    protected override void InitializeMembers() {
        base.InitializeMembers();
        _resourcesElement = GetGuiElement(GuiElementID.Resources) as ResourcesGuiElement;
    }

    protected override void AssignValuesToWidgets() {
        base.AssignValuesToWidgets();
        _resourcesElement.Resources = ((FormContent as SelectedItemHudFormContent).Report as SettlementReport).Resources;
    }


    private GuiElement GetGuiElement(GuiElementID id) {
        return gameObject.GetSafeMonoBehavioursInChildren<GuiElement>().Single(e => e.elementID == id);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

