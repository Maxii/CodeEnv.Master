﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitDesignForm.cs
// Abstract base class for Forms that are that are used to display info about a unit design in a HudWindow.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for Forms that are that are used to display info about a unit design in a HudWindow.
/// </summary>
public abstract class AUnitDesignForm : AInfoDisplayForm {

    private AUnitDesign _design;
    public AUnitDesign Design {
        get { return _design; }
        set {
            D.AssertNull(_design);  // occurs only once between Resets
            SetProperty<AUnitDesign>(ref _design, value, "Design", DesignPropSetHandler);
        }
    }

    #region Event and Property Change Handlers

    private void DesignPropSetHandler() {
        AssignValuesToMembers();
    }

    #endregion

    public override void Reset() {
        _design = null;
    }


}

