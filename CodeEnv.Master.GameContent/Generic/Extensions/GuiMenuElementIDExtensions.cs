// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiMenuElementIDExtensions.cs
// Extension methods for GuiMenuElementID values.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Extension methods for GuiMenuElementID values.
    /// </summary>
    public static class GuiMenuElementIDExtensions {

        /// <summary>
        /// Returns the PlayerPrefs Property Name associated with this GuiMenuElementID.
        /// Returns null if the elementID has no PlayerPrefs Property associated with it.
        /// </summary>
        /// <param name="elementID">The ID for this Gui element.</param>
        /// <returns></returns>
        public static string PreferencePropertyName(this GuiElementID elementID) {
            switch (elementID) {
                case GuiElementID.CameraRollCheckbox:
                    return "IsCameraRollEnabled";
                case GuiElementID.ElementIconsCheckbox:
                    return "IsElementIconsEnabled";
                case GuiElementID.PauseOnLoadCheckbox:
                    return "IsPauseOnLoadEnabled";
                case GuiElementID.ResetOnFocusCheckbox:
                    return "IsResetOnFocusEnabled";
                case GuiElementID.ZoomOutOnCursorCheckbox:
                    return "IsZoomOutOnCursorEnabled";
                case GuiElementID.GameSpeedOnLoadPopupList:
                    return "GameSpeedOnLoad";
                case GuiElementID.QualitySettingPopupList:
                    return "QualitySetting";
                case GuiElementID.UniverseSizePopupList:
                    return "UniverseSizeSelection";
                case GuiElementID.PlayerCountPopupList:
                    return "PlayerCount";

                case GuiElementID.UserPlayerSpeciesPopupList:
                    return "UserPlayerSpeciesSelection";
                case GuiElementID.AIPlayer1SpeciesPopupList:
                    return "AIPlayer1SpeciesSelection";
                case GuiElementID.AIPlayer2SpeciesPopupList:
                    return "AIPlayer2SpeciesSelection";
                case GuiElementID.AIPlayer3SpeciesPopupList:
                    return "AIPlayer3SpeciesSelection";
                case GuiElementID.AIPlayer4SpeciesPopupList:
                    return "AIPlayer4SpeciesSelection";
                case GuiElementID.AIPlayer5SpeciesPopupList:
                    return "AIPlayer5SpeciesSelection";
                case GuiElementID.AIPlayer6SpeciesPopupList:
                    return "AIPlayer6SpeciesSelection";
                case GuiElementID.AIPlayer7SpeciesPopupList:
                    return "AIPlayer7SpeciesSelection";

                case GuiElementID.UserPlayerColorPopupList:
                    return "UserPlayerColor";
                case GuiElementID.AIPlayer1ColorPopupList:
                    return "AIPlayer1Color";
                case GuiElementID.AIPlayer2ColorPopupList:
                    return "AIPlayer2Color";
                case GuiElementID.AIPlayer3ColorPopupList:
                    return "AIPlayer3Color";
                case GuiElementID.AIPlayer4ColorPopupList:
                    return "AIPlayer4Color";
                case GuiElementID.AIPlayer5ColorPopupList:
                    return "AIPlayer5Color";
                case GuiElementID.AIPlayer6ColorPopupList:
                    return "AIPlayer6Color";
                case GuiElementID.AIPlayer7ColorPopupList:
                    return "AIPlayer7Color";

                case GuiElementID.AIPlayer1IQPopupList:
                    return "AIPlayer1IQ";
                case GuiElementID.AIPlayer2IQPopupList:
                    return "AIPlayer2IQ";
                case GuiElementID.AIPlayer3IQPopupList:
                    return "AIPlayer3IQ";
                case GuiElementID.AIPlayer4IQPopupList:
                    return "AIPlayer4IQ";
                case GuiElementID.AIPlayer5IQPopupList:
                    return "AIPlayer5IQ";
                case GuiElementID.AIPlayer6IQPopupList:
                    return "AIPlayer6IQ";
                case GuiElementID.AIPlayer7IQPopupList:
                    return "AIPlayer7IQ";

                default:
                    return null;
            }
        }

    }
}

