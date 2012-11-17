// --------------------------------------------------------------------------------------------------------------------
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


        /// <summary>
        /// Returns a <see cref="System.String" /> containing a comprehensive view of the supplied object instance.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public string ToString(object obj) {
            string objContentString = string.Empty;
            if (obj == null) {
                objContentString = GeneralMessages.NullObject;
                return objContentString;
            }
            if (visited.Contains(obj)) {
                objContentString = "...";
                return objContentString;
            }
            visited.Add(obj);
            Type objType = obj.GetType();
            if (objType == typeof(string)) {
                objContentString = (string)obj;
                return objContentString;
            }
            if (objType.IsArray) {
                Array array = (Array)obj;
                objContentString = objType.GetElementType() + "[]{";    // the Type contained in the Array
                for (int i = 0; i < array.Length; i++) {
                    if (i > 0) {
                        objContentString += Constants.Comma;
                    }
                    object arrayItem = array.GetValue(i);
                    if (objType.GetElementType().IsPrimitive) {
                        objContentString += arrayItem;  // primitives like int are structures that all derive from object, so object.ToString() works
                    }
                    else {
                        objContentString += ToString(arrayItem);
                    }
                }
                objContentString += "}";
                return objContentString;
            }

            objContentString = objType.Name;
            // inspect the fields of this class and all base classes
            do {
                objContentString += "[";
                FieldInfo[] fields = objType.GetFields(allNonStaticFields); // IMPROVE consider adding static fields?
                // AccessibleObject.setAccessible(fields, true); Java equivalent allowing reflection access to private fields

                // get the names and values of all fields
                foreach (FieldInfo field in fields) {
                    if (!field.IsStatic) {
                        if (!objContentString.EndsWith("[", StringComparison.OrdinalIgnoreCase)) {  // per FxCop
                            objContentString += Constants.Comma;
                        }
                        objContentString += field.Name + "=";
                        try {
                            Type fieldType = field.FieldType;
                            object fieldValue = field.GetValue(obj);
                            if (fieldType.IsPrimitive) {
                                objContentString += fieldValue;
                            }
                            else {
                                objContentString += ToString(fieldValue);
                            }
                        }
                        catch (Exception e) {
                            Debug.WriteLine(e.StackTrace);
                            throw new Exception(ErrorMessages.ExceptionRethrow, e);
                            // IMPROVE add my own General Exception type rather than use Exception
                        }
                    }
                }
                objContentString += "]";
                objType = objType.BaseType;
            }
            while (objType != null);

            return objContentString;
        }
    }
}

