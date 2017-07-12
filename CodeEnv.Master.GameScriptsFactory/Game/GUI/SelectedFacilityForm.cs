// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SelectedFacilityForm.cs
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
public class SelectedFacilityForm : ASelectedItemForm {

    public override FormID FormID { get { return FormID.SelectedFacility; } }

    protected override void AssignValueToNameInputGuiElement() {
        base.AssignValueToNameInputGuiElement();
        _nameInput.value = (ItemData as AUnitElementData).Name;
        //D.Log("{0}: Input field has been assigned {1}.", DebugName, _nameInput.value);
    }

    protected override void HandleNameInputSubmitted() {
        base.HandleNameInputSubmitted();
        AUnitElementData eData = ItemData as AUnitElementData;
        if (eData.Name != _nameInput.value) {
            //D.Log("{0}: Name changing from {1} to {2}.", DebugName, eData.Name, _nameInput.value);
            eData.Name = _nameInput.value;
        }
        else {
            D.Warn("{0}: Name {1} submitted without being changed.", DebugName, eData.Name);
        }
        _nameInput.RemoveFocus();
    }

}

