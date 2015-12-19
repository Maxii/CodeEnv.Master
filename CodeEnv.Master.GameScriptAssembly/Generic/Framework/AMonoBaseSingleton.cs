// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMonoBaseSingleton.cs
// Abstract non-generic base class for AMonoSingleton&lt;T&gt;. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

/// <summary>
/// Abstract non-generic base class for AMonoSingleton&lt;T&gt;. Primary purpose is to allow
/// access to Properties of the singleton without knowing what T is.
/// </summary>
public abstract class AMonoBaseSingleton : AMonoBase {

    /// <summary>
    /// Determines whether this singleton is persistent across scenes. If not persistent, the instance
    /// is destroyed on each scene load. If it is persistent, then it is not destroyed on a scene
    /// load, and any extra instances already present in the new scene are destroyed.
    /// </summary>
    public virtual bool IsPersistentAcrossScenes { get { return false; } }

}

