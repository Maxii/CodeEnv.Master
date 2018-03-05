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
        ActiveCountermeasure,   // Range will handle distinctions, if any

        [EnumAttribute("Hull")]
        Hull,   // Ship vs Facility allows separate improvement techs - deferred until use case

        [EnumAttribute("Prop")]
        Propulsion, // FTL vs STL allows separate improvement techs - deferred until use case

        BeamWeapon,

        ProjectileWeapon,

        MissileWeapon,

        AssaultWeapon,

        //[EnumAttribute("Sens")]
        //Sensor,

        [EnumAttribute("ESense")]
        ElementSensor,  // Sensor. // Range will handle distinctions, if any

        [EnumAttribute("CSense")]
        CommandSensor,  // Sensor. // Range will handle distinctions, if any

        [EnumAttribute("SGen")]
        ShieldGenerator,

        [EnumAttribute("FtlD")]
        FtlDampener,

        [EnumAttribute("CmdM")]
        CommandModule,  // Fleet vs Base - deferred until use case

        //[EnumAttribute("FCM")]
        //FleetCmdModule,

        //[EnumAttribute("SbCM")]
        //StarbaseCmdModule,

        //[EnumAttribute("SeCM")]
        //SettlementCmdModule


    }
}

