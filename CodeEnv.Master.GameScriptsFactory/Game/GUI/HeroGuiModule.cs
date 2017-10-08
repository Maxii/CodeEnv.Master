// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: HeroGuiModule.cs
// Gui module that allows management of Hero assignments.
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
/// Gui module that allows management of Hero assignments.
/// </summary>
[System.Obsolete("Functionality moved to AInteractableHudUnitForm")]
public class HeroGuiModule : AMonoBase {

    private const string DebugNameFormat = "{0}[{1}]";
    private const string HeroLevelFormat = "Level {0}";

    [SerializeField]
    private UILabel _heroLevelLabel = null;

    public string DebugName {
        get {
            string unitName = UnitData != null ? UnitData.UnitName : "UnitData Property not set";
            return DebugNameFormat.Inject(GetType().Name, unitName);
        }
    }

    private AUnitCmdData _unitData;
    public AUnitCmdData UnitData {
        get { return _unitData; }
        set {
            D.AssertNull(_unitData); // only happens once between Resets
            _unitData = value;
            UnitDataPropSetHandler();  // SetProperty() only calls handler when changed
        }
    }

    private HeroIconGuiElement _heroIconGuiElement;

    protected override void Awake() {
        base.Awake();
        __ValidateOnAwake();
        InitializeValuesAndReferences();
        Subscribe();
    }

    private void InitializeValuesAndReferences() {
        _heroIconGuiElement = gameObject.GetSingleComponentInChildren<HeroIconGuiElement>();
    }

    private void Subscribe() {
        UIEventListener.Get(_heroIconGuiElement.gameObject).onClick += HeroGuiElementClickedEventHandler;
    }

    #region Event and Property Change Handlers

    private void HeroGuiElementClickedEventHandler(GameObject go) {
        HandleHeroGuiElementClicked();
    }

    private void UnitDataPropSetHandler() {
        AssignValuesToMembers();
    }

    #endregion

    private void HandleHeroGuiElementClicked() {
        if (UnitData.Hero == TempGameValues.NoHero) {
            UnitData.Hero = __CreateHero();
        }
        else {
            UnitData.Hero = TempGameValues.NoHero;
        }
        RefreshMemberValues();
    }

    private void RefreshMemberValues() {
        _heroIconGuiElement.ResetForReuse();
        AssignValuesToMembers();
    }

    private void AssignValuesToMembers() {
        _heroIconGuiElement.Hero = UnitData.Hero;
        if (UnitData.Hero == TempGameValues.NoHero) {
            _heroLevelLabel.text = null;
        }
        else {
            _heroLevelLabel.text = HeroLevelFormat.Inject(UnitData.Hero.Level);
        }
        _heroIconGuiElement.SetTooltip("Click to change Hero");
    }

    public void ResetForReuse() {
        _heroIconGuiElement.ResetForReuse();
        _unitData = null;
        _heroLevelLabel.text = null;
    }

    private void Unsubscribe() {
        UIEventListener.Get(_heroIconGuiElement.gameObject).onClick -= HeroGuiElementClickedEventHandler;
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    public override string ToString() { return DebugName; }

    #region Debug

    private void __ValidateOnAwake() {
        D.AssertNotNull(_heroLevelLabel);
    }

    private Hero __CreateHero() {  // UNDONE
        var heroStat = new HeroStat("Maureen", AtlasID.MyGui, TempGameValues.AnImageFilename, UnitData.Owner.Species,
            Hero.HeroCategory.Admiral, "Hero Description...", 0.2F, 10F);
        var hero = new Hero(heroStat);
        hero.IncrementExperienceBy(25F);
        return hero;
    }

    #endregion

}

