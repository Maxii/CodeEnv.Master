// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiInputModeControlButton.cs
// Changes the InputMode to that selected on Button LeftClick.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Changes the InputMode to that selected on Button LeftClick.
/// </summary>
public class GuiInputModeControlButton : AGuiButton {

    public GameInputMode inputModeOnClick;

    protected override void OnLeftClick() {
        if (inputModeOnClick == default(GameInputMode)) {
            D.WarnContext("{0} has not set {1}.".Inject(GetType().Name, typeof(GameInputMode).Name), gameObject);
        }
        D.Log("{0} is about to set InputMode to {1}.", GetType().Name, inputModeOnClick.GetName());
        InputManager.Instance.InputMode = inputModeOnClick;
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

