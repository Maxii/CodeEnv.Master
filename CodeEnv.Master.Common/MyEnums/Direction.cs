// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
// Derived from finnw on stackoverflow.com.
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Direction.cs
// My initial enum pattern to allow for Java-like usage.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common.MyEnums {

    using System;

    /// <summary>
    /// The specialized enum itself.
    /// </summary>
    /// <remarks>Use Enum.isDefined() to test Direction for validity before using in a switch statement.
    /// C# enums are not enumType-safe.</remarks>
    public enum Direction {
        [EnumAttribute("Left")]
        LEFT = 1,
        [EnumAttribute("Right")]
        RIGHT = 2,
        [EnumAttribute("Up")]
        UP = 4,
        [EnumAttribute("Down")]
        DOWN = 8
    }
}
