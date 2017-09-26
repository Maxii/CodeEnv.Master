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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// GuiElement handling the display and tooltip content for the Hero assigned to an item. 
/// </summary>
public class HeroGuiElement : AImageGuiElement, IComparable<HeroGuiElement> {

    public override GuiElementID ElementID { get { return GuiElementID.Hero; } }

    private bool _isHeroPropSet;
    private Hero _hero;
    public Hero Hero {
        get { return _hero; }
        set {
            D.Assert(!_isHeroPropSet);
            _hero = value;
            HeroPropSetHandler();
        }
    }

    protected override bool AreAllValuesSet { get { return _isHeroPropSet; } }

    #region Event and Property Change Handlers

    void OnClick() {
        ClickEventHandler();
    }

    private void ClickEventHandler() {
        //TODO Redirect to Hero management screen
        D.Warn("{0}.OnClick() not yet implemented.", DebugName);
    }

    private void HeroPropSetHandler() {
        _isHeroPropSet = true;
        if (AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    #endregion

    protected override void PopulateElementWidgets() {
        if (Hero == null) {
            HandleValuesUnknown();
            return;
        }

        AtlasID imageAtlasID = Hero.ImageAtlasID;
        string imageFilename = Hero.ImageFilename;
        string heroName = Hero.Name;
        PopulateValues(imageFilename, imageAtlasID, heroName);
    }

    public override void ResetForReuse() {
        base.ResetForReuse();
        _isHeroPropSet = false;
        _hero = null;
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
            result = (other.Hero == noHero || other.Hero == null) ? Constants.One : Hero.Name.CompareTo(other.Hero.Name);
        }
        return result;
    }

    #endregion

}

