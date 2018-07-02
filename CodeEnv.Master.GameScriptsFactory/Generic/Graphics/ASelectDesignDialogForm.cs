// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ASelectDesignDialogForm.cs
// Abstract APopupDialogForm supporting the selection of designs.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Abstract APopupDialogForm supporting the selection of designs.
/// </summary>
public abstract class ASelectDesignDialogForm : APopupDialogForm {

    private const string DesignIconExtension = " DesignIcon";

    [SerializeField]
    private DesignIconGuiElement _designIconPrefab = null;

    [SerializeField]
    private UIButton _acceptButton = null;

    [SerializeField]
    private UIButton _cancelButton = null;

    [SerializeField]
    private UILabel _textLabel = null;

    private PlayerAIManager AiMgr { get { return GameManager.Instance.GetAIManagerFor(Settings.Player); } }

    /// <summary>
    /// The UIGrid that allows scrolling through the icons that represent registered designs.
    /// </summary>
    private UIGrid _displayedDesignIconsGrid;

    /// <summary>
    /// List of DesignIconGuiElements that represent the Current or Current and Obsolete Designs being displayed.
    /// </summary>
    private IList<DesignIconGuiElement> _displayedIcons;
    private DesignIconGuiElement _pickedIcon;
    private UIToggle _includeObsoleteCheckbox;
    private string _defaultText;

    protected sealed override void InitializeValuesAndReferences() {
        _defaultText = _textLabel.text;

        _includeObsoleteCheckbox = gameObject.GetSingleComponentInChildren<UIToggle>();
        _includeObsoleteCheckbox.startsActive = false;
        _includeObsoleteCheckbox.Start();   // Avoid Start being called after assigning the handler as it fires onChange

        EventDelegate.Set(_includeObsoleteCheckbox.onChange, IncludeObsoleteCheckboxChangedEventHandler);
        // 5.30.18 Obsolete checkbox not currently used as it doesn't make sense to pick an obsolete design when the default Design is free
        _includeObsoleteCheckbox.gameObject.SetActive(false);

        _displayedDesignIconsGrid = gameObject.GetSingleComponentInChildren<UIGrid>();
        _displayedDesignIconsGrid.sorting = UIGrid.Sorting.Custom;
        _displayedDesignIconsGrid.onCustomSort = CompareIcons;
        _displayedIcons = new List<DesignIconGuiElement>();
    }

    protected sealed override void AssignValuesToMembers() {
        base.AssignValuesToMembers();
        if (!Settings.Text.IsNullOrEmpty()) {
            _textLabel.text = Settings.Text;
        }
        BuildDesignIcons();
        AssignInitialPickedIcon();
    }

    protected sealed override void InitializeMenuControls() {
        NGUITools.SetActive(_acceptButton.gameObject, true);
        EventDelegate.Set(_acceptButton.onClick, HandleAcceptButtonClicked);
        NGUITools.SetActive(_cancelButton.gameObject, true);
        EventDelegate.Set(_cancelButton.onClick, Settings.CancelButtonDelegate);
    }

    /// <summary>
    /// Build and show the collection of icons that represent the designs to display.
    /// </summary>
    private void BuildDesignIcons() {
        RemoveDesignIcons();

        IEnumerable<AUnitMemberDesign> designs = GetDesignChoices();
        int desiredDesignsToAccommodateInGrid = designs.Count();

        Vector2 gridContainerViewSize = _displayedDesignIconsGrid.GetComponentInParent<UIPanel>().GetViewSize();
        IntVector2 gridContainerDimensions = new IntVector2((int)gridContainerViewSize.x, (int)gridContainerViewSize.y);
        int gridColumns, unusedGridRows;
        AMultiSizeIconGuiElement.IconSize iconSize = AMultiSizeIconGuiElement.DetermineGridIconSize(gridContainerDimensions, desiredDesignsToAccommodateInGrid, _designIconPrefab, out unusedGridRows, out gridColumns);

        // configure grid for icon size
        IntVector2 iconDimensions = _designIconPrefab.GetIconDimensions(iconSize);
        _displayedDesignIconsGrid.cellHeight = iconDimensions.y;
        _displayedDesignIconsGrid.cellWidth = iconDimensions.x;

        // make grid gridColumns wide
        _displayedDesignIconsGrid.maxPerLine = gridColumns;

        designs.ForAll(design => {
            var designIcon = CreateIcon(design, iconSize, _displayedDesignIconsGrid.gameObject);
            AddIcon(designIcon);
        });
        _displayedDesignIconsGrid.repositionNow = true;
    }

    private void AssignInitialPickedIcon() {
        DesignIconGuiElement initialPickedIcon = ChooseInitialPickedDesignIcon(_displayedIcons);
        ChangePickedDesignIcon(initialPickedIcon);
    }

    private DesignIconGuiElement ChooseInitialPickedDesignIcon(IEnumerable<DesignIconGuiElement> choices) {
        bool isRefitDialog = Settings.OptionalParameter != null;
        if (!isRefitDialog) {
            var defaultChoice = choices.SingleOrDefault(icon => icon.Design.Status == AUnitMemberDesign.SourceAndStatus.SystemCreation_Default);
            if (defaultChoice != null) {
                return defaultChoice;
            }
        }
        return choices.First();
    }

    private void RemoveDesignIcons() {
        if (!_displayedIcons.Any()) {
            // Could be newly Awakened in which case it will have an icon present for debug inspection
            IList<Transform> iconTransforms = _displayedDesignIconsGrid.GetChildList();
            if (iconTransforms.Any()) {
                D.AssertEqual(Constants.One, iconTransforms.Count);
                foreach (var it in iconTransforms) {
                    var icon = it.GetComponent<DesignIconGuiElement>();
                    RemoveIcon(icon);
                }
            }
        }
        else {
            var iconsCopy = new List<DesignIconGuiElement>(_displayedIcons);
            iconsCopy.ForAll(i => {
                RemoveIcon(i);
            });
        }
    }

    /// <summary>
    /// Returns all the designs to be displayed as choices from which to pick. The designs returned are determined by
    /// which player is using the form and whether the dialog is for a refit or an initial choice.
    /// <remarks>Choices will include the default design if an initial choice as it will be usable for free.</remarks>
    /// <remarks>6.1.18 Removed includeObsolete as no longer offered to user as an option.</remarks>
    /// </summary>
    /// <returns></returns>
    protected abstract IEnumerable<AUnitMemberDesign> GetDesignChoices();

    /// <summary>
    /// Creates a DesignIconGuiElement from the provided design and parents it to parent.
    /// </summary>
    /// <param name="design">The design.</param>
    /// <param name="iconSize">Size of the icon to create.</param>
    /// <param name="parent">The parent.</param>
    /// <returns></returns>
    private DesignIconGuiElement CreateIcon(AUnitMemberDesign design, AMultiSizeIconGuiElement.IconSize iconSize, GameObject parent) {
        GameObject designIconGo = NGUITools.AddChild(parent, _designIconPrefab.gameObject);
        designIconGo.name = design.DesignName + DesignIconExtension;
        DesignIconGuiElement designIcon = designIconGo.GetSafeComponent<DesignIconGuiElement>(); ;
        designIcon.Size = iconSize;
        designIcon.Design = design;

        designIcon.IsEnabled = design.BuyoutCost <= AiMgr.Knowledge.__BankBalance;
        return designIcon;
    }

    /// <summary>
    /// Adds the provided DesignIconGuiElement to the list of available design icons displayed in
    /// a scroll view. If the icon is not already parented to _displayedDesignIconsGrid, its parent will be changed.
    /// </summary>
    /// <param name="designIcon">The design icon.</param>
    private void AddIcon(DesignIconGuiElement designIcon) {
        if (designIcon.transform.parent != _displayedDesignIconsGrid.transform) {
            UnityUtility.AttachChildToParent(designIcon.gameObject, _displayedDesignIconsGrid.gameObject);
        }
        // TODO if not enabled, don't listen for click
        var eventListener = UIEventListener.Get(designIcon.gameObject);
        eventListener.onClick += DesignIconClickedEventHandler;

        _displayedIcons.Add(designIcon);
    }

    private void RemoveIcon(DesignIconGuiElement designIcon) {
        var eventListener = UIEventListener.Get(designIcon.gameObject);
        eventListener.onClick -= DesignIconClickedEventHandler;

        _displayedIcons.Remove(designIcon);
        // Note: DestroyImmediate() because Destroy() doesn't always get rid of the existing icon before Reposition occurs on LateUpdate
        // This results in an extra 'empty' icon that stays until another Reposition() call, usually from sorting something
        DestroyImmediate(designIcon.gameObject);
    }

    /// <summary>
    /// Changes _pickedIcon to newPickedIcon and manages the IsPicked status of the icons.
    /// <remarks>Handled this way to properly deal with _pickedIcon when it is null or already destroyed.</remarks>
    /// </summary>
    /// <param name="newPickedIcon">The new picked design icon.</param>
    private void ChangePickedDesignIcon(DesignIconGuiElement newPickedIcon) {
        D.AssertNotNull(newPickedIcon);
        D.Assert(newPickedIcon.IsEnabled);
        if (_pickedIcon != null) {  // will start out null
            if (_pickedIcon.gameObject != null) {    // could already be destroyed if includeObsolete changed
                // not destroyed so displayedIcons aren't new instances so must be there
                D.Assert(_displayedIcons.Contains(_pickedIcon));
                _pickedIcon.IsPicked = false;
            }
        }
        _pickedIcon = newPickedIcon;
        _pickedIcon.IsPicked = true;
    }

    #region Event and Property Change Handlers

    private void IncludeObsoleteCheckboxChangedEventHandler() {
        HandleIncludeObsoleteCheckboxChanged();
    }

    private void DesignIconClickedEventHandler(GameObject go) {
        DesignIconGuiElement designIcon = go.GetComponent<DesignIconGuiElement>();
        D.AssertNotNull(designIcon);
        HandleDesignIconClicked(designIcon);
    }

    #endregion

    private void HandleAcceptButtonClicked() {
        AUnitMemberDesign pickedDesign = _pickedIcon.Design;

        EventDelegate acceptButtonDelegate = Settings.AcceptButtonDelegate;
        D.AssertNotNull(acceptButtonDelegate.parameters);   // parameters will not be null if the Delegate's method expects a parameter

        var parameter = new EventDelegate.Parameter(pickedDesign);
        acceptButtonDelegate.parameters.SetValue(parameter, 0);
        acceptButtonDelegate.Execute();
    }

    /// <summary>
    /// Handles when a design icon showing in available designs is clicked. Clicking 'picks' a designIcon.
    /// </summary>
    /// <param name="designIcon">The designIcon that was clicked.</param>
    private void HandleDesignIconClicked(DesignIconGuiElement designIcon) {
        if (designIcon == _pickedIcon || !designIcon.IsEnabled) {
            // current icon re-clicked by user without another choice, or designIcon clicked is grayed out (not enabled)
            return;
        }
        // a new designIcon has been picked
        ChangePickedDesignIcon(designIcon);
    }

    private void HandleIncludeObsoleteCheckboxChanged() {
        BuildDesignIcons();
        AssignInitialPickedIcon();
    }

    /// <summary>
    /// Compares the icons in support of sorting based on BuyoutCost.
    /// <remarks>OPTIMIZE Expensive sorting approach.</remarks>
    /// </summary>
    /// <param name="aIconTransform">a icon transform.</param>
    /// <param name="bIconTransform">The b icon transform.</param>
    /// <returns></returns>
    private int CompareIcons(Transform aIconTransform, Transform bIconTransform) {
        decimal aCost = aIconTransform.GetComponent<DesignIconGuiElement>().Design.BuyoutCost;
        decimal bCost = bIconTransform.GetComponent<DesignIconGuiElement>().Design.BuyoutCost;
        return aCost.CompareTo(bCost);
    }

    protected sealed override void UnsubscribeFromMenuControls() {
        EventDelegate.Remove(_acceptButton.onClick, Settings.AcceptButtonDelegate);
        EventDelegate.Remove(_cancelButton.onClick, Settings.CancelButtonDelegate);
    }

    protected override void DeactivateAllMenuControls() {
        NGUITools.SetActive(_acceptButton.gameObject, false);
        NGUITools.SetActive(_cancelButton.gameObject, false);
    }

    protected sealed override void ResetForReuse_Internal() {
        base.ResetForReuse_Internal();
        RemoveDesignIcons();
        D.AssertEqual(Constants.Zero, _displayedIcons.Count);
        _textLabel.text = _defaultText;
    }

    protected sealed override void Cleanup() {
        base.Cleanup();
        RemoveDesignIcons();
        EventDelegate.Remove(_includeObsoleteCheckbox.onChange, IncludeObsoleteCheckboxChangedEventHandler);
    }

    #region Debug

    protected sealed override void __ValidateOnAwake() {
        base.__ValidateOnAwake();
        D.AssertNotNull(_acceptButton);
        D.AssertNotNull(_cancelButton);
        D.AssertNotNull(_designIconPrefab);
        D.AssertNotNull(_textLabel);
    }

    protected sealed override void __Validate(DialogSettings settings) {
        D.Assert(settings.ShowAcceptButton);
        D.Assert(settings.ShowCancelButton);
        D.AssertNotNull(settings.AcceptButtonDelegate);
        D.AssertNotNull(settings.CancelButtonDelegate);
        D.AssertNotEqual(TempGameValues.NoPlayer, settings.Player);
    }

    #endregion

}

