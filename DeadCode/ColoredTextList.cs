// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ColoredTextList.cs
// Wrapper class that holds a list of Colored Text using color indicators recognized by Ngui.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Wrapper class that holds a list of Colored Text using color indicators recognized by Ngui.
    /// </summary>
    [System.Obsolete]
    public class ColoredTextList : IColoredTextList {

        protected IList<ColoredText> _list = new List<ColoredText>(6);

        public void Add(ColoredTextList other, bool newLine = false) {
            if (newLine) {
                AddNewLine();
            }
            (_list as List<ColoredText>).AddRange(other.List);
        }

        /// <summary>
        /// Adds the specified colored text to the list.
        /// </summary>
        /// <param name="coloredText">The colored text.</param>
        public void Add(ColoredText coloredText) {
            _list.Add(coloredText);
        }

        /// <summary>
        /// Adds the specified text annotated by the color to the list.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="color">The color.</param>
        public void Add(string text, GameColor color = GameColor.White) {
            _list.Add(new ColoredText(text, color));
        }

        /// <summary>
        /// Adds a new line marker to the list.
        /// </summary>
        public void AddNewLine() {
            _list.Add(new ColoredText(Constants.NewLine));
        }

        public override string ToString() {
            return TextElements.Concatenate();
        }

        #region IColoredTextList Members

        public IList<ColoredText> List { get { return _list; } }

        public string[] TextElements { get { return List.Select(ct => ct.Text).ToArray(); } }

        #endregion

    }
}

