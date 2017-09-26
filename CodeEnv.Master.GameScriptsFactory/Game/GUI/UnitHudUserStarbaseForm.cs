// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnitHudUserStarbaseForm.cs
// Form used by the UnitHudWindow to display info and allow changes when a user-owned Starbase is selected.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.GameContent;

/// <summary>
/// Form used by the UnitHudWindow to display info and allow changes when a user-owned Starbase is selected.
/// </summary>
public class UnitHudUserStarbaseForm : AUnitHudUserBaseForm {

    public override FormID FormID { get { return FormID.UserStarbase; } }

}

