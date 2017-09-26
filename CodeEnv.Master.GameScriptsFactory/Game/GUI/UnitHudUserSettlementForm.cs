// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnitHudUserSettlementForm.cs
// Form used by the UnitHudWindow to display info and allow changes when a user-owned Settlement is selected.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.GameContent;

/// <summary>
/// Form used by the UnitHudWindow to display info and allow changes when a user-owned Settlement is selected.
/// </summary>
public class UnitHudUserSettlementForm : AUnitHudUserBaseForm {

    public override FormID FormID { get { return FormID.UserSettlement; } }

}

