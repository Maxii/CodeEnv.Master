// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiUsernameInput.cs
// Gui Control for inputing the name of the user.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Gui Control for inputing the name of the user.
/// </summary>
public class GuiUsernameInput : ATextTooltip {

    protected override string TooltipContent { get { return "Enter your Username."; } }

    private UIInput _inputField;

    protected override void Awake() {
        base.Awake();
        InitializeValuesAndReferences();
        Subscribe();
    }

    private void InitializeValuesAndReferences() {
        _inputField = gameObject.GetComponent<UIInput>();
        _inputField.value = PlayerPrefsManager.Instance.Username;
    }

    private void Subscribe() {
        EventDelegate.Add(_inputField.onSubmit, InputFieldSubmitEventHandler);
    }

    #region Event and Property Change Handlers

    private void InputFieldSubmitEventHandler() {
        RecordUsername();
    }

    #endregion

    private void RecordUsername() { // IMPROVE illegal character filtering
        PlayerPrefsManager.Instance.Username = _inputField.value;
        _inputField.isSelected = false; // stops the caret blinking
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        EventDelegate.Remove(_inputField.onSubmit, InputFieldSubmitEventHandler);
    }


}

