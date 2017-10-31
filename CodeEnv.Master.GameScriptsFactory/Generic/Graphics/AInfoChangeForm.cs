// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AInfoChangeForm.cs
// Abstract base class for Forms that are capable of displaying and changing info.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for Forms that are capable of displaying and changing info.
/// </summary>
public abstract class AInfoChangeForm : AInfoDisplayForm {

    protected CombatStanceChangeGuiElement _combatStanceChgGuiElement;
    protected HeroChangeGuiElement _heroChgGuiElement;
    protected NameChangeGuiElement _nameChgGuiElement;
    protected FormationChangeGuiElement _formationChgGuiElement;

    protected sealed override bool InitializeGuiElement(AGuiElement e) {
        bool isFound = base.InitializeGuiElement(e);
        if (!isFound) {
            isFound = true;
            switch (e.ElementID) {
                case GuiElementID.NameChange:
                    InitializeNameChangeGuiElement(e);
                    break;
                case GuiElementID.FormationChange:
                    InitializeFormationChangeGuiElement(e);
                    break;
                case GuiElementID.HeroChange:
                    InitializeHeroChangeGuiElement(e);
                    break;
                case GuiElementID.CombatStanceChange:
                    InitializeCombatStanceChangeGuiElement(e);
                    break;
                default:
                    isFound = false;
                    break;
            }
        }
        return isFound;
    }

    private void InitializeNameChangeGuiElement(AGuiElement e) {
        _nameChgGuiElement = e as NameChangeGuiElement;
    }

    private void InitializeFormationChangeGuiElement(AGuiElement e) {
        _formationChgGuiElement = e as FormationChangeGuiElement;
    }

    private void InitializeHeroChangeGuiElement(AGuiElement e) {
        _heroChgGuiElement = e as HeroChangeGuiElement;
    }

    private void InitializeCombatStanceChangeGuiElement(AGuiElement e) {
        _combatStanceChgGuiElement = e as CombatStanceChangeGuiElement;
    }

    #region Event and Property Change Handlers

    #endregion

    protected sealed override bool AssignValueTo(GuiElementID id) {
        bool isFound = base.AssignValueTo(id);
        if (!isFound) {
            isFound = true;
            switch (id) {
                case GuiElementID.NameChange:
                    AssignValueToNameChangeGuiElement();
                    break;
                case GuiElementID.FormationChange:
                    AssignValueToFormationChangeGuiElement();
                    break;
                case GuiElementID.HeroChange:
                    AssignValueToHeroChangeGuiElement();
                    break;
                case GuiElementID.CombatStanceChange:
                    AssignValueToCombatStanceChangeGuiElement();
                    break;
                default:
                    isFound = false;
                    break;
            }
        }
        return isFound;
    }

    protected virtual void AssignValueToNameChangeGuiElement() { }

    protected virtual void AssignValueToFormationChangeGuiElement() { }

    protected virtual void AssignValueToHeroChangeGuiElement() { }

    protected virtual void AssignValueToCombatStanceChangeGuiElement() { }

    protected override bool ResetForReuse(GuiElementID id) {
        bool isFound = base.ResetForReuse(id);
        if (!isFound) {
            isFound = true;
            switch (id) {
                case GuiElementID.NameChange:
                    ResetNameChangeGuiElement();
                    break;
                case GuiElementID.FormationChange:
                    ResetFormationChangeGuiElement();
                    break;
                case GuiElementID.HeroChange:
                    ResetHeroChangeGuiElement();
                    break;
                case GuiElementID.CombatStanceChange:
                    ResetCombatStanceChangeGuiElement();
                    break;
                default:
                    isFound = false;
                    break;
            }
        }
        return isFound;
    }

    private void ResetNameChangeGuiElement() {
        _nameChgGuiElement.ResetForReuse();
    }

    private void ResetFormationChangeGuiElement() {
        _formationChgGuiElement.ResetForReuse();
    }

    private void ResetHeroChangeGuiElement() {
        _heroChgGuiElement.ResetForReuse();
    }

    private void ResetCombatStanceChangeGuiElement() {
        _combatStanceChgGuiElement.ResetForReuse();
    }

    protected sealed override bool CleanupGuiElement(AGuiElement e) {
        bool isFound = base.CleanupGuiElement(e);
        if (!isFound) {
            isFound = true;
            switch (e.ElementID) {
                case GuiElementID.NameChange:
                    CleanupNameChangeGuiElement(e);
                    break;
                case GuiElementID.FormationChange:
                    CleanupFormationChangeGuiElement(e);
                    break;
                case GuiElementID.HeroChange:
                    CleanupHeroChangeGuiElement(e);
                    break;
                case GuiElementID.CombatStanceChange:
                    CleanupCombatStanceChangeGuiElement(e);
                    break;
                default:
                    isFound = false;
                    break;
            }
        }
        return isFound;
    }

    protected virtual void CleanupNameChangeGuiElement(AGuiElement e) { }

    protected virtual void CleanupFormationChangeGuiElement(AGuiElement e) { }

    protected virtual void CleanupHeroChangeGuiElement(AGuiElement e) { }

    protected virtual void CleanupCombatStanceChangeGuiElement(AGuiElement e) { }

}

