// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameClockSpeed.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {


    public enum GameClockSpeed {

        [EnumAttribute("Error! No Game Clock Speed.")]
        None = 0,

        [EnumAttribute("Quarter Speed")]    // TODO figure way to localize
        Slowest = 1,

        [EnumAttribute("Half Speed")]
        Slow = 2,

        [EnumAttribute("Normal Speed")]
        Normal = 3,

        [EnumAttribute("Double Speed")]
        Fast = 4,

        [EnumAttribute("Quadruple Speed")]
        Fastest = 5
    }
}

