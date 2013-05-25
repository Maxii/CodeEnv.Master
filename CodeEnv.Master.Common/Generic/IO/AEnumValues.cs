// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AEnumValues.cs
// Generic inheritable Singleton that parses Xml documents used to provide externalized 
// values to the properties of the derived class in support of Enums.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR


namespace CodeEnv.Master.Common {

    using System;
    using System.Xml;

    /// <summary>
    /// Generic inheritable Singleton that parses Xml documents used to provide externalized
    /// values to the properties of the derived class in support of Enums.
    /// </summary>
    /// <typeparam name="T">The derived class type.</typeparam>
    public abstract class AEnumValues<T> : AValues<T> where T : class {

        /// <summary>
        /// The type of the enum being supported by this AEnumValues derived class.
        /// </summary>
        protected abstract Type EnumType { get; }

        /// <summary>
        /// The tag name of the root of the Xml document.
        /// </summary>
        protected override string RootTagName {
            get {
                return "EnumValues";
            }
        }

        /// <summary>
        /// The name of the XML document without extension.
        /// </summary>
        protected override string DocumentName {
            get { return EnumType.Name; }
        }


        private string enumTypeAttributeName = "EnumTypeName";

        protected override bool ValidateDocument(XmlDocument doc) {
            string enumTypeName = doc.DocumentElement.GetAttribute(enumTypeAttributeName);
            if (enumTypeName != EnumType.Name) {
                D.Error("Enum Type {0} does not match Type {1} specified in DataLibrary XML doc.".Inject(EnumType.Name, enumTypeName));
                return false;
            }
            return true;
        }
    }
}


