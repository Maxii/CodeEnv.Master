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
                case GuiElementID.GameSpeedOnLoadPopupList:
                    return "GameSpeedOnLoad";
                case GuiElementID.UserPlayerSpeciesPopupList:
                    return "UserPlayerSpeciesSelection";
                case GuiElementID.UserPlayerColorPopupList:
                    return "UserPlayerColor";
                case GuiElementID.PauseOnLoadCheckbox:
                    return "IsPauseOnLoadEnabled";
                case GuiElementID.QualitySettingPopupList:
                    return "QualitySetting";
                case GuiElementID.ResetOnFocusCheckbox:
                    return "IsResetOnFocusEnabled";
                case GuiElementID.UniverseSizePopupList:
                    return "UniverseSizeSelection";
                case GuiElementID.ZoomOutOnCursorCheckbox:
                    return "IsZoomOutOnCursorEnabled";
                default:
                    return null;
            }
        }

    }
}

