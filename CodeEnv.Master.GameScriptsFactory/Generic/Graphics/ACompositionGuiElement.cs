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
public abstract class ACompositionGuiElement : GuiElement, IComparable<ACompositionGuiElement> {

    protected override string TooltipContent { get { return "Size custom tooltip placeholder"; } }

    private IconInfo _iconInfo;
    public IconInfo IconInfo {
        get { return _iconInfo; }
        set { SetProperty<IconInfo>(ref _iconInfo, value, "IconInfo", OnIconInfoChanged); }
    }

    protected abstract bool AreAllValuesSet { get; }

    private UILabel _label;
    private UISprite _sprite;

    protected override void Awake() {
        base.Awake();
        Validate();
        _sprite = gameObject.GetSafeMonoBehaviourInChildren<UISprite>();
        _label = gameObject.GetSafeMonoBehaviourInChildren<UILabel>();
    }

    private void OnIconInfoChanged() {
        if (AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    protected void PopulateElementWidgets() {
        _sprite.atlas = MyNguiUtilities.GetAtlas(IconInfo.AtlasID);
        _sprite.spriteName = IconInfo.Filename;
        _sprite.color = IconInfo.Color.ToUnityColor();
        //D.Log("{0}.PopulateElementWidgets() called. SpriteName: {1}, Color: {2}.", GetType().Name, IconInfo.Filename, IconInfo.Color.GetName());
        // sprite size and placement are preset
        _label.text = GetCategoryName();
    }

    protected abstract string GetCategoryName();

    public override void Reset() {
        base.Reset();
        _iconInfo = default(IconInfo);
    }

    private void Validate() {
        if (elementID != GuiElementID.Composition) {
            D.Warn("{0}.ID = {1}. Fixing...", GetType().Name, elementID.GetName());
            elementID = GuiElementID.Composition;
        }
    }

    #region IComparable<ACompositionGuiElement> Members

    public abstract int CompareTo(ACompositionGuiElement other);

    #endregion

}

