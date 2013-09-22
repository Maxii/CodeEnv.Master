// --------------------------------------------------------------------------------------------------------------------
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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Enum of selection criteria that can be used to pick
    /// an icon image. Used in conjuction with IconSection.
    /// </summary>
    public enum IconSelectionCriteria {

        None,
        Science,
        Troops,
        Colony,
        Strength0,
        Strength1,
        IntelLevelUnknown
    }
}

