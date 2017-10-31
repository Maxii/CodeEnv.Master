// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ADesignHoveredHudForm.cs
// Abstract base class for Forms that are used to display info about a unit (cmd or element) Design in the HoveredHudWindow.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for Forms that are used to display info about a unit (cmd or element) Design in the HoveredHudWindow.
/// </summary>
public abstract class ADesignHoveredHudForm : AInfoDisplayForm {

    private AUnitMemberDesign _design;
    public AUnitMemberDesign Design {
        get { return _design; }
        set {
            D.AssertNull(_design);  // occurs only once between Resets
            SetProperty<AUnitMemberDesign>(ref _design, value, "Design");
        }
    }

    public sealed override void PopulateValues() {
        D.AssertNotNull(Design);
        AssignValuesToMembers();
    }

    protected override void ResetForReuse_Internal() {
        base.ResetForReuse_Internal();
        _design = null;
    }


}

