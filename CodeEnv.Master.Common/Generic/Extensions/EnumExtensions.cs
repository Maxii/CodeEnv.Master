// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: EnumExtensions.cs
// Static class providing general extension methods for enums.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using CodeEnv.Master.Common.LocalResources;


    /// <summary>
    /// Static class providing general extension methods for enums.
    /// </summary>
    public static class EnumExtensions {

        /// <summary>Gets the string name of the enum constant. Much faster than sourceEnumConstant.ToString().</summary>
        /// <param name="sourceEnumConstant">The enum constant.</param>
        /// <returns>The string name of this sourceEnumConstant. </returns>
        /// <remarks>Not localizable. For localizable descriptions, use GetDescription().</remarks>
        public static string GetName(this Enum sourceEnumConstant) {
            Type enumType = sourceEnumConstant.GetType();
            return Enum.GetName(enumType, sourceEnumConstant);    // OPTIMIZE better performance than sourceEnumConstant.ToString()?
        }

        /// <summary>Gets the alternative description from the EnumAttribute if present. If not, the name is returned.</summary>
        /// <param friendlyDescription="sourceEnumConstant">The named enum constant.</param>
        /// <returns>An alternative description as a string or toString() if no <see cref="EnumAttribute"/> is present.</returns>
        public static string GetDescription(this Enum sourceEnumConstant) {
            EnumAttribute attribute = GetAttribute(sourceEnumConstant);
            if (attribute == null) {
                return GetName(sourceEnumConstant);
            }
            return attribute.Description;
        }

        /// <summary>
        /// Converts the <see cref="Enum" /> sourceEnumType to an <see cref="IList" /> 
        /// compatible object of Descriptions.
        /// </summary>
        /// <param friendlyDescription="sourceEnumType">The <see cref="Enum"/> Type of the enum.</param>
        /// <returns>An <see cref="IList"/> containing the enumerated
        /// values (key) and descriptions of the provided Type.</returns>
        public static IList GetDescriptions(this Type sourceEnumType) {
            IList list = new ArrayList();
            Array enumValues = Enum.GetValues(sourceEnumType);

            foreach (Enum value in enumValues) {
                list.Add(new KeyValuePair<Enum, string>(value, GetDescription(value)));
            }
            return list;
        }

        private static EnumAttribute GetAttribute(Enum enumConstant) {
            EnumAttribute attribute = Attribute.GetCustomAttribute(ForValue(enumConstant), typeof(EnumAttribute)) as EnumAttribute;
            if (attribute == null) {
                D.Warn(ErrorMessages.EnumNoAttribute.Inject(enumConstant.GetName(), typeof(EnumAttribute).Name));
            }
            return attribute;
        }

        private static MemberInfo ForValue(Enum enumConstant) {
            Type enumType = enumConstant.GetType();
            return enumType.GetField(Enum.GetName(enumType, enumConstant));
        }
    }
}

