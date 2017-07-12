// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SelectedSystemForm.cs
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
public class SelectedSystemForm : ASelectedItemForm {

    public override FormID FormID { get { return FormID.SelectedSystem; } }

    protected override void AssignValueToNameInputGuiElement() {
        base.AssignValueToNameInputGuiElement();
        _nameInput.value = ItemData.Name;
        //D.Log("{0}: Input field has been assigned {1}.", DebugName, _nameInput.value);
    }

    protected override void HandleNameInputSubmitted() {
        base.HandleNameInputSubmitted();
        if (ItemData.Name != _nameInput.value) {
            //D.Log("{0}: SystemName changing from {1} to {2}.", DebugName, ItemData.Name, _nameInput.value);
            ItemData.Name = _nameInput.value;
        }
        else {
            D.Warn("{0}: SystemName {1} submitted without being changed.", DebugName, ItemData.Name);
        }
        _nameInput.RemoveFocus();
    }

}

