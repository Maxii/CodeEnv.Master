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
        /// The radius around the Item to use for the highlight when hovered.
        /// This value is used by the SphericalHighlight effect.
        /// </summary>
        float HoverHighlightRadius { get; }

        /// <summary>
        /// The radius around the Item to use for a normal highlight.
        /// This value is used by the HighlightCircle effect.
        /// </summary>
        float HighlightRadius { get; }

    }
}

