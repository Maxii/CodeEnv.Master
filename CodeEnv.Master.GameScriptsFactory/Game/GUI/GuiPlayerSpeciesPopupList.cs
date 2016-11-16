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
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Player Species selection popup list in the NewGameMenu.
/// </summary>
public class GuiPlayerSpeciesPopupList : AGuiMenuPopupList<SpeciesGuiSelection> {

    //[FormerlySerializedAs("elementID")]
    [Tooltip("The unique ID of this PlayerSpeciesPopupList GuiElement")]
    [SerializeField]
    private GuiElementID _elementID = GuiElementID.None;

    public override GuiElementID ElementID { get { return _elementID; } }

    public override string ConvertedSelectedValue {
        get {
            string unconvertedSelectedValue = SelectedValue;
            Species convertedValue = Enums<SpeciesGuiSelection>.Parse(unconvertedSelectedValue).Convert();
            return convertedValue.GetValueName();
        }
    }

    /// <summary>
    /// The SpeciesGuiSelection currently selected. Can be 'Random".
    /// </summary>
    public SpeciesGuiSelection SelectedSpecies { get { return Enums<SpeciesGuiSelection>.Parse(SelectedValue); } }

    protected override string TooltipContent { get { return "Select the species of this player"; } }

    protected override bool IncludesRandom { get { return true; } }

    protected override string[] Choices { get { return Enums<SpeciesGuiSelection>.GetNames(excludeDefault: true); } }

    private UILabel _speciesNameLabel;
    private UISprite _speciesImageSprite;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        var playerContainer = transform.parent.parent.gameObject;
        var imageFrameSprite = playerContainer.GetSafeComponentsInChildren<UISprite>().Single(s => s.spriteName == TempGameValues.ImageFrameSpriteName);
        _speciesNameLabel = imageFrameSprite.gameObject.GetSingleComponentInChildren<UILabel>();
        _speciesImageSprite = imageFrameSprite.gameObject.GetSingleComponentInChildren<UISprite>(excludeSelf: true);
    }

    #region Event and Property Change Handlers

    protected override void PopupListSelectionChangedEventHandler() {
        base.PopupListSelectionChangedEventHandler();
        RefreshSpeciesImageAndName();
    }

    #endregion

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

