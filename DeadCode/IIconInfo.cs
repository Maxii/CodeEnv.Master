// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IIconInfo.cs
// Interface for the info used to build icons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Interface for the info used to build icons.
    /// </summary>
    [System.Obsolete]
    public interface IIconInfo {

        AtlasID AtlasID { get; }

        WidgetPlacement Placement { get; }

        Vector2 Size { get; }

        /// <summary>
        /// The filename used to find the sprite in the atlas.
        /// </summary>
        string Filename { get; }

        GameColor Color { get; }

    }
}

