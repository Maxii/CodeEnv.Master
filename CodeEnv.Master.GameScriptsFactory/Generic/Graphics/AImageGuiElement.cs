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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract GuiElement handling the display and tooltip content for image-based elements.
/// </summary>
public abstract class AImageGuiElement : AGuiElement {

    [SerializeField]
    private string _tooltipContent = null;
    protected sealed override string TooltipContent { get { return _tooltipContent; } }

    protected abstract bool AreAllValuesSet { get; }

    private UILabel _imageNameLabel;
    private UISprite _imageSprite;

    protected sealed override void Awake() {
        base.Awake();
        InitializeValuesAndReferences();
    }

    private void InitializeValuesAndReferences() {
        UISprite immediateChildSprite = gameObject.GetSingleComponentInImmediateChildren<UISprite>();
        _imageSprite = immediateChildSprite.transform.childCount == Constants.Zero ? immediateChildSprite : immediateChildSprite.gameObject.GetSingleComponentInImmediateChildren<UISprite>();
        _imageNameLabel = gameObject.GetComponentInChildren<UILabel>();
    }

    protected abstract void PopulateElementWidgets();

    protected void PopulateValues(string imageFilename, AtlasID imageAtlasID, string imageLabelText) {
        _imageSprite.atlas = imageAtlasID.GetAtlas();
        _imageSprite.spriteName = imageFilename;
        if (_imageNameLabel != null) {
            _imageNameLabel.text = imageLabelText;
        }
    }

    protected void HandleValuesUnknown() {
        PopulateValues(TempGameValues.UnknownImageFilename, AtlasID.MyGui, Unknown);
    }

    public override void ResetForReuse() {
        _imageSprite.atlas = AtlasID.None.GetAtlas();
        _imageSprite.spriteName = null;
        if (_imageNameLabel != null) {
            _imageNameLabel.text = null;
        }
    }

    #region Debug

    protected override void __Validate() {
        base.__Validate();
        UnityUtility.ValidateComponentPresence<BoxCollider>(gameObject);
    }

    #endregion


}

