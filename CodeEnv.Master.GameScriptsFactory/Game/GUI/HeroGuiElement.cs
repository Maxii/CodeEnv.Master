// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: HeroGuiElement.cs
// AIconGuiElement that represents a Hero assigned to an item. Also handles no hero and unknown.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// AIconGuiElement that represents a Hero assigned to an item. Also handles no hero and unknown.
/// </summary>
public class HeroGuiElement : AIconGuiElement, IComparable<HeroGuiElement> {

    private const string DebugNameFormat = "{0}[{1}]";

    public override GuiElementID ElementID { get { return GuiElementID.Hero; } }

    public override string DebugName {
        get {
            string heroName = Hero != null ? Hero.Name : "Unknown Hero";
            return DebugNameFormat.Inject(GetType().Name, heroName);
        }
    }

    private bool _isHeroPropSet;    // reqd as value can be Hero, NoHero or null if unknown
    private Hero _hero;
    public Hero Hero {
        get { return _hero; }
        set {
            D.Assert(!_isHeroPropSet);
            _hero = value;
            HeroPropSetHandler();
        }
    }

    public override bool IsInitialized { get { return _isHeroPropSet; } }

    private string _guiElementTooltipContent;
    protected override string TooltipContent { get { return _guiElementTooltipContent; } }

    private UILabel _iconImageNameLabel;

    protected override UISprite AcquireIconImageSprite() {
        // Handles case where an ImageFrame is used
        UISprite immediateChildSprite = gameObject.GetSingleComponentInImmediateChildren<UISprite>();
        return immediateChildSprite.transform.childCount == Constants.Zero ? immediateChildSprite : immediateChildSprite.gameObject.GetSingleComponentInImmediateChildren<UISprite>();
    }

    protected override void AcquireAdditionalWidgets() {
        _iconImageNameLabel = gameObject.GetSingleComponentInImmediateChildren<UILabel>();
    }

    #region Event and Property Change Handlers

    private void HeroPropSetHandler() {
        _isHeroPropSet = true;
        if (IsInitialized) {
            PopulateMemberWidgetValues();
            Show();
        }
    }

    ////private void HeroIconImageClickedEventHandler(GameObject go) {
    ////    throw new NotImplementedException();
    ////}

    ////private void HeroIconImageTooltipEventHandler(GameObject go, bool isOver) {
    ////    if(isOver) {
    ////        TooltipHudWindow.Instance.Show("Click to change Hero");
    ////    }
    ////    else {
    ////        TooltipHudWindow.Instance.Hide();
    ////    }
    ////}

    #endregion

    protected override void HandleGuiElementHovered(bool isOver) {
        if (isOver) {
            HoveredHudWindow.Instance.Show(DebugName);
        }
        else {
            HoveredHudWindow.Instance.Hide();
        }
    }

    protected override void PopulateMemberWidgetValues() {
        base.PopulateMemberWidgetValues();
        if (Hero == null) {
            HandleValuesUnknown();
            return;
        }
        _iconImageSprite.atlas = Hero.ImageAtlasID.GetAtlas();
        _iconImageSprite.spriteName = Hero.ImageFilename;
        _iconImageNameLabel.text = Hero.Name;
        _guiElementTooltipContent = Hero.Name;
    }

    protected override void HandleValuesUnknown() {
        base.HandleValuesUnknown();
        _iconImageNameLabel.text = Unknown;
        _guiElementTooltipContent = "Unknown Hero";
    }

    public override void ResetForReuse() {
        base.ResetForReuse();
        //D.Log("{0}.Hero property is about to be reset for reuse.", DebugName);
        _isHeroPropSet = false;
        _hero = null;
        if (_iconImageNameLabel != null) {
            _iconImageNameLabel.text = null;
        }
    }

    protected override void Cleanup() { }

    #region IComparable<HeroGuiElement> Members

    public int CompareTo(HeroGuiElement other) {
        int result;
        Hero noHero = TempGameValues.NoHero;
        if (Hero == noHero) {
            result = other.Hero == noHero ? Constants.Zero : Constants.MinusOne;
        }
        else if (Hero == null) {
            // an unknown hero (hero == null) sorts higher than a hero that is known to be None
            result = other.Hero == null ? Constants.Zero : (other.Hero == noHero) ? Constants.One : Constants.MinusOne;
        }
        else {
            result = (other.Hero == noHero || other.Hero == null) ? Constants.One : Hero.Experience.CompareTo(other.Hero.Experience);
        }
        return result;
    }

    #endregion

}

