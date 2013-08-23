// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiVisibilityCommand.cs
// Command executed by the GuiManager that determines whether Gui elements
// containing UIPanels are hidden or restored to visibility.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {


    /// <summary>
    /// Command executed by the GuiManager that determines whether Gui elements
    // containing UIPanels are hidden or restored to visibility.
    /// </summary>
    public enum GuiVisibilityCommand {

        None,

        /// <summary>
        /// Command executed by the GuiManager that restores the visibility of all UIPanels
        /// that were previously made invisible (via gameobject.active) by the HideVisibleGuiPanels
        /// command. Restoration to being visible does not occur on those UIPanels listed as exceptions.
        /// </summary>
        RestoreInvisibleGuiPanels,

        /// <summary>
        /// Command executed by the GuiManager that makes all currently visible UIPanels
        /// invisible (via gameobject.active). UIPanels listed as exceptions will be excluded.
        /// </summary>
        HideVisibleGuiPanels
    }

}

