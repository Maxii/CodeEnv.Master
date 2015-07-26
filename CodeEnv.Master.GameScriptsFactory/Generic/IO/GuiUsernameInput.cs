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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

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
    }

    private void InitializeValuesAndReferences() {
        _inputField = gameObject.GetSafeMonoBehaviour<UIInput>();
        _inputField.value = PlayerPrefsManager.Instance.Username;
        EventDelegate.Add(_inputField.onSubmit, OnUsernameSubmitted);
    }

    private void OnUsernameSubmitted() {
        PlayerPrefsManager.Instance.Username = _inputField.value;
        _inputField.isSelected = false; // stops the caret blinking
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        EventDelegate.Remove(_inputField.onSubmit, OnUsernameSubmitted);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

