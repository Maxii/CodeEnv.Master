// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DesignHoveredHudForm.cs
// Form used by the HoveredHudWindow to display info about a UnitDesign.   
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
/// Form used by the HoveredHudWindow to display info about a UnitDesign.   
/// <remarks>TEMP as I expect ship, facility and cmd designs to differ in what is shown.
/// 7.12.17 Currently only shows the DesignName.</remarks>
/// </summary>
public class DesignHoveredHudForm : ADesignHoveredHudForm {

    public override FormID FormID { get { return FormID.Design; } }

    private UILabel _titleLabel;

    protected override void InitializeNonGuiElementMembers() {
        base.InitializeNonGuiElementMembers();
        var immediateChildLabels = gameObject.GetSafeComponentsInImmediateChildren<UILabel>();
        _titleLabel = immediateChildLabels.Single(l => l != _nameLabel);
    }

    protected override void AssignValueToNameGuiElement() {
        base.AssignValueToNameGuiElement();
        _nameLabel.text = Design.DesignName;
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

