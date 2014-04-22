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

/// <summary>
/// Abstract base class for Unit Creators. Primarily used as
/// a non-generic constraint for CreatorEditors.
/// </summary>
public abstract class ACreator : AMonoBase {

    public enum DiploStateWithHuman {
        Ally,
        Friend,
        Neutral,
        Enemy
    }

    public bool isOwnerHuman;
    public DiploStateWithHuman OwnerRelationshipWithHuman;

    public bool isCompositionPreset;
    public int maxRandomElements = 8;

}

