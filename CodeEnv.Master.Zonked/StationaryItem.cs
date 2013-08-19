// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StationaryItem.cs
// Lowest level instantiable class for stationary items in the universe that the camera can focus on.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// Lowest level instantiable class for stationary items in the universe that the camera can focus
/// on. Also provides support for the GuiCursorHud if there is Data for the item.
/// </summary>
public class StationaryItem : AFocusableItem {

    private GuiCursorHud _guiCursorHud;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        _guiCursorHud = GuiCursorHud.Instance;
    }

    public override void DisplayCursorHud() {
        _guiCursorHud.Set(HudPublisher.GetHudText(HumanPlayerIntelLevel));
    }

    public override void ClearCursorHud() {
        _guiCursorHud.Clear();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

