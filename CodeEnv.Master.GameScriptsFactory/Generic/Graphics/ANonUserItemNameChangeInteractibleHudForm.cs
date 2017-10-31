// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ANonUserItemNameChangeInteractibleHudForm.cs
// Abstract class for NonUser-owned Item Forms with name change capability used by the InteractibleHud.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract class for NonUser-owned Item Forms with name change capability used by the InteractibleHud.
/// </summary>
public abstract class ANonUserItemNameChangeInteractibleHudForm : ANonUserItemInteractibleHudForm {

    protected NameChangeGuiElement _nameChgGuiElement;

    protected sealed override bool InitializeGuiElement(AGuiElement e) {
        bool isFound = base.InitializeGuiElement(e);
        if (!isFound) {
            isFound = true;
            switch (e.ElementID) {
                case GuiElementID.NameChange:
                    InitializeNameChangeGuiElement(e);
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

    protected sealed override bool AssignValueTo(GuiElementID id) {
        bool isFound = base.AssignValueTo(id);
        if (!isFound) {
            isFound = true;
            switch (id) {
                case GuiElementID.NameChange:
                    AssignValueToNameChangeGuiElement();
                    break;
                default:
                    isFound = false;
                    break;
            }
        }
        return isFound;
    }

    protected virtual void AssignValueToNameChangeGuiElement() { }

    protected override bool ResetForReuse(GuiElementID id) {
        bool isFound = base.ResetForReuse(id);
        if (!isFound) {
            isFound = true;
            switch (id) {
                case GuiElementID.NameChange:
                    ResetNameChangeGuiElement();
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

    protected sealed override bool CleanupGuiElement(AGuiElement e) {
        bool isFound = base.CleanupGuiElement(e);
        if (!isFound) {
            isFound = true;
            switch (e.ElementID) {
                case GuiElementID.NameChange:
                    CleanupNameChangeGuiElement(e);
                    break;
                default:
                    isFound = false;
                    break;
            }
        }
        return isFound;
    }

    protected virtual void CleanupNameChangeGuiElement(AGuiElement e) { }

}

