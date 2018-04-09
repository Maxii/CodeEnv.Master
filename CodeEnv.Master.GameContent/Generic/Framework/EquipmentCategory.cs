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
        Hull,   // ShipHull/FacilityHull would allow separate improvement techs - deferred until use case

        [EnumAttribute("Prop")]
        Propulsion, // FtlProp/StlProp would allow separate improvement techs - deferred until use case

        [EnumAttribute("Beam")]
        BeamWeapon,

        [EnumAttribute("Bullet")]
        ProjectileWeapon,

        [EnumAttribute("Missile")]
        MissileWeapon,

        [EnumAttribute("Assault")]
        AssaultWeapon,

        [EnumAttribute("Sense")]
        Sensor,

        [EnumAttribute("SGen")]
        ShieldGenerator,

        [EnumAttribute("FtlD")]
        FtlDampener,

        [EnumAttribute("CmdM")]
        CommandModule,  // FleetCmdModule/BaseCmdModule would allow separate improvement techs - deferred until use case

        //[EnumAttribute("FCM")]
        //FleetCmdModule,

        //[EnumAttribute("SbCM")]
        //StarbaseCmdModule,

        //[EnumAttribute("SeCM")]
        //SettlementCmdModule


    }
}

