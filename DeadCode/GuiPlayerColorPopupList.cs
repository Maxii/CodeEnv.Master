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

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Player Color selection popup list in the Gui.
/// </summary>
public class GuiPlayerColorPopupList : AGuiMenuPopupList<GameColor> {

    public event Action<GameColor, GuiElementID> onSelectionChanged;

    public GuiElementID elementID;

    //private GameColor _defaultSelection;
    //public GameColor DefaultSelection {
    //    get { return _defaultSelection; }
    //    set { SetProperty<GameColor>(ref _defaultSelection, value, "DefaultSelection", OnDefaultSelectionChanged); }
    //}

    public override GuiElementID ElementID { get { return elementID; } }

    public GameColor SelectedColor { get { return Enums<GameColor>.Parse(_popupList.value); } }

    protected override string[] Choices {
        get { return Enums<GameColor>.GetNamesExcept(TempGameValues.UnAcceptablePlayerColors); }
    }

    /// <summary>
    /// Removes the provided color from the available choices within the popup list.
    /// </summary>
    /// <param name="color">The color.</param>
    public void RemoveColor(GameColor color) {
        //RemoveValueName(color.GetValueName());
        var colorName = color.GetValueName();
        D.Warn(!_popupList.items.Contains(colorName), "{0} attempting to remove {1} that is not present as a choice.", ElementID.GetValueName(), colorName);
        _popupList.RemoveItem(colorName);
        //RefreshSelection();
    }

    public void AddColor(GameColor color) {
        var colorName = color.GetValueName();
        D.Warn(_popupList.items.Contains(colorName), "{0} attempting to add {1} that is already present as a choice.", ElementID.GetValueName(), colorName);
        _popupList.AddItem(colorName);
        //InitializeSelection();
    }

    public void SetSelection(GameColor color) {
        string colorName = color.GetValueName();
        if (!_popupList.items.Contains(colorName)) {
            // the value is not among the available choices, probably because it was removed
            D.WarnContext("{0} Selection {1} not among available choices. Adding back.".Inject(ElementID.GetValueName(), colorName), gameObject);
            _popupList.AddItem(colorName);
        }
        _popupList.value = colorName;
    }

    //private void OnDefaultSelectionChanged() {
    //    string defaultSelectionName = DefaultSelection.GetValueName();
    //    if (!_popupList.items.Contains(defaultSelectionName)) {
    //        // if this selection name is not present in the available selection choices then it was previously removed.
    //        // Since this default change is from the pool of unused colors, it can safely be added back as an acceptable choice
    //        D.Log("{0} adding back previously removed {1} as a choice.", GetType().Name, defaultSelectionName);
    //        _popupList.AddItem(defaultSelectionName);
    //    }
    //    DefaultSelectionName = defaultSelectionName;
    //}

    // no need for taking an action OnPopupListSelectionChanged as changes aren't recorded 
    // from this popup list until the Menu Accept Button is pushed
    protected override void OnPopupListSelection() {
        base.OnPopupListSelection();
        if (onSelectionChangedByUser != null) {
            onSelectionChangedByUser(SelectedColor, ElementID);
        }
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

