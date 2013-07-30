// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: EnumAttribute.cs
// Uses Attribute metadata to capture whatever values we want to associate with enum values.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System;

    /// <summary>
    /// Uses Attribute metadata to capture whatever values we want to associate with enum values.
    /// </summary>
    /// 
    /// <remarks>Attribute parameters can only be a) primitive types, b) the Type enum, c) an enum enumType
    /// or delegateWithInvocationList) a one-dimensional array of any of these.
    /// <para>Uses Reflection so performance is slow. TODO - use Dictionary lookup after initial use.</para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]   // Custom Attributes - see Pro .NET 3.5 Page 548
    public sealed class EnumAttribute : Attribute {

        /// <summary>
        /// Constructor that initializes a new instance of the <see cref="EnumAttribute"/> class.
        /// </summary>
        /// <param description="description">An alternative description.</param>
        public EnumAttribute(string description) {
            Description = description;
        }

        public string Description { get; private set; }
    }
}

