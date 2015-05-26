// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiPlayerColorPopupList.cs
// Player Color selection popup list in the Gui.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Player Color selection popup list in the Gui.
/// </summary>
public class GuiPlayerColorPopupList : AGuiPopupList<GameColor> {

    public GuiElementID elementID;

    private GameColor _defaultSelection;
    public GameColor DefaultSelection {
        get { return _defaultSelection; }
        set { SetProperty<GameColor>(ref _defaultSelection, value, "DefaultSelection", OnDefaultSelectionChanged); }
    }

    public override GuiElementID ElementID { get { return elementID; } }

    protected override string[] NameValues {
        get {
            return Enums<GameColor>.GetNamesExcept(default(GameColor),
                GameColor.Clear, GameColor.Gray, GameColor.Black, GameColor.White);
        }
    }

    /// <summary>
    /// Removes the provided color from the available choices within the popup list.
    /// </summary>
    /// <param name="color">The color.</param>
    public void RemoveColor(GameColor color) {
        RemoveNameValue(color.GetName());
    }

    private void OnDefaultSelectionChanged() {
        DefaultSelectionValue = DefaultSelection.GetName();
    }

    // no need for taking an action OnPopupListSelectionChanged as changes aren't recorded 
    // from this popup list until the Menu Accept Button is pushed

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

