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
            cpl.onSelection += OnColorSelection;
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

        // initialize ai player color values
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
        _aiPlayerColorPopupLists.ForAll(pList => _aiPlayerColorsInUse.Add(pList.SelectedColor));
    }

    private void OnColorSelection(GuiPlayerColorPopupList colorPopup) {
        //D.Log("{0}.OnColorSelection event received from {1}, color selected = {2}.", 
        //    GetType().Name, colorPopup.ElementID.GetValueName(), colorPopup.SelectedColor.GetValueName());
        if (colorPopup == _userPlayerColorPopupList) {
            OnUserPlayerColorSelection();
        }
        else {
            OnAIPlayerColorSelection(colorPopup);
        }
    }

    private void OnUserPlayerColorSelection() {
        var userPlayerColorSelected = _userPlayerColorPopupList.SelectedColor;
        if (userPlayerColorSelected == _userPlayerColorSelected) {
            // ignore, as the user selected the same color that was already selected
            return;
        }
        OnUserPlayerColorSelectionChanged(userPlayerColorSelected);
    }

    private void OnUserPlayerColorSelectionChanged(GameColor userPlayerColorSelected) {
        _userPlayerColorSelected = userPlayerColorSelected;

        // remove the newly selected user color from all aiPlayer's choices
        var aiColorChoices = _allPlayerColors.Except(userPlayerColorSelected);
        _aiPlayerColorPopupLists.ForAll(aiPopup => aiPopup.AssignColorSelectionChoices(aiColorChoices));

        // find the aiPlayer that is currently using this color, if any
        GuiPlayerColorPopupList aiPopupListAlreadyUsingColor;
        if (_aiPlayerPopupLookupByColor.TryGetValue(userPlayerColorSelected, out aiPopupListAlreadyUsingColor)) {
            var unusedColors = _allPlayerColors.Except(_aiPlayerColorsInUse.ToArray());   // aiPlayerColorsInUse already includes userPlayerColorSelected
            ChangeAIPlayerColorSelection(aiPopupListAlreadyUsingColor, unusedColors);
        }
    }

    /// <summary>
    /// Changes the color selection for the provided aiPopupList using the provided unusedColors, then 
    /// restores the choices available to the popupList to allPlayerColors except the color currently used by
    /// the UserPlayer.
    /// </summary>
    /// <param name="aiPopupList">The ai popup list.</param>
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

    private void OnAIPlayerColorSelection(GuiPlayerColorPopupList aiPlayerColorPopupList) {
        var aiPlayerColorSelected = aiPlayerColorPopupList.SelectedColor;
        if (aiPlayerColorSelected == _aiPlayerColorSelectedLookup[aiPlayerColorPopupList]) {
            // ignore, as the user selected the same color that was already selected
            return;
        }
        OnAIPlayerColorSelectionChanged(aiPlayerColorPopupList, aiPlayerColorSelected);
    }

    private void OnAIPlayerColorSelectionChanged(GuiPlayerColorPopupList aiPlayerColorPopupList, GameColor aiPlayerColorSelected) {
        // the user has selected a color for an aiPlayer so refresh the colors in use
        RefreshAIPlayerColorsInUse();

        // find the aiPlayer PopupList that is currently using this color, if any
        GuiPlayerColorPopupList aiPlayerPopupListAlreadyUsingColor;
        if (_aiPlayerPopupLookupByColor.TryGetValue(aiPlayerColorSelected, out aiPlayerPopupListAlreadyUsingColor)) {
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
        _userPlayerColorPopupList.onSelection -= OnColorSelection;
        _aiPlayerColorPopupLists.ForAll(cpl => cpl.onSelection -= OnColorSelection);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }


}

