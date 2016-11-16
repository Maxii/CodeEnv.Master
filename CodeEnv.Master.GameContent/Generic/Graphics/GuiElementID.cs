// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiElementID.cs
// Unique ID for each Gui Element that needs to be distinguishable.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

using CodeEnv.Master.Common;
namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Unique ID for each Gui Element that needs to be distinguishable.
    /// Note: Ngui widgets of a particular type appear to only be distinguishable 
    /// by their gameObject name when their value changes. Therefore, I've attached
    /// this unique ID to each menu element. When the element's value changes, 
    /// I use GetComponent() to acquire the script holding this ID, thereby acquiring
    /// the ID. Accordingly, I now know which element's value has changed.
    /// </summary>
    public enum GuiElementID {

        None = 0,

        #region Checkboxes

        CameraRollCheckbox = 1,

        ResetOnFocusCheckbox = 2,

        PauseOnLoadCheckbox = 3,

        ZoomOutOnCursorCheckbox = 4,

        ElementIconsCheckbox = 5,

        #endregion

        #region PopupLists

        UserPlayerSpeciesPopupList = 6,
        AIPlayer1SpeciesPopupList = 7,
        AIPlayer2SpeciesPopupList = 8,
        AIPlayer3SpeciesPopupList = 9,
        AIPlayer4SpeciesPopupList = 10,
        AIPlayer5SpeciesPopupList = 11,
        AIPlayer6SpeciesPopupList = 12,
        AIPlayer7SpeciesPopupList = 13,


        UserPlayerColorPopupList = 14,
        AIPlayer1ColorPopupList = 15,
        AIPlayer2ColorPopupList = 16,
        AIPlayer3ColorPopupList = 17,
        AIPlayer4ColorPopupList = 18,
        AIPlayer5ColorPopupList = 19,
        AIPlayer6ColorPopupList = 20,
        AIPlayer7ColorPopupList = 21,


        AIPlayer1IQPopupList = 46,
        AIPlayer2IQPopupList = 47,
        AIPlayer3IQPopupList = 48,
        AIPlayer4IQPopupList = 49,
        AIPlayer5IQPopupList = 50,
        AIPlayer6IQPopupList = 51,
        AIPlayer7IQPopupList = 52,


        UserPlayerTeamPopupList = 55,
        AIPlayer1TeamPopupList = 56,
        AIPlayer2TeamPopupList = 57,
        AIPlayer3TeamPopupList = 58,
        AIPlayer4TeamPopupList = 59,
        AIPlayer5TeamPopupList = 60,
        AIPlayer6TeamPopupList = 61,
        AIPlayer7TeamPopupList = 62,

        UserPlayerStartLevelPopupList = 63,
        AIPlayer1StartLevelPopupList = 64,
        AIPlayer2StartLevelPopupList = 65,
        AIPlayer3StartLevelPopupList = 66,
        AIPlayer4StartLevelPopupList = 67,
        AIPlayer5StartLevelPopupList = 68,
        AIPlayer6StartLevelPopupList = 69,
        AIPlayer7StartLevelPopupList = 70,

        UserPlayerHomeDesirabilityPopupList = 71,
        AIPlayer1HomeDesirabilityPopupList = 72,
        AIPlayer2HomeDesirabilityPopupList = 73,
        AIPlayer3HomeDesirabilityPopupList = 74,
        AIPlayer4HomeDesirabilityPopupList = 75,
        AIPlayer5HomeDesirabilityPopupList = 76,
        AIPlayer6HomeDesirabilityPopupList = 77,
        AIPlayer7HomeDesirabilityPopupList = 78,

        AIPlayer1UserSeparationPopupList = 79,
        AIPlayer2UserSeparationPopupList = 80,
        AIPlayer3UserSeparationPopupList = 81,
        AIPlayer4UserSeparationPopupList = 82,
        AIPlayer5UserSeparationPopupList = 83,
        AIPlayer6UserSeparationPopupList = 84,
        AIPlayer7UserSeparationPopupList = 85,     // last

        PlayerCountPopupList = 53,

        SystemDensityPopupList = 54,

        UniverseSizePopupList = 22,

        GameSpeedOnLoadPopupList = 23,

        QualitySettingPopupList = 24,

        SavedGamesPopupList = 25,

        #endregion

        #region Sliders

        //GameSpeedSlider

        #endregion

        #region Labels

        ItemNameLabel = 26,

        OrganicsLabel = 27,

        ParticulatesLabel = 28,

        EnergyLabel = 29,

        SpeedLabel = 30,

        ScienceLabel = 31,

        CultureLabel = 32,

        PopulationLabel = 43,

        #endregion

        #region Complex Elements

        [EnumAttribute("Defensive\nStrength")]  // Attributes must be a compile time constant
        DefensiveStrength = 33,

        [EnumAttribute("Offensive\nStrength")]
        OffensiveStrength = 34,

        //TotalStrength = 35,                                           // available

        Health = 36,

        Owner = 37,

        Hero = 38,

        Location = 39,

        Resources = 40,

        Composition = 41,

        Production = 42,

        Approval = 44,

        NetIncome = 45,

        #endregion

        #region Buttons


        #endregion

        #region Miscellaneous


        #endregion

    }
}

