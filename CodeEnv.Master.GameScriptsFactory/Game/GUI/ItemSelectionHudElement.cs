// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ItemSelectionHudElement.cs
// Hud Element customized for displaying info about Selected Items.
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
/// Hud Element customized for displaying info about Selected Items.
/// </summary>
public class ItemSelectionHudElement : AHudElement {

    public override HudElementID ElementID { get { return elementID; } }

    public HudElementID elementID;

    private UILabel _titleLabel;
    private UILabel _itemNameLabel;

    protected override void Awake() {
        base.Awake();
        InitializeMembers();
    }

    private void InitializeMembers() {
        var immediateChildLabels = gameObject.GetSafeMonoBehavioursInImmediateChildren<UILabel>();
        _titleLabel = immediateChildLabels.Single(l => l.alignment == NGUIText.Alignment.Center);
        _itemNameLabel = immediateChildLabels.Single(l => l.alignment == NGUIText.Alignment.Left);
        ConfigureNameLabel();
    }

    protected override void AssignValuesToMembers() {
        var content = HudContent as SelectedItemHudContent;
        _titleLabel.text = ElementID.GetName() + " Item Selection Element";
        _itemNameLabel.text = content.Report.Name;
    }

    private void ConfigureNameLabel() {
        MyNguiEventListener.Get(_itemNameLabel.gameObject).onDoubleClick += OnNameDoubleClick;
    }

    private void OnNameDoubleClick(GameObject go) {
        IItem item = (HudContent as SelectedItemHudContent).Report.Item;
        (item as ICameraFocusable).IsFocus = true;
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

