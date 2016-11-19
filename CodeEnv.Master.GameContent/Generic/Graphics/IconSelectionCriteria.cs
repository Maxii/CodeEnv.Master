﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IconSelectionCriteria.cs
// Enum of selection criteria that can be used to pick an icon image.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum of selection criteria that can be used to pick
    /// an icon image. Used in conjunction with IconSection.
    /// </summary>
    public enum IconSelectionCriteria {

        None,
        Troop,
        Colony,
        Science,
        Level1,
        Level2,
        Level3,
        Level4,
        Level5,
        Unknown

        // WARNING: Name changes here must also be implemented in Icon.xml files

    }
}

