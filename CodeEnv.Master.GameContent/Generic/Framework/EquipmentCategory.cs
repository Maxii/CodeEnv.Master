// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: EquipmentCategory.cs
// Category that a piece of equipment belongs to.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Category that a piece of equipment belongs to.
    /// </summary>
    public enum EquipmentCategory {

        None,

        [EnumAttribute("PCM")]
        PassiveCountermeasure,

        [EnumAttribute("ACM")]
        ActiveCountermeasure,

        [EnumAttribute("Hull")]
        Hull,

        [EnumAttribute("Prop")]
        Propulsion,

        [EnumAttribute("LosW")]
        LosWeapon,

        [EnumAttribute("LauW")]
        LaunchedWeapon,

        [EnumAttribute("Sens")]
        Sensor,

        [EnumAttribute("SGen")]
        ShieldGenerator,

        [EnumAttribute("FtlD")]
        FtlDampener


    }
}

