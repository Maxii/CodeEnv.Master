// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AImageIcon.cs
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
/// <remarks>The large size is composed of an image contained within an imageFrame with the name below.</remarks>composed of an image and a name with Tooltip support.
/// <remarks>The small size has just an image.</remarks>
/// </summary>
public abstract class AImageIcon : ATextTooltip {

    public GameObject largeImageIconPrefab;

    public GameObject smallImageIconPrefab;

    private string _tooltipContent;
    protected override string TooltipContent { get { return _tooltipContent; } }

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

    private UIWidget _encompassingWidget;
    private UISprite _imageSprite;
    private UILabel _nameLabel;

    /// <summary>
    /// Gets the icon dimensions of the prefab associated with the provided size.
    /// </summary>
    /// <param name="iconSize">Size of the icon in pixels.</param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public IntVector2 GetIconDimensions(IconSize iconSize) {
        UIWidget encompassingWidget;
        switch (iconSize) {
            case IconSize.Small:
                encompassingWidget = smallImageIconPrefab.GetComponent<UIWidget>();
                break;
            case IconSize.Large:
                encompassingWidget = largeImageIconPrefab.GetComponent<UIWidget>();
                break;
            case IconSize.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(iconSize));
        }
        return new IntVector2(encompassingWidget.width, encompassingWidget.height);
    }

    /// <summary>
    /// Anchors this icon to the provided widget.
    /// </summary>
    /// <param name="parentWidget">The parent widget.</param>
    public void AnchorTo(UIWidget parentWidget) {
        _encompassingWidget.SetAnchor(parentWidget.transform);
        // UNCLEAR How to set this UnifiedAnchor to Execute OnEnable rather than Update?
    }

    public void Show(AtlasID atlasID, string imageFilename, string labelText, GameColor color = GameColor.White) {
        D.AssertNotDefault((int)Size, DebugName);
        _imageSprite.atlas = atlasID.GetAtlas();
        _imageSprite.spriteName = imageFilename;
        _imageSprite.color = color.ToUnityColor();
        if (_nameLabel != null) {
            _nameLabel.text = labelText;
        }
        _tooltipContent = labelText;
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
        if (Size == IconSize.Small) {
            _encompassingWidget = NGUITools.AddChild(gameObject, smallImageIconPrefab).GetComponent<UIWidget>();
            _imageSprite = _encompassingWidget.GetComponentInChildren<UISprite>();
        }
        else {
            D.AssertEqual(IconSize.Large, Size);
            _encompassingWidget = NGUITools.AddChild(gameObject, largeImageIconPrefab).GetComponent<UIWidget>();
            _imageSprite = _encompassingWidget.GetComponentsInChildren<UISprite>().Single(sprite => sprite.spriteName != TempGameValues.ImageFrameSpriteName);
            _nameLabel = _encompassingWidget.GetComponentInChildren<UILabel>();
        }
        _encompassingWidget.alpha = Constants.ZeroF;
    }

    #region Nested Classes

    public enum IconSize {
        None,
        Small,
        Large
    }


    #endregion

}

