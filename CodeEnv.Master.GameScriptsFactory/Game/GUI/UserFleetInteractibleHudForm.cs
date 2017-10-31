// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UserFleetInteractibleHudForm.cs
// Form used by the InteractibleHudWindow to display info and allow changes when a user-owned Item is selected.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Form used by the InteractibleHudWindow to display info and allow changes when a user-owned Item is selected.
/// </summary>
public class UserFleetInteractibleHudForm : AUserUnitInteractibleHudForm {

    public override FormID FormID { get { return FormID.UserFleet; } }

    public new FleetCmdData ItemData { get { return base.ItemData as FleetCmdData; } }

}

