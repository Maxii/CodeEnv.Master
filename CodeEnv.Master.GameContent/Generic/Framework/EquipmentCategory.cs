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
        ////[System.Obsolete]
        ActiveCountermeasure,   // Range will handle distinctions, if any

        [EnumAttribute("Hull")]
        Hull,   // ShipHull/FacilityHull would allow separate improvement techs - deferred until use case

        [EnumAttribute("STL")]
        StlPropulsion,

        [EnumAttribute("FTL")]
        FtlPropulsion,

        [EnumAttribute("Beam")]
        BeamWeapon,

        [EnumAttribute("Bullet")]
        ProjectileWeapon,

        [EnumAttribute("Missile")]
        MissileWeapon,

        [EnumAttribute("Assault")]
        AssaultWeapon,

        [EnumAttribute("LRSens")]
        LRSensor,

        [EnumAttribute("MRSens")]
        MRSensor,

        [EnumAttribute("SRSens")]
        SRSensor,

        [EnumAttribute("SGen")]
        ShieldGenerator,

        [EnumAttribute("Damp")]
        FtlDampener,

        [EnumAttribute("FCmdM")]
        FleetCmdModule,

        [EnumAttribute("SbCmdM")]
        StarbaseCmdModule,

        [EnumAttribute("StCmdM")]
        SettlementCmdModule


    }
}

