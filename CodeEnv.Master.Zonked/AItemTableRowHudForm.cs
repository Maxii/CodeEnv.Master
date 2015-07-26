// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AItemTableRowHudForm.cs
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
[Obsolete]
public abstract class AItemTableRowHudForm<ItemType, ReportType> : AHudForm
    where ItemType : IItem
    where ReportType : AItemReport {

    public override HudFormID FormID {
        get { throw new NotImplementedException(); }
    }

    private UILabel _nameLabel;
    private OwnerGuiElement _ownerElement;

    protected override void Awake() {
        base.Awake();
        InitializeMembers();
    }

    protected virtual void InitializeMembers() {
        _nameLabel = GetLabel(GuiElementID.ItemNameLabel);
        _ownerElement = GetGuiElement(GuiElementID.Owner) as OwnerGuiElement;
    }

    protected override void AssignValuesToWidgets() {
        _nameLabel.text = (FormContent as ReportHudFormContent).Report.Name;
        // _ownerElement.Owner = 
    }

    protected UILabel GetLabel(GuiElementID id) {
        return GetGuiElement(id).gameObject.GetSafeFirstMonoBehaviourInChildren<UILabel>();
    }

    protected GuiElement GetGuiElement(GuiElementID id) {
        return gameObject.GetSafeMonoBehavioursInChildren<GuiElement>().Single(e => e.elementID == id);
    }


}

