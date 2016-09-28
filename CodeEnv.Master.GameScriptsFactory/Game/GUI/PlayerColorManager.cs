// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlayerColorManager.cs
// Coordinates Player Color popupList changes in the NewGameMenu.
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
/// Coordinates Player Color popupList changes in the NewGameMenu.
/// </summary>
public class PlayerColorManager : AMonoBase {

    private static GameColor[] _allPlayerColors = TempGameValues.AllPlayerColors;

    private GameColor _userPlayerColorSelected;
    private HashSet<GameColor> _aiPlayerColorsInUse;
    private IDictionary<GameColor, GuiPlayerColorPopupList> _aiPlayerPopupLookupByColor;
    private IDictionary<GuiPlayerColorPopupList, GameColor> _aiPlayerColorSelectedLookup;

    private GuiPlayerColorPopupList _userPlayerColorPopupList;

    private IList<GuiPlayerColorPopupList> _aiPlayerColorPopupLists;

    protected override void Awake() {
        base.Awake();
        InitializeValuesAndReferences();
    }

    private void InitializeValuesAndReferences() {
        _aiPlayerColorsInUse = new HashSet<GameColor>();
        _aiPlayerColorSelectedLookup = new Dictionary<GuiPlayerColorPopupList, GameColor>(TempGameValues.MaxAIPlayers);
        _aiPlayerPopupLookupByColor = new Dictionary<GameColor, GuiPlayerColorPopupList>(TempGameValues.MaxAIPlayers);

        _aiPlayerColorPopupLists = new List<GuiPlayerColorPopupList>(TempGameValues.MaxAIPlayers);
        var colorPopupLists = gameObject.GetSafeComponentsInChildren<GuiPlayerColorPopupList>(includeInactive: true);
        colorPopupLists.ForAll(cpl => {
            if (cpl.ElementID == GuiElementID.UserPlayerColorPopupList) {
                _userPlayerColorPopupList = cpl;
            }
            else {
                _aiPlayerColorPopupLists.Add(cpl);
            }
            cpl.userSelectedColor += ColorSelectedEventHandler;
        });
    }

    protected override void Start() {
        base.Start();
        InitializeColorPopupListValues();
    }

    private void InitializeColorPopupListValues() {
        // initialize user player color values
        _userPlayerColorPopupList.AssignColorSelectionChoices(_allPlayerColors);
        _userPlayerColorPopupList.RefreshSelectionFromPreference();   // no default value needed as its preference will be used
        _userPlayerColorSelected = _userPlayerColorPopupList.SelectedColor;

        // initialize AIPlayer color values
        var aiColorChoices = _allPlayerColors.Except(_userPlayerColorSelected);

        _aiPlayerColorPopupLists.ForAll(aiCpl => {
            aiCpl.AssignColorSelectionChoices(aiColorChoices);
            aiCpl.RefreshSelectionFromPreference();   // no default value needed as its preference will be used
            UpdateAIPlayerCollections(aiCpl);
        });
        RefreshAIPlayerColorsInUse();
    }

    /// <summary>
    /// Resets the color popup list values to their initialized state.
    /// Used by the NewGameMenuCancelButton to restore the player color popupLists to their initialized state.
    /// </summary>
    public void ResetColorPopupListValues() {
        InitializeColorPopupListValues();
    }

    private void UpdateAIPlayerCollections(GuiPlayerColorPopupList aiColorPopupList) {
        var colorSelected = aiColorPopupList.SelectedColor;
        _aiPlayerPopupLookupByColor[colorSelected] = aiColorPopupList;
        _aiPlayerColorSelectedLookup[aiColorPopupList] = colorSelected;
    }

    private void RefreshAIPlayerColorsInUse() {
        _aiPlayerColorsInUse.Clear();
        _aiPlayerColorPopupLists.ForAll(pList => {
            GameColor selectedColor = pList.SelectedColor;
            if (_aiPlayerColorsInUse.Contains(selectedColor)) {
                D.Warn("{0}: PlayerColor {1} is already in use.", GetType().Name, selectedColor.GetValueName());
            }
            _aiPlayerColorsInUse.Add(selectedColor);
        });
    }

    #region Event and Property Change Handlers

    private void ColorSelectedEventHandler(object sender, EventArgs e) {
        GuiPlayerColorPopupList colorPopup = sender as GuiPlayerColorPopupList;
        //D.Log("{0}.userSelectedColor event received from {1}, color selected = {2}.", 
        //GetType().Name, colorPopup.ElementID.GetValueName(), colorPopup.SelectedColor.GetValueName());
        if (colorPopup == _userPlayerColorPopupList) {
            HandleUserPlayerColorSelection();
        }
        else {
            HandleAIPlayerColorSelection(colorPopup);
        }
    }

    #endregion

    /// <summary>
    /// The UserPlayer's color has been selected. Determine what it is and
    /// if it has changed, process the change.
    /// </summary>
    private void HandleUserPlayerColorSelection() {
        var userPlayerColorSelected = _userPlayerColorPopupList.SelectedColor;
        if (userPlayerColorSelected == _userPlayerColorSelected) {
            // ignore, as the user selected the same color that was already selected
            return;
        }
        ProcessUserPlayerColorSelectionChange(userPlayerColorSelected);
    }

    /// <summary>
    /// Processes the UserPlayer's color selection change. 
    /// </summary>
    /// <param name="changedUserPlayerColorSelected">The changed user player color.</param>
    private void ProcessUserPlayerColorSelectionChange(GameColor changedUserPlayerColorSelected) {
        _userPlayerColorSelected = changedUserPlayerColorSelected;

        // remove the newly selected user color from all aiPlayer's choices
        var aiColorChoices = _allPlayerColors.Except(changedUserPlayerColorSelected);
        _aiPlayerColorPopupLists.ForAll(aiPopup => aiPopup.AssignColorSelectionChoices(aiColorChoices));

        // find the aiPlayer that is currently using this color, if any
        GuiPlayerColorPopupList aiPopupListAlreadyUsingColor;
        if (_aiPlayerPopupLookupByColor.TryGetValue(changedUserPlayerColorSelected, out aiPopupListAlreadyUsingColor)) {
            var unusedColors = _allPlayerColors.Except(_aiPlayerColorsInUse.ToArray());   // aiPlayerColorsInUse already includes changedUserPlayerColorSelected
            ChangeAIPlayerColorSelection(aiPopupListAlreadyUsingColor, unusedColors);
        }
    }

    /// <summary>
    /// Changes the color selection for the provided aiPopupList using the provided unusedColors, then 
    /// restores the choices available to the popupList to allPlayerColors except the color currently used by
    /// the UserPlayer.
    /// </summary>
    /// <param name="aiPopupList">The AIpopup list.</param>
    /// <param name="unusedColors">The unused colors.</param>
    private void ChangeAIPlayerColorSelection(GuiPlayerColorPopupList aiPopupList, IEnumerable<GameColor> unusedColors) {
        // pick an unused color for this aiPlayer
        aiPopupList.AssignColorSelectionChoices(unusedColors);
        aiPopupList.RefreshSelectionFromPreference(); // no default needed as all unusedColor choices are acceptable
        // restore the choices so the user sees the right choices going forward
        aiPopupList.AssignColorSelectionChoices(_allPlayerColors.Except(_userPlayerColorSelected));
        RefreshAIPlayerColorsInUse();
        UpdateAIPlayerCollections(aiPopupList);
    }

    private void HandleAIPlayerColorSelection(GuiPlayerColorPopupList aiPlayerColorPopupList) {
        var aiPlayerColorSelected = aiPlayerColorPopupList.SelectedColor;
        if (aiPlayerColorSelected == _aiPlayerColorSelectedLookup[aiPlayerColorPopupList]) {
            // ignore, as the user selected the same color that was already selected
            return;
        }
        ProcessAIPlayerColorSelectionChange(aiPlayerColorPopupList, aiPlayerColorSelected);
    }

    /// <summary>
    /// Processes the AiPlayer's color selection change.
    /// </summary>
    /// <param name="aiPlayerColorPopupList">The AI player color popup list.</param>
    /// <param name="aiPlayerColorSelected">The changed AiPlayer color selected.</param>
    private void ProcessAIPlayerColorSelectionChange(GuiPlayerColorPopupList aiPlayerColorPopupList, GameColor changedAiPlayerColorSelected) {
        // the user has selected a color for an aiPlayer so refresh the colors in use
        RefreshAIPlayerColorsInUse();

        // find the aiPlayer PopupList that is currently using this color, if any
        GuiPlayerColorPopupList aiPlayerPopupListAlreadyUsingColor;
        if (_aiPlayerPopupLookupByColor.TryGetValue(changedAiPlayerColorSelected, out aiPlayerPopupListAlreadyUsingColor)) {
            var allColorsInUse = new List<GameColor>(_aiPlayerColorsInUse) {
                _userPlayerColorSelected
            }.ToArray();

            var unusedColors = _allPlayerColors.Except(allColorsInUse);
            ChangeAIPlayerColorSelection(aiPlayerPopupListAlreadyUsingColor, unusedColors);
        }
        UpdateAIPlayerCollections(aiPlayerColorPopupList);
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _userPlayerColorPopupList.userSelectedColor -= ColorSelectedEventHandler;
        _aiPlayerColorPopupLists.ForAll(cpl => cpl.userSelectedColor -= ColorSelectedEventHandler);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }


}

