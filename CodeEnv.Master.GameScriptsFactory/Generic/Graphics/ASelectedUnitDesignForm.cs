// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ASelectedUnitDesignForm.cs
// Abstract base class for AUnitDesignForms that are used to display info about a unit design in the SelectedItemHudWindow.
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
/// Abstract base class for AUnitDesignForms that are used to display info about a unit design in the SelectedItemHudWindow.
/// </summary>
public abstract class ASelectedUnitDesignForm : AUnitDesignForm {

    private UILabel _titleLabel;

    protected override void InitializeNonGuiElementMembers() {
        base.InitializeNonGuiElementMembers();
        var immediateChildLabels = gameObject.GetSafeComponentsInImmediateChildren<UILabel>();
        _titleLabel = immediateChildLabels.Single(l => l != _nameLabel);
    }

    protected override void AssignValuesToNonGuiElementMembers() {
        base.AssignValuesToNonGuiElementMembers();
        _titleLabel.text = FormID.GetValueName();
    }

}

