// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: HeroChangeGuiElement.cs
// AGuiElement that represents and allows changes to a Unit's Hero.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// AGuiElement that represents and allows changes to a Unit's Hero.
/// </summary>
public class HeroChangeGuiElement : AIconGuiElement, IComparable<HeroChangeGuiElement> {

    private const string HeroLevelFormat = "Level {0}";

    public override GuiElementID ElementID { get { return GuiElementID.HeroChange; } }

    private Reference<Hero> _heroDataReference;
    public Reference<Hero> HeroDataReference {
        get { return _heroDataReference; }
        set {
            D.AssertNull(_heroDataReference);
            _heroDataReference = value;
            HeroDataReferencePropSetHandler();
        }
    }

    public override bool IsInitialized { get { return _heroDataReference != null; } }

    private string _guiElementTooltipContent;
    protected override string TooltipContent { get { return _guiElementTooltipContent; } }

    private UIProgressBar _heroLevelProgressBar;
    private UILabel _heroLevelLabel;
    private UILabel _iconImageNameLabel;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        Subscribe();
    }

    private void Subscribe() {
        var imageEventListener = UIEventListener.Get(_iconImageSprite.gameObject);
        imageEventListener.onClick += HeroIconImageClickedEventHandler;
        imageEventListener.onTooltip += HeroIconImageTooltipEventHandler;
    }

    protected override UISprite AcquireIconImageSprite() {
        // Handles case where an ImageFrame is used
        UISprite immediateChildSprite = gameObject.GetSingleComponentInImmediateChildren<UISprite>();
        return immediateChildSprite.transform.childCount == Constants.Zero ? immediateChildSprite : immediateChildSprite.gameObject.GetSingleComponentInImmediateChildren<UISprite>();
    }

    protected override void AcquireAdditionalWidgets() {
        _iconImageNameLabel = gameObject.GetSingleComponentInImmediateChildren<UILabel>();
        _heroLevelProgressBar = gameObject.GetSingleComponentInChildren<UIProgressBar>();
        _heroLevelLabel = gameObject.GetComponentsInChildren<UILabel>().Single(label => label != _iconImageNameLabel);
    }

    #region Event and Property Change Handlers

    private void HeroIconImageClickedEventHandler(GameObject go) {
        HandleHeroIconImageClicked();
    }

    private void HeroIconImageTooltipEventHandler(GameObject go, bool isOver) {
        if (isOver) {
            TooltipHudWindow.Instance.Show("Click to change Hero");
        }
        else {
            TooltipHudWindow.Instance.Hide();
        }
    }

    private void HeroDataReferencePropSetHandler() {
        if (IsInitialized) {
            PopulateMemberWidgetValues();
            Show();
        }
    }

    #endregion

    protected override void HandleGuiElementHovered(bool isOver) {
        if (isOver) {
            HoveredHudWindow.Instance.Show(HeroDataReference.Value.Name);
        }
        else {
            HoveredHudWindow.Instance.Hide();
        }
    }

    protected override void PopulateMemberWidgetValues() {
        base.PopulateMemberWidgetValues();
        AssignHeroValuesToWidgets();
    }

    private void AssignHeroValuesToWidgets() {
        Hero hero = HeroDataReference.Value;

        _iconImageSprite.atlas = hero.ImageAtlasID.GetAtlas();
        _iconImageSprite.spriteName = hero.ImageFilename;
        _iconImageNameLabel.text = hero.Name;
        _guiElementTooltipContent = hero.Name;

        if (hero != TempGameValues.NoHero) {
            _heroLevelLabel.text = HeroLevelFormat.Inject(hero.Level);
            _heroLevelProgressBar.value = hero.NextLevelCompletionPercentage;
            NGUITools.SetActive(_heroLevelProgressBar.gameObject, true);
        }
        else {
            _heroLevelLabel.text = null;
            _heroLevelProgressBar.value = Constants.ZeroF;
            NGUITools.SetActive(_heroLevelProgressBar.gameObject, false);
        }
    }

    private void HandleHeroIconImageClicked() {
        HeroDataReference.Value = HeroDataReference.Value == TempGameValues.NoHero ? __CreateHero() : TempGameValues.NoHero;
        AssignHeroValuesToWidgets();
    }

    public override void ResetForReuse() {
        base.ResetForReuse();
        _heroDataReference = null;
        if (_iconImageNameLabel != null) {
            _iconImageNameLabel.text = null;
        }
        if (_heroLevelProgressBar != null) {
            _heroLevelProgressBar.value = Constants.ZeroF;
        }
        if (_heroLevelLabel != null) {
            _heroLevelLabel.text = null;
        }
    }

    private void Unsubscribe() {
        var imageEventListener = UIEventListener.Get(_iconImageSprite.gameObject);
        imageEventListener.onClick -= HeroIconImageClickedEventHandler;
        imageEventListener.onTooltip -= HeroIconImageTooltipEventHandler;
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    #region Debug

    protected override void HandleValuesUnknown() {
        base.HandleValuesUnknown();
        throw new NotImplementedException("{0}.Hero cannot be unknown.".Inject(DebugName));
    }

    private Hero __CreateHero() {  // UNDONE needs to directly access HeroMgmt screen providing HeroRef to allow selection/reassignment
        var heroStat = new HeroStat("Maureen", AtlasID.MyGui, TempGameValues.AnImageFilename, Enums<Species>.GetRandom(excludeDefault: true),
            Enums<Hero.HeroCategory>.GetRandom(excludeDefault: true), "Hero Description...", 0.2F, 10F);
        var hero = new Hero(heroStat);
        hero.IncrementExperienceBy(UnityEngine.Random.Range(Constants.ZeroF, 2000F));
        return hero;
    }

    #endregion

    #region IComparable<HeroChangeGuiElement> Members

    public int CompareTo(HeroChangeGuiElement other) {
        return HeroDataReference.Value.Experience.CompareTo(other.HeroDataReference.Value.Experience);
    }

    #endregion

}

