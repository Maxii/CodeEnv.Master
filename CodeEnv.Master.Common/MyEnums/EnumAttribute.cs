// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: EnumAttribute.cs
// TODO - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common.MyEnums {

    using System;

    /// <summary>
    /// Uses Attribute metadata to capture whatever values we want to associate with each Direction Value.
    /// </summary>
    /// 
    /// <remarks>Attribute parameters can only be a) primitive types, b) the Type enumType, c) an enum enumType
    /// or d) a one-dimensional array of any of these.
    /// <para>Uses Reflection so performance is slow. TODO - use Dictionary lookup after initial use.</para>
    /// </remarks>
    /// 
    public sealed class EnumAttribute : Attribute {

        /// <summary>
        /// Constructor that initializes a new instance of the <see cref="EnumAttribute"/> class.
        /// </summary>
        /// <param friendlyDescription="friendlyDescription">A user-friendly description.</param>
        internal EnumAttribute(string friendlyDescription) {
            FriendlyDescription = friendlyDescription;
        }

        public string FriendlyDescription { get; private set; }
    }
}

