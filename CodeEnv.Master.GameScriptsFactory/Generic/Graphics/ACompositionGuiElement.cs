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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for a GuiElement handling the display and tooltip content for the Composition of a Command.  
/// </summary>
public abstract class ACompositionGuiElement : AGuiElement, IComparable<ACompositionGuiElement> {

    public override GuiElementID ElementID { get { return GuiElementID.Composition; } }

    private IconInfo _iconInfo;
    public IconInfo IconInfo {
        get { return _iconInfo; }
        set {
            D.Assert(_iconInfo == default(IconInfo));   // only occurs once between Resets
            SetProperty<IconInfo>(ref _iconInfo, value, "IconInfo", IconInfoPropSetHandler); }
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
        _sprite.atlas = IconInfo.AtlasID.GetAtlas();
        _sprite.spriteName = IconInfo.Filename;
        _sprite.color = IconInfo.Color.ToUnityColor();
        //D.Log("{0}.PopulateElementWidgets() called. SpriteName: {1}, Color: {2}.", GetType().Name, IconInfo.Filename, IconInfo.Color.GetName());
        //sprite size and placement are preset
        _label.text = GetCategoryName();
    }

    protected abstract string GetCategoryName();

    public override void Reset() {
        _iconInfo = default(IconInfo);
    }

    #region IComparable<ACompositionGuiElement> Members

    public abstract int CompareTo(ACompositionGuiElement other);

    #endregion

}

