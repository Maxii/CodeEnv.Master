// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IViewable.cs
//  Interface used by Presenters to communicate with their associated Views.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    ///  Interface used by Presenters to communicate with their associated Views.
    /// </summary>
    public interface IViewable {

        /// <summary>
        /// Provides the ability to update the text for the GuiCursorHud. Can be null if there
        /// is no data for the GuiCursorHud to show for this item.
        /// </summary>
        IGuiHudPublisher HudPublisher { get; set; }

        IntelLevel PlayerIntelLevel { get; set; }

        /// <summary>
        /// The radius in units of the conceptual 'globe' that
        /// encompasses this gameObject.
        /// </summary>
        float Radius { get; }

    }
}

