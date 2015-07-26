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
    [System.Obsolete]
    public abstract class ALabelText {

        private static string _phraseSeparator = Constants.SemiColon + Constants.Space;

        // This is needed as the order of Dictionary.Keys is not defined when iterating through it, even if they were added in the right order
        private static IList<LabelContentID> _displayOrder = new List<LabelContentID>() {
                {LabelContentID.IntelState},
                {LabelContentID.Name},
                {LabelContentID.ParentName},
                {LabelContentID.Owner},
                {LabelContentID.Category},
                {LabelContentID.Target},
                {LabelContentID.TargetDistance},
                {LabelContentID.CurrentSpeed}, 
                {LabelContentID.FullSpeed},
                {LabelContentID.MaxTurnRate},
                {LabelContentID.SectorIndex},
                {LabelContentID.Capacity},

                {LabelContentID.Resources}, 
                {LabelContentID.MaxHitPoints},
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
                {LabelContentID.Population},
                {LabelContentID.UnitHealth}, 
                {LabelContentID.UnitMaxHitPts},

                {LabelContentID.Science},
                {LabelContentID.Culture},
                {LabelContentID.NetIncome},
                {LabelContentID.UnitScience},
                {LabelContentID.UnitCulture},
                {LabelContentID.UnitNetIncome},
                {LabelContentID.Approval},

                {LabelContentID.UnitFullSpeed},
                {LabelContentID.UnitMaxTurnRate},
                {LabelContentID.OrbitalSpeed},
                {LabelContentID.CameraDistance}
};

        public DisplayTargetID LabelID { get; private set; }

        /// <summary>
        /// Indicates whether any content has changed since the last time its text was consumed by a label.
        /// </summary>
        public bool IsChanged { get; private set; }

        /// <summary>
        /// The Report this LabelText is derived from. 
        /// Used by the Publisher to check whether this cached LabelText was derived from a current report.
        /// </summary>
        public AReport Report { get; private set; }

        private IDictionary<LabelContentID, string> _phraseFormatLookup = new Dictionary<LabelContentID, string>();
        private IDictionary<LabelContentID, IColoredTextList> _contentLookup = new Dictionary<LabelContentID, IColoredTextList>();

        private StringBuilder _sb = new StringBuilder();
        private bool _isDedicatedLinePerContentID;

        /// <summary>
        /// Initializes a new instance of the <see cref="ALabelText"/> class.
        /// </summary>
        /// <param name="displayTgtID">The label identifier.</param>
        /// <param name="report">The report this LabelText is derived from.</param>
        /// <param name="isDedicatedLinePerContentID">if set to <c>true</c> the text associated with each key will be displayed on a separate line.</param>
        public ALabelText(DisplayTargetID displayTgtID, AReport report, bool isDedicatedLinePerContentID) {
            LabelID = displayTgtID;
            Report = report;
            _isDedicatedLinePerContentID = isDedicatedLinePerContentID;
        }

        /// <summary>
        /// Only used to populate the initial LabelText, this method adds the content and format.
        /// </summary>
        /// <param name="contentID">The content identifier.</param>
        /// <param name="content">The content.</param>
        /// <param name="phraseFormat">The phrase as a string.Format for insertion of content.</param>
        public void Add(LabelContentID contentID, IColoredTextList content, string phraseFormat) {
            D.Assert(!_contentLookup.ContainsKey(contentID), "ContentID {0} already present.".Inject(contentID.GetValueName()));
            D.Assert(!_phraseFormatLookup.ContainsKey(contentID), "ContentID {0} already present.".Inject(contentID.GetValueName()));

            _contentLookup.Add(contentID, content);
            _phraseFormatLookup.Add(contentID, phraseFormat);
            IsChanged = true;
        }

        /// <summary>
        /// Only used to update existing content, this method changes the content 
        /// and returns true if it is different from what already exists.
        /// </summary>
        /// <param name="contentID">The content identifier.</param>
        /// <param name="content">The content.</param>
        /// <returns></returns>
        public bool TryUpdate(LabelContentID contentID, IColoredTextList content) {
            D.Assert(_contentLookup.ContainsKey(contentID), "Missing ContentID: {0}.".Inject(contentID.GetValueName()));
            D.Assert(_phraseFormatLookup.ContainsKey(contentID), "Missing ContentID: {0}.".Inject(contentID.GetValueName()));

            IColoredTextList existingContent = _contentLookup[contentID];
            if (IsEqual(existingContent, content)) {
                //D.Log("{0} content update not needed. ContentID: {1}, Content: [{2}].", GetType().Name, contentID.GetName(), content);
                return false;
            }
            //D.Log("{0} content being updated. ContentID: {1}, Content: [{2}].", GetType().Name, contentID.GetName(), content);
            _contentLookup.Remove(contentID);
            _contentLookup.Add(contentID, content);
            IsChanged = true;
            return true;
        }

        /// <summary>
        /// Gets the text for all <c>LabelContentID</c>'s held by this ALabelText.
        /// </summary>
        /// <returns></returns>
        public string GetText() {
            if (IsChanged) {
                RebuildText();
            }
            return _sb.ToString();
        }

        /// <summary>
        /// Gets the text for this specific <c>LabelContentID</c>.
        /// </summary>
        /// <param name="contentID">The content identifier.</param>
        /// <returns></returns>
        public string GetText(LabelContentID contentID) {
            string textPhrase = string.Empty;
            if (!TryGetTextPhrase(contentID, out textPhrase)) {
                D.Warn("{0} found no content for ID {1}.", GetType().Name, contentID.GetValueName());
            }
            return textPhrase;
        }

        private void RebuildText() {
            _sb.Clear();
            foreach (var contentID in _displayOrder) {
                string textPhrase;
                if (TryGetTextPhrase(contentID, out textPhrase)) {
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
                else {
                    continue;
                }
            }
            IsChanged = false;
            D.Log("{0} rebuilt its text. IsChanged now false.", GetType().Name);
        }

        private bool TryGetTextPhrase(LabelContentID contentID, out string textPhrase) {
            IColoredTextList content;
            if (_contentLookup.TryGetValue(contentID, out content)) {
                if (content.List.Count == Constants.Zero) {
                    D.Warn("{0} content for ID {1} is empty.", GetType().Name, contentID.GetValueName());
                }
                textPhrase = ConstructTextPhrase(contentID, content);
                return true;
            }
            textPhrase = string.Empty;
            return false;
        }

        /// <summary>
        /// Constructs and returns a text phrase by taking the base content (determined by the lineKey) and inserting colored elements of text.
        /// </summary>
        /// <param name="contentID">The line key.</param>
        /// <param name="content">The colored text elements.</param>
        /// <returns></returns>
        private string ConstructTextPhrase(LabelContentID contentID, IColoredTextList content) {
            string phraseFormat;
            if (_phraseFormatLookup.TryGetValue(contentID, out phraseFormat)) {
                var textElements = content.TextElements;
                D.Log("PhraseFormat = {0}, TextElements = {1}.", phraseFormat, textElements.Concatenate());
                string textPhrase = phraseFormat.Inject(textElements);
                return textPhrase;
            }

            string warn = "No format found for {0}.".Inject(contentID.GetValueName());
            D.Warn(warn);
            return warn;
        }

        private bool IsEqual(IColoredTextList contentA, IColoredTextList contentB) {
            return contentA.List.OrderBy(c => c.Text).SequenceEqual(contentB.List.OrderBy(c => c.Text));
        }

    }
}

