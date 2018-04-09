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
/// what the icon represents in the HoveredHud. As such, it can be used in Screens, the UnitHud and the InteractibleHud.</remarks>
/// </summary>
public abstract class AIconGuiElement : AGuiElement {

    protected virtual UISprite.Type IconImageSpriteType { get { return UIBasicSprite.Type.Simple; } }

    /// <summary>
    /// The GameObject that contains the widget you wish to use for Show/Hide control of all icon-related widgets.
    /// <remarks>Derived GuiElements should override this Property if the GameObject containing this widget is not this GameObject.</remarks>
    /// </summary>
    protected virtual GameObject ShowHideControlWidgetGameObject { get { return gameObject; } }

    /// <summary>
    /// The sprite that holds the image of the icon.
    /// </summary>
    protected UISprite _iconImageSprite;

    /// <summary>
    /// The widget used for Show/Hide control of all icon-related widgets.
    /// </summary>
    protected UIWidget _iconShowHideControlWidget;

    protected override void InitializeValuesAndReferences() {
        _iconShowHideControlWidget = ShowHideControlWidgetGameObject.GetComponent<UIWidget>();
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
        _iconShowHideControlWidget.alpha = Constants.OneF;
    }

    protected void Hide() {
        _iconShowHideControlWidget.alpha = Constants.ZeroF;
    }

    #region Event and Property Change Handlers

    void OnHover(bool isOver) {
        GuiElementHoveredEventHandler(isOver);
    }

    private void GuiElementHoveredEventHandler(bool isOver) {
        HandleGuiElementHovered(isOver);
    }

    #endregion

    protected abstract void HandleGuiElementHovered(bool isOver);

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
        UnityUtility.ValidateComponentPresence<UIWidget>(ShowHideControlWidgetGameObject);
        // there must be a collider on this element's GameObject to enable Hover and Click functionality
        UnityUtility.ValidateComponentPresence<BoxCollider>(gameObject);
    }

    #endregion



}

