// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: HeroGuiElement.cs
// GuiElement handling the display and tooltip content for the Hero assigned to an item. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// GuiElement handling the display and tooltip content for the Hero assigned to an item. 
/// UNDONE once Hero is introduced, pattern this element after the OwnerGuiElement.
/// </summary>
public class HeroGuiElement : GuiElement, IComparable<HeroGuiElement> {

    protected override string TooltipContent { get { return "Hero custom tooltip placeholder"; } }

    private string __heroName;
    public string __HeroName {
        get { return __heroName; }
        set { SetProperty<string>(ref __heroName, value, "__HeroName", OnHeroChanged); }
    }

    private UILabel _label;
    private UISprite _imageSprite;

    protected override void Awake() {
        base.Awake();
        Validate();
        _label = gameObject.GetSafeMonoBehaviourInChildren<UILabel>();
        _imageSprite = gameObject.GetSafeMonoBehavioursInChildren<UISprite>().Single(s => s.type == UIBasicSprite.Type.Simple);
    }

    void OnClick() {
        D.Warn("{0}.OnClick() not yet implemented. TODO: Redirect to Hero management screen.", GetType().Name);
    }

    private void OnHeroChanged() {
        _label.text = __HeroName;
        _imageSprite.spriteName = __HeroName;   // should result in a null (no image) sprite
        //_imageSprite.spriteName = Hero.ImageFilename;
    }

    public override void Reset() {
        base.Reset();
        // UNDONE
        __heroName = null;
    }

    private void Validate() {
        if (elementID != GuiElementID.Hero) {
            D.Warn("{0}.ID = {1}. Fixing...", GetType().Name, elementID.GetName());
            elementID = GuiElementID.Hero;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IComparable<HeroGuiElement> Members

    public int CompareTo(HeroGuiElement other) {
        int result = __HeroName.CompareTo(other.__HeroName);
        // TODO use same logic as OwnerGuiElement as Hero can be null (unknown) too
        return result;
    }

    #endregion

}

