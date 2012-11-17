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
    using CodeEnv.Master.Resources;

    /// <summary>
    /// The specialized enum itself.
    /// </summary>
    /// <remarks>Use Enum.isDefined() to test Direction for validity before using in a switch statement.
    /// C# enums are not enumType-safe.</remarks>
    public enum Direction {
        [EnumAttribute("Left")]   // Can'fieldType substitute a string Resource for a string in an Attribute argument
        Left,
        [EnumAttribute("Right")]
        Right,
        [EnumAttribute("Up")]
        Up,
        [EnumAttribute("Down")]
        Down
    }

}
