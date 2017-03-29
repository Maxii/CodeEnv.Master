// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IRecurringDateMinderClient.cs
// Interface for clients that use GameTime's RecurringDateMinder as the mechanism by which
// they get notified when dates are reached, separated by a recurring GameTimeDuration.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for clients that use GameTime's RecurringDateMinder as the mechanism by which
    /// they get notified when dates are reached, separated by a recurring GameTimeDuration.
    /// </summary>
    public interface IRecurringDateMinderClient {

        string DebugName { get; }

        void HandleDateReached(DateMinderDuration recurringDuration);

    }
}

