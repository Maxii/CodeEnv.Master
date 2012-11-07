// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ResourceMgrFactory.cs
// SingletonPattern. TODO - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common.ResourceMgmt {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Resources;
    using CodeEnv.Master.Common.General;
    using CodeEnv.Master.Common.Extensions;
    using System.Reflection;
    using System.IO;
    using Microsoft.CSharp;
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.Diagnostics;
    using System.Resources.Tools;

    /// <summary>
    /// SingletonPattern. TODO
    /// </summary>
    public sealed class ResourceMgrFactory {

        #region SingletonPattern
        private static readonly ResourceMgrFactory instance = new ResourceMgrFactory();

        /// <summary>
        /// Explicit static constructor that enables lazy instantiation by telling C# compiler
        /// not to mark type as beforefieldinit.
        /// </summary>
        static ResourceMgrFactory() { }

        /// <summary>
        /// Private constructor that prevents the creation of another externally requested instance of <see cref="ResourceMgrFactory"/>.
        /// </summary>
        private ResourceMgrFactory() { }

        /// <summary>Returns the singleton instance.</summary>
        public static ResourceMgrFactory Instance {
            get { return instance; }
        }
        #endregion

        private bool isInitialized = false;

        /// <summary>
        /// Initializes the Singleton with the root namespace where the Application's resources reside.
        /// </summary>
        /// <param name="rootResourceNamespace">The root resource namespace.</param>
        public void Initialize(string rootResourceNamespace) {
            Utility.ValidateAccess(!isInitialized);
            RootResourceNamespace = rootResourceNamespace;
            InitializeStrongTypedResourceAccessor(rootResourceNamespace);
            isInitialized = true;
        }

        /// <summary>
        /// Initializes the strongly typed resource accessor, allowing syntax like StronglyTypedResourceAccessor.NameOfResource.
        /// TODO improve this to generate an Accessor for EACH .resx Resource file
        /// </summary>
        /// <param name="rootResourceNamespace">The root resource namespace.</param>
        private void InitializeStrongTypedResourceAccessor(string rootResourceNamespace) {
            string desiredAccessorClassName = "StronglyTypedResourceAccessor";
            string desiredAccessorFileName = @".\" + desiredAccessorClassName + Constants.Period + ".cs";
            string accessorCodeNamespace = GetType().Namespace;
            StreamWriter sw = new StreamWriter(desiredAccessorFileName);    // creates the .cs file to write code too
            string resxFilePath = rootResourceNamespace + Constants.FileSeparator + "ErrorStrings.resx";
            string[] errors = null;
            CSharpCodeProvider provider = new CSharpCodeProvider(); // specifies use of C# as the code language of the accessor

            bool generateInternalClass = false;   // means accessor class created will be public
            CodeCompileUnit code = StronglyTypedResourceBuilder.Create(resxFilePath, desiredAccessorClassName,
                                                                       accessorCodeNamespace, provider,
                                                                       generateInternalClass, out errors);
            if (errors.Length > 0) {
                foreach (var error in errors) {
                    Debug.WriteLine(error);
                }
            }
            provider.GenerateCodeFromCompileUnit(code, sw, new CodeGeneratorOptions());
            sw.Close();
        }

        private string rootResourceNamespace;
        public string RootResourceNamespace {
            get {
                Utility.ValidateAccess(isInitialized);
                return rootResourceNamespace;
            }
            private set { rootResourceNamespace = value; }
        }

        public enum ResourceFileName {
            ErrorStrings = 0,
            MsgStrings = 1,
            UiStrings = 2
        }

        private static Dictionary<ResourceFileName, ResourceManager> resourceMgrs = new Dictionary<ResourceFileName, ResourceManager>();

        /// <summary>
        /// Gets the <c>ResourceManager</c> referred to by <c>ResourceFileName</c>.
        /// </summary>
        /// <param name="resxFileName">Name of the .resx file.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public ResourceManager GetManager(ResourceFileName resxFileName) {
            Utility.ValidateAccess(isInitialized);
            if (!Enum.IsDefined(typeof(ResourceFileName), resxFileName)) {
                throw new ArgumentOutOfRangeException(String.Format("Enum {0} is not defined.", resxFileName));
                // TODO turn this into an Argument checker using reflection to determine the type of the Enum in the arguments args
            }

            ResourceManager rm = null;
            if (!resourceMgrs.ContainsKey(resxFileName)) {
                rm = resourceMgrs[resxFileName];
            }
            else {
                string resMgrAccessName = RootResourceNamespace + Constants.Period + resxFileName.GetName();
                rm = new ResourceManager(resMgrAccessName, Assembly.GetEntryAssembly());
                resourceMgrs.Add(resxFileName, rm);
                // string ns = Assembly.GetEntryAssembly().GetType("NeedsFullyQualifiedName", true, true).Namespace;
            }
            return rm;
        }

        /// <summary>
        /// Gets the string Resource associated with the keyName provided from the resource file designated.
        /// </summary>
        /// <param name="resxFileName">Name of the .resx file.</param>
        /// <param name="keyName">The key name of the string value we want.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">ResourceFileName does not refer to a .resx file of Type string.</exception>
        public string GetString(ResourceFileName resxFileName, string keyName) {
            Utility.ValidateAccess(isInitialized);
            ResourceManager rm = GetManager(resxFileName);
            if (!rm.ResourceSetType.Equals(typeof(string))) {
                throw new ArgumentException("ResourceFileName {0} does not refer to a .resx file of Type string".Inject(resxFileName));
            }
            return rm.GetString(keyName);
        }
    }
}


