// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AXmlReader.cs
// Generic abstract Singleton that parses Xml files used to provide externalized values to the derived class.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System.Xml.Linq;

    /// <summary>
    /// Generic abstract Singleton that parses Xml files used to provide externalized values to the derived class.
    /// </summary>
    /// <typeparam name="T">The derived class type.</typeparam>
    public abstract class AXmlReader<T> : AGenericSingleton<T> where T : AXmlReader<T> {

        private const string XmlFilepathFormat = "{0}{1}.xml";

        /// <summary>
        /// The tag name of the root of the Xml DOM.
        /// </summary>
        protected virtual string RootTagName { get { return "Values"; } }

        /// <summary>
        /// The name of the Xml file without extension. Default is the name of the derived class T.
        /// </summary>
        protected virtual string XmlFilename { get { return typeof(T).Name; } }

        protected XElement _xElement;

        ///<summary>
        /// IMPORTANT: This must be called from the PRIVATE constructor in the derived class.
        /// </summary>
        protected sealed override void Initialize() {
            _xElement = LoadAndValidateXElement();
            InitializeValuesAndReferences();
        }

        /// <summary>
        /// Initializes any values and references required. 
        /// Called from the constructor after the XElement is loaded and validated.
        /// </summary>
        protected virtual void InitializeValuesAndReferences() { }

        private XElement LoadAndValidateXElement() {
            string xmlFilePath = XmlFilepathFormat.Inject(UnityConstants.DataLibraryDir, XmlFilename);  //string xmlFilePath = UnityConstants.DataLibraryDir + XmlFilename + ".xml";
            D.Log("The path to the Xml file is {0}.", xmlFilePath);
            XElement xElement = XElement.Load(xmlFilePath);
            D.Assert(ValidateElement(xElement), "Invalid XDocument found at {0}.", xmlFilePath);
            return xElement;
        }

        protected virtual bool ValidateElement(XElement xElement) {
            return RootTagName.Equals(xElement.Name.ToString());
        }

    }
}

