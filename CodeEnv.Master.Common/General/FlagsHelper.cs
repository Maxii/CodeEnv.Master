// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FlagsHelper.cs
// Helper class for logical bitwise combination setting of Enums with the Flag attribute.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.Resources;

    /// <summary>
    /// Helper class for logical bitwise combination setting of Enums with the Flag attribute. 
    /// This allows code as below where Names is an enum containing a number of names.
    ///<c>Names names = Names.Susan | Names.Bob;
    ///bool susanIsIncluded = FlagsHelper.IsSet(names, Names.Susan);
    ///bool karenIsIncluded = FlagsHelper.IsSet(names, Names.Karen);</c>
    ///Karen is added to the set like:
    ///<c>FlagsHelper.Set(ref names, Names.Karen);</c>
    ///Susan is removed in a similar way:
    ///<c>FlagsHelper.Unset(ref names, Names.Susan);</c>
    /// </summary>
    public static class FlagsHelper {

        public static bool IsSet<T>(T flags, T flag) where T : struct {
            int flagsValue = (int)(object)flags;
            int flagValue = (int)(object)flag;
            return (flagsValue & flagValue) != 0;
        }

        public static void Set<T>(ref T flags, T flag) where T : struct {
            int flagsValue = (int)(object)flags;
            int flagValue = (int)(object)flag;
            flags = (T)(object)(flagsValue | flagValue);
        }

        public static void UnSet<T>(ref T flags, T flag) where T : struct {
            int flagsValue = (int)(object)flags;
            int flagValue = (int)(object)flag;
            flags = (T)(object)(flagsValue & (~flagValue));
        }



    }
}

