// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AInteractableHudUnitForm.cs
// Abstract base class for InteractableHud forms for Units.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for InteractableHud forms for Units.
/// </summary>
public abstract class AInteractableHudUnitForm : AInteractableHudItemDataForm {

    protected abstract List<string> AcceptableFormationNames { get; }

    private UIPopupList _formationPopupList;
    private UILabel _formationPopupListLabel;
    private GuiHeroModule _heroModule;

    protected override void InitializeNonGuiElementMembers() {
        base.InitializeNonGuiElementMembers();
        _formationPopupList = gameObject.GetSingleComponentInChildren<UIPopupList>();
        _formationPopupList.keepValue = true;
        EventDelegate.Add(_formationPopupList.onChange, FormationChangedEventHandler);
        _formationPopupListLabel = _formationPopupList.GetComponentInChildren<UILabel>();
        _heroModule = gameObject.GetSingleComponentInChildren<GuiHeroModule>();
    }

    #region Event and Property Change Handlers

    private void FormationChangedEventHandler() {
        HandleFormationChanged();
    }

    #endregion

    protected override void AssignValuesToNonGuiElementMembers() {
        base.AssignValuesToNonGuiElementMembers();
        _formationPopupList.items = AcceptableFormationNames;
        string currentFormationName = (ItemData as AUnitCmdData).UnitFormation.GetValueName();
        _formationPopupList.Set(currentFormationName, notify: false);
        _formationPopupListLabel.text = currentFormationName;

        _heroModule.UnitData = ItemData as AUnitCmdData;
    }

    protected override void AssignValueToNameInputGuiElement() {
        base.AssignValueToNameInputGuiElement();
        _nameInput.value = (ItemData as AUnitCmdData).UnitName;
        //D.Log("{0}: Input field has been assigned {1}.", DebugName, _nameInput.value);
    }

    private void HandleFormationChanged() {
        var formation = Enums<Formation>.Parse(_formationPopupList.value);
        AUnitCmdData cmdData = ItemData as AUnitCmdData;
        if (cmdData.UnitFormation != formation) {
            D.Log("{0}: UnitFormation changing from {1} to {2}.", DebugName, cmdData.UnitFormation.GetValueName(), formation.GetValueName());
            cmdData.UnitFormation = formation;
        }
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

    protected override void ResetForReuse_Internal() {
        base.ResetForReuse_Internal();
        _nameInput.Set(null, notify: false);    // could also do _nameInput.value = null as don't subscribe to the onChange event
        _formationPopupList.Set(null, notify: false);
        _formationPopupListLabel.text = null;
        _heroModule.ResetForReuse();
    }

    protected override void CleanupNonGuiElementMembers() {
        base.CleanupNonGuiElementMembers();
        EventDelegate.Remove(_formationPopupList.onChange, FormationChangedEventHandler);
    }

}

