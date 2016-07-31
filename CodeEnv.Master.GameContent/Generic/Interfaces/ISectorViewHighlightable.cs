// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ISectorViewHighlightable.cs
// Interface for Items that can be highlighted when contained within
// a sector that itself is highlighted in SectorView mode.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for Items that can be highlighted when contained within
    /// a sector that itself is highlighted in SectorView mode.
    /// </summary>
    public interface ISectorViewHighlightable : IWidgetTrackable {

        bool IsSectorViewHighlightShowing { get; }

        void ShowSectorViewHighlight(bool toShow);

    }
}

