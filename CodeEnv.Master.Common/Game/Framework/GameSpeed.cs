// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameSpeed.cs
// Enum identifying the speed at which the GameClock is running.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {


    /// <summary>
    /// Enum identifying the speed at which the GameClock is running.
    /// The speed multiplier is acquired from GameSpeedExtensions as
    /// GameSpeed.value.SpeedMultiplier();
    /// </summary>
    public enum GameSpeed {

        [EnumAttribute("Error! No Game Speed.")]
        None,

        [EnumAttribute("Quarter Speed")]    //TODO figure way to localize
        Slowest,

        [EnumAttribute("Half Speed")]
        Slow,

        [EnumAttribute("Normal Speed")]
        Normal,

        [EnumAttribute("Double Speed")]
        Fast,

        [EnumAttribute("Quadruple Speed")]
        Fastest
    }
}

