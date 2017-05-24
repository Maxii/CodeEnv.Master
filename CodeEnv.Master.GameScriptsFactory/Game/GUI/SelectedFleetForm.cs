// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SelectedFleetForm.cs
// Form used by the SelectedItemHudWindow to display info from a FleetCmdReport when a fleet is selected.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Form used by the SelectedItemHudWindow to display info from a FleetCmdReport when a fleet is selected.
/// </summary>
public class SelectedFleetForm : ASelectedItemForm {

    public override FormID FormID { get { return FormID.SelectedFleet; } }


}

