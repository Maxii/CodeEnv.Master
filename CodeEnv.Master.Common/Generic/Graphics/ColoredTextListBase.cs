// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ColoredTextListBase.cs
// Base class for strategy objects that provide lists of Colored Text for Huds.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System.Collections.Generic;

    /// <summary>
    /// Base class for strategy objects that provide lists of Colored Text for Huds.
    /// </summary>
    public class ColoredTextListBase : IColoredTextList {

        protected IList<ColoredText> _list = new List<ColoredText>(6);

        public override string ToString() {
            return TextElements.Concatenate();
        }

        #region IColoredTextList Members

        public IList<ColoredText> List { get { return _list; } }

        public string[] TextElements {
            get {
                var count = _list.Count;
                var result = new string[count];
                for (int i = 0; i < count; i++) {
                    result[i] = _list[i].Text;
                }
                return result;
            }
        }

        #endregion

    }
}

