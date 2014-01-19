// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IntelSource.cs
// The current realtime source of Intel data for an object.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// The current realtime source of Intel data for an object.
    /// </summary>
    public enum IntelSource {

        /// <summary>
        /// Their is no current source of intel data.
        /// </summary>
        None,

        /// <summary>
        /// The current source of intel data is from LongRangeSensors.
        /// </summary>
        LongRangeSensors,

        /// <summary>
        /// The current source of intel data is from MediumRangeSensors.
        /// </summary>
        MediumRangeSensors,

        /// <summary>
        /// The current source of intel data is from ShortRangeSensors.
        /// </summary>
        ShortRangeSensors,

        /// <summary>
        /// The current source of intel data is from the all knowing InfoNet.
        /// </summary>
        InfoNet

    }
}

