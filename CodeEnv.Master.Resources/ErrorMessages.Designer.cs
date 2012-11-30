﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.17929
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CodeEnv.Master.Resources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class ErrorMessages {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal ErrorMessages() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("CodeEnv.Master.Resources.ErrorMessages", typeof(ErrorMessages).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This collection should not be empty.  Calling Method: {0}.
        /// </summary>
        public static string CollectionEmpty {
            get {
                return ResourceManager.GetString("CollectionEmpty", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This string should not be null or empty.  Calling Method: {0}.
        /// </summary>
        public static string EmptyOrNullString {
            get {
                return ResourceManager.GetString("EmptyOrNullString", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Enum Constant {0} has no attribute of type {1} defined.  Calling Method: {2}.
        /// </summary>
        public static string EnumNoAttribute {
            get {
                return ResourceManager.GetString("EnumNoAttribute", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The following exception was thrown.  Calling Method: {0}.
        /// </summary>
        public static string ExceptionRethrow {
            get {
                return ResourceManager.GetString("ExceptionRethrow", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Accessing an object or method that is either not initialized, closed or otherwise invalid.  Calling Method: {0}.
        /// </summary>
        public static string InvalidAccess {
            get {
                return ResourceManager.GetString("InvalidAccess", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The low value {0} is greater than the high value {1}.  Calling Method: {2}.
        /// </summary>
        public static string LowGreaterThanHigh {
            get {
                return ResourceManager.GetString("LowGreaterThanHigh", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This value {0} should not be negative.  Calling Method: {1}.
        /// </summary>
        public static string NegativeValue {
            get {
                return ResourceManager.GetString("NegativeValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to String {0} has no Enum counterpart.  Calling Method: {1}.
        /// </summary>
        public static string NoEnumForString {
            get {
                return ResourceManager.GetString("NoEnumForString", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Expected no exception but got: {0}.  Calling Method: {1}.
        /// </summary>
        public static string NoExceptionExpected {
            get {
                return ResourceManager.GetString("NoExceptionExpected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The string {0} is not a boolean equivalent.  Calling Method: {1}.
        /// </summary>
        public static string NotBoolean {
            get {
                return ResourceManager.GetString("NotBoolean", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Argument is null.  Calling Method: {0}.
        /// </summary>
        public static string Null {
            get {
                return ResourceManager.GetString("Null", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Property or Method called on already disposed object.  Calling Method: {0}.
        /// </summary>
        public static string ObjectDisposed {
            get {
                return ResourceManager.GetString("ObjectDisposed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The value {0} is not within the range {1} to {2}.  Calling Method: {3}.
        /// </summary>
        public static string OutOfRange {
            get {
                return ResourceManager.GetString("OutOfRange", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The resource {0} you are trying to reach is not of type String.   Calling Method: {1}.
        /// </summary>
        public static string ResourceNotString {
            get {
                return ResourceManager.GetString("ResourceNotString", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Enum Constant {0} is not defined.  Calling Method: {1}.
        /// </summary>
        public static string UndefinedEnum {
            get {
                return ResourceManager.GetString("UndefinedEnum", resourceCulture);
            }
        }
    }
}
