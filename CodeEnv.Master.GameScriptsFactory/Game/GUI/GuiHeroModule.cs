// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiHeroModule.cs
// Gui module that allows display and management of Hero assignments.
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
/// Gui module that allows display and management of Hero assignments.
/// </summary>
public class GuiHeroModule : AMonoBase {

    private const string HeroLevelFormat = "Level {0}";

    [SerializeField]
    private UIWidget _emptyHeroContainer = null;
    [SerializeField]
    private UIWidget _assignedHeroContainer = null;
    [SerializeField]
    private UILabel _heroNameLabel = null;
    [SerializeField]
    private UILabel _heroLevelLabel = null;
    [SerializeField]
    private UISprite _heroImage = null;

    public string DebugName { get { return GetType().Name; } }

    private AUnitCmdData _unitData;
    public AUnitCmdData UnitData {
        get { return _unitData; }
        set {
            D.AssertNull(_unitData); // only happens once between Resets
            _unitData = value;
            UnitDataPropSetHandler();  // SetProperty() only calls handler when changed
        }
    }

    private UIButton _heroAssignButton;
    private UIButton _heroMgmtButton;

    protected override void Awake() {
        base.Awake();

        __Validate();
        InitializeValuesAndReferences();
    }

    private void InitializeValuesAndReferences() {
        _heroAssignButton = _emptyHeroContainer.gameObject.GetSingleComponentInChildren<UIButton>();
        _heroMgmtButton = _assignedHeroContainer.gameObject.GetSingleComponentInChildren<UIButton>();

        EventDelegate.Add(_heroAssignButton.onClick, HeroAssignButtonClickedEventHandler);
        EventDelegate.Add(_heroMgmtButton.onClick, HeroMgmtButtonClickedEventHandler);

        _emptyHeroContainer.alpha = Constants.OneF;
        _assignedHeroContainer.alpha = Constants.ZeroF;
    }

    #region Event and Property Change Handlers

    private void UnitDataPropSetHandler() {
        AssignValuesToMembers();
    }

    private void HeroMgmtButtonClickedEventHandler() {
        HandleHeroMgmtButtonClicked();
    }

    private void HeroAssignButtonClickedEventHandler() {
        HandleHeroAssignButtonClicked();
    }

    #endregion

    private void AssignValuesToMembers() {
        if (UnitData.Hero == TempGameValues.NoHero) {
            _emptyHeroContainer.alpha = Constants.OneF;
            _assignedHeroContainer.alpha = Constants.ZeroF;
        }
        else {
            ShowHero();
        }
    }

    private void HandleHeroAssignButtonClicked() {
        D.Log("{0}.HandleHeroAssignButtonClicked() called.", DebugName);
        __CreateAndAssignHero();
        ShowHero();
    }

    private void HandleHeroMgmtButtonClicked() {
        D.Warn("{0}.HandleHeroMgmtButtonClicked() not yet implemented.", DebugName);
    }

    private void ShowHero() {
        _emptyHeroContainer.alpha = Constants.ZeroF;
        _heroImage.atlas = UnitData.Hero.ImageAtlasID.GetAtlas();
        _heroImage.spriteName = UnitData.Hero.ImageFilename;
        _heroNameLabel.text = UnitData.Hero.Name;
        _heroLevelLabel.text = HeroLevelFormat.Inject(UnitData.Hero.Level);
        _assignedHeroContainer.alpha = Constants.OneF;
    }

    public void ResetForReuse() {
        _unitData = null;
        _emptyHeroContainer.alpha = Constants.OneF;
        _assignedHeroContainer.alpha = Constants.ZeroF;
        _heroImage.atlas = AtlasID.None.GetAtlas();
        _heroImage.spriteName = null;
        _heroNameLabel.text = null;
        _heroLevelLabel.text = null;
    }

    protected override void Cleanup() {
        EventDelegate.Remove(_heroMgmtButton.onClick, HeroMgmtButtonClickedEventHandler);
        EventDelegate.Remove(_heroAssignButton.onClick, HeroAssignButtonClickedEventHandler);
    }

    public override string ToString() {
        return DebugName;
    }

    #region Debug

    private void __Validate() {
        D.AssertNotNull(_emptyHeroContainer);
        D.AssertNotNull(_assignedHeroContainer);
        D.AssertNotNull(_heroNameLabel);
        D.AssertNotNull(_heroLevelLabel);
        D.AssertNotNull(_heroImage);
    }

    private void __CreateAndAssignHero() {  // UNDONE
        var heroStat = new HeroStat("Maureen", AtlasID.MyGui, TempGameValues.AnImageFilename, UnitData.Owner.Species,
            Hero.HeroCategory.Admiral, "Hero Description...", 0.2F, 10F);
        var hero = new Hero(heroStat);
        hero.IncrementExperienceBy(25F);
        UnitData.Hero = hero;
    }

    #endregion

}

