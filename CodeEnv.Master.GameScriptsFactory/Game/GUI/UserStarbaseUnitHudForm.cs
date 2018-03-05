// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UserStarbaseUnitHudForm.cs
// Form used by the UnitHudWindow to display info and allow changes when a user-owned Starbase is selected.
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

/// <summary>
/// Form used by the UnitHudWindow to display info and allow changes when a user-owned Starbase is selected.
/// </summary>
public class UserStarbaseUnitHudForm : ABaseUnitHudForm {

    public override FormID FormID { get { return FormID.UserStarbase; } }

    public new StarbaseCmdItem SelectedUnit { get { return base.SelectedUnit as StarbaseCmdItem; } }

    protected override void AssessInteractibleHud() {
        if (_pickedFacilityIcons.Count == Constants.One) {
            InteractibleHudWindow.Instance.Show(FormID.UserFacility, _pickedFacilityIcons.First().Element.Data);
        }
        else if (SelectedUnit != null) {    // 9.14.17 if SelectedUnit has been destroyed, reference will test as null
            InteractibleHudWindow.Instance.Show(FormID.UserStarbase, SelectedUnit.Data);
        }
        else {
            InteractibleHudWindow.Instance.Hide();
        }
    }

}

