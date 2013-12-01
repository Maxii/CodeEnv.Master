// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiHudText.cs
// Wrapper class for a StringBuilder that holds the text to be displayed in a GuiCursorHUD.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;


    /// <summary>
    /// Wrapper class for a StringBuilder that holds the text to be displayed in a GuiCursorHUD.
    /// </summary>
    public class GuiHudText {

        // This is needed as the order of Dictionary.Keys is not defined when iterating through it, even if they were added in the right order
        private static IList<GuiHudLineKeys> _displayLineOrder = new List<GuiHudLineKeys>() {
                {GuiHudLineKeys.Name},
                {GuiHudLineKeys.ParentName},
                {GuiHudLineKeys.Type},
                {GuiHudLineKeys.IntelState},
                {GuiHudLineKeys.Capacity},
                {GuiHudLineKeys.Resources},
                {GuiHudLineKeys.Specials}, 
                {GuiHudLineKeys.SettlementDetails},
                {GuiHudLineKeys.Owner},
                {GuiHudLineKeys.CombatStrength},
                {GuiHudLineKeys.CombatStrengthDetails},
                {GuiHudLineKeys.Health}, 
                {GuiHudLineKeys.Speed}, 
                {GuiHudLineKeys.Composition},
                {GuiHudLineKeys.CompositionDetails},
                {GuiHudLineKeys.ShipDetails},
                {GuiHudLineKeys.Distance}
        };

        private static IDictionary<GuiHudLineKeys, string> _baseDisplayLineContent;

        private StringBuilder _text = new StringBuilder();

        private IDictionary<GuiHudLineKeys, IColoredTextList> _textLine;

        public bool IsDirty { get; private set; }
        public IntelLevel IntelLevel { get; private set; }

        static GuiHudText() {
            _baseDisplayLineContent = InitializeBaseDisplayLineContent();
            D.Assert(_baseDisplayLineContent.Count == _displayLineOrder.Count, "Missing content in list.");
        }

        private static IDictionary<GuiHudLineKeys, string> InitializeBaseDisplayLineContent() {

            // initialized in static constructor because formats that are dynamically constructed cannot be used in a static initializer
            IDictionary<GuiHudLineKeys, string> baseDisplayLineContent = new Dictionary<GuiHudLineKeys, string>() {
                {GuiHudLineKeys.Name, "{0}"},
                {GuiHudLineKeys.ParentName, "{0}"},
                {GuiHudLineKeys.Type, "Type: {0}"},
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
                {GuiHudLineKeys.Distance, "Distance from Camera: {0} Units"} 
            };
            return baseDisplayLineContent;
        }

        public GuiHudText(IntelLevel intelLevel)
            : this(intelLevel, new Dictionary<GuiHudLineKeys, IColoredTextList>()) { }

        private GuiHudText(IntelLevel intelLevel, IDictionary<GuiHudLineKeys, IColoredTextList> textLine) {
            IntelLevel = intelLevel;
            _textLine = textLine;
            IsDirty = true;
        }

        /// <summary>
        /// Adds the specified key and text list to this GuiCursorHudText.
        /// </summary>
        /// <param name="lineKey">The line key.</param>
        /// <param name="textList">The text list.</param>
        /// <exception cref="ArgumentException" >Attempting to add a line key that is already present.</exception>
        public void Add(GuiHudLineKeys lineKey, IColoredTextList textList) {
            _textLine.Add(lineKey, textList);
            //_data[lineKey] = textList;
            IsDirty = true;
        }

        /// <summary>
        /// Replaces any existing list of text elements for this lineKey with the provided list. If no such list already
        /// exists, the new textElements list is simply added.
        /// </summary>
        /// <param name="lineKey">The line key.</param>
        /// <param name="textList">The text elements.</param>
        public void Replace(GuiHudLineKeys lineKey, IColoredTextList textList) {
            if (_textLine.ContainsKey(lineKey)) {
                _textLine.Remove(lineKey);
            }
            Add(lineKey, textList);
        }

        public void Clear() {
            _textLine.Clear();
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
                if (_textLine.TryGetValue(key, out coloredTextList)) {
                    if (coloredTextList.List.Count == 0) {
                        continue;
                    }
                    _text.AppendLine(ConstructTextLine(key, coloredTextList));
                }
            }
            IsDirty = false;
        }

        /// <summary>
        /// Constructs and returns a line of text by taking the base content (determined by the lineKey) and inserting colored elements of text.
        /// </summary>
        /// <param name="lineKey">The line key.</param>
        /// <param name="coloredTextList">The colored text elements.</param>
        /// <returns></returns>
        private string ConstructTextLine(GuiHudLineKeys lineKey, IColoredTextList coloredTextList) {
            IList<string> textElements = new List<string>();
            foreach (var ct in coloredTextList.List) {
                textElements.Add(ct.TextWithEmbeddedColor);
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

