// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseSizeGuiSelection.cs
// The Universe size choices that can be selected from the Gui.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// The Universe size choices that can be selected from the Gui.
    /// </summary>
    public enum UniverseSizeGuiSelection {

        None,

        Random,

        [EnumAttribute("The smallest.")]
        Tiny,

        [EnumAttribute("Smaller than Normal.")]
        Small,

        [EnumAttribute("The most common.")]
        Normal,

        [EnumAttribute("Larger than Normal.")]
        Large,

        [EnumAttribute("Larger than Large.")]
        Enormous,

        [EnumAttribute("The largest.")]
        Gigantic

    }
}

