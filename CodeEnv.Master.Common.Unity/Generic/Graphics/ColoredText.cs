﻿// --------------------------------------------------------------------------------------------------------------------
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
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

namespace CodeEnv.Master.Common.Unity {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Data class that embeds a color hex marker into text for use
    /// with Ngui Gui elements.
    /// </summary>
    public class ColoredText {

        public string TextWithEmbeddedColor { get; private set; }

        public ColoredText(string text) :
            this(text, Color.white) {
        }

        public ColoredText(string text, Color color) {
            if (color != Color.white) {
                string colorHex = MyNguiUtilities.ColorToHex(color);
                string colorNgui = MyNguiConstants.NguiEmbeddedColorFormat.Inject(colorHex);
                TextWithEmbeddedColor = colorNgui + text + MyNguiConstants.NguiEmbeddedColorTerminator;
            }
            else {
                TextWithEmbeddedColor = text;
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}
