// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IconSection.cs
//  Enum of sections of an icon that an image can be applied too.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Enum of sections of an icon that an image can be applied too.
    /// Used in conjunction with IconSelectionCriteria.
    /// </summary>
    public enum IconSection {

        Base,
        Top,
        Bottom,
        Left,
        Right,
        Layer0,
        Layer1

    }
}

