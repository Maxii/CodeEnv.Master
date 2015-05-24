﻿// --------------------------------------------------------------------------------------------------------------------
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


        UniverseSizePopupList = 22,

        GameSpeedOnLoadPopupList = 23,

        QualitySettingPopupList = 24,

        SavedGamesPopupList = 25,

        #endregion

        #region Sliders

        //GameSpeedSlider

        #endregion

        #region Labels

        NameLabel = 26,

        OrganicsLabel = 27,

        ParticulatesLabel = 28,

        EnergyLabel = 29,

        SpeedLabel = 30,

        ScienceLabel = 31,

        CultureLabel = 32,

        PopulationLabel = 43,

        #endregion

        #region Complex Elements

        DefensiveStrength = 33,

        OffensiveStrength = 34,

        TotalStrength = 35,

        Health = 36,

        Owner = 37,

        Hero = 38,

        Location = 39,

        StrategicResources = 40,

        Composition = 41,

        Production = 42,

        Approval = 44,

        NetIncome = 45  // last

        #endregion

        #region Buttons


        #endregion

        #region Miscellaneous


        #endregion

    }
}

