// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: InteractableHudPlanetoidForm.cs
// Form used by the InteractableHudWindow to display info and allow changes when a user-owned Item is selected.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Form used by the InteractableHudWindow to display info and allow changes when a user-owned Item is selected.
/// </summary>
public class InteractableHudPlanetoidForm : AInteractableHudItemDataForm {

    public override FormID FormID { get { return FormID.UserPlanetoid; } }

    protected override void AssignValueToNameGuiElement() {
        base.AssignValueToNameGuiElement();
        _nameLabel.text = ItemData.Name;
    }

}

