// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AInfoDisplayForm.cs
// Abstract base class for Forms that are capable of displaying info.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for Forms that are capable of displaying info.
/// </summary>
public abstract class AInfoDisplayForm : AForm {

    protected UILabel _nameLabel;
    protected NetIncomeGuiElement _netIncomeElement;
    protected UILabel _cultureLabel;
    protected UILabel _scienceLabel;
    protected StrengthGuiElement _defensiveStrengthElement;
    protected StrengthGuiElement _offensiveStrengthElement;
    protected HealthGuiElement _healthElement;
    protected HeroGuiElement _heroElement;
    protected LocationGuiElement _locationElement;
    protected UILabel _speedLabel;
    protected ResourcesGuiElement _resourcesElement;
    protected UILabel _organicsLabel;
    protected UILabel _particulatesLabel;
    protected UILabel _energyLabel;
    protected UILabel _populationLabel;
    protected ApprovalGuiElement _approvalElement;
    protected ProductionGuiElement _productionElement;
    protected OwnerGuiElement _ownerElement;

    private IDictionary<GuiElementID, AGuiElement> _guiElementsPresent;

    protected sealed override void InitializeValuesAndReferences() {
        var guiElementsPresent = gameObject.GetSafeComponentsInChildren<AGuiElement>();
        _guiElementsPresent = guiElementsPresent.ToDictionary<AGuiElement, GuiElementID>(e => e.ElementID, GuiElementIDEqualityComparer.Default);
        foreach (var guiElement in _guiElementsPresent.Values) {
            bool isFound = InitializeGuiElement(guiElement);
            D.Assert(isFound, guiElement.ElementID.GetValueName());
        }
        InitializeNonGuiElementMembers();
    }

    protected virtual bool InitializeGuiElement(AGuiElement e) {
        bool isFound = true;
        switch (e.ElementID) {
            case GuiElementID.NameLabel:
                InitializeNameGuiElement(e);
                break;
            case GuiElementID.Owner:
                InitializeOwnerGuiElement(e);
                break;
            case GuiElementID.Location:
                InitializeLocationGuiElement(e);
                break;
            case GuiElementID.Hero:
                InitializeHeroGuiElement(e);
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
            case GuiElementID.Resources:
                InitializeStrategicResourcesGuiElement(e);
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
            case GuiElementID.OrganicsLabel:
                InitializeOrganicsGuiElement(e);
                break;
            case GuiElementID.ParticulatesLabel:
                InitializeParticulatesGuiElement(e);
                break;
            case GuiElementID.EnergyLabel:
                InitializeEnergyGuiElement(e);
                break;
            case GuiElementID.Composition:
                InitializeCompositionGuiElement(e);
                break;
            case GuiElementID.Approval:
                InitializeApprovalGuiElement(e);
                break;
            case GuiElementID.PopulationLabel:
                InitializePopulationGuiElement(e);
                break;
            case GuiElementID.Production:
                InitializeProductionGuiElement(e);
                break;
            case GuiElementID.SpeedLabel:
                InitializeSpeedGuiElement(e);
                break;
            default:
                isFound = false;
                break;
        }
        return isFound;
    }

    private void InitializeSpeedGuiElement(AGuiElement e) {
        _speedLabel = GetLabel(e);
    }

    private void InitializeProductionGuiElement(AGuiElement e) {
        _productionElement = e as ProductionGuiElement;
    }

    private void InitializePopulationGuiElement(AGuiElement e) {
        _populationLabel = GetLabel(e);
    }

    private void InitializeApprovalGuiElement(AGuiElement e) {
        _approvalElement = e as ApprovalGuiElement;
    }

    protected virtual void InitializeCompositionGuiElement(AGuiElement e) { }

    private void InitializeEnergyGuiElement(AGuiElement e) {
        _energyLabel = GetLabel(e);
    }

    private void InitializeParticulatesGuiElement(AGuiElement e) {
        _particulatesLabel = GetLabel(e);
    }

    private void InitializeOrganicsGuiElement(AGuiElement e) {
        _organicsLabel = GetLabel(e);
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

    private void InitializeStrategicResourcesGuiElement(AGuiElement e) {
        _resourcesElement = e as ResourcesGuiElement;
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

    private void InitializeHeroGuiElement(AGuiElement e) {
        _heroElement = e as HeroGuiElement;
    }

    private void InitializeLocationGuiElement(AGuiElement e) {
        _locationElement = e as LocationGuiElement;
    }

    private void InitializeOwnerGuiElement(AGuiElement e) {
        _ownerElement = e as OwnerGuiElement;
    }

    protected virtual void InitializeNameGuiElement(AGuiElement e) {
        _nameLabel = GetLabel(e);
    }

    protected virtual void InitializeNonGuiElementMembers() { }

    #region Event and Property Change Handlers

    private void ReportPropSetHandler() {
        AssignValuesToMembers();
    }

    #endregion

    protected sealed override void AssignValuesToMembers() {
        foreach (GuiElementID id in _guiElementsPresent.Keys) {
            bool isFound = AssignValueTo(id);
            D.Assert(isFound, id.GetValueName());
        }
        AssignValuesToNonGuiElementMembers();
    }

    protected virtual bool AssignValueTo(GuiElementID id) {
        bool isFound = true;
        switch (id) {
            case GuiElementID.NameLabel:
                AssignValueToNameGuiElement();
                break;
            case GuiElementID.Owner:
                AssignValueToOwnerGuiElement();
                break;
            case GuiElementID.Approval:
                AssignValueToApprovalGuiElement();
                break;
            case GuiElementID.Composition:
                AssignValueToCompositionGuiElement();
                break;
            case GuiElementID.CultureLabel:
                AssignValueToCultureGuiElement();
                break;
            case GuiElementID.DefensiveStrength:
                AssignValueToDefensiveStrengthGuiElement();
                break;
            case GuiElementID.EnergyLabel:
                AssignValueToEnergyGuiElement();
                break;
            case GuiElementID.Health:
                AssignValueToHealthGuiElement();
                break;
            case GuiElementID.Hero:
                AssignValueToHeroGuiElement();
                break;
            case GuiElementID.Location:
                AssignValueToLocationGuiElement();
                break;
            case GuiElementID.NetIncome:
                AssignValueToNetIncomeGuiElement();
                break;
            case GuiElementID.OffensiveStrength:
                AssignValueToOffensiveStrengthGuiElement();
                break;
            case GuiElementID.OrganicsLabel:
                AssignValueToOrganicsGuiElement();
                break;
            case GuiElementID.ParticulatesLabel:
                AssignValueToParticulatesGuiElement();
                break;
            case GuiElementID.PopulationLabel:
                AssignValueToPopulationGuiElement();
                break;
            case GuiElementID.Production:
                AssignValueToProductionGuiElement();
                break;
            case GuiElementID.ScienceLabel:
                AssignValueToScienceGuiElement();
                break;
            case GuiElementID.SpeedLabel:
                AssignValueToSpeedGuiElement();
                break;
            case GuiElementID.Resources:
                AssignValueToStrategicResourcesGuiElement();
                break;
            default:
                isFound = false;
                break;
        }
        return isFound;
    }

    protected virtual void AssignValueToStrategicResourcesGuiElement() { }
    protected virtual void AssignValueToSpeedGuiElement() { }
    protected virtual void AssignValueToScienceGuiElement() { }
    protected virtual void AssignValueToProductionGuiElement() { }
    protected virtual void AssignValueToPopulationGuiElement() { }
    protected virtual void AssignValueToParticulatesGuiElement() { }
    protected virtual void AssignValueToOrganicsGuiElement() { }
    protected virtual void AssignValueToOffensiveStrengthGuiElement() { }
    protected virtual void AssignValueToNetIncomeGuiElement() { }
    protected virtual void AssignValueToLocationGuiElement() { }
    protected virtual void AssignValueToHeroGuiElement() { }
    protected virtual void AssignValueToHealthGuiElement() { }
    protected virtual void AssignValueToEnergyGuiElement() { }
    protected virtual void AssignValueToDefensiveStrengthGuiElement() { }
    protected virtual void AssignValueToCultureGuiElement() { }
    protected virtual void AssignValueToCompositionGuiElement() { }
    protected virtual void AssignValueToApprovalGuiElement() { }
    protected virtual void AssignValueToOwnerGuiElement() { }
    protected virtual void AssignValueToNameGuiElement() { }

    protected virtual void AssignValuesToNonGuiElementMembers() { }

    protected virtual bool CleanupGuiElement(AGuiElement e) {
        bool isFound = true;
        switch (e.ElementID) {
            case GuiElementID.NameLabel:
                CleanupNameGuiElement(e);
                break;
            case GuiElementID.Owner:
                CleanupOwnerGuiElement(e);
                break;
            case GuiElementID.Location:
                CleanupLocationGuiElement(e);
                break;
            case GuiElementID.Hero:
                CleanupHeroGuiElement(e);
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
            case GuiElementID.Resources:
                CleanupStrategicResourcesGuiElement(e);
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
            case GuiElementID.OrganicsLabel:
                CleanupOrganicsGuiElement(e);
                break;
            case GuiElementID.ParticulatesLabel:
                CleanupParticulatesGuiElement(e);
                break;
            case GuiElementID.EnergyLabel:
                CleanupEnergyGuiElement(e);
                break;
            case GuiElementID.Composition:
                CleanupCompositionGuiElement(e);
                break;
            case GuiElementID.Approval:
                CleanupApprovalGuiElement(e);
                break;
            case GuiElementID.PopulationLabel:
                CleanupPopulationGuiElement(e);
                break;
            case GuiElementID.Production:
                CleanupProductionGuiElement(e);
                break;
            case GuiElementID.SpeedLabel:
                CleanupSpeedGuiElement(e);
                break;
            default:
                isFound = false;
                break;
        }
        return isFound;
    }

    protected virtual void CleanupNameGuiElement(AGuiElement e) { }
    protected virtual void CleanupOwnerGuiElement(AGuiElement e) { }
    protected virtual void CleanupLocationGuiElement(AGuiElement e) { }
    protected virtual void CleanupHeroGuiElement(AGuiElement e) { }
    protected virtual void CleanupHealthGuiElement(AGuiElement e) { }
    protected virtual void CleanupOffensiveStrengthGuiElement(AGuiElement e) { }
    protected virtual void CleanupDefensiveStrengthGuiElement(AGuiElement e) { }
    protected virtual void CleanupStrategicResourcesGuiElement(AGuiElement e) { }
    protected virtual void CleanupScienceGuiElement(AGuiElement e) { }
    protected virtual void CleanupCultureGuiElement(AGuiElement e) { }
    protected virtual void CleanupNetIncomeGuiElement(AGuiElement e) { }
    protected virtual void CleanupOrganicsGuiElement(AGuiElement e) { }
    protected virtual void CleanupParticulatesGuiElement(AGuiElement e) { }
    protected virtual void CleanupEnergyGuiElement(AGuiElement e) { }
    protected virtual void CleanupCompositionGuiElement(AGuiElement e) { }
    protected virtual void CleanupApprovalGuiElement(AGuiElement e) { }
    protected virtual void CleanupPopulationGuiElement(AGuiElement e) { }
    protected virtual void CleanupProductionGuiElement(AGuiElement e) { }
    protected virtual void CleanupSpeedGuiElement(AGuiElement e) { }

    protected virtual void CleanupNonGuiElementMembers() { }

    protected override void Cleanup() {
        foreach (var guiElement in _guiElementsPresent.Values) {
            bool isFound = CleanupGuiElement(guiElement);
            D.Assert(isFound, guiElement.ElementID.GetValueName());
        }
        CleanupNonGuiElementMembers();
    }

}

