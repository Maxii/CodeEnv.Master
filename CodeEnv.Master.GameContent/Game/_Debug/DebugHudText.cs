// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugHudText.cs
// Wrapper class for a StringBuilder that holds the text to be displayed in a DebugHud.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Wrapper class for a StringBuilder that holds the text to be displayed in a DebugHud.
    /// </summary>
    public class DebugHudText {

        // This is needed as the order of Dictionary.Keys is not defined when iterating through it, even if they were added in the right order
        private static IList<DebugHudLineKeys> _displayLineOrder = new List<DebugHudLineKeys>() {
                DebugHudLineKeys.CameraMode, 
                DebugHudLineKeys.PlayerViewMode,
                DebugHudLineKeys.PauseState,
                DebugHudLineKeys.GraphicsQuality
        };

        private static IDictionary<DebugHudLineKeys, string> _baseDisplayLineContent;

        private StringBuilder _text = new StringBuilder();

        private IDictionary<DebugHudLineKeys, IColoredTextList> _textLine;

        public bool IsDirty { get; private set; }

        static DebugHudText() {
            _baseDisplayLineContent = InitializeBaseDisplayLineContent();
            D.Assert(_baseDisplayLineContent.Count == _displayLineOrder.Count, "Missing content in list.");
        }

        private static IDictionary<DebugHudLineKeys, string> InitializeBaseDisplayLineContent() {

            // initialized in static constructor because formats that are dynamically constructed cannot be used in a static initializer
            IDictionary<DebugHudLineKeys, string> baseDisplayLineContent = new Dictionary<DebugHudLineKeys, string>() {
                {DebugHudLineKeys.CameraMode, "CameraMode: {0}"},
                {DebugHudLineKeys.PlayerViewMode, "ViewMode: {0}"},
                {DebugHudLineKeys.PauseState, "{0}"},
                {DebugHudLineKeys.GraphicsQuality, "Quality: {0}"}
            };
            return baseDisplayLineContent;
        }

        public DebugHudText()
            : this(new Dictionary<DebugHudLineKeys, IColoredTextList>()) { }

        private DebugHudText(IDictionary<DebugHudLineKeys, IColoredTextList> textLine) {
            _textLine = textLine;
            IsDirty = true;
        }

        /// <summary>
        /// Adds the specified key and text list to this DebugHudText.
        /// </summary>
        /// <param name="lineKey">The line key.</param>
        /// <param name="textList">The text list.</param>
        /// <exception cref="ArgumentException" >Attempting to add a line key that is already present.</exception>
        public void Add(DebugHudLineKeys lineKey, IColoredTextList textList) {
            _textLine.Add(lineKey, textList);
            D.Log("DebugHudText.Add() called. Content = {0}.", textList.List[0].TextWithEmbeddedColor);
            //_data[lineKey] = textList;
            IsDirty = true;
        }

        public void Replace(DebugHudLineKeys lineKey, string text) {
            Replace(lineKey, new ColoredTextList_String(text));
        }

        /// <summary>
        /// Replaces any existing list of text elements for this lineKey with the provided list. If no such list already
        /// exists, the new textElements list is simply added.
        /// </summary>
        /// <param name="lineKey">The line key.</param>
        /// <param name="textList">The text elements.</param>
        public void Replace(DebugHudLineKeys lineKey, IColoredTextList textList) {
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
        private string ConstructTextLine(DebugHudLineKeys lineKey, IColoredTextList coloredTextList) {
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

