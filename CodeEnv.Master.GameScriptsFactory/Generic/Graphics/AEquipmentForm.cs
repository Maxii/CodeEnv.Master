// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AEquipmentForm.cs
// Abstract base class for a Form that is used to display info about a piece of Equipment in a HudWindow.
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
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for a Form that is used to display info about a piece of Equipment in a HudWindow.
/// </summary>
public abstract class AEquipmentForm : AForm {

    private AEquipmentStat _equipmentStat;
    public AEquipmentStat EquipmentStat {
        get { return _equipmentStat; }
        set {
            D.AssertNull(_equipmentStat);  // occurs only once between Resets
            SetProperty<AEquipmentStat>(ref _equipmentStat, value, "EquipmentStat", EquipmentStatPropSetHandler);
        }
    }

    protected UILabel _nameLabel;
    protected NetIncomeGuiElement _netIncomeElement;
    protected UILabel _cultureLabel;
    protected UILabel _scienceLabel;
    protected StrengthGuiElement _defensiveStrengthElement;
    protected StrengthGuiElement _offensiveStrengthElement;
    protected HealthGuiElement _healthElement;
    protected UILabel _speedLabel;
    protected ProductionGuiElement _productionElement;

    private OwnerGuiElement _ownerElement;
    private IDictionary<GuiElementID, AGuiElement> _guiElementsPresent;

    protected sealed override void InitializeValuesAndReferences() {
        var guiElementsPresent = gameObject.GetSafeComponentsInChildren<AGuiElement>();
        _guiElementsPresent = guiElementsPresent.ToDictionary<AGuiElement, GuiElementID>(e => e.ElementID, GuiElementIDEqualityComparer.Default);
        _guiElementsPresent.Values.ForAll(e => InitializeGuiElement(e));
        InitializeNonGuiElementMembers();
    }

    private void InitializeGuiElement(AGuiElement e) {
        switch (e.ElementID) {
            case GuiElementID.ItemNameLabel:
                InitializeNameGuiElement(e);
                break;
            case GuiElementID.Owner:
                InitializeOwnerGuiElement(e);
                break;
            case GuiElementID.Health:
                InitializeHealthGuiElement(e);
                break;
            case GuiElementID.OffensiveStrength:
                InitializeOffensiveStrengthGuiElement(e);
                break;
            case GuiElementID.DefensiveStrength:
                InitializeDefensiveStrengthGuiElement(e);
                break;
            case GuiElementID.ScienceLabel:
                InitializeScienceGuiElement(e);
                break;
            case GuiElementID.CultureLabel:
                InitializeCultureGuiElement(e);
                break;
            case GuiElementID.NetIncome:
                InitializeNetIncomeGuiElement(e);
                break;
            case GuiElementID.Production:
                InitializeProductionGuiElement(e);
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(e.ElementID));
        }
    }

    private void InitializeProductionGuiElement(AGuiElement e) {
        _productionElement = e as ProductionGuiElement;
    }

    private void InitializeNetIncomeGuiElement(AGuiElement e) {
        _netIncomeElement = e as NetIncomeGuiElement;
    }

    private void InitializeCultureGuiElement(AGuiElement e) {
        _cultureLabel = GetLabel(e);
    }

    private void InitializeScienceGuiElement(AGuiElement e) {
        _scienceLabel = GetLabel(e);
    }

    private void InitializeDefensiveStrengthGuiElement(AGuiElement e) {
        _defensiveStrengthElement = e as StrengthGuiElement;
    }

    private void InitializeOffensiveStrengthGuiElement(AGuiElement e) {
        _offensiveStrengthElement = e as StrengthGuiElement;
    }

    private void InitializeHealthGuiElement(AGuiElement e) {
        _healthElement = e as HealthGuiElement;
    }

    private void InitializeOwnerGuiElement(AGuiElement e) {
        _ownerElement = e as OwnerGuiElement;
    }

    protected virtual void InitializeNameGuiElement(AGuiElement e) {
        _nameLabel = GetLabel(e);
    }

    protected virtual void InitializeNonGuiElementMembers() { }

    #region Event and Property Change Handlers

    private void EquipmentStatPropSetHandler() {
        AssignValuesToMembers();
    }

    #endregion

    protected sealed override void AssignValuesToMembers() {
        _guiElementsPresent.Keys.ForAll(id => AssignValueTo(id));
        AssignValuesToNonGuiElementMembers();
    }

    private void AssignValueTo(GuiElementID id) {
        switch (id) {
            case GuiElementID.ItemNameLabel:
                AssignValueToNameGuiElement();
                break;
            case GuiElementID.Owner:
                AssignValueToOwnerGuiElement();
                break;
            case GuiElementID.CultureLabel:
                AssignValueToCultureGuiElement();
                break;
            case GuiElementID.DefensiveStrength:
                AssignValueToDefensiveStrengthGuiElement();
                break;
            case GuiElementID.Health:
                AssignValueToHealthGuiElement();
                break;
            case GuiElementID.NetIncome:
                AssignValueToNetIncomeGuiElement();
                break;
            case GuiElementID.OffensiveStrength:
                AssignValueToOffensiveStrengthGuiElement();
                break;
            case GuiElementID.Production:
                AssignValueToProductionGuiElement();
                break;
            case GuiElementID.ScienceLabel:
                AssignValueToScienceGuiElement();
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(id));
        }
    }

    protected virtual void AssignValueToScienceGuiElement() { }
    protected virtual void AssignValueToProductionGuiElement() { }
    protected virtual void AssignValueToOffensiveStrengthGuiElement() { }
    protected virtual void AssignValueToNetIncomeGuiElement() { }
    protected virtual void AssignValueToHealthGuiElement() { }
    protected virtual void AssignValueToDefensiveStrengthGuiElement() { }
    protected virtual void AssignValueToCultureGuiElement() { }

    private void AssignValueToOwnerGuiElement() { _ownerElement.Owner = GameManager.Instance.UserPlayer; }

    protected virtual void AssignValueToNameGuiElement() { _nameLabel.text = EquipmentStat.Name; }

    protected virtual void AssignValuesToNonGuiElementMembers() { }

    public override void Reset() {
        _equipmentStat = null;
    }

    private void CleanupGuiElement(AGuiElement e) {
        switch (e.ElementID) {
            case GuiElementID.ItemNameLabel:
                CleanupNameGuiElement(e);
                break;
            case GuiElementID.Owner:
                CleanupOwnerGuiElement(e);
                break;
            case GuiElementID.Health:
                CleanupHealthGuiElement(e);
                break;
            case GuiElementID.OffensiveStrength:
                CleanupOffensiveStrengthGuiElement(e);
                break;
            case GuiElementID.DefensiveStrength:
                CleanupDefensiveStrengthGuiElement(e);
                break;
            case GuiElementID.ScienceLabel:
                CleanupScienceGuiElement(e);
                break;
            case GuiElementID.CultureLabel:
                CleanupCultureGuiElement(e);
                break;
            case GuiElementID.NetIncome:
                CleanupNetIncomeGuiElement(e);
                break;
            case GuiElementID.Production:
                CleanupProductionGuiElement(e);
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(e.ElementID));
        }
    }

    protected virtual void CleanupNameGuiElement(AGuiElement e) { }
    protected virtual void CleanupOwnerGuiElement(AGuiElement e) { }
    protected virtual void CleanupHealthGuiElement(AGuiElement e) { }
    protected virtual void CleanupOffensiveStrengthGuiElement(AGuiElement e) { }
    protected virtual void CleanupDefensiveStrengthGuiElement(AGuiElement e) { }
    protected virtual void CleanupScienceGuiElement(AGuiElement e) { }
    protected virtual void CleanupCultureGuiElement(AGuiElement e) { }
    protected virtual void CleanupNetIncomeGuiElement(AGuiElement e) { }
    protected virtual void CleanupProductionGuiElement(AGuiElement e) { }

    protected virtual void CleanupNonGuiElementMembers() { }

    protected override void Cleanup() {
        _guiElementsPresent.Values.ForAll(e => CleanupGuiElement(e));
        CleanupNonGuiElementMembers();
    }

}

