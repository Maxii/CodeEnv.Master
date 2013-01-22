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

namespace CodeEnv.Master.Common {

    using System;
    using CodeEnv.Master.Common.Resources;

    /// <summary>
    /// The specialized enum itself.
    /// </summary>
    /// <remarks>Use Enum.isDefined() to test Direction for validity before using in a switch statement.
    /// C# enums are not Type-safe.</remarks>
    public enum Direction {
        None = 0,
        [EnumAttribute("Left")]   // IMPROVE Resource strings cannot be used in an Attribute argument as they aren't available at compile time
        Left = 1,
        [EnumAttribute("Right")]
        Right = 2,
        [EnumAttribute("Up")]
        Up = 3,
        [EnumAttribute("Down")]
        Down = 4
    }

}
