// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiLabelText.cs
// Wrapper class for a StringBuilder that holds the text to be displayed in a Label.
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
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Wrapper class for a StringBuilder that holds the text to be displayed in a Label.
    /// </summary>
    public class GuiLabelText {

        private static string _phraseSeparator = Constants.SemiColon + Constants.Space;

        // This is needed as the order of Dictionary.Keys is not defined when iterating through it, even if they were added in the right order
        private static IList<GuiHudLineKeys> _displayLineOrder = new List<GuiHudLineKeys>() {
                {GuiHudLineKeys.Name},
                {GuiHudLineKeys.TargetName},
                {GuiHudLineKeys.TargetDistance},
                {GuiHudLineKeys.Speed}, 
                {GuiHudLineKeys.CameraDistance},
                {GuiHudLineKeys.SectorIndex},
                {GuiHudLineKeys.Category},
                {GuiHudLineKeys.IntelState},
                {GuiHudLineKeys.ParentName},
                {GuiHudLineKeys.Capacity},
                {GuiHudLineKeys.Resources},
                {GuiHudLineKeys.Specials}, 
                {GuiHudLineKeys.SettlementDetails},
                {GuiHudLineKeys.Owner},
                {GuiHudLineKeys.CombatStrength},
                {GuiHudLineKeys.CombatStrengthDetails},
                {GuiHudLineKeys.Health}, 
                {GuiHudLineKeys.Composition},
                {GuiHudLineKeys.CompositionDetails},
                {GuiHudLineKeys.ShipDetails},
                {GuiHudLineKeys.Density}
        };

        private static IDictionary<GuiHudLineKeys, string> _baseDisplayLineContent;

        /// <summary>
        /// Static Constructor. Initializes the <see cref="GuiLabelText"/> class.
        /// </summary>
        static GuiLabelText() {
            _baseDisplayLineContent = InitializeBaseDisplayLineContent();
            D.Assert(_baseDisplayLineContent.Count == _displayLineOrder.Count, "Missing content in list.");
        }

        private static IDictionary<GuiHudLineKeys, string> InitializeBaseDisplayLineContent() {
            // initialized in static constructor because formats that are dynamically constructed cannot be used in a static initializer
            IDictionary<GuiHudLineKeys, string> baseDisplayLineContent = new Dictionary<GuiHudLineKeys, string>() {
                {GuiHudLineKeys.Name, "{0}"},
                {GuiHudLineKeys.ParentName, "{0}"},
                {GuiHudLineKeys.Category, "{0}: {1}"},
                {GuiHudLineKeys.IntelState, "< {0} >"},
                {GuiHudLineKeys.Capacity, "Capacity: {0} Slots"},   
                {GuiHudLineKeys.Resources, "Resources: O: {0}, P: {1}, E: {2}"},
                {GuiHudLineKeys.Specials, "[800080]Specials:[-] {0} {1}..."},
                {GuiHudLineKeys.SettlementDetails, "Settlement: {0}, P: {1}, C: {2}, OPE: {3}, X: {4}"},
                {GuiHudLineKeys.Owner, "Owner: {0}"},
                {GuiHudLineKeys.CombatStrength, "Combat: {0}"}, 
                {GuiHudLineKeys.CombatStrengthDetails, "Combat: {0}, B: {1}/{2}, M: {3}/{4}, P: {5}/{6}"},
                {GuiHudLineKeys.Health, "Health: {0}%, Max {1} HP"},  
                {GuiHudLineKeys.Speed, CursorHudPhrases.Speed},  // the format is dynamically constructed within the ColoredText_Speed class
                {GuiHudLineKeys.Composition, "{0}"},   // the format is dynamically constructed within the ColoredText_Composition class
                {GuiHudLineKeys.CompositionDetails, "{0}"}, // the format is dynamically constructed within the ColoredText_Composition class
                {GuiHudLineKeys.ShipDetails, "{0}, Mass: {1}, TurnRate: {2}"},
                {GuiHudLineKeys.SectorIndex, "Sector {0}"},
                {GuiHudLineKeys.Density, "Density: {0}"},
                {GuiHudLineKeys.CameraDistance, "Camera Distance: {0} Units"},
                {GuiHudLineKeys.TargetName, "Target Name: {0}"}, 
                {GuiHudLineKeys.TargetDistance, "Target Distance: {0} Units"}
            };
            return baseDisplayLineContent;
        }

        public bool IsDirty { get; private set; }

        private IDictionary<GuiHudLineKeys, IColoredTextList> _textLineLookup = new Dictionary<GuiHudLineKeys, IColoredTextList>();
        private StringBuilder _text = new StringBuilder();
        private bool _isDedicatedLinePerKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="GuiLabelText"/> class.
        /// </summary>
        /// <param name="isDedicatedLinePerKey">if set to <c>true</c> the text associated with each key will be displayed on a separate line.</param>
        public GuiLabelText(bool isDedicatedLinePerKey) {
            _isDedicatedLinePerKey = isDedicatedLinePerKey;
        }

        /// <summary>
        /// Adds or replaces any existing list of text elements for this lineKey with the provided list. 
        /// If the existing list and the provided list are identical, this method does nothing.
        /// </summary>
        /// <param name="lineKey">The line key.</param>
        /// <param name="textList">The list of text elements.</param>
        public void Add(GuiHudLineKeys lineKey, IColoredTextList textList) {
            if (_textLineLookup.ContainsKey(lineKey)) {
                IColoredTextList existingList = _textLineLookup[lineKey];
                if (IsEqual(textList, existingList)) {
                    //D.Warn("{0} key {1} has identical content [{2}].", GetType().Name, lineKey.GetName(), textList.List.Concatenate());
                    return;
                }
                _textLineLookup.Remove(lineKey);
                D.Log("Removing {0} HUD line [{1}].", lineKey.GetName(), existingList.List.Concatenate());
            }
            D.Log("Adding {0} HUD line [{1}].", lineKey.GetName(), textList.List.Concatenate());
            _textLineLookup.Add(lineKey, textList);
            //_data[lineKey] = textList;
            IsDirty = true;
        }

        private bool IsEqual(IColoredTextList textListA, IColoredTextList textListB) {
            if (textListA.List.Except(textListB.List).Any()) {
                return false;
            }
            return true;
        }

        public void Clear() {
            _textLineLookup.Clear();
            IsDirty = true;
        }

        public StringBuilder GetText() {
            if (IsDirty) {
                UpdateText();
            }
            return _text;
        }

        private void UpdateText() {
            _text.Clear();
            foreach (var key in _displayLineOrder) {
                IColoredTextList coloredTextList;
                if (_textLineLookup.TryGetValue(key, out coloredTextList)) {
                    if (coloredTextList.List.Count == Constants.Zero) {
                        continue;
                    }
                    var textPhrase = ConstructTextPhrase(key, coloredTextList);
                    if (_isDedicatedLinePerKey) {
                        _text.AppendLine(textPhrase);
                    }
                    else {
                        if (_text.Length > Constants.Zero) {
                            _text.Append(_phraseSeparator);
                        }
                        _text.Append(textPhrase);
                    }
                }
            }
            IsDirty = false;
        }

        /// <summary>
        /// Constructs and returns a text phrase by taking the base content (determined by the lineKey) and inserting colored elements of text.
        /// </summary>
        /// <param name="lineKey">The line key.</param>
        /// <param name="coloredTextList">The colored text elements.</param>
        /// <returns></returns>
        private string ConstructTextPhrase(GuiHudLineKeys lineKey, IColoredTextList coloredTextList) {
            IList<string> textElements = new List<string>();
            foreach (var ct in coloredTextList.List) {
                textElements.Add(ct.Text);
                //D.Log("ConstructTextLine called. ColoredTextElement = {0}".Inject(ct.TextWithEmbeddedColor));
            }

            string baseText;
            if (_baseDisplayLineContent.TryGetValue(lineKey, out baseText)) {
                //D.Log("BaseText = {0}", baseText);
                //D.Log("Text Elements = {0}", textElements.Concatenate<string>(Constants.Comma));
                string colorEmbeddedLineText = baseText.Inject(textElements.ToArray<string>());
                return colorEmbeddedLineText;
            }

            string warn = "No LineKey {0} found.".Inject(lineKey.GetName());
            D.Warn(warn);
            return warn;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

