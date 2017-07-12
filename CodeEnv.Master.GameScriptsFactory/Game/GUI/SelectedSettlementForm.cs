// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SelectedSettlementForm.cs
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
public class SelectedSettlementForm : ASelectedItemForm {

    public override FormID FormID { get { return FormID.SelectedSettlement; } }

    protected override void AssignValueToNameInputGuiElement() {
        base.AssignValueToNameInputGuiElement();
        _nameInput.value = (ItemData as AUnitCmdData).UnitName;
        //D.Log("{0}: Input field has been assigned {1}.", DebugName, _nameInput.value);
    }

    protected override void AssignValueToStrategicResourcesGuiElement() {
        base.AssignValueToStrategicResourcesGuiElement();
        _resourcesElement.Reset();  // IMPORTANT Always Reset GuiElements used by AItemSelectedForms as the same instance is being reused
        _resourcesElement.Resources = (ItemData as SettlementCmdData).Resources;
    }

    protected override void HandleNameInputSubmitted() {
        base.HandleNameInputSubmitted();
        AUnitCmdData cmdData = ItemData as AUnitCmdData;
        if (cmdData.UnitName != _nameInput.value) {
            //D.Log("{0}: UnitName changing from {1} to {2}.", DebugName, cmdData.UnitName, _nameInput.value);
            cmdData.UnitName = _nameInput.value;
        }
        else {
            D.Warn("{0}: UnitName {1} submitted without being changed.", DebugName, cmdData.UnitName);
        }
        _nameInput.RemoveFocus();
    }
}

