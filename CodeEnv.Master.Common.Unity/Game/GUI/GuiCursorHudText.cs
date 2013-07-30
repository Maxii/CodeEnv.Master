// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiCursorHudText.cs
// Wrapper class for a StringBuilder that holds the text to be displayed in a GuiCursorHUD.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common.Unity {

    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;


    /// <summary>
    /// Wrapper class for a StringBuilder that holds the text to be displayed in a GuiCursorHUD.
    /// </summary>
    public class GuiCursorHudText {

        // This is needed as the order of Dictionary.Keys is not defined when iterating through it, even if they were added in the right order
        private static IList<GuiCursorHudLineKeys> _displayLineOrder = new List<GuiCursorHudLineKeys>() {
                {GuiCursorHudLineKeys.ItemName},
                {GuiCursorHudLineKeys.PieceName},
                {GuiCursorHudLineKeys.IntelState},
                {GuiCursorHudLineKeys.Capacity},
                {GuiCursorHudLineKeys.Resources},
                {GuiCursorHudLineKeys.Specials}, 
                {GuiCursorHudLineKeys.SettlementSize},
                {GuiCursorHudLineKeys.SettlementDetails},
                {GuiCursorHudLineKeys.Owner},
                {GuiCursorHudLineKeys.CombatStrength},
                {GuiCursorHudLineKeys.CombatStrengthDetails},
                {GuiCursorHudLineKeys.Health}, 
                {GuiCursorHudLineKeys.Speed}, 
                {GuiCursorHudLineKeys.Composition},
                {GuiCursorHudLineKeys.CompositionDetails},
                {GuiCursorHudLineKeys.ShipSize},
                {GuiCursorHudLineKeys.ShipDetails},
                {GuiCursorHudLineKeys.Distance}
        };

        private static IDictionary<GuiCursorHudLineKeys, string> _baseDisplayLineContent;

        private StringBuilder text = new StringBuilder();

        private IDictionary<GuiCursorHudLineKeys, IColoredTextList> _data;

        public bool IsDirty { get; private set; }
        public IntelLevel IntelLevel { get; private set; }

        static GuiCursorHudText() {
            _baseDisplayLineContent = InitializeBaseDisplayLineContent();
            D.Assert(_baseDisplayLineContent.Count == _displayLineOrder.Count, "Missing content in list.");
        }

        private static IDictionary<GuiCursorHudLineKeys, string> InitializeBaseDisplayLineContent() {

            // initialized in static constructor because formats that are dynamically constructed cannot be used in a static initializer
            IDictionary<GuiCursorHudLineKeys, string> baseDisplayLineContent = new Dictionary<GuiCursorHudLineKeys, string>() {
                {GuiCursorHudLineKeys.ItemName, "{0}"},
                {GuiCursorHudLineKeys.PieceName, "{0}"},
                {GuiCursorHudLineKeys.IntelState, "< {0} >"},
                {GuiCursorHudLineKeys.Capacity, "Capacity: {0} Slots"},   
                {GuiCursorHudLineKeys.Resources, "Resources: O: {0}, P: {1}, E: {2}"},
                {GuiCursorHudLineKeys.Specials, "[800080]Specials:[-] {0} {1}"},
                {GuiCursorHudLineKeys.SettlementSize, "Settlement: {0}"},
                {GuiCursorHudLineKeys.SettlementDetails, "Settlement: {0}, P: {1}, C: {2}, OPE: {3}, X: {4}"},
                {GuiCursorHudLineKeys.Owner, "Owner: {0}"},
                {GuiCursorHudLineKeys.CombatStrength, "Combat: {0}"}, 
                {GuiCursorHudLineKeys.CombatStrengthDetails, "Combat: {0}, B: {1}/{2}, M: {3}/{4}, P: {5}/{6}"},
                {GuiCursorHudLineKeys.Health, "Health: {0} of {1} HP"},  
                {GuiCursorHudLineKeys.Speed, CursorHudPhrases.Speed},  // the format is dynamically constructed within the ColoredText_Speed class
                {GuiCursorHudLineKeys.Composition, "{0}"},   // the format is dynamically constructed within the ColoredText_Composition class
                {GuiCursorHudLineKeys.CompositionDetails, "{0}"}, // the format is dynamically constructed within the ColoredText_Composition class
                {GuiCursorHudLineKeys.ShipSize, "Size: {0}"},   
                {GuiCursorHudLineKeys.ShipDetails, "{0}, Mass: {1}, TurnRate: {2}"},
                {GuiCursorHudLineKeys.Distance, "Distance from Camera: {0} Units"} 
            };
            return baseDisplayLineContent;
        }

        public GuiCursorHudText(IntelLevel intelLevel)
            : this(intelLevel, new Dictionary<GuiCursorHudLineKeys, IColoredTextList>()) { }

        private GuiCursorHudText(IntelLevel intelLevel, IDictionary<GuiCursorHudLineKeys, IColoredTextList> data) {
            IntelLevel = intelLevel;
            _data = data;
            IsDirty = true;
        }

        /// <summary>
        /// Adds the specified key and text list to this GuiCursorHudText.
        /// </summary>
        /// <param name="lineKey">The line key.</param>
        /// <param name="textList">The text list.</param>
        /// <exception cref="ArgumentException" >Attempting to add a line key that is already present.</exception>
        public void Add(GuiCursorHudLineKeys lineKey, IColoredTextList textList) {
            _data.Add(lineKey, textList);
            //_data[lineKey] = textList;
            IsDirty = true;
        }

        /// <summary>
        /// Replaces any existing list of text elements for this lineKey with the provided list. If no such list already
        /// exists, the new textElements list is simply added.
        /// </summary>
        /// <param name="lineKey">The line key.</param>
        /// <param name="textList">The text elements.</param>
        public void Replace(GuiCursorHudLineKeys lineKey, IColoredTextList textList) {
            if (_data.ContainsKey(lineKey)) {
                _data.Remove(lineKey);
            }
            Add(lineKey, textList);
        }

        public void Clear() {
            _data.Clear();
            IsDirty = true;
        }

        public StringBuilder GetText() {
            if (IsDirty) {
                UpdateText();
            }
            return text;
        }

        private void UpdateText() {
            text.Clear();
            foreach (var key in _displayLineOrder) {
                IColoredTextList coloredTextList;
                if (_data.TryGetValue(key, out coloredTextList)) {
                    if (coloredTextList.GetList().Count == 0) {
                        continue;
                    }
                    text.AppendLine(ConstructTextLine(key, coloredTextList));
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
        private string ConstructTextLine(GuiCursorHudLineKeys lineKey, IColoredTextList coloredTextList) {
            IList<string> textElements = new List<string>();
            foreach (var ct in coloredTextList.GetList()) {
                textElements.Add(ct.TextWithEmbeddedColor);
                //Debug.Log("ConstructTextLine called. ColoredTextElement = {0}".Inject(ct.TextWithEmbeddedColor));
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

