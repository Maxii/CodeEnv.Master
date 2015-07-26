﻿// --------------------------------------------------------------------------------------------------------------------
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
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// GuiElement handling the display and tooltip content for the Hero assigned to an item. 
/// UNDONE once Hero is introduced, pattern this element after the OwnerGuiElement.
/// </summary>
public class HeroGuiElement : AImageGuiElement, IComparable<HeroGuiElement> {

    public override GuiElementID ElementID { get { return GuiElementID.Hero; } }

    private bool _isHeroSet;
    private string __heroName;
    public string __HeroName {
        get { return __heroName; }
        set { SetProperty<string>(ref __heroName, value, "__HeroName", __OnHeroChanged); }
    }

    protected override bool AreAllValuesSet { get { return _isHeroSet; } }

    void OnClick() {
        D.Warn("{0}.OnClick() not yet implemented. TODO: Redirect to Hero management screen.", GetType().Name);
    }

    private void __OnHeroChanged() {
        _isHeroSet = true;
        if (AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    protected override void PopulateElementWidgets() {
        if (__HeroName == null) {
            OnValuesUnknown();
            return;
        }

        AtlasID imageAtlasID = AtlasID.MyGui;
        string imageFilename = __HeroName;   // should result in a null (no image) sprite
        string heroName_Colored = __HeroName;

        switch (widgetsPresent) {
            case WidgetsPresent.Image:
                PopulateImageValues(imageFilename, imageAtlasID);
                _tooltipContent = "Hero custom tooltip placeholder";
                break;
            case WidgetsPresent.Label:
                _imageNameLabel.text = heroName_Colored;
                break;
            case WidgetsPresent.Both:
                PopulateImageValues(imageFilename, imageAtlasID);
                _imageNameLabel.text = heroName_Colored;
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(widgetsPresent));
        }
    }

    public override void Reset() {
        // UNDONE
        __heroName = null;
        _isHeroSet = false;
    }

    protected override void Cleanup() { }

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

