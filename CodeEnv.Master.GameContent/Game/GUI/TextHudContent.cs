// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TextHudContent.cs
// Content containing simple text for the TextHudElement.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Content containing simple text for the TextHudElement.
    /// </summary>
    public class TextHudContent : AHudElementContent {

        public string Text { get; private set; }

        public TextHudContent(string text)
            : base(HudElementID.Text) {
            Text = text;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

