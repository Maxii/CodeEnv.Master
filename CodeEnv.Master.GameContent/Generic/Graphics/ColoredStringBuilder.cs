// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ColoredStringBuilder.cs
// Wrapper class providing convenience methods for incorporating color into text.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Text;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Wrapper class providing convenience methods for incorporating color into text.
    /// </summary>
    public class ColoredStringBuilder {

        private StringBuilder _sb;

        public ColoredStringBuilder(string text = null, GameColor color = GameColor.White) {
            _sb = new StringBuilder();
            if (!text.IsNullOrEmpty()) {
                Append(text, color);
            }
        }

        public void Append(string text, GameColor color = GameColor.White) {
            string textResult = color != GameColor.White ? text.SurroundWith(color) : text;
            _sb.Append(textResult);
        }

        public void AppendLine(string text = null) {
            if (!text.IsNullOrEmpty()) {
                _sb.Append(text);
            }
            _sb.AppendLine();
        }

        public override string ToString() {
            return _sb.ToString();
        }

    }
}

