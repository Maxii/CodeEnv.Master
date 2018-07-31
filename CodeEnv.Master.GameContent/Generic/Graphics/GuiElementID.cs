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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

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

        // Newly available: 83, 84, 85
        // last: PlayerSeparationPopupList = 112  

        #region Checkboxes

        CameraRollCheckbox = 1,

        ResetOnFocusCheckbox = 2,

        PauseOnLoadCheckbox = 3,

        ZoomOutOnCursorCheckbox = 4,

        ShowElementIconsCheckbox = 5,

        // 7.8.17 Currently used in DesignScreensManager
        ShipDesignWindowCheckbox = 93,
        FacilityDesignWindowCheckbox = 94,
        FleetCmdDesignWindowCheckbox = 95,
        StarbaseCmdDesignWindowCheckbox = 96,
        SettlementCmdDesignWindowCheckbox = 97,

        AiHandlesUserCmdModuleInitialDesignsCheckbox = 79,
        AiHandlesUserCmdModuleRefitDesignsCheckbox = 80,
        AiHandlesUserCentralHubInitialDesignsCheckbox = 81,
        AiHandlesUserElementRefitDesignsCheckbox = 82,

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

        PlayersSeparationPopupList = 112,

        PlayerCountPopupList = 53,

        SystemDensityPopupList = 54,

        UniverseSizePopupList = 22,

        GameSpeedOnLoadPopupList = 23,

        QualitySettingPopupList = 24,

        SavedGamesPopupList = 25,


        #endregion

        #region Labels

        Name = 26,

        // 10.14.17 Used for sorting table rows in Systems
        Organics = 27,
        Particulates = 28,
        Energy = 29,

        // 10.14.17 Used for sorting table rows in Cmds
        Food = 103,
        Production = 104,
        Income = 105,
        Expense = 106,
        Science = 31,
        Culture = 32,

        Speed = 30,
        Formation = 109,
        CombatStance = 111,

        DesignerUITitleLabel = 87,

        #endregion

        #region Complex Elements

        [EnumAttribute("Defensive\nStrength")]  // Attributes must be a compile time constant
        DefensiveStrength = 33,

        [EnumAttribute("Offensive\nStrength")]
        OffensiveStrength = 34,


        Health = 36,

        Owner = 37,

        Hero = 38,

        Location = 39,

        Resources = 40,

        Composition = 41,

        Construction = 42,

        Approval = 44,

        NetIncome = 45,

        Outputs = 102,

        Population = 108,

        FormationChange = 107,

        NameChange = 98,

        HeroChange = 43,

        CombatStanceChange = 110,

        #endregion

        #region Miscellaneous

        DesignerUIContainer = 86,

        ThreeDStageUIContainer = 88,

        DesignsUIContainer = 89,

        MenuControlsUIContainer = 90,

        CreateDesignPopupWindow = 91,

        RenameObsoleteDesignPopupWindow = 92,

        #endregion

        #region Icons

        DesignIcon = 35,

        EquipmentIcon = 99,

        ElementIcon = 100,

        UnitIcon = 101,

        #endregion

    }
}

