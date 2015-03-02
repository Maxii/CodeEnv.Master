// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IHighlighter.cs
// Interface for IHighlightable object highlighters.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for IHighlightable object highlighters.
    /// </summary>
    public interface IHighlighter {

        /// <summary>
        /// Shows the indicated Highlights on or around the IHighlightable object.
        /// Any IDs not provided will be hidden.
        /// </summary>
        /// <param name="highlightIDs">The highlightIDs.</param>
        void Show(params HighlightID[] highlightIDs);

        /// <summary>
        /// Shows the hovered Highlight around the IHighlightable object.
        /// </summary>
        /// <param name="toShow">if set to <c>true</c> [to show].</param>
        void ShowHovered(bool toShow);

    }
}

