// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IDateMinderClient.cs
// Interface for clients that use GameTime's DateMinder.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for clients that use GameTime's DateMinder.
    /// </summary>
    public interface IDateMinderClient {

        string DebugName { get; }

        void HandleDateReached(GameDate date);

    }
}

