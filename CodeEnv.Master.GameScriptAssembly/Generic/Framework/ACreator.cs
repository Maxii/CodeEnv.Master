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

/// <summary>
/// Abstract base class for Unit Creators. Primarily used as
/// a non-generic constraint for CreatorEditors.
/// </summary>
[SerializeAll]
public abstract class ACreator : AMonoBase {

    public bool isOwnerUser;
    public __DiploStateWithUser ownerRelationshipWithUser;

    public bool isCompositionPreset;
    public int maxElementsInRandomUnit = 8;

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

    #region Nested Classes

    public enum __DiploStateWithUser {    // avoids offering None or Self
        Ally,
        Friend,
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

    #endregion

}

