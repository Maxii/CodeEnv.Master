// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ResourceInfoFactory.cs
// Singleton. COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// Singleton. COMMENT
    /// </summary>
    [Obsolete]
    public class ResourceInfoFactory : AGenericSingleton<ResourceInfoFactory> {

        private static string _resourceNameTagName = "ResourceName";
        private static string _resourceNameAttributeTagName = "Name";
        private static string _imageFilenameTagName = "ImageFilename";
        private static string _descriptionTagName = "Description";
        private static string _categoryTagName = "ResourceCategory";

        /// <summary>
        /// The tag name of the root of the Xml DOM.
        /// </summary>
        private string RootTagName { get { return "Resources"; } }

        /// <summary>
        /// The name of the Xml file without extension. 
        /// </summary>
        private string XmlFilename { get { return "ResourceInfo"; } }
        private AtlasID AtlasID { get { return AtlasID.MyGui; } }
        private XElement _xElement;

        private ResourceInfoFactory() {
            Initialize();
        }

        protected override void Initialize() {
            _xElement = LoadAndValidateXElement();
        }

        private XElement LoadAndValidateXElement() {
            string xmlFilePath = UnityConstants.DataLibraryDir + XmlFilename + ".xml";
            D.Log("The path to the Xml file is {0}.", xmlFilePath);
            XElement xElement = XElement.Load(xmlFilePath);
            D.Assert(ValidateElement(xElement), "Invalid XDocument found at {0}.".Inject(xmlFilePath), pauseOnFail: true);
            return xElement;
        }

        private bool ValidateElement(XElement xElement) {
            D.Log("{0}.ValidateElement: RootTagName = {1}, xElementName = {2}.", GetType().Name, RootTagName, xElement.Name.ToString());
            return RootTagName.Equals(xElement.Name.ToString());
        }

        public ResourceInfo MakeInstance(RareResourceID resourceID) {
            XElement resourceNameNode = _xElement.Elements(_resourceNameTagName).Where(e => e.Attribute(_resourceNameAttributeTagName).Value.Equals(resourceID.GetName())).Single();
            string imageFilename = resourceNameNode.Elements(_imageFilenameTagName).Single().Value;
            string description = resourceNameNode.Elements(_descriptionTagName).Single().Value;
            string categoryName = resourceNameNode.Elements(_categoryTagName).Single().Value;
            ResourceCategory category = Enums<ResourceCategory>.Parse(categoryName);
            return new ResourceInfo(resourceID, category, imageFilename, AtlasID, description);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}


