// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ISelectable.cs
// Interface that supports the ability to select a game object.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Interface that supports the ability to select a game object.
    /// </summary>
    public interface ISelectable {

        void OnLeftClick();

        bool IsSelected { get; set; }

    }
}

