// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementSize.cs
// Enum identifying the alternative sizes of a Settlement.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

using CodeEnv.Master.Common;
namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum identifying the alternative sizes of a Settlement.
    /// </summary>
    public enum SettlementCategory {

        None = 0,

        [EnumAttribute("Small Colony")]
        Colony = 1,

        [EnumAttribute("Growing City")]
        City = 2,

        [EnumAttribute("Mature City")]
        CityState = 3,

        [EnumAttribute("Regional Province")]
        Province = 4,

        [EnumAttribute("Expansive Territory")]
        Territory = 5

    }
}

