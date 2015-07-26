﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiPlayerColorPopupList.cs
// A PopupList that handles the selection of player colors in the NewGameMenu. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// A PopupList that handles the selection of player colors in the NewGameMenu.
/// </summary>
public class GuiPlayerColorPopupList : AGuiMenuPopupList<GameColor> {

    /// <summary>
    /// Occurs when the USER makes a selection from the ColorPopupList.
    /// Disabled when the selection change is initiated by the PlayerColorManager.
    /// </summary>
    public event Action<GuiPlayerColorPopupList> onSelection;

    public GuiElementID elementID;

    public override GuiElementID ElementID { get { return elementID; } }

    protected override bool SelfInitializeSelection { get { return false; } }

    /// <summary>
    /// The GameColor currently selected.
    /// </summary>
    public GameColor SelectedColor { get { return Enums<GameColor>.Parse(_popupList.value); } }

    private string[] _choices;
    protected override string[] Choices { get { return _choices; } }

    private bool _isSelectionEventsEnabled;

    /// <summary>
    ///Assigns the color choices available to this PlayerColorPopupList.
    /// </summary>
    /// <param name="selectionChoices">The color choices.</param>
    public void AssignColorSelectionChoices(IEnumerable<GameColor> selectionChoices) {
        _choices = selectionChoices.Select(color => color.GetValueName()).ToArray();
        AssignSelectionChoices();
    }

    /// <summary>
    /// Refreshes the selection of this PlayerColorPopupList from its stored preference.
    /// onSelection events are disabled during this refresh.
    /// </summary>
    /// <param name="defaultSelection">The optional default selection.</param>
    public void RefreshSelectionFromPreference(GameColor defaultSelection = default(GameColor)) {
        Arguments.ValidateNotNullOrEmpty<string>(Choices);
        DefaultSelection = defaultSelection != default(GameColor) ? defaultSelection.GetValueName() : null;
        _isSelectionEventsEnabled = false;
        TryMakePreferenceSelection();
        _isSelectionEventsEnabled = true;
    }

    private void RefreshLabelColor() {
        _label.color = SelectedColor.ToUnityColor();
    }

    protected override void OnPopupListSelection() {
        base.OnPopupListSelection();
        RefreshLabelColor();
        if (_isSelectionEventsEnabled && onSelection != null) {
            onSelection(this);
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

