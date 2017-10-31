// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AiFleetInteractibleHudForm.cs
// Form used by the InteractibleHudWindow to display info (and allow name changes) when a AI-owned Unit is selected.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.GameContent;

/// <summary>
/// Form used by the InteractibleHudWindow to display info (and allow name changes) when a AI-owned Unit is selected.
/// </summary>
public class AiFleetInteractibleHudForm : AAiUnitInteractibleHudForm {

    public override FormID FormID { get { return FormID.AiFleet; } }

    public new FleetCmdReport Report { get { return base.Report as FleetCmdReport; } }

}

