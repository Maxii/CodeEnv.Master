// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ColoredTextList.cs
// Generic version of class for strategy objects that provide lists of Colored Text for Huds.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {


    /// <summary>
    /// Generic version of class for strategy objects that provide lists of Colored Text for TextLabels.
    /// </summary>
    /// <typeparam name="T">A structure that is not an Enum.</typeparam>
    public class ColoredTextList<T> : ColoredTextListBase where T : struct {

        public ColoredTextList(params T[] values) : this(Constants.FormatNumber_Default, values) { }

        public ColoredTextList(string format, params T[] values) {
            D.Assert(!typeof(T).IsEnum, "IllegalType {0}".Inject(typeof(T).Name));  // Enums need different format specifiers than other structs
            foreach (T v in values) {
                _list.Add(new ColoredText(format.Inject(v)));
            }
        }

    }
}

