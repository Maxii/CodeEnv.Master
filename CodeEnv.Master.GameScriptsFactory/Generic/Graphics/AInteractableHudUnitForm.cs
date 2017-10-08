// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AInteractableHudUnitForm.cs
// Abstract base class for InteractableHud forms for Units.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for InteractableHud forms for Units.
/// </summary>
public abstract class AInteractableHudUnitForm : AInteractableHudItemDataForm {

    private const string HeroLevelFormat = "Level {0}";


    [SerializeField]
    private UILabel _heroLevelLabel = null;

    public new AUnitCmdData ItemData { get { return base.ItemData as AUnitCmdData; } }

    protected abstract List<string> AcceptableFormationNames { get; }

    private UIPopupList _formationPopupList;
    private UILabel _formationPopupListLabel;

    protected override void InitializeHeroGuiElement(AGuiElement e) {
        base.InitializeHeroGuiElement(e);
        D.AssertEqual(e, _heroGuiElement);
        UIEventListener.Get(_heroGuiElement.gameObject).onClick += HeroGuiElementClickedEventHandler;
    }

    protected override void InitializeNonGuiElementMembers() {
        base.InitializeNonGuiElementMembers();
        _formationPopupList = gameObject.GetSingleComponentInChildren<UIPopupList>();
        _formationPopupList.keepValue = true;
        EventDelegate.Add(_formationPopupList.onChange, FormationChangedEventHandler);
        _formationPopupListLabel = _formationPopupList.GetComponentInChildren<UILabel>();
    }

    #region Event and Property Change Handlers

    private void HeroGuiElementClickedEventHandler(GameObject go) {
        HandleHeroGuiElementClicked();
    }

    private void FormationChangedEventHandler() {
        HandleFormationChanged();
    }

    #endregion

    private void HandleHeroGuiElementClicked() {
        ItemData.Hero = ItemData.Hero == TempGameValues.NoHero ? __CreateHero() : TempGameValues.NoHero;
        RefreshHeroDisplayValues();
    }

    private void RefreshHeroDisplayValues() {
        _heroGuiElement.ResetForReuse();
        AssignHeroElementValues();
        AssignHeroLevelValues();
    }

    private void AssignHeroElementValues() {
        _heroGuiElement.Hero = ItemData.Hero;
        _heroGuiElement.SetTooltip("Click to change Hero");
    }

    private void AssignHeroLevelValues() {
        _heroLevelLabel.text = ItemData.Hero != TempGameValues.NoHero ? HeroLevelFormat.Inject(ItemData.Hero.Level) : null;
    }

    protected override sealed void AssignValueToHeroGuiElement() {
        base.AssignValueToHeroGuiElement();
        AssignHeroElementValues();
    }

    protected override sealed void AssignValueToNameInputGuiElement() {
        base.AssignValueToNameInputGuiElement();
        _nameInput.value = ItemData.UnitName;
        //D.Log("{0}: Input field has been assigned {1}.", DebugName, _nameInput.value);
    }

    protected override void AssignValuesToNonGuiElementMembers() {
        base.AssignValuesToNonGuiElementMembers();
        _formationPopupList.items = AcceptableFormationNames;
        string currentFormationName = ItemData.UnitFormation.GetValueName();
        _formationPopupList.Set(currentFormationName, notify: false);
        _formationPopupListLabel.text = currentFormationName;

        AssignHeroLevelValues();
    }

    private void HandleFormationChanged() {
        var formation = Enums<Formation>.Parse(_formationPopupList.value);
        if (ItemData.UnitFormation != formation) {
            D.Log("{0}: UnitFormation changing from {1} to {2}.", DebugName, ItemData.UnitFormation.GetValueName(), formation.GetValueName());
            ItemData.UnitFormation = formation;
        }
    }

    protected override sealed void HandleNameInputSubmitted() {
        base.HandleNameInputSubmitted();
        AUnitCmdData cmdData = ItemData as AUnitCmdData;
        if (cmdData.UnitName != _nameInput.value) {
            //D.Log("{0}: UnitName changing from {1} to {2}.", DebugName, cmdData.UnitName, _nameInput.value);
            cmdData.UnitName = _nameInput.value;
        }
        else {
            D.Warn("{0}: UnitName {1} submitted without being changed.", DebugName, cmdData.UnitName);
        }
        _nameInput.RemoveFocus();
    }

    protected override void ResetNonGuiElementMembers() {
        base.ResetNonGuiElementMembers();
        _formationPopupList.Set(null, notify: false);
        _formationPopupListLabel.text = null;
        _heroLevelLabel.text = null;
    }

    #region Cleanup

    protected override void CleanupHeroGuiElement(AGuiElement e) {
        base.CleanupHeroGuiElement(e);
        UIEventListener.Get(_heroGuiElement.gameObject).onClick -= HeroGuiElementClickedEventHandler;
    }

    protected override void CleanupNonGuiElementMembers() {
        base.CleanupNonGuiElementMembers();
        EventDelegate.Remove(_formationPopupList.onChange, FormationChangedEventHandler);
    }

    #endregion

    #region Debug

    protected override void __ValidateOnAwake() {
        base.__ValidateOnAwake();
        D.AssertNotNull(_heroLevelLabel);
    }

    private Hero __CreateHero() {  // UNDONE
        var heroStat = new HeroStat("Maureen", AtlasID.MyGui, TempGameValues.AnImageFilename, ItemData.Owner.Species,
            Hero.HeroCategory.Admiral, "Hero Description...", 0.2F, 10F);
        var hero = new Hero(heroStat);
        hero.IncrementExperienceBy(Random.Range(Constants.ZeroF, 2000F));
        return hero;
    }

    #endregion

}

