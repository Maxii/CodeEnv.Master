// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiSizeElement.cs
// GuiElement handling the display and tooltip content for the Size of an item.
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
/// GuiElement handling the display and tooltip content for the Size of an item. 
/// </summary>
[Obsolete]
public class GuiSizeElement : GuiElement, IComparable<GuiSizeElement> {

    protected override string TooltipContent { get { return "Size custom tooltip placeholder"; } }

    private FleetCategory _category;
    private IIconInfo _iconInfo;

    private UISprite _sprite;
    private UILabel _label;

    protected override void Awake() {
        base.Awake();
        Validate();
        _sprite = gameObject.GetSafeFirstMonoBehaviourInChildren<UISprite>();
        _label = _sprite.gameObject.GetSafeMonoBehaviourInChildren<UILabel>();
    }

    public void SetValues(IIconInfo iconInfo, FleetCategory category) {
        _iconInfo = iconInfo;
        _category = category;

        _sprite.atlas = MyNguiUtilities.GetAtlas(iconInfo.AtlasID);
        _sprite.spriteName = iconInfo.Filename;
        _sprite.color = iconInfo.Color.ToUnityColor();
        // sprite size and placement will be preset for screen

        _label.text = category.GetValueName();
    }

    private void Validate() {
        if (elementID != GuiElementID.Size) {
            D.Warn("{0}.ID = {1}. Fixing...", GetType().Name, elementID.GetValueName());
            elementID = GuiElementID.Size;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IComparable<GuiSizeElement> Members

    public int CompareTo(GuiSizeElement other) {
        return _category.CompareTo(other._category);
    }

    #endregion
}

