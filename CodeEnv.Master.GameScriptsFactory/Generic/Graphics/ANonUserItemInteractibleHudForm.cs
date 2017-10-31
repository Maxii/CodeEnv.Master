// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ANonUserItemInteractibleHudForm.cs
// Abstract class for NonUser-owned Item Forms used by the InteractibleHud.
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
/// Abstract class for NonUser-owned Item Forms used by the InteractibleHud.
/// </summary>
public abstract class ANonUserItemInteractibleHudForm : AItemReportForm {

    private UILabel _titleLabel;

    protected override void InitializeNonGuiElementMembers() {
        base.InitializeNonGuiElementMembers();
        var immediateChildLabels = gameObject.GetSafeComponentsInImmediateChildren<UILabel>();
        _titleLabel = immediateChildLabels.Single(l => l != _nameLabel);
    }

    public sealed override void PopulateValues() {
        base.PopulateValues();
        D.Assert(GameManager.Instance.IsPaused);    // Only Selection (which pauses game) can raise InteractibleHud
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

