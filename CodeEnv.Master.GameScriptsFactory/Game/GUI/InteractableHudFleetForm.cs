﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: InteractableHudFleetForm.cs
// Form used by the InteractableHudWindow to display info and allow changes when a user-owned Item is selected.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Form used by the InteractableHudWindow to display info and allow changes when a user-owned Item is selected.
/// </summary>
public class InteractableHudFleetForm : AInteractableHudUnitForm {

    public override FormID FormID { get { return FormID.UserFleet; } }

    protected override List<string> AcceptableFormationNames {
        get { return TempGameValues.AcceptableFleetFormations.Select(f => f.GetValueName()).ToList(); }
    }

}
