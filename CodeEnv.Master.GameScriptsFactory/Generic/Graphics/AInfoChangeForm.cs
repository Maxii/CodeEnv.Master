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

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for Forms that are capable of displaying and changing info.
/// </summary>
public abstract class AInfoChangeForm : AInfoDisplayForm {

    protected UIInput _nameInput;

    protected sealed override bool InitializeGuiElement(AGuiElement e) {
        bool isFound = base.InitializeGuiElement(e);
        if (!isFound) {
            isFound = true;
            switch (e.ElementID) {
                case GuiElementID.NameInput:
                    InitializeNameInputGuiElement(e);
                    break;
                default:
                    isFound = false;
                    break;
            }
        }
        return isFound;
    }

    private void InitializeNameInputGuiElement(AGuiElement e) {
        _nameInput = GetInput(e);
        EventDelegate.Add(_nameInput.onSubmit, NameInputSubmittedEventHandler);
    }

    #region Event and Property Change Handlers

    private void NameInputSubmittedEventHandler() {
        HandleNameInputSubmitted();
    }

    #endregion

    protected virtual void HandleNameInputSubmitted() {
        SFXManager.Instance.PlaySFX(SfxClipID.Swipe);
    }

    protected sealed override bool AssignValueTo(GuiElementID id) {
        bool isFound = base.AssignValueTo(id);
        if (!isFound) {
            isFound = true;
            switch (id) {
                case GuiElementID.NameInput:
                    AssignValueToNameInputGuiElement();
                    break;
                default:
                    isFound = false;
                    break;
            }
        }
        return isFound;
    }

    protected virtual void AssignValueToNameInputGuiElement() { }

    protected sealed override bool CleanupGuiElement(AGuiElement e) {
        bool isFound = base.CleanupGuiElement(e);
        if (!isFound) {
            isFound = true;
            switch (e.ElementID) {
                case GuiElementID.NameInput:
                    CleanupNameInputGuiElement(e);
                    break;
                default:
                    isFound = false;
                    break;
            }
        }
        return isFound;
    }

    protected virtual void CleanupNameInputGuiElement(AGuiElement e) {
        EventDelegate.Remove(_nameInput.onSubmit, NameInputSubmittedEventHandler);
    }

    /// <summary>
    /// Returns the single UIInput component that is present with or a child of the provided GuiElement's GameObject.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns></returns>
    protected UIInput GetInput(AGuiElement element) {
        return element.gameObject.GetSingleComponentInChildren<UIInput>();
    }

}

