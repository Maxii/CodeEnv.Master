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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Singleton. Abstract, generic base Factory that makes instances of IconInfo for Commands.
    /// </summary>
    /// <typeparam name="ReportType">The type of the Report.</typeparam>
    /// <typeparam name="FactoryType">The type of the derived factory.</typeparam>
    public abstract class ACmdIconInfoFactory<ReportType, FactoryType> : AGenericSingleton<FactoryType>
        where ReportType : AUnitCmdReport
        where FactoryType : ACmdIconInfoFactory<ReportType, FactoryType> {

        private static readonly Layers Layer = TempGameValues.CmdIconCullLayer;

        private static readonly WidgetPlacement Placement = WidgetPlacement.Above;

        private static readonly IntVector2 Size = new IntVector2(40, 40);

        protected abstract AtlasID AtlasID { get; }

        private IDictionary<IconSection, IDictionary<GameColor, IDictionary<IEnumerable<IconSelectionCriteria>, TrackingIconInfo>>> _infoCache;

        protected override void Initialize() {
            _infoCache = new Dictionary<IconSection, IDictionary<GameColor, IDictionary<IEnumerable<IconSelectionCriteria>, TrackingIconInfo>>>(IconSectionEqualityComparer.Default);
            // WARNING: Do not use Instance or _instance in here as this is still part of Constructor
        }

        /// <summary>
        /// Makes an instance of IconInfo. Clients should only use this method to make IconInfo 
        /// as they are compared against each other to determine equality.
        /// </summary>
        /// <param name="userRqstdCmdReport">The Command Report that was requested by the user.</param>
        /// <returns></returns>
        public TrackingIconInfo MakeInstance(ReportType userRqstdCmdReport) {
            D.AssertEqual(GameReferences.GameManager.UserPlayer, userRqstdCmdReport.Player);
            IconSection section = GetIconSection();
            IconSelectionCriteria[] criteria = GetSelectionCriteria(userRqstdCmdReport);
            GameColor color = userRqstdCmdReport.Owner != null ? userRqstdCmdReport.Owner.Color : GameColor.White;
            var iconInfo = GetInstance(section, color, criteria);
            return iconInfo;
        }

        protected abstract IconSelectionCriteria[] GetSelectionCriteria(ReportType userRqstdCmdReport);

        protected virtual IconSection GetIconSection() { return IconSection.Base; }

        /// <summary>
        /// Makes or retrieves an already made instance of IconInfo based on the provided section, color and criteria.
        /// These instances hold the filename, atlas and color of the icon. 
        /// </summary>
        /// <param name="section">The section.</param>
        /// <param name="color">The color.</param>
        /// <param name="criteria">The criteria.</param>
        /// <returns></returns>
        private TrackingIconInfo GetInstance(IconSection section, GameColor color, params IconSelectionCriteria[] criteria) {
            TrackingIconInfo info;
            if (!TryCheckCache(section, color, out info, criteria)) {
                info = MakeInstance(section, color, criteria);
                RecordToCache(info, section, color, criteria);
            }
            return info;
        }

        private TrackingIconInfo MakeInstance(IconSection section, GameColor color, params IconSelectionCriteria[] criteria) {
            string filename = AcquireFilename(section, criteria);
            return new TrackingIconInfo(filename, AtlasID, color, Size, Placement, Layer);
        }

        protected abstract string AcquireFilename(IconSection section, params IconSelectionCriteria[] criteria);

        private bool TryCheckCache(IconSection section, GameColor color, out TrackingIconInfo info, params IconSelectionCriteria[] criteria) {
            IDictionary<GameColor, IDictionary<IEnumerable<IconSelectionCriteria>, TrackingIconInfo>> colorCache;
            if (_infoCache.TryGetValue(section, out colorCache)) {
                IDictionary<IEnumerable<IconSelectionCriteria>, TrackingIconInfo> criteriaCache;
                if (colorCache.TryGetValue(color, out criteriaCache)) {
                    if (criteriaCache.TryGetValue(criteria, out info)) {
                        //D.LogBold("{0}: {1} has been reused from cache. SectionKey: {2}, ColorKey: {3}, CriteriaSequenceKey {4}.",
                        //    DebugName, info, section.GetValueName(), color.GetValueName(), criteria.Select(c => c.GetValueName()).Concatenate());
                        return true;
                    }
                }
            }
            info = default(TrackingIconInfo);
            return false;
        }

        #region TryCheckCache Archive

        //private bool TryCheckCache(IconSection section, GameColor color, out IconInfo info, params IconSelectionCriteria[] criteria) {
        //    IDictionary<GameColor, IDictionary<IEnumerable<IconSelectionCriteria>, IconInfo>> colorCache;
        //    if (_infoCache.TryGetValue(section, out colorCache)) {
        //        IDictionary<IEnumerable<IconSelectionCriteria>, IconInfo> criteriaCache;
        //        if (colorCache.TryGetValue(color, out criteriaCache)) {
        //            IList<IEnumerable<IconSelectionCriteria>> cacheCritieraList = criteriaCache.Keys.ToList();
        //            foreach (var criteriaSequenceInCache in cacheCritieraList) {
        //                // Note: cannot use SequenceEquals as enums don't implement IComparable<T>, just IComparable
        //                bool criteriaIsEqual = criteriaSequenceInCache.OrderBy(c => c).SequenceEqual(criteria.OrderBy(c => c));
        //                if (criteriaIsEqual) {
        //                    IEnumerable<IconSelectionCriteria> criteriaKey = criteriaSequenceInCache;
        //                    info = criteriaCache[criteriaKey];
        //                    D.LogBold("{0}: {1} has been reused from cache. SectionKey: {2}, ColorKey: {3}, CriteriaSequenceKey {4}.",
        //                        DebugName, info, section.GetValueName(), color.GetValueName(), criteria.Select(c => c.GetValueName()).Concatenate());
        //                    return true;
        //                }
        //            }
        //        }
        //    }
        //    info = default(IconInfo);
        //    return false;
        //}

        #endregion

        private void RecordToCache(TrackingIconInfo info, IconSection section, GameColor color, params IconSelectionCriteria[] criteria) {
            if (!_infoCache.ContainsKey(section)) {
                _infoCache.Add(section, new Dictionary<GameColor, IDictionary<IEnumerable<IconSelectionCriteria>, TrackingIconInfo>>(GameColorEqualityComparer.Default));
            }

            var colorCache = _infoCache[section];
            if (!colorCache.ContainsKey(color)) {
                colorCache.Add(color, new Dictionary<IEnumerable<IconSelectionCriteria>, TrackingIconInfo>(IconSelectionCriteriaSequenceEqualityComparer.Default));
            }

            var criteriaCache = colorCache[color];
            if (!criteriaCache.ContainsKey(criteria)) {
                criteriaCache.Add(criteria, info);
                //D.Log("{0}: {1} has been added to cache. SectionKey: {2}, ColorKey: {3}, CriteriaSequenceKey: {4}.",
                //    DebugName, info, section.GetValueName(), color.GetValueName(), criteria.Select(c => c.GetValueName()).Concatenate());
            }
        }

        #region Nested Classes

        /// <summary>
        /// EqualityComparer for IconSelectionCriteria sequences.
        /// <remarks>The key to making this work is GetHashCode(). If it doesn't comply with the rule
        /// below, Equals is never even called.</remarks>
        /// </summary>
        /// <see cref="http://stackoverflow.com/questions/14675720/iequalitycomparer-for-sequenceequal" />
        private class IconSelectionCriteriaSequenceEqualityComparer : IEqualityComparer<IEnumerable<IconSelectionCriteria>> {

            public static readonly IconSelectionCriteriaSequenceEqualityComparer Default = new IconSelectionCriteriaSequenceEqualityComparer();

            public string DebugName { get { return GetType().Name; } }

            public override string ToString() {
                return DebugName;
            }

            #region IEqualityComparer<IEnumerable<IconSelectionCriteria>> Members

            public bool Equals(IEnumerable<IconSelectionCriteria> sequence1, IEnumerable<IconSelectionCriteria> sequence2) {
                var orderedSequence1 = sequence1.OrderBy(c => c);
                var orderedSequence2 = sequence2.OrderBy(c => c);
                bool result = orderedSequence1.SequenceEqual(orderedSequence2, IconSelectionCriteriaEqualityComparer.Default);
                //D.Log("CriteriaSequence.Equals result = {0}, Sequence1: {1}, Sequence2: {2}.", result, orderedSequence1.Select(s => s.GetValueName()).Concatenate(), orderedSequence2.Select(s => s.GetValueName()).Concatenate());
                return result;
            }

            public int GetHashCode(IEnumerable<IconSelectionCriteria> sequence) {
                // Rule: If two things are equal then they MUST return the same value for GetHashCode()
                // http://stackoverflow.com/questions/371328/why-is-it-important-to-override-gethashcode-when-equals-method-is-overridden
                if (sequence == null) {
                    return Constants.Zero;
                }
                unchecked {
                    return sequence.Select(c => c.GetHashCode()).Aggregate(17, (a, b) => 31 * a + b);
                }
                //// return sequence.GetHashCode();
            }

            #endregion

        }

        public abstract class ACmdIconInfoXmlReader<ReaderType> : AXmlReader<ReaderType>
            where ReaderType : ACmdIconInfoXmlReader<ReaderType> {

            private string _sectionTagName = "Section";
            private string _sectionAttributeTagName = "SectionName";
            private string _selectionTagName = "Selection";
            private string _criteriaTagName = "Criteria";
            private string _iconFilenameTagName = "Filename";

            protected sealed override string RootTagName { get { return "Icon"; } }

            public string AcquireFilename(IconSection section, IconSelectionCriteria[] criteria) {
                XElement sectionNode = _xElement.Elements(_sectionTagName).Where(e => e.Attribute(_sectionAttributeTagName).Value.Equals(section.GetValueName())).Single();
                var selectionNodes = sectionNode.Elements(_selectionTagName);
                foreach (var selectionNode in selectionNodes) {
                    var criteriaValues = selectionNode.Elements(_criteriaTagName).Select(node => node.Value);
                    if (criteriaValues.OrderBy(v => v).SequenceEqual(criteria.Select(c => c.GetValueName()).OrderBy(n => n))) {
                        // found the criteria values we were looking for in this node
                        return selectionNode.Element(_iconFilenameTagName).Value;
                    }
                }
                D.Error("No filename for {0} using Section {1} and Criteria {2} found.", GetType().Name, section.GetValueName(), criteria.Concatenate());
                return string.Empty;
            }

        }

    }

    #endregion

    #region Archive

    //private IconSelectionCriteria[] GetSelectionCriteria(ReportType userRqstdCmdReport) {
    //    IList<IconSelectionCriteria> criteria = new List<IconSelectionCriteria>();
    //    IntelCoverage intelCoverageUserHasWithCmd = userRqstdCmdReport.IntelCoverage;
    //    //D.Log("{0} found UserIntelCoverage = {1}.", GetType().Name, intelCoverageUserHasWithCmd.GetValueName());

    //    /**************************************************************************************************************
    //    * Concept: Cmd's IntelCoverage is same as HQElement coverage. If Cmd/HQ not yet detected, then invisible icon 
    //    *   - does this matter? Icon won't be shown anyhow?
    //    * If Cmd only detected LongRange (aka Basic), then unknown icon no matter how many other elements are known
    //    * If Cmd IntelCoverage Essential, then icon will depend on what is known about other elements 
    //    *   - aka Category which is size of Cmd (# of elements known)
    //    * If Cmd IntelCoverage Broad or Comprehensive, then icon will also include whether there are special elements 
    //    *   - aka Science, Colony or Troop
    //    ****************************************************************************************************************/
    //    switch (intelCoverageUserHasWithCmd) {
    //        case IntelCoverage.None:
    //            // always returns None
    //            criteria.Add(IconSelectionCriteria.None);
    //            break;
    //        case IntelCoverage.Basic:
    //            // always returns Unknown
    //            criteria.Add(IconSelectionCriteria.Unknown);
    //            break;
    //        case IntelCoverage.Essential:
    //            // always returns level 1-5
    //            criteria.Add(GetCriteriaFromCategory(userRqstdCmdReport));
    //            break;
    //        case IntelCoverage.Broad:
    //        case IntelCoverage.Comprehensive:
    //            // always returns a comprehensive icon
    //            criteria.Add(GetCriteriaFromCategory(userRqstdCmdReport));
    //            GetCriteriaFromComposition(userRqstdCmdReport).ForAll(iSc => criteria.Add(iSc));
    //            break;
    //        default:
    //            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(intelCoverageUserHasWithCmd));
    //    }
    //    return criteria.ToArray();
    //}

    //protected abstract IconSelectionCriteria GetCriteriaFromCategory(ReportType cmdReport);

    //protected abstract IEnumerable<IconSelectionCriteria> GetCriteriaFromComposition(ReportType cmdReport);

    #endregion

}




