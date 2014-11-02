﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameSpecificExtensions.cs
// Extensions specific to this game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Extensions specific to this game.
/// </summary>
public static class GameSpecificExtensions {

    /// <summary>
    /// Finds the item closest to <c>item</c> from those provided. Throws an
    /// InvalidOperationException if <c>items</c> is empty.
    /// </summary>
    /// <typeparam name="R"></typeparam>
    /// <typeparam name="T"></typeparam>
    /// <param name="item">The item to measure from.</param>
    /// <param name="items">The items to search.</param>
    /// <returns></returns>
    //public static T FindClosest<R, T>(this R item, IEnumerable<T> items)
    //    where R : AItem
    //    where T : AItem {
    //    return items.MinBy(i => Vector3.SqrMagnitude(i.Position - item.Position));
    //}

    /// <summary>
    /// Finds the IUnitTarget closest to <c>item</c> from those targets provided. Throws an
    /// InvalidOperationException if <c>unitTargets</c> is empty.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="item">The item to measure from.</param>
    /// <param name="unitTargets">The unitTargets to search.</param>
    /// <returns></returns>
    public static IUnitTarget FindClosest<T>(this T item, IEnumerable<IUnitTarget> unitTargets)
        where T : AItem {
        return unitTargets.MinBy(t => Vector3.SqrMagnitude(t.Position - item.Position));
    }

    /// <summary>
    /// Finds the item furthest away from <c>item</c> from those provided.  Throws an
    /// InvalidOperationException if <c>items</c> is empty.
    /// </summary>
    /// <typeparam name="R"></typeparam>
    /// <typeparam name="T"></typeparam>
    /// <param name="item">The item to measure from.</param>
    /// <param name="items">The items to search.</param>
    /// <returns></returns>
    //public static T FindFurthest<R, T>(this R item, IEnumerable<T> items)
    //    where R : AItem
    //    where T : AItem {
    //    return items.MaxBy(i => Vector3.SqrMagnitude(i.Position - item.Position));
    //}

    /// <summary>
    /// Finds the IUnitTarget furthest away from <c>item</c> from those targets provided. Throws an
    /// InvalidOperationException if <c>unitTargets</c> is empty.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="item">The item to measure from.</param>
    /// <param name="unitTargets">The unitTargets to search.</param>
    /// <returns></returns>
    public static IUnitTarget FindFurthest<T>(this T item, IEnumerable<IUnitTarget> unitTargets)
        where T : AItem {
        return unitTargets.MaxBy(t => Vector3.SqrMagnitude(t.Position - item.Position));
    }


}
