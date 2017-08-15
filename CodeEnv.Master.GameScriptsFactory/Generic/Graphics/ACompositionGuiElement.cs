// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ACompositionGuiElement.cs
// Abstract base class for a GuiElement handling the display and tooltip content for the Composition of a Command.  
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for a GuiElement handling the display and tooltip content for the Composition of a Command.  
/// </summary>
public abstract class ACompositionGuiElement : AGuiElement, IComparable<ACompositionGuiElement> {

    public override GuiElementID ElementID { get { return GuiElementID.Composition; } }

    private TrackingIconInfo _iconInfo;
    public TrackingIconInfo IconInfo {
        get { return _iconInfo; }
        set {
            D.AssertNull(_iconInfo);   // only occurs once between Resets
            SetProperty<TrackingIconInfo>(ref _iconInfo, value, "IconInfo", IconInfoPropSetHandler);
        }
    }

    protected override string TooltipContent { get { return "Composition custom tooltip placeholder"; } }

    protected abstract bool AreAllValuesSet { get; }

    private UILabel _label;
    private UISprite _sprite;

    protected override void Awake() {
        base.Awake();
        _sprite = gameObject.GetSingleComponentInChildren<UISprite>();
        _label = gameObject.GetSingleComponentInChildren<UILabel>();
    }

    #region Event and Property Change Handlers

    private void IconInfoPropSetHandler() {
        if (AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    #endregion

    protected void PopulateElementWidgets() {
        PopulateIcon();
        _label.text = GetTextForCategory();
    }

    private void PopulateIcon() {
        //D.Log("{0} populating Icon. SpriteName: {1}, Color: {2}.", GetType().Name, IconInfo.Filename, IconInfo.Color.GetValueName());
        _sprite.atlas = IconInfo.AtlasID.GetAtlas();
        _sprite.spriteName = IconInfo.Filename;
        _sprite.color = IconInfo.Color.ToUnityColor();
        //sprite size and placement are preset
    }

    protected abstract string GetTextForCategory();

    public override void ResetForReuse() {
        _iconInfo = default(TrackingIconInfo);
    }

    #region IComparable<ACompositionGuiElement> Members

    public abstract int CompareTo(ACompositionGuiElement other);

    #endregion

}

