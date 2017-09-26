// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMultiSizeGuiIcon.cs
// Abstract Gui 'icon' with tooltip support that has multiple sizes available.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract Gui 'icon' with tooltip support that has multiple sizes available.
/// <remarks>This 'icon' is composed of one or more UIWidgets (Sprites, Labels, ProgressBars, etc.).
/// The base version contains an image sprite and a label.</remarks>
/// <remarks>7.31.17 Formerly called AImageIcon.</remarks>
/// <remarks>9.22.17 Formerly called AGuiIcon.</remarks>
/// </summary>
public abstract class AMultiSizeGuiIcon : ATextTooltip {

    /// <summary>
    /// Determines the size of the AMultiSizeGuiIcon to use to fully populate the cells of a UIGrid without any left over.
    /// Also provides the number of rows and columns in the grid that can be accommodated for the returned IconSize.
    /// Warns if the dimensions of the grid container are not sufficient to fully accommodate the number of 
    /// cells desired without scrolling.
    /// </summary>
    /// <param name="gridContainerSize">Size of the grid container.</param>
    /// <param name="desiredGridCells">The desired number of grid cells to accommodate.</param>
    /// <param name="cellIconPrefab">The cell icon prefab.</param>
    /// <param name="gridRows">The number of rows that can be accommodated within gridContainerSize.</param>
    /// <param name="gridColumns">The number of columns that can be accommodated within gridContainerSize.</param>
    /// <returns></returns>
    public static AMultiSizeGuiIcon.IconSize DetermineGridIconSize(IntVector2 gridContainerSize, int desiredGridCells, AMultiSizeGuiIcon cellIconPrefab,
        out int gridRows, out int gridColumns) {
        IntVector2 iconDimensions = cellIconPrefab.GetIconDimensions(AMultiSizeGuiIcon.IconSize.Large);
        gridRows = gridContainerSize.y / iconDimensions.y;
        gridColumns = gridContainerSize.x / iconDimensions.x;
        if (desiredGridCells <= gridRows * gridColumns) {
            return AMultiSizeGuiIcon.IconSize.Large;
        }

        iconDimensions = cellIconPrefab.GetIconDimensions(AMultiSizeGuiIcon.IconSize.Medium);
        gridRows = gridContainerSize.y / iconDimensions.y;
        gridColumns = gridContainerSize.x / iconDimensions.x;
        if (desiredGridCells <= gridRows * gridColumns) {
            return AMultiSizeGuiIcon.IconSize.Medium;
        }

        iconDimensions = cellIconPrefab.GetIconDimensions(AMultiSizeGuiIcon.IconSize.Small);
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
        return AMultiSizeGuiIcon.IconSize.Small;
    }

    public GameObject largeIconPrefab;

    public GameObject mediumIconPrefab;

    public GameObject smallIconPrefab;

    public bool IsShowing { get; private set; }

    private IconSize _size = IconSize.None;
    public IconSize Size {
        get { return _size; }
        set {
            D.AssertDefault((int)_size);
            _size = value;
            IconSizePropertySetHandler();
        }
    }

    private string _tooltipContent;
    protected override string TooltipContent { get { return _tooltipContent; } }

    protected virtual UISprite.Type ImageSpriteType { get { return UIBasicSprite.Type.Simple; } }

    /// <summary>
    /// The UIWidget encompassing the instantiated version of the icon prefab that matches Size.
    /// </summary>
    private UIWidget _encompassingWidget;
    private UISprite _imageSprite;
    private UILabel _nameLabel;

    protected sealed override void Awake() {
        base.Awake();
        InitializeValuesAndReferences();
        __Validate();
    }

    protected virtual void InitializeValuesAndReferences() { }

    /// <summary>
    /// Gets the icon dimensions of the prefab associated with the provided size.
    /// </summary>
    /// <param name="iconSize">Size of the icon in pixels.</param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public IntVector2 GetIconDimensions(IconSize iconSize) {
        GameObject prefab;
        switch (iconSize) {
            case IconSize.Small:
                prefab = smallIconPrefab;
                break;
            case IconSize.Medium:
                prefab = mediumIconPrefab;
                break;
            case IconSize.Large:
                prefab = largeIconPrefab;
                break;
            case IconSize.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(iconSize));
        }
        UIWidget encompassingWidget = prefab.GetComponent<UIWidget>();
        return new IntVector2(encompassingWidget.width, encompassingWidget.height);
    }

    /// <summary>
    /// Anchors this icon to the provided widget.
    /// </summary>
    /// <param name="parentWidget">The parent widget.</param>
    protected void AnchorTo(UIWidget parentWidget) {
        _encompassingWidget.SetAnchor(parentWidget.transform);
        // UNCLEAR How to set this UnifiedAnchor to Execute OnEnable rather than Update?
    }

    protected void Show(AtlasID atlasID, string imageFilename, string nameLabelText, GameColor color = GameColor.White) {
        D.AssertNotDefault((int)Size, DebugName);
        _imageSprite.atlas = atlasID.GetAtlas();
        _imageSprite.spriteName = imageFilename;
        _imageSprite.color = color.ToUnityColor();
        if (_nameLabel != null) {
            _nameLabel.text = nameLabelText;
            _nameLabel.color = color.ToUnityColor();
        }
        _tooltipContent = nameLabelText;
        _encompassingWidget.alpha = Constants.OneF;
        IsShowing = true;
    }

    public void Hide() {
        _encompassingWidget.alpha = Constants.ZeroF;
        _tooltipContent = null;
        IsShowing = false;
    }

    #region Event and Property Change Handlers

    private void IconSizePropertySetHandler() {
        HandleIconSizeSet();
    }

    #endregion

    protected virtual void HandleIconSizeSet() {
        GameObject prefab;
        switch (Size) {
            case IconSize.Small:
                prefab = smallIconPrefab;
                break;
            case IconSize.Medium:
                prefab = mediumIconPrefab;
                break;
            case IconSize.Large:
                prefab = largeIconPrefab;
                break;
            case IconSize.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(Size));
        }
        GameObject iconGo = NGUITools.AddChild(gameObject, prefab);

        _encompassingWidget = iconGo.GetComponent<UIWidget>();
        _imageSprite = AcquireImageSprite(iconGo);
        _imageSprite.type = ImageSpriteType;
        _nameLabel = AcquireNameLabel(iconGo);
        AcquireAdditionalIconWidgets(iconGo);

        _encompassingWidget.alpha = Constants.ZeroF;
    }

    protected virtual UISprite AcquireImageSprite(GameObject topLevelIconGo) {
        return topLevelIconGo.GetSingleComponentInImmediateChildren<UISprite>();
    }

    protected virtual UILabel AcquireNameLabel(GameObject topLevelIconGo) {
        return topLevelIconGo.GetSingleComponentInImmediateChildren<UILabel>();
    }

    /// <summary>
    /// Acquire any additional widgets that may be present as children of the provided topLevelIconGo.
    /// <remarks>"Additional' beyond the ImageSprite and NameLabel.</remarks>
    /// </summary>
    /// <param name="topLevelIconGo">The top level icon go.</param>
    protected abstract void AcquireAdditionalIconWidgets(GameObject topLevelIconGo);

    public virtual void ResetForReuse() {
        IsShowing = false;
        _size = IconSize.None;
        _encompassingWidget.alpha = Constants.ZeroF;
        _tooltipContent = null;
    }

    #region Debug

    protected virtual void __Validate() {
        UnityUtility.ValidateComponentPresence<UIWidget>(gameObject);
        D.AssertNotNull(smallIconPrefab, DebugName);
        D.AssertNotNull(mediumIconPrefab, DebugName);
        D.AssertNotNull(largeIconPrefab, DebugName);
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

