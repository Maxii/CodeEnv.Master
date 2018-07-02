// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameUtility.cs
// Collection of tools and utilities specific to the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using MoreLinq;
    using UnityEngine;

    /// <summary>
    /// Collection of tools and utilities specific to the game.
    /// </summary>
    public static class GameUtility {

        public static IntelCoverage GetLowestCommonCoverage(IEnumerable<IntelCoverage> coverages) {
            IntelCoverage lowestCommonCoverage = IntelCoverage.Comprehensive;
            foreach (var coverage in coverages) {
                if (coverage < lowestCommonCoverage) {
                    lowestCommonCoverage = coverage;
                }
            }
            return lowestCommonCoverage;
        }

        public static IntelCoverage GetHighestCoverage(IEnumerable<IntelCoverage> coverages) {
            IntelCoverage highestCoverage = IntelCoverage.None;
            foreach (var coverage in coverages) {
                if (coverage > highestCoverage) {
                    highestCoverage = coverage;
                }
            }
            return highestCoverage;
        }

        public static bool IsLocationContainedInUniverse(Vector3 worldLocation, UniverseSize universeSize) {
            float universeRadius = universeSize.Radius();
            float universeRadiusSqrd = universeRadius * universeRadius;
            return IsLocationContainedInUniverse(worldLocation, universeRadiusSqrd);
        }

        public static bool IsLocationContainedInUniverse(Vector3 worldLocation, float universeRadiusSqrd) {
            return universeRadiusSqrd > Vector3.SqrMagnitude(worldLocation - GameConstants.UniverseOrigin);
        }

        public static bool IsLocationContainedInUniverse(Vector3 worldLocation) {
            var gameSettings = GameReferences.GameManager.GameSettings;
            D.AssertNotNull(gameSettings);
            float universeRadius = gameSettings.UniverseSize.Radius();
            return IsLocationContainedInUniverse(worldLocation, universeRadius * universeRadius);
        }

        public static bool IsSphereCompletelyContainedInUniverse(Vector3 containedCenter, float containedRadius, float universeRadiusSqrd) {
            // isContained = containingRadius >= distanceBetweenCenters + containedRadius ->
            // isContained = containingRadius - containedRadius >= distanceBetweenCenters
            float sqrDistanceBetweenCenters = Vector3.SqrMagnitude(GameConstants.UniverseOrigin - containedCenter);
            float containedRadiusSqrd = containedRadius * containedRadius;
            return universeRadiusSqrd - containedRadiusSqrd >= sqrDistanceBetweenCenters;
        }

        /// <summary>
        /// Selects the most effective command based off of CmdEffectiveness and returns it.
        /// <remarks>IMPROVE Remove any effect the hero might have on CmdEffectivenss and best heros are selected separately.</remarks>
        /// </summary>
        /// <param name="commands">The fleet commands.</param>
        /// <returns></returns>
        public static IFleetCmd SelectMostEffectiveCommand(IEnumerable<IFleetCmd> commands) {
            D.Assert(commands.Count() > Constants.One);
            var orderedCmds = commands.OrderByDescending(cmd => cmd.CmdEffectiveness);
            return orderedCmds.First();
        }

        public static Hero SelectMostExperiencedHero(IEnumerable<Hero> heros) {
            Utility.ValidateNotNullOrEmpty(heros);
            var orderedHeros = heros.OrderByDescending(hero => hero.Experience);
            return orderedHeros.First();
        }

        /// <summary>
        /// Calculates and returns the maximum attainable speed from the provided values.
        /// <remarks>See Flight.txt</remarks>
        /// </summary>
        /// <param name="maxPropulsionPower">The maximum propulsion power.</param>
        /// <param name="mass">The mass.</param>
        /// <param name="drag">The drag.</param>
        /// <returns></returns>
        public static float CalculateMaxAttainableSpeed(float maxPropulsionPower, float mass, float drag) {
            if (drag * Time.fixedDeltaTime > 0.1F) {
                D.Warn("Values getting very high!");
            }
            return (maxPropulsionPower * ((1F / drag) - Time.fixedDeltaTime)) / mass;
        }

        /// <summary>
        /// Calculates the reqd propulsion power to reach and maintain the desired speed.
        /// <remarks>See Flight.txt</remarks>
        /// </summary>
        /// <param name="desiredSpeed">The desired speed to reach or maintain.</param>
        /// <param name="mass">The mass.</param>
        /// <param name="drag">The drag.</param>
        /// <returns></returns>
        public static float CalculateReqdPropulsionPower(float desiredSpeed, float mass, float drag) {
            float dragDeltaTimeFactor = drag * Time.fixedDeltaTime;
            if (dragDeltaTimeFactor > 0.1F) {
                D.Warn("Values getting very high!");
            }
            return (desiredSpeed * mass * drag) / (1F - dragDeltaTimeFactor);
        }

        /// <summary>
        /// Derives the enum value of type E from within the provided name. Case insensitive.
        /// </summary>
        /// <typeparam name="E"></typeparam>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static E DeriveEnumFromName<E>(string name) where E : struct {
            D.Assert(typeof(E).IsEnum, typeof(E).Name);
            Utility.ValidateForContent(name);
            return Enums<E>.GetValues().Single(e => name.Trim().ToLower().Contains(e.ToString().ToLower()));
        }

        /// <summary>
        /// Validates the provided GameDate is within the designated range, inclusive.
        /// </summary>
        /// <param name="date">The date to validate.</param>
        /// <param name="earliest">The earliest acceptable date.</param>
        /// <param name="latest">The latest acceptable date.</param>
        /// <exception cref="IllegalArgumentException"></exception>
        public static void ValidateForRange(GameDate date, GameDate earliest, GameDate latest) {
            if (latest <= earliest || date < earliest || date > latest) {
                string callingMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
                throw new ArgumentOutOfRangeException(ErrorMessages.OutOfRange.Inject(date, earliest, latest, callingMethodName));
            }
        }

        /// <summary>
        /// Changes a GameColor value into a 6 digit Hex RGB string, ignoring the alpha channel.
        /// </summary>
        /// <param name="color">The GameColor.</param>
        /// <returns></returns>
        public static string ColorToHex(GameColor color) {
            return ColorToHex(color.ToUnityColor());
        }

        /// <summary>
        /// Changes a Unity Color value into a 6 digit Hex RGB string, ignoring the alpha channel.
        /// <remarks>Note that Color32 and Color implicitly convert to each other. You may pass a Color object to this method without first casting it.</remarks>
        /// </summary>
        /// <param name="color">The Unity Color.</param>
        /// <returns></returns>
        public static string ColorToHex(Color32 color) {
            string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
            return hex;
        }

        /// <summary>
        /// Changes the first 6 digits of a hex value string into a Unity Color value.
        /// <remarks>Note that Color32 and Color implicitly convert to each other. You may pass a Color object to this method without first casting it.</remarks>
        /// </summary>
        /// <param name="hex">The hex value string.</param>
        /// <returns></returns>
        public static Color HexToColor(string hex) {
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color32(r, g, b, 255);
        }

        // 12.16.16 CalcWarningDateForRotation() moved to DebugUtility.

        #region GetClosest

        public static StationaryLocation GetClosest(Vector3 myPosition, IEnumerable<StationaryLocation> locations) {
            return locations.MinBy(loc => Vector3.SqrMagnitude(loc.Position - myPosition));
        }

        public static IFleetNavigableDestination GetClosest(Vector3 myPosition, IEnumerable<IFleetNavigableDestination> navigables) {
            return navigables.MinBy(nav => Vector3.SqrMagnitude(nav.Position - myPosition));
        }

        public static IShipNavigableDestination GetClosest(Vector3 myPosition, IEnumerable<IShipNavigableDestination> navigables) {
            return navigables.MinBy(nav => Vector3.SqrMagnitude(nav.Position - myPosition));
        }

        public static INavigableDestination GetClosest(Vector3 myPosition, IEnumerable<INavigableDestination> navigables) {
            return navigables.MinBy(nav => Vector3.SqrMagnitude(nav.Position - myPosition));
        }

        public static ISystem_Ltd GetClosest(Vector3 myPosition, IEnumerable<ISystem_Ltd> systems) {
            return systems.MinBy(sys => Vector3.SqrMagnitude(sys.Position - myPosition));
        }

        public static ISector_Ltd GetClosest(Vector3 myPosition, IEnumerable<ISector_Ltd> sectors) {
            return sectors.MinBy(sector => Vector3.SqrMagnitude(sector.Position - myPosition));
        }

        public static IShipExplorable GetClosest(Vector3 myPosition, IEnumerable<IShipExplorable> explorables) {
            return explorables.MinBy(exp => Vector3.SqrMagnitude(exp.Position - myPosition));
        }

        public static IFleetExplorable GetClosest(Vector3 myPosition, IEnumerable<IFleetExplorable> explorables) {
            return explorables.MinBy(exp => Vector3.SqrMagnitude(exp.Position - myPosition));
        }

        public static IStarbaseCmd_Ltd GetClosest(Vector3 myPosition, IEnumerable<IStarbaseCmd_Ltd> starbases) {
            return starbases.MinBy(sb => Vector3.SqrMagnitude(sb.Position - myPosition));
        }


        #endregion

        /// <summary>
        /// Checks whether the MonoBehaviour Interface provided is not null or already destroyed.
        /// This is necessary as interfaces in Unity (unlike MonoBehaviours) do not return null when slated for destruction.
        /// Returns <c>true</c> if not null and not destroyed, otherwise returns false.
        /// </summary>
        /// <typeparam name="I">The interface Type.</typeparam>
        /// <param name="i">The interface.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">If i is not a Component.</exception>
        public static bool CheckNotNullOrAlreadyDestroyed<I>(I i) where I : class {
            if (i != null) {
                if (!(i is Component)) {
                    throw new System.ArgumentException("Interface is of Type {0}, which is not a Component.".Inject(typeof(I).Name));
                }
                var c = i as Component;
                if (c != null) {
                    // i is not destroyed
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Destroys the gameObject associated with the Interface i, if i is not null or already destroyed.
        /// This is necessary as interfaces in Unity (unlike MonoBehaviours) do not return null when slated for destruction.
        /// </summary>
        /// <typeparam name="I">The Interface type.</typeparam>
        /// <param name="i">The Interface instance.</param>
        /// <param name="delayInSeconds">The delay in seconds.</param>
        /// <param name="onCompletion">Optional delegate that fires onCompletion.</param>
        /// <exception cref="System.ArgumentException">If i is not a Component.</exception>
        public static void DestroyIfNotNullOrAlreadyDestroyed<I>(I i, float delayInHours = Constants.ZeroF, Action onCompletion = null) where I : class {
            if (CheckNotNullOrAlreadyDestroyed<I>(i)) {
                Destroy((i as Component).gameObject, delayInHours, onCompletion);
            }
        }

        public static void Destroy(GameObject gameObject) {
            Destroy(gameObject, Constants.ZeroF);
        }

        /// <summary>
        /// Destroys the specified game object.
        /// <remarks>Warning: If delayInHours > Zero, a pause or gameSpeed change will effect when the gameObject is destroyed.</remarks>
        /// </summary>
        /// <param name="gameObject">The game object.</param>
        /// <param name="delayInHours">The delay in game hours.</param>
        /// <param name="onCompletion">Optional delegate that fires onCompletion.</param>
        public static void Destroy(GameObject gameObject, float delayInHours, Action onCompletion = null) {
            if (gameObject == null) {
                D.Warn("Trying to destroy a GameObject that has already been destroyed.");
                if (onCompletion != null) { onCompletion(); }
                return;
            }
            string goName = gameObject.name;
            //D.Log("Initiating destruction of {0} with delay of {1} hours.", goName, delayInHours);
            if (delayInHours == Constants.ZeroF) {
                // avoids launching a Job. Jobs don't provide call tracing in Console
                GameObject.Destroy(gameObject);
                if (onCompletion != null) { onCompletion(); }
                return;
            }
            string jobName = "{0}.Destroy".Inject(typeof(GameUtility).Name);
            GameReferences.JobManager.WaitForHours(delayInHours, jobName, waitFinished: (jobWasKilled) => {
                if (gameObject == null) {
                    D.Warn("Trying to destroy GameObject {0} that has already been destroyed.", goName);
                }
                else {
                    GameObject.Destroy(gameObject);
                }
                if (onCompletion != null) { onCompletion(); }
            });
        }

        public static void __ValidateLocationContainedInUniverse(Vector3 worldLocation, float universeRadiusSqrd) {
            D.Assert(IsLocationContainedInUniverse(worldLocation, universeRadiusSqrd));
        }

        public static void __ValidateLocationContainedInUniverse(Vector3 worldLocation) {
            var gameSettings = GameReferences.GameManager.GameSettings;
            D.AssertNotNull(gameSettings);
            float universeRadius = gameSettings.UniverseSize.Radius();
            __ValidateLocationContainedInUniverse(worldLocation, universeRadius * universeRadius);
        }
    }
}

