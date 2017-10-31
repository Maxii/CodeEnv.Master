// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UcHoveredHudForm.cs
// Form used by the HoveredHudWindow to display info about the UniverseCenter.   
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Form used by the HoveredHudWindow to display info about the UniverseCenter.   
/// </summary>
public class UcHoveredHudForm : AItemReportForm {

    public override FormID FormID { get { return FormID.UniverseCenter; } }

    private UILabel _titleLabel;

    protected override void InitializeNameGuiElement(AGuiElement e) {
        base.InitializeNameGuiElement(e);
        MyEventListener.Get(e.gameObject).onDoubleClick += NameDoubleClickEventHandler;
    }

    protected override void InitializeNonGuiElementMembers() {
        base.InitializeNonGuiElementMembers();
        var immediateChildLabels = gameObject.GetSafeComponentsInImmediateChildren<UILabel>();
        _titleLabel = immediateChildLabels.Single(l => l != _nameLabel);
    }

    protected override void AssignValueToNameGuiElement() {
        base.AssignValueToNameGuiElement();
        _nameLabel.text = Report.Name != null ? Report.Name : Unknown;
    }

    protected override void AssignValuesToNonGuiElementMembers() {
        base.AssignValuesToNonGuiElementMembers();
        _titleLabel.text = FormID.GetValueName();
    }

    #region Event and Property Change Handlers

    private void NameDoubleClickEventHandler(GameObject go) {
        (Report.Item as ICameraFocusable).IsFocus = true;
    }

    #endregion

    protected override void ResetNonGuiElementMembers() {
        base.ResetNonGuiElementMembers();
        _titleLabel.text = null;
    }

    protected override void CleanupNameGuiElement(AGuiElement e) {
        base.CleanupNameGuiElement(e);
        MyEventListener.Get(e.gameObject).onDoubleClick -= NameDoubleClickEventHandler;
    }

}

