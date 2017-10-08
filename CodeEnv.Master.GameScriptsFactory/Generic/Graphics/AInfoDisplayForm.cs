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

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for Forms that are capable of displaying info.
/// </summary>
public abstract class AInfoDisplayForm : AForm {

    protected UILabel _nameLabel;
    protected OwnerIconGuiElement _ownerGuiElement;
    protected HealthGuiElement _healthGuiElement;
    protected LocationGuiElement _locationGuiElement;
    protected NetIncomeGuiElement _netIncomeGuiElement;
    protected UILabel _cultureLabel;
    protected UILabel _scienceLabel;
    protected StrengthGuiElement _defensiveStrengthGuiElement;
    protected StrengthGuiElement _offensiveStrengthGuiElement;
    protected HeroIconGuiElement _heroGuiElement;
    protected UILabel _speedLabel;
    protected ResourcesGuiElement _resourcesGuiElement;
    protected UILabel _organicsLabel;
    protected UILabel _particulatesLabel;
    protected UILabel _energyLabel;
    protected UILabel _populationLabel;
    protected ApprovalGuiElement _approvalGuiElement;
    protected ConstructionIconGuiElement _constructionGuiElement;
    protected AUnitCompositionGuiElement _compositionGuiElement;

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

    /// <summary>
    /// Initializes the provided AGuiElement.
    /// <remarks>Called once from Awake.</remarks>
    /// </summary>
    /// <param name="e">The AGuiElement.</param>
    /// <returns></returns>
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
            case GuiElementID.Construction:
                InitializeConstructionGuiElement(e);
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

    /// <summary>
    /// Initializes the provided AGuiElement. Base version simply acquires the reference to the element.
    /// <remarks>Called once from Awake.</remarks>
    /// <remarks>Virtual to allow derived classes to further initialize the element, i.e. subscribe to events...</remarks>
    /// </summary>
    /// <param name="e">The AGuiElement.</param>
    protected virtual void InitializeSpeedGuiElement(AGuiElement e) {
        _speedLabel = GetLabel(e);
    }

    /// <summary>
    /// Initializes the provided AGuiElement. Base version simply acquires the reference to the element.
    /// <remarks>Called once from Awake.</remarks>
    /// <remarks>Virtual to allow derived classes to further initialize the element, i.e. subscribe to events...</remarks>
    /// </summary>
    /// <param name="e">The AGuiElement.</param>
    protected virtual void InitializeConstructionGuiElement(AGuiElement e) {
        _constructionGuiElement = e as ConstructionIconGuiElement;
    }

    /// <summary>
    /// Initializes the provided AGuiElement. Base version simply acquires the reference to the element.
    /// <remarks>Called once from Awake.</remarks>
    /// <remarks>Virtual to allow derived classes to further initialize the element, i.e. subscribe to events...</remarks>
    /// </summary>
    /// <param name="e">The AGuiElement.</param>
    protected virtual void InitializePopulationGuiElement(AGuiElement e) {
        _populationLabel = GetLabel(e);
    }

    /// <summary>
    /// Initializes the provided AGuiElement. Base version simply acquires the reference to the element.
    /// <remarks>Called once from Awake.</remarks>
    /// <remarks>Virtual to allow derived classes to further initialize the element, i.e. subscribe to events...</remarks>
    /// </summary>
    /// <param name="e">The AGuiElement.</param>
    protected virtual void InitializeApprovalGuiElement(AGuiElement e) {
        _approvalGuiElement = e as ApprovalGuiElement;
    }

    /// <summary>
    /// Initializes the provided AGuiElement. Base version simply acquires the reference to the element.
    /// <remarks>Called once from Awake.</remarks>
    /// <remarks>Virtual to allow derived classes to further initialize the element, i.e. subscribe to events...</remarks>
    /// </summary>
    /// <param name="e">The AGuiElement.</param>
    protected virtual void InitializeCompositionGuiElement(AGuiElement e) {
        _compositionGuiElement = e as AUnitCompositionGuiElement;
    }

    /// <summary>
    /// Initializes the provided AGuiElement. Base version simply acquires the reference to the element.
    /// <remarks>Called once from Awake.</remarks>
    /// <remarks>Virtual to allow derived classes to further initialize the element, i.e. subscribe to events...</remarks>
    /// </summary>
    /// <param name="e">The AGuiElement.</param>
    protected virtual void InitializeEnergyGuiElement(AGuiElement e) {
        _energyLabel = GetLabel(e);
    }

    /// <summary>
    /// Initializes the provided AGuiElement. Base version simply acquires the reference to the element.
    /// <remarks>Called once from Awake.</remarks>
    /// <remarks>Virtual to allow derived classes to further initialize the element, i.e. subscribe to events...</remarks>
    /// </summary>
    /// <param name="e">The AGuiElement.</param>
    protected virtual void InitializeParticulatesGuiElement(AGuiElement e) {
        _particulatesLabel = GetLabel(e);
    }

    /// <summary>
    /// Initializes the provided AGuiElement. Base version simply acquires the reference to the element.
    /// <remarks>Called once from Awake.</remarks>
    /// <remarks>Virtual to allow derived classes to further initialize the element, i.e. subscribe to events...</remarks>
    /// </summary>
    /// <param name="e">The AGuiElement.</param>
    protected virtual void InitializeOrganicsGuiElement(AGuiElement e) {
        _organicsLabel = GetLabel(e);
    }

    /// <summary>
    /// Initializes the provided AGuiElement. Base version simply acquires the reference to the element.
    /// <remarks>Called once from Awake.</remarks>
    /// <remarks>Virtual to allow derived classes to further initialize the element, i.e. subscribe to events...</remarks>
    /// </summary>
    /// <param name="e">The AGuiElement.</param>
    protected virtual void InitializeNetIncomeGuiElement(AGuiElement e) {
        _netIncomeGuiElement = e as NetIncomeGuiElement;
    }

    /// <summary>
    /// Initializes the provided AGuiElement. Base version simply acquires the reference to the element.
    /// <remarks>Called once from Awake.</remarks>
    /// <remarks>Virtual to allow derived classes to further initialize the element, i.e. subscribe to events...</remarks>
    /// </summary>
    /// <param name="e">The AGuiElement.</param>
    protected virtual void InitializeCultureGuiElement(AGuiElement e) {
        _cultureLabel = GetLabel(e);
    }

    /// <summary>
    /// Initializes the provided AGuiElement. Base version simply acquires the reference to the element.
    /// <remarks>Called once from Awake.</remarks>
    /// <remarks>Virtual to allow derived classes to further initialize the element, i.e. subscribe to events...</remarks>
    /// </summary>
    /// <param name="e">The AGuiElement.</param>
    protected virtual void InitializeScienceGuiElement(AGuiElement e) {
        _scienceLabel = GetLabel(e);
    }

    /// <summary>
    /// Initializes the provided AGuiElement. Base version simply acquires the reference to the element.
    /// <remarks>Called once from Awake.</remarks>
    /// <remarks>Virtual to allow derived classes to further initialize the element, i.e. subscribe to events...</remarks>
    /// </summary>
    /// <param name="e">The AGuiElement.</param>
    protected virtual void InitializeStrategicResourcesGuiElement(AGuiElement e) {
        _resourcesGuiElement = e as ResourcesGuiElement;
    }

    /// <summary>
    /// Initializes the provided AGuiElement. Base version simply acquires the reference to the element.
    /// <remarks>Called once from Awake.</remarks>
    /// <remarks>Virtual to allow derived classes to further initialize the element, i.e. subscribe to events...</remarks>
    /// </summary>
    /// <param name="e">The AGuiElement.</param>
    protected virtual void InitializeDefensiveStrengthGuiElement(AGuiElement e) {
        _defensiveStrengthGuiElement = e as StrengthGuiElement;
    }

    /// <summary>
    /// Initializes the provided AGuiElement. Base version simply acquires the reference to the element.
    /// <remarks>Called once from Awake.</remarks>
    /// <remarks>Virtual to allow derived classes to further initialize the element, i.e. subscribe to events...</remarks>
    /// </summary>
    /// <param name="e">The AGuiElement.</param>
    protected virtual void InitializeOffensiveStrengthGuiElement(AGuiElement e) {
        _offensiveStrengthGuiElement = e as StrengthGuiElement;
    }

    /// <summary>
    /// Initializes the provided AGuiElement. Base version simply acquires the reference to the element.
    /// <remarks>Called once from Awake.</remarks>
    /// <remarks>Virtual to allow derived classes to further initialize the element, i.e. subscribe to events...</remarks>
    /// </summary>
    /// <param name="e">The AGuiElement.</param>
    protected virtual void InitializeHealthGuiElement(AGuiElement e) {
        _healthGuiElement = e as HealthGuiElement;
    }

    /// <summary>
    /// Initializes the provided AGuiElement. Base version simply acquires the reference to the element.
    /// <remarks>Called once from Awake.</remarks>
    /// <remarks>Virtual to allow derived classes to further initialize the element, i.e. subscribe to events...</remarks>
    /// </summary>
    /// <param name="e">The AGuiElement.</param>
    protected virtual void InitializeHeroGuiElement(AGuiElement e) {
        _heroGuiElement = e as HeroIconGuiElement;
    }

    /// <summary>
    /// Initializes the provided AGuiElement. Base version simply acquires the reference to the element.
    /// <remarks>Called once from Awake.</remarks>
    /// <remarks>Virtual to allow derived classes to further initialize the element, i.e. subscribe to events...</remarks>
    /// </summary>
    /// <param name="e">The AGuiElement.</param>
    protected virtual void InitializeLocationGuiElement(AGuiElement e) {
        _locationGuiElement = e as LocationGuiElement;
    }

    /// <summary>
    /// Initializes the provided AGuiElement. Base version simply acquires the reference to the element.
    /// <remarks>Called once from Awake.</remarks>
    /// <remarks>Virtual to allow derived classes to further initialize the element, i.e. subscribe to events...</remarks>
    /// </summary>
    /// <param name="e">The AGuiElement.</param>
    protected virtual void InitializeOwnerGuiElement(AGuiElement e) {
        _ownerGuiElement = e as OwnerIconGuiElement;
    }

    /// <summary>
    /// Initializes the provided AGuiElement. Base version simply acquires the reference to the element.
    /// <remarks>Called once from Awake.</remarks>
    /// <remarks>Virtual to allow derived classes to further initialize the element, i.e. subscribe to events...</remarks>
    /// </summary>
    /// <param name="e">The AGuiElement.</param>
    protected virtual void InitializeNameGuiElement(AGuiElement e) {
        _nameLabel = GetLabel(e);
    }

    /// <summary>
    /// Hook for derived classes to initialize any non-GuiElement members.
    /// <remarks>Called once from Awake.</remarks>
    /// </summary>
    protected virtual void InitializeNonGuiElementMembers() { }

    /// <summary>
    /// Returns the single UILabel component that is present with or a child of the provided GuiElement's GameObject.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns></returns>
    private UILabel GetLabel(AGuiElement element) {
        return element.gameObject.GetSingleComponentInChildren<UILabel>();
    }

    protected sealed override void AssignValuesToMembers() {
        foreach (GuiElementID id in _guiElementsPresent.Keys) {
            bool isFound = AssignValueTo(id);
            D.Assert(isFound, id.GetValueName());
        }
        AssignValuesToNonGuiElementMembers();
    }

    /// <summary>
    /// Opportunity for derived classes to assign values to a GuiElement.
    /// <remarks>Called when the source of these values is set, aka when the 
    /// Report or Data property is set.</remarks>
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns></returns>
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
            case GuiElementID.Construction:
                AssignValueToConstructionGuiElement();
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
    protected virtual void AssignValueToConstructionGuiElement() { }
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


    protected override void ResetForReuse_Internal() {
        foreach (GuiElementID id in _guiElementsPresent.Keys) {
            bool isFound = ResetForReuse(id);
            D.Assert(isFound, id.GetValueName());
        }
        ResetNonGuiElementMembers();
    }

    /// <summary>
    /// Opportunity for derived classes to reset the value of a GuiElement.
    /// <remarks>Called from ResetForReuse_Internal().</remarks>
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns></returns>
    protected virtual bool ResetForReuse(GuiElementID id) {
        bool isFound = true;
        switch (id) {
            case GuiElementID.NameLabel:
                ResetNameGuiElement();
                break;
            case GuiElementID.Owner:
                ResetOwnerGuiElement();
                break;
            case GuiElementID.Location:
                ResetLocationGuiElement();
                break;
            case GuiElementID.Hero:
                ResetHeroGuiElement();
                break;
            case GuiElementID.Health:
                ResetHealthGuiElement();
                break;
            case GuiElementID.OffensiveStrength:
                ResetOffensiveStrengthGuiElement();
                break;
            case GuiElementID.DefensiveStrength:
                ResetDefensiveStrengthGuiElement();
                break;
            case GuiElementID.Resources:
                ResetStrategicResourcesGuiElement();
                break;
            case GuiElementID.ScienceLabel:
                ResetScienceGuiElement();
                break;
            case GuiElementID.CultureLabel:
                ResetCultureGuiElement();
                break;
            case GuiElementID.NetIncome:
                ResetNetIncomeGuiElement();
                break;
            case GuiElementID.OrganicsLabel:
                ResetOrganicsGuiElement();
                break;
            case GuiElementID.ParticulatesLabel:
                ResetParticulatesGuiElement();
                break;
            case GuiElementID.EnergyLabel:
                ResetEnergyGuiElement();
                break;
            case GuiElementID.Composition:
                ResetCompositionGuiElement();
                break;
            case GuiElementID.Approval:
                ResetApprovalGuiElement();
                break;
            case GuiElementID.PopulationLabel:
                ResetPopulationGuiElement();
                break;
            case GuiElementID.Construction:
                ResetConstructionGuiElement();
                break;
            case GuiElementID.SpeedLabel:
                ResetSpeedGuiElement();
                break;
            default:
                isFound = false;
                break;
        }
        return isFound;
    }

    private void ResetNameGuiElement() {
        _nameLabel.text = null;
    }

    private void ResetOwnerGuiElement() {
        _ownerGuiElement.ResetForReuse();
    }

    private void ResetLocationGuiElement() {
        _locationGuiElement.ResetForReuse();
    }

    private void ResetHeroGuiElement() {
        _heroGuiElement.ResetForReuse();
    }

    private void ResetHealthGuiElement() {
        _healthGuiElement.ResetForReuse();
    }

    private void ResetOffensiveStrengthGuiElement() {
        _offensiveStrengthGuiElement.ResetForReuse();
    }

    private void ResetDefensiveStrengthGuiElement() {
        _defensiveStrengthGuiElement.ResetForReuse();
    }

    private void ResetStrategicResourcesGuiElement() {
        _resourcesGuiElement.ResetForReuse();
    }

    private void ResetScienceGuiElement() {
        _scienceLabel.text = null;
    }

    private void ResetCultureGuiElement() {
        _cultureLabel.text = null;
    }

    private void ResetNetIncomeGuiElement() {
        _netIncomeGuiElement.ResetForReuse();
    }

    private void ResetOrganicsGuiElement() {
        _organicsLabel.text = null;
    }

    private void ResetParticulatesGuiElement() {
        _particulatesLabel.text = null;
    }

    private void ResetEnergyGuiElement() {
        _energyLabel.text = null;
    }

    private void ResetCompositionGuiElement() {
        _compositionGuiElement.ResetForReuse();
    }

    private void ResetApprovalGuiElement() {
        _approvalGuiElement.ResetForReuse();
    }

    private void ResetPopulationGuiElement() {
        _populationLabel.text = null;
    }

    private void ResetConstructionGuiElement() {
        _constructionGuiElement.ResetForReuse();
    }

    private void ResetSpeedGuiElement() {
        _speedLabel.text = null;
    }

    /// <summary>
    /// Hook for derived classes to reset any non-GuiElement members for reuse.
    /// </summary>
    protected virtual void ResetNonGuiElementMembers() { }

    /// <summary>
    /// Opportunity for derived classes to cleanup GuiElements.
    /// <remarks>Called by Cleanup when being destroyed.</remarks>
    /// </summary>
    /// <param name="e">The e.</param>
    /// <returns></returns>
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
            case GuiElementID.Construction:
                CleanupConstructionGuiElement(e);
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
    protected virtual void CleanupConstructionGuiElement(AGuiElement e) { }
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

