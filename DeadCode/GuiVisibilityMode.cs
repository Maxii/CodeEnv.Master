// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiVisibilityMode.cs
// Mode that determines whether selected Gui elements are visible or hidden.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Mode that determines whether selected Gui elements are visible or hidden.
    /// </summary>
    [System.Obsolete]
    public enum GuiVisibilityMode {

        None,

        /// <summary>
        /// Selected Gui elements previously hidden are made visible.
        /// </summary>
        Visible,

        /// <summary>
        /// SelectedGui elements that were visible are now hidden.
        /// </summary>
        Hidden
    }

}

