// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: HudDisplayText.cs
// Wrapper class for a StringBuilder that holds the text to be displayed in a GuiCursorHUD.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Wrapper class for a StringBuilder that holds the text to be displayed in a GuiCursorHUD.
    /// </summary>
    public class GuiCursorHudText {

        // This is needed as the order of Dictionary.Keys is not defined when iterating through it, even if they were added in the right order
        private static IList<GuiCursorHudDisplayLineKeys> _displayLineOrder = new List<GuiCursorHudDisplayLineKeys>() {
                {GuiCursorHudDisplayLineKeys.Name},
                {GuiCursorHudDisplayLineKeys.Capacity},
                {GuiCursorHudDisplayLineKeys.Resources},
                {GuiCursorHudDisplayLineKeys.Specials}, 
                {GuiCursorHudDisplayLineKeys.Owner},
                {GuiCursorHudDisplayLineKeys.CombatStrength},
                {GuiCursorHudDisplayLineKeys.Health}, 
                {GuiCursorHudDisplayLineKeys.Speed}, 
                {GuiCursorHudDisplayLineKeys.Composition},
                {GuiCursorHudDisplayLineKeys.IntelState},
                {GuiCursorHudDisplayLineKeys.Distance}
        };

        private static IDictionary<GuiCursorHudDisplayLineKeys, string> _baseDisplayLineContent = new Dictionary<GuiCursorHudDisplayLineKeys, string>() {

            {GuiCursorHudDisplayLineKeys.Name, "{0}"},
            {GuiCursorHudDisplayLineKeys.Distance, "Distance: {0} Units"},  
            {GuiCursorHudDisplayLineKeys.Capacity, "Capacity: {0} Slots"},   
            {GuiCursorHudDisplayLineKeys.Resources, "Resources: O: {0}, P: {1}, E: {2}"},
            {GuiCursorHudDisplayLineKeys.Specials, "[800080]Special Resources:[-] {0} {1}"},
            {GuiCursorHudDisplayLineKeys.IntelState, "< {0} >"},
            {GuiCursorHudDisplayLineKeys.Owner, "Owner: {0}"},
            {GuiCursorHudDisplayLineKeys.Health, "Health: {0} of {1} HP"},  
            {GuiCursorHudDisplayLineKeys.CombatStrength, "Strength: {0}"}, 
            {GuiCursorHudDisplayLineKeys.Speed, "Speed: {0} Units per day"},  
            {GuiCursorHudDisplayLineKeys.Composition, "Class/Design/Qty TBD"}
        };

        private StringBuilder text = new StringBuilder();

        private IDictionary<GuiCursorHudDisplayLineKeys, IColoredTextList> _data;

        public bool IsDirty { get; private set; }
        public IntelLevel IntelLevel { get; private set; }

        public GuiCursorHudText(IntelLevel intelLevel)
            : this(intelLevel, new Dictionary<GuiCursorHudDisplayLineKeys, IColoredTextList>()) { }

        private GuiCursorHudText(IntelLevel intelLevel, IDictionary<GuiCursorHudDisplayLineKeys, IColoredTextList> data) {
            IntelLevel = intelLevel;
            _data = data;
            IsDirty = true;
        }

        public void Add(GuiCursorHudDisplayLineKeys lineKey, IColoredTextList textList) {
            if (_data.ContainsKey(lineKey)) {
                D.Error("{0} already contains Key {1}.", typeof(GuiCursorHudText), lineKey.GetName());
                return;
            }
            _data.Add(lineKey, textList);
            IsDirty = true;
        }


        //public void Add(GuiCursorHudDisplayLineKeys lineKey, IList<ColoredText> textElements) {
        //    if (_data.ContainsKey(lineKey)) {
        //        D.Error("{0} already contains Key {1}.", typeof(GuiCursorHudText), lineKey.GetName());
        //        return;
        //    }
        //    _data.Add(lineKey, textElements);
        //    IsDirty = true;
        //}

        //public void Add(GuiCursorHudDisplayLineKeys lineKey, ColoredText textElement) {
        //    if (!_data.ContainsKey(lineKey)) {
        //        _data.Add(lineKey, new List<ColoredText>());
        //    }
        //    IList<ColoredText> textList = _data[lineKey];
        //    textList.Add(textElement);
        //    IsDirty = true;
        //}

        /// <summary>
        /// Replaces any existing list of text elements for this lineKey with the provided list. If no such list already
        /// exists, the new textElements list is simply added.
        /// </summary>
        /// <param name="lineKey">The line key.</param>
        /// <param name="textList">The text elements.</param>
        public void Replace(GuiCursorHudDisplayLineKeys lineKey, IColoredTextList textList) {
            if (_data.ContainsKey(lineKey)) {
                _data.Remove(lineKey);
            }
            Add(lineKey, textList);
        }

        /// <summary>
        /// Replaces any existing list of text elements for this lineKey with the provided single textElement. If no such list already
        /// exists, the new textElements list is simply added.
        /// </summary>
        /// <param name="lineKey">The line key.</param>
        /// <param name="textElement">The text element.</param>
        //public void Replace(GuiCursorHudDisplayLineKeys lineKey, ColoredText textElement) {
        //    if (_data.ContainsKey(lineKey)) {
        //        if (_data[lineKey].Count != 1) {
        //            D.Warn("{0} {1} replacing text element with more than 1 value.", typeof(ColoredText), textElement.TextWithEmbeddedColor);
        //        }
        //        _data.Remove(lineKey);
        //    }
        //    Add(lineKey, textElement);
        //}

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
        private string ConstructTextLine(GuiCursorHudDisplayLineKeys lineKey, IColoredTextList coloredTextList) {
            IList<string> textElements = new List<string>();
            foreach (var ct in coloredTextList.GetList()) {
                textElements.Add(ct.TextWithEmbeddedColor);
                //Debug.Log("ConstructTextLine called. ColoredTextElement = {0}".Inject(ct.TextWithEmbeddedColor));
            }

            string baseText;
            if (_baseDisplayLineContent.TryGetValue(lineKey, out baseText)) {
                D.Log("BaseText = {0}", baseText);
                D.Log("Text Elements = {0}", textElements.Concatenate<string>(Constants.Comma));
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

