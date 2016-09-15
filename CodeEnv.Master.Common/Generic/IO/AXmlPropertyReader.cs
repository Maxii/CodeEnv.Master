// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AXmlPropertyReader.cs
// Generic abstract Singleton that parses Xml files used to provide externalized 
// values to the properties of the derived class.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Xml.Linq;

    /// <summary>
    /// Generic abstract Singleton that parses Xml files used to provide externalized 
    /// values to the properties of the derived class.
    /// </summary>
    /// <typeparam name="T">The derived class type.</typeparam>
    public abstract class AXmlPropertyReader<T> : AXmlReader<T> where T : AXmlPropertyReader<T> {

        private string _booleanTagName = "Boolean";
        private string _intTagName = "Integer";
        private string _floatTagName = "Float";
        private string _textTagName = "Text";
        private string _settingTagName = "Setting";
        private string _propertyNameTagName = "PropertyName";
        private string _propertyValueTagName = "PropertyValue";

        private bool _isPropertyValuesInitialized = false;

        protected void CheckValuesInitialized() {
            if (!_isPropertyValuesInitialized) {
                InitializePropertyValues();
            }
        }

        private void InitializePropertyValues() {
            IEnumerable<XElement> topBooleanNodes = _xElement.Elements(_booleanTagName);
            foreach (var topBooleanNode in topBooleanNodes) {
                InitializeBooleanPropertyValuesFor(topBooleanNode);
            }

            IEnumerable<XElement> topIntegerNodes = _xElement.Elements(_intTagName);
            foreach (var topIntegerNode in topIntegerNodes) {
                InitializeIntegerPropertyValuesFor(topIntegerNode);
            }

            IEnumerable<XElement> topFloatNodes = _xElement.Elements(_floatTagName);
            foreach (var topFloatNode in topFloatNodes) {
                InitializeFloatPropertyValuesFor(topFloatNode);
            }

            IEnumerable<XElement> topTextNodes = _xElement.Elements(_textTagName);
            foreach (var topTextNode in topTextNodes) {
                InitializeTextPropertyValuesFor(topTextNode);
            }

            _isPropertyValuesInitialized = true;
        }

        private void InitializeBooleanPropertyValuesFor(XElement topNode) {
            D.Assert(topNode != null);
            var settingNodes = topNode.Elements(_settingTagName);
            foreach (var settingNode in settingNodes) {
                XElement propertyNameNode = settingNode.Element(_propertyNameTagName);
                if (!propertyNameNode.IsEmpty && !propertyNameNode.Value.Equals(string.Empty)) {    // Elements.isEmpty is true only if they are in the format <ElementName/> 
                    D.Log("Boolean PropertyName = {0}.", propertyNameNode.Value);
                    XElement propertyValueNode = settingNode.Element(_propertyValueTagName);
                    string propertyValue = "false";
                    if (propertyValueNode.IsEmpty || propertyValueNode.Value.Equals(string.Empty)) {
                        D.Warn("Value of Property {0} is empty, defaulting to {1}.", propertyNameNode.Value, propertyValue);
                    }
                    else {
                        propertyValue = propertyValueNode.Value;
                        D.Log("Boolean PropertyValue = {0}.", propertyValue);
                    }
                    AssignValueToProperty(propertyNameNode.Value, bool.Parse(propertyValue));
                }
            }
        }

        private void InitializeIntegerPropertyValuesFor(XElement topNode) {
            D.Assert(topNode != null);
            var settingNodes = topNode.Elements(_settingTagName);
            foreach (var settingNode in settingNodes) {
                XElement propertyNameNode = settingNode.Element(_propertyNameTagName);
                if (!propertyNameNode.IsEmpty && !propertyNameNode.Value.Equals(string.Empty)) {    // Elements.isEmpty is true only if they are in the format <ElementName/> 
                    D.Log("Integer PropertyName = {0}.", propertyNameNode.Value);
                    XElement propertyValueNode = settingNode.Element(_propertyValueTagName);
                    string propertyValue = "0";
                    if (propertyValueNode.IsEmpty || propertyValueNode.Value.Equals(string.Empty)) {
                        D.Warn("Value of Property {0} is empty, defaulting to {1}.", propertyNameNode.Value, propertyValue);
                    }
                    else {
                        propertyValue = propertyValueNode.Value;
                        D.Log("Integer PropertyValue = {0}.", propertyValue);
                    }
                    AssignValueToProperty(propertyNameNode.Value, int.Parse(propertyValue));
                }
            }
        }

        private void InitializeFloatPropertyValuesFor(XElement topNode) {
            D.Assert(topNode != null);
            var settingNodes = topNode.Elements(_settingTagName);
            foreach (var settingNode in settingNodes) {
                XElement propertyNameNode = settingNode.Element(_propertyNameTagName);
                if (!propertyNameNode.IsEmpty && !propertyNameNode.Value.Equals(string.Empty)) {    // Elements.isEmpty is true only if they are in the format <ElementName/> 
                    D.Log("Float PropertyName = {0}.", propertyNameNode.Value);
                    XElement propertyValueNode = settingNode.Element(_propertyValueTagName);
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
        }

        private void InitializeTextPropertyValuesFor(XElement topNode) {
            D.Assert(topNode != null);
            var settingNodes = topNode.Elements(_settingTagName);
            foreach (var settingNode in settingNodes) {
                XElement propertyNameNode = settingNode.Element(_propertyNameTagName);
                if (!propertyNameNode.IsEmpty && !propertyNameNode.Value.Equals(string.Empty)) { // Elements.isEmpty is true only if they are in the format <ElementName/> 
                    D.Log("Text PropertyName = {0}.", propertyNameNode.Value);
                    XElement propertyValueNode = settingNode.Element(_propertyValueTagName);
                    string propertyValue = string.Empty;
                    if (propertyValueNode.IsEmpty || propertyValueNode.Value.Equals(string.Empty)) {
                        D.Warn("Value of Property {0} is empty, defaulting to {1}.", propertyNameNode.Value, propertyValue);
                    }
                    else {
                        propertyValue = propertyValueNode.Value;
                        D.Log("Text PropertyValue = {0}.", propertyValue);
                    }
                    AssignValueToProperty(propertyNameNode.Value, propertyValue);
                }
            }
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

        private void AssignValueToProperty(string propertyName, string value) {
            PropertyInfo propertyInfo = typeof(T).GetProperty(propertyName);
            if (propertyInfo == null) {
                D.Error("No {0} property named {1} found!".Inject(typeof(T).Name, propertyName));
                return;
            }
            Action<string> propertySet = (Action<string>)Delegate.CreateDelegate(typeof(Action<string>), Instance, propertyInfo.GetSetMethod(true));
            propertySet(value);
        }
    }
}

