// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ACmdIconInfoFactory.cs
// Singleton. Abstract, generic base Factory that makes instances of IconInfo for Commands.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Singleton. Abstract, generic base Factory that makes instances of IconInfo for Commands.
    /// As searching XML docs to find the filename is expensive, this implementation caches and reuses the 
    /// IconInfo instances, even though they are structures. 
    /// </summary>
    /// <typeparam name="ReportType">The type of the Report.</typeparam>
    /// <typeparam name="FactoryType">The type of the Factory.</typeparam>
    public abstract class ACmdIconInfoFactory<ReportType, FactoryType> : AGenericSingleton<FactoryType>
        where ReportType : ACmdReport
        where FactoryType : class {

        private static string _sectionTagName = "Section";
        private static string _sectionAttributeTagName = "SectionName";
        private static string _selectionTagName = "Selection";
        private static string _criteriaTagName = "Criteria";
        private static string _iconFilenameTagName = "Filename";

        /// <summary>
        /// The tag name of the root of the Xml DOM.
        /// </summary>
        protected virtual string RootTagName { get { return "Icon"; } }

        /// <summary>
        /// The name of the Xml file without extension. 
        /// </summary>
        protected abstract string XmlFilename { get; }

        protected abstract AtlasID AtlasID { get; }

        private XElement _xElement;
        private IDictionary<IconSection, IDictionary<GameColor, IDictionary<IEnumerable<IconSelectionCriteria>, IconInfo>>> _infoCache;

        protected sealed override void Initialize() {
            _infoCache = new Dictionary<IconSection, IDictionary<GameColor, IDictionary<IEnumerable<IconSelectionCriteria>, IconInfo>>>();
            _xElement = LoadAndValidateXElement();
        }

        private XElement LoadAndValidateXElement() {
            string xmlFilePath = UnityConstants.DataLibraryDir + XmlFilename + ".xml";
            //D.Log("The path to the Xml file is {0}.", xmlFilePath);
            XElement xElement = XElement.Load(xmlFilePath);
            D.Assert(ValidateElement(xElement), "Invalid XDocument found at {0}.".Inject(xmlFilePath), pauseOnFail: true);
            return xElement;
        }

        protected virtual bool ValidateElement(XElement xElement) {
            //D.Log("{0}.ValidateElement: RootTagName = {1}, xElementName = {2}.", GetType().Name, RootTagName, xElement.Name.ToString());
            return RootTagName.Equals(xElement.Name.ToString());
        }

        /// <summary>
        /// Makes an instance of IIconInfo. Clients should only use this method to make IIconInfo 
        /// as they are compared against each other to determine equality.
        /// </summary>
        /// <param name="cmdReport">The Command's Report.</param>
        /// <returns></returns>
        public IconInfo MakeInstance(ReportType cmdReport) {
            IconSection section = GetIconSection();
            IconSelectionCriteria[] criteria = GetSelectionCriteria(cmdReport);
            GameColor color = cmdReport.Owner != null ? cmdReport.Owner.Color : GameColor.White;
            var iconInfo = GetInstance(section, color, criteria);
            return iconInfo;
        }

        private IconSelectionCriteria[] GetSelectionCriteria(ReportType cmdReport) {
            IList<IconSelectionCriteria> criteria = new List<IconSelectionCriteria>();
            IntelCoverage cmdUserIntelCoverage = cmdReport.IntelCoverage;
            //D.Log("{0} found UserIntelCoverage = {1}.", GetType().Name, cmdUserIntelCoverage.GetName());

            /***************************************************************************************************************************************************************
                            * Concept: Cmd's IntelCoverage is same as HQElement coverage. If Cmd/HQ not yet detected, then invisible icon - does this matter? Icon won't be shown anyhow?
                            * If Cmd only detected LongRange (aka Basic), then unknown icon no matter how many other elements are known
                            * If Cmd IntelCoverage Essential, then icon will depend on what is known about other elements - aka Category which is size of Cmd (# of elements known)
                            * If Cmd IntelCoverage Broad or Comprehensive, then icon will also include whether there are special elements (aka Science, Colony or Troop)
                            *****************************************************************************************************************************************************************/
            switch (cmdUserIntelCoverage) {
                case IntelCoverage.None:
                    // always returns None
                    criteria.Add(IconSelectionCriteria.None);
                    break;
                case IntelCoverage.Basic:
                    // always returns Unknown
                    criteria.Add(IconSelectionCriteria.Unknown);
                    break;
                case IntelCoverage.Essential:
                    // always returns level 1-5
                    criteria.Add(GetCriteriaFromCategory(cmdReport));
                    break;
                case IntelCoverage.Broad:
                case IntelCoverage.Comprehensive:
                    // always returns a comprehensive icon
                    criteria.Add(GetCriteriaFromCategory(cmdReport));
                    GetCriteriaFromComposition(cmdReport).ForAll(isc => criteria.Add(isc));
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(cmdUserIntelCoverage));
            }
            return criteria.ToArray();
        }

        protected abstract IconSelectionCriteria GetCriteriaFromCategory(ReportType cmdReport);

        protected abstract IEnumerable<IconSelectionCriteria> GetCriteriaFromComposition(ReportType cmdReport);

        protected virtual IconSection GetIconSection() { return IconSection.Base; }

        /// <summary>
        /// Makes or retrieves an already made instance of IconInfo based on the provided section, color and criteria.
        /// These instances hold the filename, atlas and color of the icon. 
        /// </summary>
        /// <param name="section">The section.</param>
        /// <param name="color">The color.</param>
        /// <param name="criteria">The criteria.</param>
        /// <returns></returns>
        private IconInfo GetInstance(IconSection section, GameColor color, params IconSelectionCriteria[] criteria) {
            IconInfo info;
            if (!TryCheckCache(section, color, out info, criteria)) {
                info = MakeInstance(section, color, criteria);
                RecordToCache(info, section, color, criteria);
            }
            return info;
        }

        private IconInfo MakeInstance(IconSection section, GameColor color, params IconSelectionCriteria[] criteria) {
            string filename = AcquireFilename(section, criteria);
            return new IconInfo(filename, AtlasID, color);
        }

        private bool TryCheckCache(IconSection section, GameColor color, out IconInfo info, params IconSelectionCriteria[] criteria) {
            IDictionary<GameColor, IDictionary<IEnumerable<IconSelectionCriteria>, IconInfo>> colorCache;
            if (_infoCache.TryGetValue(section, out colorCache)) {
                IDictionary<IEnumerable<IconSelectionCriteria>, IconInfo> criteriaCache;
                if (colorCache.TryGetValue(color, out criteriaCache)) {
                    IList<IEnumerable<IconSelectionCriteria>> cacheCritieraList = criteriaCache.Keys.ToList();
                    foreach (var criteriaSequenceInCache in cacheCritieraList) {
                        // Note: cannot use SequenceEquals as enums don't implement IComparable<T>, just IComparable
                        bool criteriaIsEqual = criteriaSequenceInCache.OrderBy(c => c).SequenceEqual(criteria.OrderBy(c => c));
                        if (criteriaIsEqual) {
                            IEnumerable<IconSelectionCriteria> criteriaKey = criteriaSequenceInCache;
                            info = criteriaCache[criteriaKey];
                            D.Log("{0} has been reused from cache.", info);
                            return true;
                        }
                    }
                }
            }
            info = default(IconInfo);
            return false;
        }

        private void RecordToCache(IconInfo info, IconSection section, GameColor color, params IconSelectionCriteria[] criteria) {
            if (!_infoCache.ContainsKey(section)) {
                _infoCache.Add(section, new Dictionary<GameColor, IDictionary<IEnumerable<IconSelectionCriteria>, IconInfo>>());
            }

            var colorCache = _infoCache[section];
            if (!colorCache.ContainsKey(color)) {
                colorCache.Add(color, new Dictionary<IEnumerable<IconSelectionCriteria>, IconInfo>());
            }

            var criteriaCache = colorCache[color];
            if (!criteriaCache.ContainsKey(criteria)) {
                criteriaCache.Add(criteria, info);
                D.Log("{0} has been added to cache.", info);
            }
        }

        private string AcquireFilename(IconSection section, IconSelectionCriteria[] criteria) {
            XElement sectionNode = _xElement.Elements(_sectionTagName).Where(e => e.Attribute(_sectionAttributeTagName).Value.Equals(section.GetName())).Single();
            var selectionNodes = sectionNode.Elements(_selectionTagName);
            foreach (var selectionNode in selectionNodes) {
                var criteriaValues = selectionNode.Elements(_criteriaTagName).Select(node => node.Value);
                if (criteriaValues.OrderBy(v => v).SequenceEqual(criteria.Select(c => c.GetName()).OrderBy(n => n))) {
                    // found the criteria values we were looking for in this node
                    return selectionNode.Element(_iconFilenameTagName).Value;
                }
            }
            D.Error("No filename for {0} using Section {1} and Criteria {2} found.", GetType().Name, section.GetName(), criteria.Concatenate());
            return string.Empty;
        }

    }
}


