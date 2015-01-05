// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiMenuElementID.cs
// Unique ID for each Gui Element of a Menu.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Unique ID for each Gui Element of a Menu.
    /// Note: Ngui widgets of a particular type appear to only be distinguishable 
    /// by their gameObject name when their value changes. Therefore, I assign the
    /// string equivalent of this ID as the GameObject's name, then, when the element's value
    /// changes, I acquire the name and parse it back into its unique ID. Accordingly,
    /// I now know which element's value has changed.
    /// </summary>
    public enum GuiMenuElementID {

        None,

        #region Checkboxes

        CameraRollCheckbox,

        ResetOnFocusCheckbox,

        PauseOnLoadCheckbox,

        ZoomOutOnCursorCheckbox,

        ElementIconsCheckbox,

        #endregion

        #region PopupLists

        HumanPlayerSpeciesPopupList,

        AIPlayer1SpeciesPopupList,

        AIPlayer2SpeciesPopupList,

        AIPlayer3SpeciesPopupList,

        AIPlayer4SpeciesPopupList,

        AIPlayer5SpeciesPopupList,

        AIPlayer6SpeciesPopupList,

        AIPlayer7SpeciesPopupList,


        HumanPlayerColorPopupList,

        AIPlayer1ColorPopupList,

        AIPlayer2ColorPopupList,

        AIPlayer3ColorPopupList,

        AIPlayer4ColorPopupList,

        AIPlayer5ColorPopupList,

        AIPlayer6ColorPopupList,

        AIPlayer7ColorPopupList,


        UniverseSizePopupList,

        GameSpeedOnLoadPopupList,

        QualitySettingPopupList,

        SavedGamesPopupList

        #endregion

        #region Sliders

        //GameSpeedSlider

        #endregion

    }
}

