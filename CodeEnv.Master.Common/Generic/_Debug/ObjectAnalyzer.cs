// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ObjectAnalyzer.cs
// Class for use within ToString() that converts an instance's content to a string.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using System.Text;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Class to convert the supplied object to a string
    /// representation that lists all fields, including base class fields. It does
    /// not show methods.
    /// Syntax:
    /// <c>
    /// public override string ToString() {
    ///	   return new ObjectAnalyzer().ToString(this);
    /// }
    /// </c>
    /// 
    /// This class has been successfully tested.
    /// 
    /// @author James L. Evans, derived from Cay Horstmann, CoreJava.
    /// </summary>
    public class ObjectAnalyzer {

        /// <summary>
        /// Tracks objects that have already been visited to avoid infinite recursion.
        /// </summary>
        private IList<object> _visited = new List<object>();
        private BindingFlags _allNonStaticFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private BindingFlags _nonStaticPublicFields = BindingFlags.Instance | BindingFlags.Public;
        private bool _showPrivate = false;

        private static Type _systemObjectType = Type.GetType("System.Object");
        private static DebugSettings _debugSettings = DebugSettings.Instance;


        /// <summary>
        /// Returns a <see cref="System.String" /> containing a comprehensive view of the supplied object instance.
        /// </summary>
        /// <param name="objectToConvert">The object to convert to a string.</param>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public string ToString(object objectToConvert) {
#if DEBUG
            if (_debugSettings.EnableVerboseDebugLog) {
                _showPrivate = true;
            }
#endif
            return ConvertToString(objectToConvert);
        }

        private string ConvertToString(object obj) {
            StringBuilder objContentMsg = new StringBuilder();
            if (obj == null) {
                objContentMsg.Append(GeneralMessages.NullObject);
                return objContentMsg.ToString();
            }
            if (_visited.Contains(obj)) {
                objContentMsg.Append(Constants.Ellipsis);
                return objContentMsg.ToString();
            }
            _visited.Add(obj);
            Type objType = obj.GetType();
            if (objType == typeof(string)) {
                objContentMsg.Append((string)obj);
                return objContentMsg.ToString();
            }
            if (objType.IsArray) {
                Array array = (Array)obj;
                objContentMsg.Append(objType.GetElementType());
                objContentMsg.Append("[]{");    // the Type contained in the Array   
                for (int i = 0; i < array.Length; i++) {
                    if (i > 0) {
                        objContentMsg.Append(Constants.Comma);
                    }
                    object arrayItem = array.GetValue(i);
                    if (objType.GetElementType().IsPrimitive) {
                        objContentMsg.Append(arrayItem);    // primitives like int are structures that all derive from object, so object.ToString() works 
                    }
                    else {
                        objContentMsg.Append(ConvertToString(arrayItem));  // recursive
                    }
                }
                objContentMsg.Append("}");
                return objContentMsg.ToString();
            }

            objContentMsg.Append(objType.Name);
            // inspect the fields of this class and all base classes               
            BindingFlags fieldsToInclude = (_showPrivate) ? _allNonStaticFields : _nonStaticPublicFields;
            do {
                objContentMsg.Append("[");
                FieldInfo[] fields = objType.GetFields(fieldsToInclude); // IMPROVE consider adding static fields?
                // AccessibleObject.setAccessible(fields, true); Java equivalent allowing reflection access to private fields

                // get the names and values of all fields
                foreach (FieldInfo field in fields) {
                    if (!field.IsStatic) {
                        if (!objContentMsg.ToString().EndsWith("[", StringComparison.OrdinalIgnoreCase)) {  // per FxCop
                            objContentMsg.Append(Constants.Comma);
                        }
                        objContentMsg.Append(field.Name);
                        objContentMsg.Append("=");
                        try {
                            Type fieldType = field.FieldType;
                            object fieldValue = field.GetValue(obj);
                            if (fieldType.IsEnum) {
                                Enum enumValue = (Enum)fieldValue;
                                objContentMsg.Append(enumValue.GetName());
                            }
                            else if (fieldType.IsPrimitive) {
                                objContentMsg.Append(fieldValue);
                            }
                            else {
                                objContentMsg.Append(ConvertToString(fieldValue));
                            }
                        }
                        catch (SystemException e) {
                            Debug.WriteLine(e.StackTrace);
                        }
                    }
                }
                objContentMsg.Append("]");
                objType = objType.BaseType;
            }
            while (objType != _systemObjectType);

            return objContentMsg.ToString();
        }
    }
}

