﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ObjectAnalyzer.cs
// Class to convert the supplied object to a string representation that lists all fields.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Security.Permissions;
    using System.Text;
    using CodeEnv.Master.Resources;

    /// <summary>
    /// Class to convert the supplied object to a string
    /// representation that lists all fields.
    /// Syntax:
    /// <c>
    /// public override string ToString() {
    ///	   return new ObjectAnalyzer().ToString(this);
    /// }
    /// </c>
    /// 
    /// @author James L. Evans, derived from Cay Horstmann, CoreJava.
    /// </summary>
    public class ObjectAnalyzer {

        /// <summary>
        /// Tracks objects that have already been visited to avoid infinite recursion.
        /// </summary>
        private IList<object> visited = new List<object>();
        private BindingFlags allNonStaticFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private BindingFlags nonStaticPublicFields = BindingFlags.Instance | BindingFlags.Public;
        private bool showPrivate = false;


        /// <summary>
        /// Returns a <see cref="System.String" /> containing a comprehensive view of the supplied object instance.
        /// </summary>
        /// <param name="objectToConvert">The object to convert to a string.</param>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public string ToString(object objectToConvert) {
#if DEBUG
            showPrivate = true;
#endif
            return ConvertToString(objectToConvert);
        }

        private string ConvertToString(object obj) {
            StringBuilder objContentMsg = new StringBuilder();
            if (obj == null) {
                objContentMsg.Append(GeneralMessages.NullObject);
                return objContentMsg.ToString();
            }
            if (visited.Contains(obj)) {
                objContentMsg.Append(Constants.Ellipsis);
                return objContentMsg.ToString();
            }
            visited.Add(obj);
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
            BindingFlags fieldsToInclude = (showPrivate) ? allNonStaticFields : nonStaticPublicFields;
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
                            if (fieldType.IsPrimitive) {
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
            while (objType != null);

            return objContentMsg.ToString();
        }
    }
}

