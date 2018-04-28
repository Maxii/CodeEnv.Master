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

        [EnumAttribute("ACM_SR")]
        SRActiveCountermeasure,

        [EnumAttribute("ACM_MR")]
        MRActiveCountermeasure,

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

        [EnumAttribute("Sensor_LR")]
        LRSensor,

        [EnumAttribute("Sensor_MR")]
        MRSensor,

        [EnumAttribute("Sensor_SR")]
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
        SettlementCmdModule,

        // Facility Hulls

        [EnumAttribute("FHull_Hub")]
        FHullCentralHub,

        [EnumAttribute("FHull_Fac")]
        FHullFactory,

        [EnumAttribute("FHull_$")]
        FHullEconomic,

        [EnumAttribute("FHull_Lab")]
        FHullLaboratory,

        [EnumAttribute("FHull_Def")]
        FHullDefense,

        [EnumAttribute("FHull_Bar")]
        FHullBarracks,

        [EnumAttribute("FHull_Col")]
        FHullColonyHab,

        // Ship Hulls

        [EnumAttribute("SHull_FF")]
        SHullFrigate,

        [EnumAttribute("SHull_DD")]
        SHullDestroyer,

        [EnumAttribute("SHull_CA")]
        SHullCruiser,

        [EnumAttribute("SHull_DN")]
        SHullDreadnought,

        [EnumAttribute("SHull_CV")]
        SHullCarrier,

        [EnumAttribute("SHull_CO")]
        SHullColonizer,

        [EnumAttribute("SHull_IV")]
        SHullInvestigator,

        [EnumAttribute("SHull_TT")]
        SHullTroop,

        [EnumAttribute("SHull_SU")]
        SHullSupport,




    }
}

