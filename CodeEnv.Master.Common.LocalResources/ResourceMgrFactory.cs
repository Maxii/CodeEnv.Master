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

        public enum ResourceFileName {
            None = 0,
            ErrorMessages = 1,
            GeneralMessages = 2,
            CommonTerms = 3,
            UIMessages = 4
        }

        private static Dictionary<ResourceFileName, ResourceManager> resourceMgrs = new Dictionary<ResourceFileName, ResourceManager>();
        private static string rootResourceNamespace = typeof(ResourceMgrFactory).Namespace;
        /// <summary>
        /// Gets the <c>ResourceManager</c> referred to by <c>ResourceFileName</c>.
        /// </summary>
        /// <param name="resxFileName">Name of the .resx file.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public static ResourceManager GetManager(ResourceFileName resxFileName) {
            if (!Enum.IsDefined(typeof(ResourceFileName), resxFileName)) {  // Circular ref w/Enums<> 
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentOutOfRangeException(String.Format(CultureInfo.InvariantCulture, ErrorMessages.UndefinedEnum, resxFileName, callingMethodName));    // Circular ref w/Inject()
            }

            ResourceManager rm = null;
            if (!resourceMgrs.ContainsKey(resxFileName)) {
                rm = resourceMgrs[resxFileName];
            }
            else {
                string resMgrAccessName = rootResourceNamespace + "." + Enum.GetName(typeof(ResourceFileName), resxFileName);
                rm = new ResourceManager(resMgrAccessName, Assembly.GetExecutingAssembly());
                resourceMgrs.Add(resxFileName, rm);
                // string ns = Assembly.GetEntryAssembly().GetType("NeedsFullyQualifiedName", true, true).Namespace;
            }
            return rm;
        }

        /// <summary>
        /// Gets the string Resource associated with the resourceKeyName provided from the resource file designated.
        /// </summary>
        /// <param name="resxFileName">Name of the .resx file.</param>
        /// <param name="resourceKeyName">The key name of the string value we want.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">ResourceFileName does not refer to a .resx file of Type string.</exception>
        public static string GetString(ResourceFileName resxFileName, string resourceKeyName) {
            ResourceManager rm = GetManager(resxFileName);
            if (!rm.ResourceSetType.Equals(typeof(string))) {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, ErrorMessages.ResourceNotString, resxFileName, callingMethodName));
            }
            return rm.GetString(resourceKeyName);
        }

        /// <summary>
        /// Initializes the strongly typed resource accessor, allowing syntax like StronglyTypedResourceAccessor.NameOfResource.
        /// </summary>
        /// <param name="rootResourceNamespace">The root resource namespace.</param>
        //private void InitializeStrongTypedResourceAccessor(string rootResourceNamespace) {
        //    string desiredAccessorClassName = "StronglyTypedResourceAccessor";
        //    string desiredAccessorFileName = @".\" + desiredAccessorClassName + ".cs";
        //    string accessorCodeNamespace = GetType().Namespace;
        //    StreamWriter sw = new StreamWriter(desiredAccessorFileName);    // creates the .cs file to write code too
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
        //    provider.GenerateCodeFromCompileUnit(code, sw, new CodeGeneratorOptions());
        //    sw.Close();
        //}

    }
}


