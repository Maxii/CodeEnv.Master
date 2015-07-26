// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AImageGuiElement.cs
// Abstract GuiElement handling the display and tooltip content for image-based elements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract GuiElement handling the display and tooltip content for image-based elements.
/// </summary>
public abstract class AImageGuiElement : AGuiElement {

    [Tooltip("The widgets that are present to display the content of this GuiElement.")]
    public WidgetsPresent widgetsPresent = WidgetsPresent.Both;

    protected string _tooltipContent;
    protected sealed override string TooltipContent { get { return _tooltipContent; } }

    protected abstract bool AreAllValuesSet { get; }

    protected UILabel _imageNameLabel;
    private UISprite _imageSprite;

    protected sealed override void Awake() {
        base.Awake();
        InitializeValuesAndReferences();
    }

    private void InitializeValuesAndReferences() {
        switch (widgetsPresent) {
            case WidgetsPresent.Image:
                var imageFrameSprite = gameObject.GetSafeSingleMonoBehaviourInImmediateChildrenOnly<UISprite>();
                _imageSprite = imageFrameSprite.gameObject.GetSafeSingleMonoBehaviourInImmediateChildrenOnly<UISprite>();
                NGUITools.AddWidgetCollider(gameObject);
                break;
            case WidgetsPresent.Label:
                _imageNameLabel = gameObject.GetSafeFirstMonoBehaviourInChildrenOnly<UILabel>();
                break;
            case WidgetsPresent.Both:
                imageFrameSprite = gameObject.GetSafeSingleMonoBehaviourInImmediateChildrenOnly<UISprite>();
                _imageSprite = imageFrameSprite.gameObject.GetSafeSingleMonoBehaviourInImmediateChildrenOnly<UISprite>();
                _imageNameLabel = gameObject.GetSafeFirstMonoBehaviourInChildrenOnly<UILabel>();
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(widgetsPresent));
        }
    }

    protected abstract void PopulateElementWidgets();

    protected void PopulateImageValues(string filename, AtlasID atlasID) {
        _imageSprite.atlas = atlasID.GetAtlas();
        _imageSprite.spriteName = filename;
    }

    protected void OnValuesUnknown() {
        switch (widgetsPresent) {
            case WidgetsPresent.Image:
                _imageSprite.atlas = AtlasID.MyGui.GetAtlas();
                _imageSprite.spriteName = TempGameValues.UnknownImageFilename;
                _tooltipContent = "Unknown";
                break;
            case WidgetsPresent.Label:
                _imageNameLabel.text = Constants.QuestionMark;
                break;
            case WidgetsPresent.Both:
                _imageSprite.atlas = AtlasID.MyGui.GetAtlas();
                _imageSprite.spriteName = TempGameValues.UnknownImageFilename;
                _imageNameLabel.text = Constants.QuestionMark;
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(widgetsPresent));
        }
    }


    #region Nested Classes

    /// <summary>
    /// Enum that identifies the Widget's that are present in this GuiElement.
    /// </summary>
    public enum WidgetsPresent {

        /// <summary>
        /// A multi-widget Image for showing the content of this GuiElement.
        /// </summary>
        Image,

        /// <summary>
        /// A label for showing the name of the image to represent the content of this GuiElement.
        /// </summary>
        Label,

        /// <summary>
        /// Both widgets are present.
        /// </summary>
        Both

    }

    #endregion

}

