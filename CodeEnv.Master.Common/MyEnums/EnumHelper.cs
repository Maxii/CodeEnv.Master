// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: EnumHelper.cs
// TODO - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common.MyEnums {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// Static class encapsulating extension methods for Direction, allowing
    /// usage like: string friendlyDescription = Direction.FIRST.GetDescription()
    /// </summary>
    public static class EnumHelper {

        /// <summary>Gets the string name of the enum constant. Equivalent to enumConstant.toString().</summary>
        /// <param name="enumConstant">The enum constant.</param>
        /// <returns>The string name of this enumConstant. </returns>
        /// <remarks>Not localizable. For localizable descriptions, use GetDescription().</remarks>
        public static string GetName(this Enum enumConstant) {
            return enumConstant.ToString();
        }

        /// <summary>Gets the friendly description.</summary>
        /// <param friendlyDescription="enumConstant">The named enum constant.</param>
        /// <returns>A friendly description as a string or toString() if no <see cref="EnumAttribute"/> is present.</returns>
        public static string GetDescription(this Enum enumConstant) {
            if (enumConstant == null) {
                throw new ArgumentNullException("enumConstant");
            }

            EnumAttribute attribute = GetAttribute(enumConstant);
            if (attribute == null) {
                return enumConstant.ToString();
            }
            return attribute.FriendlyDescription;
            // TODO convert to a localizable version using resources
        }

        /// <summary>
        /// Converts the <see cref="Enum" /> enumType to an <see cref="IList" /> 
        /// compatible object.
        /// </summary>
        /// <param friendlyDescription="enumType">The <see cref="Enum"/> Type of the enum.</param>
        /// <returns>An <see cref="IList"/> containing the enumerated
        /// values (key) and descriptions of the provided Type.</returns>
        public static IList ToList(this Type enumType) {
            if (enumType == null) {
                throw new ArgumentNullException("enumType");
            }

            ArrayList list = new ArrayList();
            Array enumValues = Enum.GetValues(enumType);

            foreach (Enum value in enumValues) {
                list.Add(new KeyValuePair<Enum, string>(value, GetDescription(value)));
            }
            
            return list;
        }

        private static EnumAttribute GetAttribute(Enum enumConstant) {
            return (EnumAttribute)Attribute.GetCustomAttribute(ForValue(enumConstant), typeof(EnumAttribute));
        }

        private static MemberInfo ForValue(Enum enumConstant) {
            Type enumType = enumConstant.GetType();
            return enumType.GetField(Enum.GetName(enumType, enumConstant));
        }
    }   
}

