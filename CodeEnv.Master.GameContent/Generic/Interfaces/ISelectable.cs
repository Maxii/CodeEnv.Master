// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ISelectable.cs
// Interface for Items that can be selected by the User.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for Items that can be selected by the User.
    /// </summary>
    public interface ISelectable {

        string DebugName { get; }

        /// <summary>
        /// Indicates whether this item is the current selection in the game.
        /// <remarks>Anyone can set this property to true to become the Item selected. Only SelectionManager 
        /// should set this property to false. If you wish to remove the selected state from an Item, you 
        /// should set SelectionManager's CurrentSelection property to null.</remarks>
        /// </summary>
        bool IsSelected { get; set; }


    }
}

