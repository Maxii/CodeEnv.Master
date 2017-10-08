// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AIconGuiElement.cs
// Abstract AGuiElement for members of the Gui that contain an image and operate as an icon.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract AGuiElement for members of the Gui that contain an image and operate as an icon.
/// <remarks>Contains an image sprite to represent the icon.</remarks>
/// <remarks>10.4.17 An AIconGuiElement supports hovering over the body of the icon by showing more info about 
/// what the icon represents in the HoveredHud. As such, it can be used in Screens, the UnitHud and the InteractableHud.</remarks>
/// </summary>
public abstract class AIconGuiElement : AGuiElement {

    protected virtual UISprite.Type IconImageSpriteType { get { return UIBasicSprite.Type.Simple; } }

    /// <summary>
    /// The sprite that holds the image of the icon.
    /// </summary>
    protected UISprite _iconImageSprite;

    /// <summary>
    /// The top level widget that encompasses all other widgets as children.
    /// <remarks>Used for Show/Hide control.</remarks>
    /// </summary>
    protected UIWidget _encompassingWidget;

    protected override void InitializeValuesAndReferences() {
        _encompassingWidget = gameObject.GetComponent<UIWidget>();
        _iconImageSprite = AcquireIconImageSprite();
        _iconImageSprite.type = IconImageSpriteType;
        AcquireAdditionalWidgets();
        Hide();
    }

    protected abstract UISprite AcquireIconImageSprite();

    protected abstract void AcquireAdditionalWidgets();

    protected virtual void Show(GameColor color = GameColor.White) {
        D.Assert(IsInitialized);
        _iconImageSprite.color = color.ToUnityColor();
        _encompassingWidget.alpha = Constants.OneF;
    }

    protected void Hide() {
        _encompassingWidget.alpha = Constants.ZeroF;
    }

    #region Event and Property Change Handlers

    void OnHover(bool isOver) {
        IconHoveredEventHandler(isOver);
    }

    private void IconHoveredEventHandler(bool isOver) {
        HandleIconHovered(isOver);
    }

    #endregion

    protected abstract void HandleIconHovered(bool isOver);

    protected virtual void HandleValuesUnknown() {
        _iconImageSprite.atlas = AtlasID.MyGui.GetAtlas();
        _iconImageSprite.spriteName = TempGameValues.UnknownImageFilename;
    }

    public override void ResetForReuse() {
        if (_iconImageSprite != null) {
            _iconImageSprite.atlas = AtlasID.None.GetAtlas();
            _iconImageSprite.spriteName = null;
        }
        Hide();
    }

    #region Debug

    protected override void __ValidateOnAwake() {
        base.__ValidateOnAwake();
        UnityUtility.ValidateComponentPresence<UIWidget>(gameObject);
        UnityUtility.ValidateComponentPresence<BoxCollider>(gameObject);
    }

    #endregion



}

