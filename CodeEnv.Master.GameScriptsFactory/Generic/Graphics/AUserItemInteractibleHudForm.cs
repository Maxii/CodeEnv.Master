// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUserItemInteractibleHudForm.cs
// Abstract class for User-owned Forms used by the InteractibleHud.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract class for User-owned Forms used by the InteractibleHud.
/// </summary>
public abstract class AUserItemInteractibleHudForm : AItemDataForm {

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

    #region Event and Property Change Handlers

    #endregion

    protected override void ResetNonGuiElementMembers() {
        base.ResetNonGuiElementMembers();
        _titleLabel.text = null;
    }

}

