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

        None,

        [EnumAttribute("Small Colony")]
        Colony,

        [EnumAttribute("Growing City")]
        City,

        [EnumAttribute("Mature City")]
        CityState,

        [EnumAttribute("Regional Province")]
        Province,

        [EnumAttribute("Expansive Territory")]
        Territory

    }
}

