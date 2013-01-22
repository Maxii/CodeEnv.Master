// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: EnumHelper.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using CodeEnv.Master.Common.Resources;

    /// <summary>
    /// Static class encapsulating extension methods for Direction, allowing
    /// usage like: string friendlyDescription = Direction.FIRST.GetDescription()
    /// </summary>
    public static class EnumExtensions {

        /// <summary>Gets the string name of the enum constant. Much faster than sourceEnumConstant.ToString().</summary>
        /// <param name="sourceEnumConstant">The enum constant.</param>
        /// <returns>The string name of this sourceEnumConstant. </returns>
        /// <remarks>Not localizable. For localizable descriptions, use GetDescription().</remarks>
        public static string GetName(this Enum sourceEnumConstant) {
            //  'this' sourceEnumConstant can never be null without the CLR throwing a Null reference exception
            Type enumType = sourceEnumConstant.GetType();
            return Enum.GetName(enumType, sourceEnumConstant);    // OPTIMIZE better performance than sourceEnumConstant.ToString()?
        }

        /// <summary>Gets the friendly description.</summary>
        /// <param friendlyDescription="sourceEnumConstant">The named enum constant.</param>
        /// <returns>A friendly description as a string or toString() if no <see cref="EnumAttribute"/> is present.</returns>
        public static string GetDescription(this Enum sourceEnumConstant) {
            //  'this' sourceEnumConstant can never be null without the CLR throwing a Null reference exception
            EnumAttribute attribute = GetAttribute(sourceEnumConstant);
            if (attribute == null) {
                return GetName(sourceEnumConstant);
            }
            return attribute.FriendlyDescription;
        }

        /// <summary>
        /// Converts the <see cref="Enum" /> sourceEnumType to an <see cref="IList" /> 
        /// compatible object.
        /// </summary>
        /// <param friendlyDescription="sourceEnumType">The <see cref="Enum"/> Type of the enum.</param>
        /// <returns>An <see cref="IList"/> containing the enumerated
        /// values (key) and descriptions of the provided Type.</returns>
        public static IList ToList(this Type sourceEnumType) {
            //  'this' sourceEnumType can never be null without the CLR throwing a Null reference exception

            ArrayList list = new ArrayList();
            Array enumValues = Enum.GetValues(sourceEnumType);

            foreach (Enum value in enumValues) {
                list.Add(new KeyValuePair<Enum, string>(value, GetDescription(value)));
            }
            return list;
        }

        private static EnumAttribute GetAttribute(Enum enumConstant) {
            EnumAttribute attribute = Attribute.GetCustomAttribute(ForValue(enumConstant), typeof(EnumAttribute)) as EnumAttribute;
            if (attribute == null) {
                Debug.WriteLine(ErrorMessages.EnumNoAttribute.Inject(enumConstant.GetName(), typeof(EnumAttribute).Name));
            }
            return attribute;
        }

        private static MemberInfo ForValue(Enum enumConstant) {
            Type enumType = enumConstant.GetType();
            return enumType.GetField(Enum.GetName(enumType, enumConstant));
        }
    }
}

