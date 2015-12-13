// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlayerColorManager.cs
// COMMENT - one line to give a brief idea of what this file does.
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
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// COMMENT 
/// </summary>
public class PlayerColorManager : AMonoBase {

    private GameColor _currentUserPlayerColor;
    //private HashSet<GameColor> _colorsInUse;
    //private GameColor[] _colorsInUse;
    private IDictionary<GameColor, GuiPlayerColorPopupList> _aiPlayerColorsInUse;

    private GuiPlayerColorPopupList _userPlayerColorPopupList;

    //private IDictionary<GuiElementID, GuiPlayerColorPopupList> _aiPlayerColorPopupLists;
    private IList<GuiPlayerColorPopupList> _aiPlayerColorPopupLists;

    private MenuCancelButton _menuCancelButton;

    protected override void Awake() {
        base.Awake();
        InitializeValuesAndReferences();
        //Subscribe();
    }


    private void InitializeValuesAndReferences() {
        _aiPlayerColorPopupLists = new List<GuiPlayerColorPopupList>(TempGameValues.MaxAIPlayers);
        //_aiPlayerColorPopupLists = new Dictionary<GuiElementID, GuiPlayerColorPopupList>(TempGameValues.MaxAIPlayers);
        var colorPopupLists = gameObject.GetSafeMonoBehavioursInChildren<GuiPlayerColorPopupList>(includeInactive: true);
        colorPopupLists.ForAll(cpl => {
            if (cpl.ElementID == GuiElementID.UserPlayerColorPopupList) {
                _userPlayerColorPopupList = cpl;
            }
            else {
                // _aiPlayerColorPopupLists.Add(cpl.ElementID, cpl);
                _aiPlayerColorPopupLists.Add(cpl);
            }
            //cpl.onSelectionChanged += OnColorSelectionChanged;
        });
        //_colorsInUse = new HashSet<GameColor>();
        //_aiPlayerColorsInUse = new Dictionary<GameColor, GuiPlayerColorPopupList>(TempGameValues.MaxAIPlayers);
        //SubscribeToColorSelectionChanges();
        _menuCancelButton = gameObject.GetSafeFirstMonoBehaviourInChildren<MenuCancelButton>();
    }

    protected override void Start() {
        base.Start();
        //RefreshColorsInUse();   // GuiPlayerColorPopupList selection initialization takes place in Awake so defer capture until Start
        _currentUserPlayerColor = _userPlayerColorPopupList.SelectedColor;
        RefreshAIPlayerColorsInUse();
        //RefreshColorChoicesAvailableToAIPlayers(colorToRemove: _currentUserPlayerColor, refreshSelection: false);
        RefreshColorChoicesAvailableToAIPlayers(colorToRemove: _currentUserPlayerColor);
        Subscribe();    // subscribe to color selection changes AFTER removing the current userPlayer color so we don't hear about any change events that result
    }

    private void Subscribe() {
        SubscribeToColorSelectionChanges();
        // Note: When the cancel button is clicked it restores the color popupLists to their state when the window began showing.
        // These UIPopupList changes generate selection change events which this PlayerColorManager is not designed to handle.
        // Accordingly, during this restoration period, we don't want to hear about these selection change events
        _menuCancelButton.onRestoreMenuStateBegin += UnsubscribeToColorSelectionChanges;
        _menuCancelButton.onRestoreMenuStateComplete += SubscribeToColorSelectionChanges;
    }

    //private void RefreshColorsInUse() {
    //    _colorsInUse.Clear();
    //    _colorsInUse.Add(_userPlayerColorPopupList.SelectedColor);
    //    _aiPlayerColorPopupLists.Values.ForAll(cpl => {
    //        if (!_colorsInUse.Add(cpl.SelectedColor)) {
    //            D.Warn("{0} attempting to add duplicate {1} to colors in use for {2}.", GetType().Name, cpl.SelectedColor.GetValueName(), cpl.elementID.GetValueName());
    //        }
    //    });
    //}

    private void RefreshAIPlayerColorsInUse() {
        //_aiPlayerColorsInUse = _aiPlayerColorPopupLists.Values.ToDictionary<GuiPlayerColorPopupList, GameColor>(cpl => cpl.SelectedColor);
        _aiPlayerColorsInUse = _aiPlayerColorPopupLists.ToDictionary<GuiPlayerColorPopupList, GameColor>(cpl => cpl.SelectedColor);
    }

    private void OnColorSelectionChanged(GameColor selectedColor, GuiElementID elementID) {
        D.Log("{0}.OnColorSelectionChanged event received from {1}, selected color = {2}.", GetType().Name, elementID.GetValueName(), selectedColor.GetValueName());
        if (elementID == GuiElementID.UserPlayerColorPopupList) {
            OnUserPlayerColorSelectionChanged(selectedColor);
        }
        else {
            OnAIPlayerColorSelectionChanged(selectedColor, elementID);
        }
    }

    private void OnUserPlayerColorSelectionChanged(GameColor selectedColor) {
        //var aiDefaultColor = GetRandomUnusedColor();
        //_aiPlayerColorPopupLists.Values.ForAll(cpl => {
        //    cpl.DefaultSelectionName = aiDefaultColor.GetValueName();
        //    if (_currentUserPlayerColor != default(GameColor)) {
        //        cpl.AddColor(_currentUserPlayerColor);
        //    }
        //    cpl.RemoveColor(selectedColor);
        //});
        //RefreshColorChoicesAvailableToAIPlayers(colorToRemove: selectedColor, refreshSelection: true, colorToAdd: _currentUserPlayerColor);
        RefreshColorChoicesAvailableToAIPlayers(colorToRemove: selectedColor, colorToAdd: _currentUserPlayerColor);
        _currentUserPlayerColor = selectedColor;
    }

    private void RefreshColorChoicesAvailableToAIPlayers(GameColor colorToRemove, GameColor colorToAdd = default(GameColor)) {
        var aiDefaultColorName = GetRandomUnusedColor().GetValueName(); ;
        //_aiPlayerColorPopupLists.Values.ForAll(cpl => {
        //    if (cpl.DefaultSelectionName != aiDefaultColorName) {
        //        cpl.DefaultSelectionName = aiDefaultColorName;  // avoids equal backing store warning
        //    }
        //    if (colorToAdd != default(GameColor)) {
        //        cpl.AddColor(colorToAdd);
        //    }
        //    cpl.RemoveColor(colorToRemove);
        //    cpl.RefreshSelection();
        //});
        _aiPlayerColorPopupLists.ForAll(cpl => {
            if (cpl.DefaultSelection != aiDefaultColorName) {
                cpl.DefaultSelection = aiDefaultColorName;  // avoids equal backing store warning
            }
            if (colorToAdd != default(GameColor)) {
                cpl.AddColor(colorToAdd);
            }
            cpl.RemoveColor(colorToRemove);
            cpl.RefreshSelectionName();
        });
    }
    //private void RefreshColorChoicesAvailableToAIPlayers(GameColor colorToRemove, bool refreshSelection, GameColor colorToAdd = default(GameColor)) {
    //    var aiDefaultColorName = GetRandomUnusedColor().GetValueName(); ;
    //    _aiPlayerColorPopupLists.Values.ForAll(cpl => {
    //        if (cpl.DefaultSelectionName != aiDefaultColorName) {
    //            cpl.DefaultSelectionName = aiDefaultColorName;  // avoids equal backing store warning
    //        }
    //        if (colorToAdd != default(GameColor)) {
    //            cpl.AddColor(colorToAdd);
    //        }
    //        cpl.RemoveColor(colorToRemove);
    //        if (refreshSelection) {
    //            cpl.RefreshSelection();
    //        }
    //    });
    //}
    //private void RefreshColorChoicesAvailableToAIPlayers(GameColor colorToRemove) {
    //    var aiDefaultColor = GetRandomUnusedColor();
    //    _aiPlayerColorPopupLists.Values.ForAll(cpl => {
    //        cpl.DefaultSelectionName = aiDefaultColor.GetValueName();
    //        if (_currentUserPlayerColor != default(GameColor)) {
    //            cpl.AddColor(_currentUserPlayerColor);
    //        }
    //        cpl.RemoveColor(colorToRemove);
    //    });
    //}


    private GameColor _unusedColorUsedToSetSelection;

    private void OnAIPlayerColorSelectionChanged(GameColor selectedColor, GuiElementID elementID) {
        if (selectedColor == _unusedColorUsedToSetSelection) {
            // this is the Selection change event generated by SetSelection below so don't propogate
            _unusedColorUsedToSetSelection = default(GameColor);
        }
        else {
            GuiPlayerColorPopupList aiPlayerPopupListAlreadyUsingColor;
            if (_aiPlayerColorsInUse.TryGetValue(selectedColor, out aiPlayerPopupListAlreadyUsingColor)) {
                // newly selected ai color is already used by another ai player
                var unusedColor = GetRandomUnusedColor();
                _unusedColorUsedToSetSelection = unusedColor;
                D.Log("{0} setting {1} selection to {2}.", GetType().Name, aiPlayerPopupListAlreadyUsingColor.ElementID.GetValueName(), unusedColor.GetValueName());
                aiPlayerPopupListAlreadyUsingColor.SetSelection(unusedColor);
            }
        }
        RefreshAIPlayerColorsInUse();
    }

    private GameColor GetRandomUnusedColor() {
        var unusedColors = Enums<GameColor>.GetValuesExcept(TempGameValues.UnAcceptablePlayerColors).Except(_aiPlayerColorsInUse.Keys).Except(_currentUserPlayerColor).ToArray();
        return Enums<GameColor>.GetRandomFrom(unusedColors);
    }

    private void SubscribeToColorSelectionChanges() {
        _userPlayerColorPopupList.onSelectionChangedByUser += OnColorSelectionChanged;
        //_aiPlayerColorPopupLists.Values.ForAll(cpl => cpl.onSelectionChanged += OnColorSelectionChanged);
        _aiPlayerColorPopupLists.ForAll(cpl => cpl.onSelectionChangedByUser += OnColorSelectionChanged);
    }

    private void UnsubscribeToColorSelectionChanges() {
        _userPlayerColorPopupList.onSelectionChangedByUser -= OnColorSelectionChanged;
        //_aiPlayerColorPopupLists.Values.ForAll(cpl => cpl.onSelectionChanged -= OnColorSelectionChanged);
        _aiPlayerColorPopupLists.ForAll(cpl => cpl.onSelectionChangedByUser -= OnColorSelectionChanged);
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        UnsubscribeToColorSelectionChanges();
        _menuCancelButton.onRestoreMenuStateBegin -= UnsubscribeToColorSelectionChanges;
        _menuCancelButton.onRestoreMenuStateComplete -= SubscribeToColorSelectionChanges;
    }


    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

