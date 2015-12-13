// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File:HudFormID.cs
// Unique identifier for each HudForm.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Unique identifier for each HudForm.
    /// </summary>
    [System.Obsolete]
    public enum HudFormID {

        None,

        Text,

        Resource,

        System,

        Starbase,

        Settlement,

        Fleet,

        Ship

    }
}

