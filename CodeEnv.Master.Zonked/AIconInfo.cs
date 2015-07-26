// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AIconInfo.cs
// Abstract base class that acquires the filename of an Icon image based on a provided set of criteria.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Linq;
    using System.Xml.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract base class that acquires the filename of an Icon image based on a provided set of criteria.
    /// Acquires the filename of the appropriate image from the xml file holding the filenames, derived from the IconSection and 
    /// IconSelectionCriteria provided.
    /// </summary>
    [System.Obsolete]
    public abstract class AIconInfo : IIconInfo {

        protected static string _toStringFormat = "{0}.AtlasID: {1}, Size: {2}, Filename: {3}, Color: {4}, Placement: {5}.";

        private static string _sectionTagName = "Section";
        private static string _sectionAttributeTagName = "SectionName";
        private static string _selectionTagName = "Selection";
        private static string _criteriaTagName = "Criteria";
        private static string _iconFilenameTagName = "Filename";

        public AtlasID AtlasID { get; set; }

        public WidgetPlacement Placement { get; set; }

        public Vector2 Size { get; set; }

        private string _filename;
        public string Filename {
            get {
                if (!Utility.CheckForContent(_filename)) {
                    _filename = AcquireFilename();
                }
                return _filename;
            }
        }

        public GameColor Color { get; set; }

        /// <summary>
        /// The tag name of the root of the Xml DOM.
        /// </summary>
        protected virtual string RootTagName { get { return "Icon"; } }

        /// <summary>
        /// The name of the Xml file without extension. WARNING: Default is the name of the derived class.
        /// </summary>
        protected virtual string XmlFilename { get { return GetType().Name; } }
        private XElement _xElement;

        private IconSection _section;
        private IconSelectionCriteria[] _criteria;

        /// <summary>
        /// Initializes a new instance of the <see cref="AIconInfo"/> class. WARNING: Clients of derived types
        /// should use IconFactory.MakeInstance() rather than this constructor so the instances can be used in 
        /// equality tests with each other.
        /// </summary>
        /// <param name="section">The section of the icon the image should be applied too.</param>
        /// <param name="criteria">The selection criteria to use in picking the image.</param>
        public AIconInfo(IconSection section, params IconSelectionCriteria[] criteria) {
            _section = section;
            _criteria = criteria;
            Initialize();
        }

        protected virtual void Initialize() {
            _xElement = LoadAndValidateXElement();
        }

        private XElement LoadAndValidateXElement() {
            string xmlFilePath = UnityConstants.DataLibraryDir + XmlFilename + ".xml";
            D.Log("The path to the Xml file is {0}.", xmlFilePath);
            XElement xElement = XElement.Load(xmlFilePath);
            D.Assert(ValidateElement(xElement), "Invalid XDocument found at {0}.".Inject(xmlFilePath), pauseOnFail: true);
            return xElement;
        }

        protected virtual bool ValidateElement(XElement xElement) {
            //D.Log("{0}.ValidateElement: RootTagName = {1}, xElementName = {2}.", GetType().Name, RootTagName, xElement.Name.ToString());
            return RootTagName.Equals(xElement.Name.ToString());
        }

        private string AcquireFilename() {
            XElement sectionNode = _xElement.Elements(_sectionTagName).Where(e => e.Attribute(_sectionAttributeTagName).Value.Equals(_section.GetValueName())).Single();
            var selectionNodes = sectionNode.Elements(_selectionTagName);
            foreach (var selectionNode in selectionNodes) {
                var criteriaValues = selectionNode.Elements(_criteriaTagName).Select(node => node.Value);
                if (criteriaValues.OrderBy(v => v).SequenceEqual(_criteria.Select(c => c.GetValueName()).OrderBy(n => n))) {
                    // found the criteria values we were looking for in this node
                    return selectionNode.Element(_iconFilenameTagName).Value;
                }
            }
            D.Error("No filename for {0} using Section {1} and Criteria {2} found.", GetType().Name, _section.GetValueName(), _criteria.Concatenate());
            return string.Empty;
        }

    }
}


