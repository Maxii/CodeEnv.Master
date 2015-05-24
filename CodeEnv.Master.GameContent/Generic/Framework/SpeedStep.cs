// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SpeedStep.cs
//  Enum indicating the amount of increase or decrease to make from one Speed constant to another.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum indicating the amount of increase or decrease to make from
    /// one Speed constant to another. e.g. A <c>Minimum</c> decrease
    /// from Speed.TwoThirds gives Speed.OneThird.
    /// </summary>
    public enum SpeedStep {

        None,

        Minimum,

        One,

        Two,

        Three,

        Four,

        Five,

        Maximum

    }
}

