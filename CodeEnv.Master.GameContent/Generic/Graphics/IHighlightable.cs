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

        float HoverHighlightRadius { get; }

        float HighlightRadius { get; }

    }
}

