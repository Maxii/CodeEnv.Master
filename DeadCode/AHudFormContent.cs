// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AHudFormContent.cs
// Abstract base class for contents associated with customized HudForms.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Abstract base class for contents associated with customized HudForms.
    /// </summary>
    [System.Obsolete]
    public abstract class AHudFormContent {

        public HudFormID FormID { get; private set; }

        public AHudFormContent(HudFormID formID) {
            FormID = formID;
        }
    }
}

