// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiPlayerSpeciesPopupList.cs
// Player Species selection popup list in the NewGameMenu.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Player Species selection popup list in the NewGameMenu.
/// </summary>
public class GuiPlayerSpeciesPopupList : AGuiMenuPopupList<SpeciesGuiSelection> {

    public GuiElementID elementID;

    public override GuiElementID ElementID { get { return elementID; } }

    /// <summary>
    /// The SpeciesGuiSelection currently selected. Can be 'Random".
    /// </summary>
    public SpeciesGuiSelection SelectedSpecies { get { return Enums<SpeciesGuiSelection>.Parse(_popupList.value); } }

    protected override bool IncludesRandom { get { return true; } }

    protected override string[] Choices { get { return Enums<SpeciesGuiSelection>.GetNames(excludeDefault: true); } }

    private UILabel _speciesNameLabel;
    private UISprite _speciesImageSprite;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        var playerContainer = transform.parent.parent.gameObject;
        var imageFrameSprite = playerContainer.GetSafeMonoBehavioursInChildren<UISprite>().Single(s => s.spriteName == TempGameValues.ImageFrameSpriteName);
        _speciesNameLabel = imageFrameSprite.gameObject.GetSafeFirstMonoBehaviourInChildren<UILabel>();
        _speciesImageSprite = imageFrameSprite.gameObject.GetSafeFirstMonoBehaviourInChildrenOnly<UISprite>();
    }

    protected override void OnPopupListSelection() {
        base.OnPopupListSelection();
        RefreshSpeciesImageAndName();
    }

    private void RefreshSpeciesImageAndName() {
        if (SelectedSpecies == SpeciesGuiSelection.Random) {
            _speciesNameLabel.text = Constants.QuestionMark;
            _speciesImageSprite.atlas = AtlasID.MyGui.GetAtlas();
            _speciesImageSprite.spriteName = TempGameValues.UnknownImageFilename;
        }
        else {
            SpeciesStat speciesStat = SpeciesFactory.Instance.MakeInstance(SelectedSpecies.Convert());
            _speciesNameLabel.text = speciesStat.Name;
            _speciesImageSprite.atlas = speciesStat.ImageAtlasID.GetAtlas();
            _speciesImageSprite.spriteName = speciesStat.ImageFilename;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

