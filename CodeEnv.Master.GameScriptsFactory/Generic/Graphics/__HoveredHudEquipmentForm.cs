// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: __HoveredHudEquipmentForm.cs
// Form used by the HoveredHudWindow to display info about Equipment.   
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Form used by the HoveredHudWindow to display info about Equipment.   
/// <remarks>TEMP as I expect equipment to differ in what is shown.
/// 7.12.17 Currently only shows the EquipmentName.</remarks>
/// </summary>
public class __HoveredHudEquipmentForm : AHoveredHudEquipmentForm {

    public override FormID FormID { get { return FormID.Equipment; } }

    private UILabel _titleLabel;

    protected override void InitializeNonGuiElementMembers() {
        base.InitializeNonGuiElementMembers();
        var immediateChildLabels = gameObject.GetSafeComponentsInImmediateChildren<UILabel>();
        _titleLabel = immediateChildLabels.Single(l => l != _nameLabel);
    }

    protected override void AssignValueToNameGuiElement() {
        base.AssignValueToNameGuiElement();
        _nameLabel.text = EquipmentStat.Name;
    }

    protected override void AssignValuesToNonGuiElementMembers() {
        base.AssignValuesToNonGuiElementMembers();
        _titleLabel.text = FormID.GetValueName();
    }

    protected override void ResetNonGuiElementMembers() {
        base.ResetNonGuiElementMembers();
        _titleLabel.text = null;
    }

}

