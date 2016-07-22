// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IHighlightable.cs
// Interface for Items that can be highlighted.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for Items that can be highlighted.
    /// </summary>
    public interface IHighlightable : IWidgetTrackable {

        /// <summary>
        /// The radius around the Item to use for a SphericalHighlight effect.
        /// </summary>
        float SphericalHighlightEffectRadius { get; }

        /// <summary>
        /// The radius around the Item to use for a CircleHighlight effect.
        /// </summary>
        float CircleHighlightEffectRadius { get; }

    }
}

