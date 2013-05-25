// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AValues.cs
// Generic inheritable Singleton that parses Xml documents used to provide externalized 
// values to the properties of the derived class.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Reflection;
    using System.Xml;

    /// <summary>
    /// Generic inheritable Singleton that parses Xml documents used to provide externalized 
    /// values to the properties of the derived class.
    /// </summary>
    /// <typeparam name="T">The derived class type.</typeparam>
    public abstract class AValues<T> where T : class {

        /// <summary>
        /// The tag name of the root of the Xml document.
        /// </summary>
        protected virtual string RootTagName {
            get {
                return "Values";
            }
        }

        /// <summary>
        /// The name of the XML document without extension.
        /// </summary>
        protected abstract string DocumentName { get; }

        protected bool isPropertyValuesInitialized = false;

        private XmlDocument document;
        private string settingTagName = "Setting";

        // XPath criteria needed with XmlDocument
        private string booleanTagXPath;
        private string intTagXPath;
        private string floatTagXPath;

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
            booleanTagXPath = "/" + RootTagName + "/Boolean";
            intTagXPath = "/" + RootTagName + "/Integer";
            floatTagXPath = "/" + RootTagName + "/Float";

            document = LoadAndValidateDocument();
        }

        private XmlDocument LoadAndValidateDocument() {
            XmlDocument doc = new XmlDocument();

            string xmlDocumentPath = UnityConstants.DataLibraryDir + DocumentName + ".xml";
            doc.Load(xmlDocumentPath);
            if (!ValidateDocument(doc)) {
                D.Error("Document {0} not validated.", doc.Name);
            }
            return doc;
        }

        protected abstract bool ValidateDocument(XmlDocument doc);

        /// <summary>
        /// Acquires the value assigned to each property from the Xml document and assigns 
        /// it to the appropriate property. Call this method the first time any property is accessed.
        /// <remarks>Uses the old XmlDocument class which works with the WebPlayer API, unlike System.Xml.Linq</remarks>
        /// </summary>
        protected void InitializePropertyValues() {
            XmlNode topNode = document.SelectSingleNode(booleanTagXPath);
            XmlNodeList settingNodes = topNode.SelectNodes(settingTagName);
            foreach (XmlNode settingNode in settingNodes) {
                XmlNode propertyNameNode = settingNode.FirstChild;
                D.Log("PropertyNameNodeTag = {0}.", propertyNameNode.Name);
                string propertyName = propertyNameNode.InnerText;
                D.Log("Boolean PropertyName = {0}.", propertyName);
                if (Utility.CheckForContent(propertyName)) {
                    string propertyValue = settingNode.LastChild.InnerText;
                    D.Log("Boolean PropertyValue = {0}.", propertyValue);
                    if (!Utility.CheckForContent(propertyValue)) {
                        propertyValue = "false";    // empty property value nodes default to false
                    }
                    AssignValueToProperty(propertyName, bool.Parse(propertyValue));
                }
            }

            topNode = document.SelectSingleNode(intTagXPath);
            settingNodes = topNode.SelectNodes(settingTagName);
            foreach (XmlNode settingNode in settingNodes) {
                XmlNode propertyNameNode = settingNode.FirstChild;
                string propertyName = propertyNameNode.InnerText;
                D.Log("PropertyName = {0}.", propertyName);
                if (Utility.CheckForContent(propertyName)) {
                    string propertyValue = settingNode.LastChild.InnerText;
                    D.Log("PropertyValue = {0}.", propertyValue);
                    AssignValueToProperty(propertyName, int.Parse(propertyValue));
                }
            }

            topNode = document.SelectSingleNode(floatTagXPath);
            settingNodes = topNode.SelectNodes(settingTagName);
            foreach (XmlNode settingNode in settingNodes) {
                XmlNode propertyNameNode = settingNode.FirstChild;
                string propertyName = propertyNameNode.InnerText;
                D.Log("PropertyName = {0}.", propertyName);
                if (Utility.CheckForContent(propertyName)) {
                    string propertyValue = settingNode.LastChild.InnerText;
                    D.Log("PropertyValue = {0}.", propertyValue);
                    AssignValueToProperty(propertyName, float.Parse(propertyValue));
                }
            }
            isPropertyValuesInitialized = true;
        }

        /// <summary>
        /// Assigns the float value keyed by the property name in the Xml document to the property
        /// of the same name in the derived class.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="value">The float value.</param>
        private void AssignValueToProperty(string propertyName, float value) {
            Type derivedType = typeof(T);
            PropertyInfo propertyInfo = derivedType.GetProperty(propertyName);
            if (propertyInfo == null) {
                D.Error("No {0} property named {1} found!".Inject(derivedType.Name, propertyName));
                return;
            }
            Action<float> propertySet = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), Instance, propertyInfo.GetSetMethod());
            propertySet(value);
        }

        /// <summary>
        /// Assigns the int value keyed by the property name in the Xml document to the property
        /// of the same name in the derived class.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="value">The int value.</param>
        private void AssignValueToProperty(string propertyName, int value) {
            Type derivedType = typeof(T);
            PropertyInfo propertyInfo = derivedType.GetProperty(propertyName);
            if (propertyInfo == null) {
                D.Error("No {0} property named {1} found!".Inject(derivedType.Name, propertyName));
                return;
            }
            Action<int> propertySet = (Action<int>)Delegate.CreateDelegate(typeof(Action<int>), Instance, propertyInfo.GetSetMethod());
            propertySet(value);
        }

        /// <summary>
        /// Assigns the boolean value keyed by the property name in the Xml document to the property
        /// of the same name in the derived class.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="value">The boolean value.</param>
        private void AssignValueToProperty(string propertyName, bool value) {
            Type derivedType = typeof(T);
            PropertyInfo propertyInfo = derivedType.GetProperty(propertyName);
            if (propertyInfo == null) {
                D.Error("No {0} property named {1} found!".Inject(derivedType.Name, propertyName));
                return;
            }
            Action<bool> propertySet = (Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>), Instance, propertyInfo.GetSetMethod());
            propertySet(value);
        }

        // ---------------------------- Keep for future reference - Xml.Linq approach, not supported by the Webplayer API ----------------------
        //using System.Xml.Linq;

        //private string booleanTagName = "Boolean";
        //private string intTagName = "Integer";
        //private string floatTagName = "Float";
        //private string propertyNameTagName = "PropertyName";
        //private string propertyValueTagName = "PropertyValue";


        // uses System.Xml.Linq which is not supported by the WebPlayer API
        //[Obsolete]
        //private void InitializePropertyValues(XElement doc) {
        //    XElement topNode = doc.Element(booleanTagName);
        //    var settingNodes = topNode.Elements(settingTagName);
        //    foreach (var settingNode in settingNodes) {
        //        XElement propertyNameNode = settingNode.Element(propertyNameTagName);
        //        if (!propertyNameNode.IsEmpty) {
        //            string propertyValue = settingNode.Element(propertyValueTagName).Value;
        //            if (propertyValue == string.Empty) {
        //                propertyValue = "false";    // empty property value nodes default to false
        //            }
        //            booleanSettings.Add(propertyNameNode.Value, bool.Parse(propertyValue));
        //        }
        //    }

        //    topNode = doc.Element(intTagName);
        //    settingNodes = topNode.Elements(settingTagName);
        //    foreach (var settingNode in settingNodes) {
        //        XElement propertyNameNode = settingNode.Element(propertyNameTagName);
        //        if (!propertyNameNode.IsEmpty) {
        //            integerSettings.Add(propertyNameNode.Value, int.Parse(settingNode.Element(propertyValueTagName).Value));
        //        }
        //    }

        //    topNode = doc.Element(floatTagName);
        //    settingNodes = topNode.Elements(settingTagName);
        //    foreach (var settingNode in settingNodes) {
        //        XElement propertyNameNode = settingNode.Element(propertyNameTagName);
        //        if (!propertyNameNode.IsEmpty) {
        //            floatSettings.Add(propertyNameNode.Value, float.Parse(settingNode.Element(propertyValueTagName).Value));
        //        }
        //    }
        //}


    }
}

