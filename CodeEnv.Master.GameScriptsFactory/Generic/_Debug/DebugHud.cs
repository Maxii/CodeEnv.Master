// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugHud.cs
// Stationary HUD supporting Debug data on the screen.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// Stationary HUD supporting Debug data on the screen.
/// </summary>
public sealed class DebugHud : AGuiHud<DebugHud>, IDebugHud {

    private void OnDebugHudTextChanged() {
        Set(DebugHudText);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDebugHud Members

    private DebugHudText _debugHudText;
    public DebugHudText DebugHudText {
        get { return _debugHudText = _debugHudText ?? new DebugHudText(); }
    }

    public void Set(DebugHudText debugHudText) {
        Set(debugHudText.GetText());
    }

    #endregion

}

