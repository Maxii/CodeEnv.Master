// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AEnumValuesHelper.cs
// Generic inheritable Singleton that parses Xml documents used to provide externalized 
// values to the properties of the derived class in support of Enums.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Xml;
    using System.Xml.Linq;

    /// <summary>
    /// Generic inheritable Singleton that parses Xml documents used to provide externalized
    /// values to the properties of the derived class in support of Enums.
    /// </summary>
    /// <typeparam name="T">The derived class type.</typeparam>
    public abstract class AEnumValuesHelper<T> : AValuesHelper<T> where T : class {

        private static string _enumTypeAttributeName = "EnumTypeName";

        /// <summary>
        /// The type of the enum being supported by this AEnumValues derived class.
        /// </summary>
        protected abstract Type EnumType { get; }

        /// <summary>
        /// The tag name of the root of the Xml document.
        /// </summary>
        protected override string RootTagName { get { return "EnumValues"; } }

        /// <summary>
        /// The name of the XML document without extension. Default is the name of the EnumType.
        /// </summary>
        protected override string XmlFilename {
            get { return EnumType.Name; }
        }

        protected override bool ValidateElement(XElement xElement) {
            bool isBaseValid = base.ValidateElement(xElement);
            bool isDerivedValid = EnumType.Name.Equals(xElement.Attribute(_enumTypeAttributeName).Value);
            return isBaseValid && isDerivedValid;
        }
    }
}


