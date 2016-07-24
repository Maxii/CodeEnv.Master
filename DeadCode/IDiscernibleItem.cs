// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IDiscernibleItem.cs
// Interface for easy access to MonoBehaviours that are ADiscernibleItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for easy access to MonoBehaviours that are ADiscernibleItems.
    /// </summary>
    [System.Obsolete]
    public interface IDiscernibleItem : IItem {

        bool IsDiscernibleToUser { get; }

    }
}

