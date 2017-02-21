// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ResourceMgrFactory.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common.LocalResources {

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Reflection;
    using System.Resources;

    /// <summary>
    /// Static class that provides backup access to Resources via acquiring an instance of a Resource Manager. Use of the 
    /// Strongly-typed Designer classes is preferred. 
    /// </summary>
    public static class ResourceMgrFactory {

        private static Dictionary<ResourceFilename, ResourceManager> resourceMgrs = new Dictionary<ResourceFilename, ResourceManager>(ResourceFilenameEqualityComparer.Default);
        private static string rootResourceNamespace = typeof(ResourceMgrFactory).Namespace;
        /// <summary>
        /// Gets the <c>ResourceManager</c> referred to by <c>ResourceFilename</c>.
        /// </summary>
        /// <param name="resxFilename">Name of the .resx file.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public static ResourceManager GetManager(ResourceFilename resxFilename) {
            if (!Enum.IsDefined(typeof(ResourceFilename), resxFilename)) {  // Circular ref w/Enums<> 
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentOutOfRangeException(String.Format(CultureInfo.InvariantCulture, ErrorMessages.UndefinedEnum, resxFilename, callingMethodName));    // Circular ref w/Inject()
            }

            ResourceManager rm = null;
            if (!resourceMgrs.ContainsKey(resxFilename)) {
                rm = resourceMgrs[resxFilename];
            }
            else {
                string resMgrAccessName = rootResourceNamespace + "." + Enum.GetName(typeof(ResourceFilename), resxFilename);
                rm = new ResourceManager(resMgrAccessName, Assembly.GetExecutingAssembly());
                resourceMgrs.Add(resxFilename, rm);
                //// string ns = Assembly.GetEntryAssembly().GetType("NeedsFullyQualifiedName", true, true).Namespace;
            }
            return rm;
        }

        /// <summary>
        /// Gets the string Resource associated with the resourceKeyName provided from the resource file designated.
        /// </summary>
        /// <param name="resxFileName">Name of the .resx file.</param>
        /// <param name="resourceKeyName">The key name of the string value we want.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">ResourceFilename does not refer to a .resx file of Type string.</exception>
        public static string GetString(ResourceFilename resxFileName, string resourceKeyName) {
            ResourceManager rm = GetManager(resxFileName);
            if (!rm.ResourceSetType.Equals(typeof(string))) {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, ErrorMessages.ResourceNotString, resxFileName, callingMethodName));
            }
            return rm.GetString(resourceKeyName);
        }

        #region Nested Classes

        public enum ResourceFilename {
            None = 0,
            ErrorMessages = 1,
            GeneralMessages = 2,
            CommonTerms = 3,
            UIMessages = 4
        }

        /// <summary>
        /// IEqualityComparer for ResourceFilename. 
        /// <remarks>For use when ResourceFilename is used as a Dictionary key as it avoids boxing from use of object.Equals.</remarks>
        /// </summary>
        public class ResourceFilenameEqualityComparer : IEqualityComparer<ResourceFilename> {

            public static readonly ResourceFilenameEqualityComparer Default = new ResourceFilenameEqualityComparer();

            #region IEqualityComparer<DiplomaticRelationship> Members

            public bool Equals(ResourceFilename value1, ResourceFilename value2) {
                return value1 == value2;
            }

            public int GetHashCode(ResourceFilename value) {
                return value.GetHashCode();
            }

            #endregion

        }

        #endregion

        #region Archive

        /// <summary>
        /// Initializes the strongly typed resource accessor, allowing syntax like StronglyTypedResourceAccessor.NameOfResource.
        /// </summary>
        /// <param name="rootResourceNamespace">The root resource namespace.</param>
        //private void InitializeStrongTypedResourceAccessor(string rootResourceNamespace) {
        //    string desiredAccessorClassName = "StronglyTypedResourceAccessor";
        //    string desiredAccessorFileName = @".\" + desiredAccessorClassName + ".cs";
        //    string accessorCodeNamespace = GetType().Namespace;
        //    StreamWriter w = new StreamWriter(desiredAccessorFileName);    // creates the .cs file to write code too
        //    string resxFilePath = rootResourceNamespace + "\\ErrorStrings.resx";
        //    string[] errors = null;
        //    CSharpCodeProvider provider = new CSharpCodeProvider(); // specifies use of C# as the code language of the accessor

        //    bool generateInternalClass = false;   // means accessor class created will be public
        //    CodeCompileUnit code = StronglyTypedResourceBuilder.Create(resxFilePath, desiredAccessorClassName,
        //                                                               accessorCodeNamespace, provider,
        //                                                               generateInternalClass, out errors);
        //    if (errors.Length > 0) {
        //        foreach (var error in errors) {
        //            Debug.WriteLine(error);
        //        }
        //    }
        //    provider.GenerateCodeFromCompileUnit(code, w, new CodeGeneratorOptions());
        //    w.Close();
        //}

        #endregion

    }
}


