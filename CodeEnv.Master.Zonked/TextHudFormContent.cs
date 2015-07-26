// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TextHudFormContent.cs
// Content containing simple text for the TextHudForm.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Content containing simple text for the TextHudForm.
    /// </summary>
    [System.Obsolete]
    public class TextHudFormContent : AHudFormContent {

        public string Text { get; private set; }

        public TextHudFormContent(string text)
            : base(HudFormID.Text) {
            Text = text;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

