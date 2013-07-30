// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AValuesHelper.cs
// Generic abstract Singleton that parses Xml files used to provide externalized 
// values to the properties of the derived class.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Reflection;
    using System.Xml.Linq;

    /// <summary>
    /// Generic abstract Singleton that parses Xml files used to provide externalized 
    /// values to the properties of the derived class.
    /// </summary>
    /// <typeparam name="T">The derived class type.</typeparam>
    public abstract class AValuesHelper<T> where T : class {

        private static string booleanTagName = "Boolean";
        private static string intTagName = "Integer";
        private static string floatTagName = "Float";
        private static string _settingTagName = "Setting";
        private static string propertyNameTagName = "PropertyName";
        private static string propertyValueTagName = "PropertyValue";

        /// <summary>
        /// The tag name of the root of the Xml DOM.
        /// </summary>
        protected virtual string RootTagName { get { return "Values"; } }

        /// <summary>
        /// The name of the Xml file without extension. Default is the name of the derived class T.
        /// </summary>
        protected virtual string XmlFilename { get { return typeof(T).Name; } }

        protected bool isPropertyValuesInitialized = false;

        private XElement _xElement;

        #region SingletonPattern

        private static T instance;

        /// <summary>Returns the singleton instance of this class.</summary>
        public static T Instance {
            get {
                if (instance == null) {
                    instance = (T)Activator.CreateInstance(typeof(T), true);
                }
                return instance;
            }
        }

        #endregion

        ///<summary>
        /// IMPORTANT: This must be called from the PRIVATE constructor in the derived class.
        /// </summary>
        protected virtual void Initialize() {
            _xElement = LoadAndValidateXElement();
        }

        private XElement LoadAndValidateXElement() {
            string xmlFilePath = UnityConstants.DataLibraryDir + XmlFilename + ".xml";
            D.Log("The path to the Xml file is {0}.", xmlFilePath);
            XElement xElement = XElement.Load(xmlFilePath);
            D.Assert(ValidateElement(xElement), "Invalid XDocument found at {0}.".Inject(xmlFilePath), pauseOnFail: true);
            return xElement;
        }

        protected virtual bool ValidateElement(XElement xElement) {
            return RootTagName.Equals(xElement.Name.ToString());
        }

        protected void InitializePropertyValues() {
            XElement topNode = _xElement.Element(booleanTagName);
            var settingNodes = topNode.Elements(_settingTagName);
            foreach (var settingNode in settingNodes) {
                XElement propertyNameNode = settingNode.Element(propertyNameTagName);
                if (!propertyNameNode.IsEmpty && !propertyNameNode.Value.Equals(string.Empty)) {    // Elements.isEmpty is true only if they are in the format <ElementName/> 
                    D.Log("Boolean PropertyName = {0}.", propertyNameNode.Value);
                    XElement propertyValueNode = settingNode.Element(propertyValueTagName);
                    string propertyValue = "false";
                    if (propertyValueNode.IsEmpty || propertyValueNode.Value.Equals(string.Empty)) {
                        D.Warn("Value of Property {0} is empty, defaulting to {1}.", propertyNameNode.Value, propertyValue);
                    }
                    else {
                        propertyValue = propertyValueNode.Value;
                        D.Log("Float PropertyValue = {0}.", propertyValue);
                    }
                    AssignValueToProperty(propertyNameNode.Value, bool.Parse(propertyValue));
                }
            }

            topNode = _xElement.Element(intTagName);
            settingNodes = topNode.Elements(_settingTagName);
            foreach (var settingNode in settingNodes) {
                XElement propertyNameNode = settingNode.Element(propertyNameTagName);
                if (!propertyNameNode.IsEmpty && !propertyNameNode.Value.Equals(string.Empty)) {    // Elements.isEmpty is true only if they are in the format <ElementName/> 
                    D.Log("Integer PropertyName = {0}.", propertyNameNode.Value);
                    XElement propertyValueNode = settingNode.Element(propertyValueTagName);
                    string propertyValue = "0";
                    if (propertyValueNode.IsEmpty || propertyValueNode.Value.Equals(string.Empty)) {
                        D.Warn("Value of Property {0} is empty, defaulting to {1}.", propertyNameNode.Value, propertyValue);
                    }
                    else {
                        propertyValue = propertyValueNode.Value;
                        D.Log("Float PropertyValue = {0}.", propertyValue);
                    }
                    AssignValueToProperty(propertyNameNode.Value, int.Parse(propertyValue));
                }
            }

            topNode = _xElement.Element(floatTagName);
            settingNodes = topNode.Elements(_settingTagName);
            foreach (var settingNode in settingNodes) {
                XElement propertyNameNode = settingNode.Element(propertyNameTagName);
                if (!propertyNameNode.IsEmpty && !propertyNameNode.Value.Equals(string.Empty)) {    // Elements.isEmpty is true only if they are in the format <ElementName/> 
                    D.Log("Float PropertyName = {0}.", propertyNameNode.Value);
                    XElement propertyValueNode = settingNode.Element(propertyValueTagName);
                    string propertyValue = "0.0";
                    if (propertyValueNode.IsEmpty || propertyValueNode.Value.Equals(string.Empty)) {
                        D.Warn("Value of Property {0} is empty, defaulting to {1}.", propertyNameNode.Value, propertyValue);
                    }
                    else {
                        propertyValue = propertyValueNode.Value;
                        D.Log("Float PropertyValue = {0}.", propertyValue);
                    }
                    AssignValueToProperty(propertyNameNode.Value, float.Parse(propertyValue));
                }
            }
            isPropertyValuesInitialized = true;
        }

        /// <summary>
        /// Assigns the value of Type P keyed by the property name in the Xml document to the property
        /// of the same name in the derived class.
        /// </summary>
        /// <typeparam name="P">The type of the value.</typeparam>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="value">The boolean value.</param>
        private void AssignValueToProperty<P>(string propertyName, P value) where P : struct {
            PropertyInfo propertyInfo = typeof(T).GetProperty(propertyName);
            if (propertyInfo == null) {
                D.Error("No {0} property named {1} found!".Inject(typeof(T).Name, propertyName));
                return;
            }
            Action<P> propertySet = (Action<P>)Delegate.CreateDelegate(typeof(Action<P>), Instance, propertyInfo.GetSetMethod(true));
            propertySet(value);
        }
    }
}

