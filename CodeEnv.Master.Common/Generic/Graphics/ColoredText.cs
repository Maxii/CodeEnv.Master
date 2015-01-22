// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ColoredText.cs
// Wrapper class that embeds a color hex marker into text for use with Ngui Gui elements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using UnityEngine;

    /// <summary>
    /// Data class that embeds a color hex marker into text for use
    /// with Ngui Gui elements.
    /// </summary>
    public struct ColoredText : IEquatable<ColoredText> {

        #region Equality Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(ColoredText left, ColoredText right) {
            return left.Equals(right);
        }

        public static bool operator !=(ColoredText left, ColoredText right) {
            return !left.Equals(right);
        }

        #endregion

        public string TextWithEmbeddedColor { get; private set; }

        public ColoredText(string text) :
            this(text, GameColor.White) {
        }

        public ColoredText(string text, GameColor color)
            : this() {
            if (color != GameColor.White) {
                string colorHex = MyNguiUtilities.ColorToHex(color);
                string colorNgui = MyNguiConstants.NguiEmbeddedColorFormat.Inject(colorHex);
                TextWithEmbeddedColor = colorNgui + text + MyNguiConstants.NguiEmbeddedColorTerminator;
            }
            else {
                TextWithEmbeddedColor = text;
            }
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is ColoredText)) { return false; }
            return Equals((ColoredText)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// See Page 254, C# 4.0 in a Nutshell.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() {
            int hash = 17;  // 17 = some prime number
            hash = hash * 31 + TextWithEmbeddedColor.GetHashCode(); // 31 = another prime number
            return hash;
        }

        #endregion

        public override string ToString() {
            return TextWithEmbeddedColor;
        }

        #region IEquatable<ColoredText> Members

        public bool Equals(ColoredText other) {
            return TextWithEmbeddedColor == other.TextWithEmbeddedColor;
        }

        #endregion

    }
}

