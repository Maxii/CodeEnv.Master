﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SelectedShipForm.cs
// Form used by the SelectedItemHudWindow to display info about a 'selected' ship.   
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Form used by the SelectedItemHudWindow to display info about a 'selected' ship.   
/// </summary>
public class SelectedShipForm : ASelectedItemForm {

    public override FormID FormID { get { return FormID.SelectedShip; } }


}

