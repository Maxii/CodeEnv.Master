// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Tags.cs
// Simple Enum for Tags that avoids typing out strings. Use [TagsConstant].GetName() extension.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common.Game {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.Resources;

    /// <summary>
    /// Simple Enum for Tags that avoids typing out strings. Use [TagsConstant].GetName() extension.
    /// </summary>
    public enum Tags {

        Untagged,

        Respawn,

        Finish,

        EditorOnly,

        MainCamera,
        [Obsolete]
        Player,
        [Obsolete]
        GameController,

        FpsHUD

    }
}

