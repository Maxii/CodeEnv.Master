// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SelectedItemHudForm.cs
//HudForm customized for displaying info about Selected Items in a fixed location.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// HudForm customized for displaying info about Selected Items in a fixed location.
/// </summary>
[System.Obsolete]
public class SelectedItemHudForm : AHudForm {

    public override HudFormID FormID { get { return formID; } }

    public HudFormID formID;

    private UILabel _titleLabel;
    private UILabel _itemNameLabel;

    protected override void Awake() {
        base.Awake();
        InitializeMembers();
    }

    protected virtual void InitializeMembers() {
        _itemNameLabel = GetLabel(GuiElementID.ItemNameLabel);
        var immediateChildLabels = gameObject.GetSafeMonoBehavioursInImmediateChildrenOnly<UILabel>();
        _titleLabel = immediateChildLabels.Single(l => l != _itemNameLabel);
        ConfigureNameLabel();
    }

    protected override void AssignValuesToWidgets() {
        var content = FormContent as SelectedItemHudFormContent;
        _titleLabel.text = FormID.GetValueName() + " Selected Item Hud";
        _itemNameLabel.text = content.Report.Name;
    }

    private void ConfigureNameLabel() {
        MyNguiEventListener.Get(_itemNameLabel.gameObject).onDoubleClick += OnNameDoubleClick;
    }

    private void OnNameDoubleClick(GameObject go) {
        IItem item = (FormContent as SelectedItemHudFormContent).Report.Item;
        (item as ICameraFocusable).IsFocus = true;
    }

    private UILabel GetLabel(GuiElementID elementID) {
        return GetGuiElement(elementID).gameObject.GetSafeFirstMonoBehaviourInChildren<UILabel>();
    }

    private GuiElement GetGuiElement(GuiElementID elementID) {
        var elements = gameObject.GetSafeMonoBehavioursInImmediateChildrenOnly<GuiElement>();
        return elements.Single(e => e.gameObject.GetSafeMonoBehaviour<GuiElement>().elementID == elementID);
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

