// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ColoredTextList`1.cs
// Generic version of wrapper class for structs that holds a list of Colored Text using color indicators recognized by Ngui.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Generic version of wrapper class for structs that holds a list of Colored Text using color indicators recognized by Ngui.
    /// </summary>
    /// <typeparam name="T">A structure that is not an Enum.</typeparam>
    [System.Obsolete]
    public class ColoredTextList<T> : ColoredTextList where T : struct {

        public ColoredTextList(params T[] values) : this(Constants.FormatNumber_Default, values) { }

        public ColoredTextList(string format, params T[] values) {
            D.Assert(!typeof(T).IsEnum, "IllegalType {0}".Inject(typeof(T).Name));  // Enums need different format specifiers than other structs
            foreach (T v in values) {
                _list.Add(new ColoredText(format.Inject(v)));
            }
        }

        public override string ToString() {
            return TextElements.Concatenate();
        }

    }
}

