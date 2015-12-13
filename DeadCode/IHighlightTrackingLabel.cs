// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IHighlightTrackingLabel.cs
// Interface implemented by classes that want clients to be able to highlight their tracking label.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface implemented by classes that want clients to be able to highlight
    /// their tracking label.
    /// </summary>
    public interface IHighlightTrackingLabel {

        void HighlightTrackingLabel(bool toHighlight);

    }
}

