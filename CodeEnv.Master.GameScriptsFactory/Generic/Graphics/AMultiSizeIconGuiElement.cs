// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMultiSizeIconGuiElement.cs
// Abstract AIconGuiElement that has multiple IconSizes available to choose from.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using UnityEngine;

/// <summary>
/// Abstract AIconGuiElement that has multiple IconSizes available to choose from.
/// <remarks>Used for debugging and allowing the Gui to show on the Editor's small Game window.</remarks>
/// </summary>
public abstract class AMultiSizeIconGuiElement : AIconGuiElement {

    /// <summary>
    /// Determines the IconSize to use to fully populate the cells of a UIGrid without any left over.
    /// Also provides the number of rows and columns in the grid that can be accommodated for the returned IconSize.
    /// Warns if the dimensions of the grid container are not sufficient to fully accommodate the number of 
    /// cells desired without scrolling.
    /// </summary>
    /// <param name="gridContainerSize">Size of the grid container in pixels.</param>
    /// <param name="desiredGridCells">The desired number of grid cells to accommodate.</param>
    /// <param name="cellIconPrefab">The cell icon prefab.</param>
    /// <param name="gridRows">The number of rows that can be accommodated within gridContainerSize.</param>
    /// <param name="gridColumns">The number of columns that can be accommodated within gridContainerSize.</param>
    /// <returns></returns>
    public static AMultiSizeIconGuiElement.IconSize DetermineGridIconSize(IntVector2 gridContainerSize, int desiredGridCells, AMultiSizeIconGuiElement cellIconPrefab,
        out int gridRows, out int gridColumns) {
        IntVector2 iconDimensions = cellIconPrefab.GetIconDimensions(IconSize.Large);
        gridRows = gridContainerSize.y / iconDimensions.y;
        gridColumns = gridContainerSize.x / iconDimensions.x;
        if (desiredGridCells <= gridRows * gridColumns) {
            return IconSize.Large;
        }

        iconDimensions = cellIconPrefab.GetIconDimensions(IconSize.Medium);
        gridRows = gridContainerSize.y / iconDimensions.y;
        gridColumns = gridContainerSize.x / iconDimensions.x;
        if (desiredGridCells <= gridRows * gridColumns) {
            return IconSize.Medium;
        }

        iconDimensions = cellIconPrefab.GetIconDimensions(IconSize.Small);
        gridRows = gridContainerSize.y / iconDimensions.y;
        if (gridRows == Constants.Zero) {
            D.Warn("Grid will not fully show even 1 small {0} icon from top to bottom.", cellIconPrefab.DebugName);
        }
        gridColumns = gridContainerSize.x / iconDimensions.x;
        if (gridColumns == Constants.Zero) {
            D.Warn("Grid will not fully show even 1 small {0} icon from left to right.", cellIconPrefab.DebugName);
        }
        if (desiredGridCells > gridRows * gridColumns) {
            D.Log("Scrolling will be required. Only {0} {1} icons of the {2} desired can be accommodated in the grid without scrolling.",
                gridRows * gridColumns, cellIconPrefab.DebugName, desiredGridCells);
        }
        return IconSize.Small;
    }

    [SerializeField]
    private GameObject _largeIconPrefab = null;
    [SerializeField]
    private GameObject _mediumIconPrefab = null;
    [SerializeField]
    private GameObject _smallIconPrefab = null;

    private IconSize _size = IconSize.None;
    public IconSize Size {
        get { return _size; }
        set {
            D.AssertDefault((int)_size);
            _size = value;
            IconSizePropertySetHandler();
        }
    }

    /// <summary>
    /// The top level widget from the instantiated version of the icon prefab that matches Size.
    /// <remarks>Use this to find newly instantiated child widgets whenever Size is changed.</remarks>
    /// </summary>
    protected UIWidget _topLevelIconWidget;

    protected override void Awake() {    // 10.2.17 Handled this way to avoid calling InitializeValuesAndReferences before Size set
        useGUILayout = false;
        _debugSettings = DebugSettings.Instance;
        __ValidateOnAwake();
        // adjust any existing child widgets present before Size is chosen
        AdjustWidgetDepths(gameObject);
    }

    /// <summary>
    /// Initializes values and references.
    /// <remarks>Called every time Icon Size is set rather than from Awake.</remarks>
    /// </summary>
    /// <exception cref="System.NotImplementedException"></exception>
    protected override void InitializeValuesAndReferences() {
        D.AssertNotDefault((int)Size);
        GameObject prefab;
        switch (Size) {
            case IconSize.Small:
                prefab = _smallIconPrefab;
                break;
            case IconSize.Medium:
                prefab = _mediumIconPrefab;
                break;
            case IconSize.Large:
                prefab = _largeIconPrefab;
                break;
            case IconSize.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(Size));
        }
        GameObject iconGo = NGUITools.AddChild(gameObject, prefab);
        AdjustWidgetDepths(iconGo);
        _topLevelIconWidget = iconGo.GetComponent<UIWidget>();

        base.InitializeValuesAndReferences();
    }

    /// <summary>
    /// Gets the dimensions of this GuiElement for the provided size.
    /// </summary>
    /// <param name="iconSize">Size of the icon in pixels.</param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public IntVector2 GetIconDimensions(IconSize iconSize) {
        GameObject prefab;
        switch (iconSize) {
            case IconSize.Small:
                prefab = _smallIconPrefab;
                break;
            case IconSize.Medium:
                prefab = _mediumIconPrefab;
                break;
            case IconSize.Large:
                prefab = _largeIconPrefab;
                break;
            case IconSize.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(iconSize));
        }
        UIWidget topLevelPrefabWidget = prefab.GetComponent<UIWidget>();
        return new IntVector2(topLevelPrefabWidget.width, topLevelPrefabWidget.height);
    }

    #region Event and Property Change Handlers

    private void IconSizePropertySetHandler() {
        HandleIconSizeSet();
    }

    #endregion

    protected virtual void HandleIconSizeSet() {
        InitializeValuesAndReferences();
        ResizeEncompassingWidgetAndAnchorIcon();
    }

    private void ResizeEncompassingWidgetAndAnchorIcon() {
        IntVector2 iconDimensions = GetIconDimensions(Size);
        _encompassingWidget.SetDimensions(iconDimensions.x, iconDimensions.y);
        _topLevelIconWidget.SetAnchor(_encompassingWidget.transform);
        // UNCLEAR How to set this UnifiedAnchor to Execute OnEnable rather than Update?
    }

    public override void ResetForReuse() {
        base.ResetForReuse();
        _size = IconSize.None;
        _iconImageSprite = null;
        if (_topLevelIconWidget != null) {
            Destroy(_topLevelIconWidget.gameObject);
            _topLevelIconWidget = null;
        }
    }

    #region Debug

    protected override void __ValidateOnAwake() {
        base.__ValidateOnAwake();
        D.AssertNotNull(_smallIconPrefab, DebugName);
        D.AssertNotNull(_mediumIconPrefab, DebugName);
        D.AssertNotNull(_largeIconPrefab, DebugName);
    }

    #endregion

    #region Nested Classes

    public enum IconSize {
        None,
        Small,
        Medium,
        Large
    }

    #endregion

}

