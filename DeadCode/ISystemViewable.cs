// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ISystemViewable.cs
//  Interface used by SystemPresenters to communicate with their associated SystemViews.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    ///  Interface used by SystemPresenters to communicate with their associated SystemViews.
    /// </summary>
    public interface ISystemViewable : IViewable {

        void HighlightTrackingLabel(bool toHighlight);

    }
}

