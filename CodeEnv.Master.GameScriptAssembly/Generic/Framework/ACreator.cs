// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ACreator.cs
// Abstract base class for Unit Creators.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for Unit Creators. Primarily used as
/// a non-generic constraint for CreatorEditors.
/// </summary>
public abstract class ACreator : AMonoBase {

    /// <summary>
    /// Static counter used to provide a unique name for each element.
    /// </summary>
    protected static int _elementInstanceIDCounter = Constants.One;

    public bool isOwnerUser;
    public __DiploStateWithUser ownerRelationshipWithUser;

    public bool isCompositionPreset;
    public int elementsInRandomUnit = 8;

    public bool toDelayOperations;
    public bool toDelayBuild;
    public int hourDelay = 0;
    public int dayDelay = 0;
    public int yearDelay = 0;

    public WeaponLoadout losWeaponsPerElement;
    public WeaponLoadout missileWeaponsPerElement;

    public int activeCMsPerElement = 2;
    public int shieldGeneratorsPerElement = 2;
    public int passiveCMsPerElement = 2;
    public int sensorsPerElement = 2;
    public int countermeasuresPerCmd = 2;

    public bool enableTrackingLabel = false;
    public bool showHQDebugLog = false;

    #region Nested Classes

    public enum __DiploStateWithUser {    // avoids offering None and Self
        Alliance,
        Friendly,
        Neutral,
        ColdWar,
        War
    }

    public enum WeaponLoadout {

        /// <summary>
        /// No weapons will be carried by the element.
        /// </summary>
        None,

        /// <summary>
        /// One weapon will be carried by the element.
        /// </summary>
        One,

        /// <summary>
        /// The number of weapons carried by the element will 
        /// be a random value between 0 and the maximum allowed by the element category, inclusive.
        /// </summary>
        Random,

        /// <summary>
        /// The number of weapons carried by the element will 
        /// be the maximum allowed by the element category.
        /// </summary>
        Max
    }

    public enum DebugFleetFormation {
        Random,
        Globe,
        Plane,
        Diamond,
        Spread,
        Wedge
    }

    public enum DebugBaseFormation {
        Random,
        Globe,
        Plane,
        Diamond,
        Spread
    }

    #endregion
}

