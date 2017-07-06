// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SelectedShipDesignForm.cs
// Form used by the SelectedItemHudWindow to display info about a 'selected' ShipDesign.   
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.GameContent;

/// <summary>
/// Form used by the SelectedItemHudWindow to display info about a 'selected' ShipDesign.   
/// </summary>
public class SelectedShipDesignForm : ASelectedUnitDesignForm {

    public override FormID FormID { get { return FormID.SelectedShipDesign; } }

}

