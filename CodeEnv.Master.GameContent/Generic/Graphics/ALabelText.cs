// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ALabelText.cs
// Abstract base wrapper for a StringBuilder that holds the text to be displayed in a Label.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Abstract base wrapper for a StringBuilder that holds the text to be displayed in a Label.
    /// </summary>
    public abstract class ALabelText {

        private static string _phraseSeparator = Constants.SemiColon + Constants.Space;

        // This is needed as the order of Dictionary.Keys is not defined when iterating through it, even if they were added in the right order
        private static IList<LabelContentID> _displayOrder = new List<LabelContentID>() {
                {LabelContentID.Name},
                {LabelContentID.ParentName},
                {LabelContentID.Target},
                {LabelContentID.TargetDistance},
                {LabelContentID.FullSpeed},
                {LabelContentID.CurrentSpeed}, 
                {LabelContentID.MaxTurnRate},
                {LabelContentID.SectorIndex},
                {LabelContentID.Category},
                {LabelContentID.IntelCoverage},
                {LabelContentID.IntelState},
                {LabelContentID.CameraDistance},
                {LabelContentID.Capacity},
                {LabelContentID.Resources},
                {LabelContentID.Specials}, 
                {LabelContentID.MaxHitPoints},
                {LabelContentID.Owner},
                {LabelContentID.Defense},
                {LabelContentID.Offense},
                {LabelContentID.Health}, 
                {LabelContentID.Composition},
                {LabelContentID.CurrentHitPoints},
                {LabelContentID.Mass},
                {LabelContentID.Density},
                {LabelContentID.Formation},
                {LabelContentID.CurrentCmdEffectiveness},
                {LabelContentID.UnitMaxWeaponsRange},
                {LabelContentID.UnitMaxSensorRange}, 
                {LabelContentID.CombatStance},
                {LabelContentID.UnitOffense},
                {LabelContentID.UnitDefense},
                {LabelContentID.UnitCurrentHitPts},
                {LabelContentID.CapacityUsed},
                {LabelContentID.Population},
                {LabelContentID.UnitHealth}, 
                {LabelContentID.UnitMaxHitPts},
                {LabelContentID.ResourcesUsed},
                {LabelContentID.SpecialsUsed},
                {LabelContentID.UnitFullSpeed},
                {LabelContentID.UnitMaxTurnRate}
};

        public LabelID LabelID { get; private set; }

        /// <summary>
        /// Indicates whether any content has changed since the last time its text was consumed by a label.
        /// </summary>
        public bool IsChanged { get; private set; }

        /// <summary>
        /// The Report this LabelText is derived from. 
        /// Used by the Publisher to check whether this cached LabelText was derived from a current report.
        /// </summary>
        public AReport Report { get; private set; }

        private IDictionary<LabelContentID, string> _formatLookup = new Dictionary<LabelContentID, string>();
        private IDictionary<LabelContentID, IColoredTextList> _contentLookup = new Dictionary<LabelContentID, IColoredTextList>();

        private StringBuilder _sb = new StringBuilder();
        private bool _isDedicatedLinePerContentID;

        /// <summary>
        /// Initializes a new instance of the <see cref="LabelText" /> class.
        /// </summary>
        /// <param name="labelID">The label identifier.</param>
        /// <param name="report">The report this LabelText is derived from.</param>
        /// <param name="isDedicatedLinePerContentID">if set to <c>true</c> the text associated with each key will be displayed on a separate line.</param>
        public ALabelText(LabelID labelID, AReport report, bool isDedicatedLinePerContentID) {
            LabelID = labelID;
            Report = report;
            _isDedicatedLinePerContentID = isDedicatedLinePerContentID;
        }

        /// <summary>
        /// Only used to populate the initial LabelText, this method adds the content and format.
        /// </summary>
        /// <param name="contentID">The content identifier.</param>
        /// <param name="content">The content.</param>
        /// <param name="format">The format.</param>
        public void Add(LabelContentID contentID, IColoredTextList content, string format) {
            D.Assert(!_contentLookup.ContainsKey(contentID));
            D.Assert(!_formatLookup.ContainsKey(contentID));

            _contentLookup.Add(contentID, content);
            _formatLookup.Add(contentID, format);
            IsChanged = true;
        }

        /// <summary>
        /// Only used to update existing content, this method changes the content if it is different from what aleady exists.
        /// </summary>
        /// <param name="contentID">The content identifier.</param>
        /// <param name="content">The content.</param>
        public bool TryUpdate(LabelContentID contentID, IColoredTextList content) {
            D.Assert(_contentLookup.ContainsKey(contentID));
            D.Assert(_formatLookup.ContainsKey(contentID));

            IColoredTextList existingContent = _contentLookup[contentID];
            if (IsEqual(existingContent, content)) {
                //D.Log("{0} Update not needed. ContentID: {1}, Content: [{2}].", GetType().Name, contentID.GetName(), content.List.Concatenate());
                return false;
            }
            _contentLookup.Remove(contentID);
            _contentLookup.Add(contentID, content);
            IsChanged = true;
            return true;
        }

        public string GetText() {
            if (IsChanged) {
                UpdateText();
            }
            return _sb.ToString();
        }

        private void UpdateText() {
            _sb.Clear();
            foreach (var contentID in _displayOrder) {
                IColoredTextList content;
                if (_contentLookup.TryGetValue(contentID, out content)) {
                    if (content.List.Count == Constants.Zero) {
                        D.Warn("{0} content for ID {1} is empty.", GetType().Name, contentID.GetName());
                        continue;
                    }
                    var textPhrase = ConstructTextPhrase(contentID, content);
                    if (_isDedicatedLinePerContentID) {
                        _sb.AppendLine(textPhrase);
                    }
                    else {
                        if (_sb.Length > Constants.Zero) {
                            _sb.Append(_phraseSeparator);
                        }
                        _sb.Append(textPhrase);
                    }
                }
            }
            IsChanged = false;
        }

        /// <summary>
        /// Constructs and returns a text phrase by taking the base content (determined by the lineKey) and inserting colored elements of text.
        /// </summary>
        /// <param name="contentID">The line key.</param>
        /// <param name="content">The colored text elements.</param>
        /// <returns></returns>
        private string ConstructTextPhrase(LabelContentID contentID, IColoredTextList content) {
            string format;
            if (_formatLookup.TryGetValue(contentID, out format)) {
                var textElements = content.TextElements;
                D.Log("Format = {0}, TextElements = {1}.", format, textElements.Concatenate());
                string phrase = format.Inject(textElements);
                return phrase;
            }

            string warn = "No format found for {0}.".Inject(contentID.GetName());
            D.Warn(warn);
            return warn;
        }

        private bool IsEqual(IColoredTextList contentA, IColoredTextList contentB) {
            return contentA.List.OrderBy(c => c.Text).SequenceEqual(contentB.List.OrderBy(c => c.Text));
        }

    }
}

