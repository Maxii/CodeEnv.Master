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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for Items that can be selected by the User.
    /// </summary>
    public interface ISelectable {

        bool IsSelected { get; set; }

        string DisplayName { get; }

    }
}

