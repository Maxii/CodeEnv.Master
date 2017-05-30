// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PooledSphericalHighlight.cs
// Spawnable Spherical highlight MonoBehaviour (with a spherical mesh and label) that tracks the designated target.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;

/// <summary>
/// Spawnable Spherical highlight MonoBehaviour (with a spherical mesh and label) that tracks the designated target.
/// <remarks>2.15.17 Added IEquatable to allow pool-generated instances to be used in Dictionary and HashSet.
/// Without it, a reused instance appears to be equal to another reused instance if from the same instance. Probably doesn't matter
/// as only 1 reused instance from an instance can exist at the same time, but...</remarks>
/// </summary>
public class PooledSphericalHighlight : SphericalHighlight, IEquatable<PooledSphericalHighlight> {

    private static int _UniqueIDCount = Constants.One;

    public string DebugName { get { return GetType().Name; } }

    private int _uniqueID;

    #region Event and Property Change Handlers

    private void OnSpawned() {
        ValidateReuseable();
        D.AssertEqual(Constants.Zero, _uniqueID);
        _uniqueID = _UniqueIDCount;
        _UniqueIDCount++;
    }

    private void OnDespawned() {
        ResetForReuse();
        D.AssertNotEqual(Constants.Zero, _uniqueID);
        _uniqueID = Constants.Zero;
    }

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        DestroyTrackingLabel();
    }

    #endregion

    #region Object.Equals and GetHashCode Override

    public override bool Equals(object obj) {
        if (!(obj is PooledSphericalHighlight)) { return false; }
        return Equals((PooledSphericalHighlight)obj);
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// See "Page 254, C# 4.0 in a Nutshell."
    /// </summary>
    /// <returns>
    /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
    /// </returns>
    public override int GetHashCode() {
        unchecked { // http://dobrzanski.net/2010/09/13/csharp-gethashcode-cause-overflowexception/
            int hash = base.GetHashCode();
            hash = hash * 31 + _uniqueID.GetHashCode(); // 31 = another prime number
            return hash;
        }
    }

    #endregion

    public override string ToString() {
        return DebugName;
    }

    #region IEquatable<PooledSphericalHighlight> Members

    public bool Equals(PooledSphericalHighlight other) {
        // if the same instance and _uniqueID are equal, then its the same
        return base.Equals(other) && _uniqueID == other._uniqueID;  // need instance comparison as _uniqueID is 0 in PoolMgr
    }

    #endregion

}

