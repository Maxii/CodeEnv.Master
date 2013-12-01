// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetoidType.cs
// Enum denoting types of planetoids including both planets and moons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

using CodeEnv.Master.Common;
namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum denoting types of planetoids including both planets and moons.
    /// </summary>
    public enum PlanetoidType {

        None,

        [EnumAttribute("Earth-like")]
        Terrestrial,

        [EnumAttribute("Volcanic")]
        Volcanic,

        [EnumAttribute("Frozen Ball")]
        Ice,

        [EnumAttribute("Gas Giant")]
        GasGiant,

        // TODO change to my own names like DesolateMoon, VolcanicMoon, etc.
        // will also need to align the name of the Moon gameobject with this change
        // as parsing the name of the gameobject which holds a mesh and material
        // below it is how I figure out the type
        Moon_001,

        Moon_002,

        Moon_003,

        Moon_004,

        Moon_005

    }
}

