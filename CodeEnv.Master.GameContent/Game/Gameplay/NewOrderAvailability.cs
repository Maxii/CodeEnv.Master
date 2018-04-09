// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NewOrderAvailability.cs
// Enum indicating how available a Unit or Element is to receiving new orders.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum indicating how available a Unit or Element is to receiving new orders.
    /// </summary>
    public enum NewOrderAvailability {

        /// <summary>
        /// Error detection.
        /// </summary>
        None = 0,

        /// <summary>
        /// The Element is not available to receive and execute new orders with only a couple of exceptions.
        /// <remarks>An Element that is Constructing, Refitting or Disbanding is not available to receive and execute any new order except scuttle.</remarks>
        /// <remarks>A Unit which has instructed its Elements to Construct, Refit or Disband is not available to receive any new order except scuttle or ChangeHQ.</remarks>
        /// </summary>
        Unavailable = 1,

        /// <summary>
        /// The Unit or Element is executing critical orders but is available to receive and execute life and death orders.
        /// <remarks>Example: An Element or Unit undergoing critical repairs should only be issued new orders in an emergency.</remarks>
        /// <remarks>Example: A Fleet in the final stage of delivering ships to a hanger for refit should only be issued new orders in an emergency.</remarks>
        /// </summary>
        BarelyAvailable = 2,

        /// <summary>
        /// The Unit or Element is executing important orders but is available to receive and execute higher priority orders.
        /// <remarks>Example: An Element or Unit undergoing significant repairs is executing important orders.</remarks>
        /// <remarks>Example: A Fleet underway to deliver ships to a hanger for refitting or disbanding is executing important orders.</remarks>
        /// </summary>
        FairlyAvailable = 3,

        /// <summary>
        /// The Unit or Element is executing relatively unimportant orders and is available to receive and execute new orders.
        /// <remarks>Example: An Element or Unit that is AssumingStation/Formation or undergoing minor repairs.</remarks>
        /// </summary>
        EasilyAvailable = 4,

        /// <summary>
        /// The Unit or Element is not executing any orders and is available to receive and execute new orders.
        /// <remarks>An Element or Unit that is Idling.</remarks>
        /// </summary>
        Available = 5

    }
}

