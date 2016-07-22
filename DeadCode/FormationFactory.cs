// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FormationFactory.cs
// Factory that generates Formations for Unit Commands.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// Factory that generates Formations for Unit Commands.
    /// </summary>
    [Obsolete]
    public class FormationFactory : AGenericSingleton<FormationFactory> {

        private static float _oneThird = 1F / 3F;
        private static float _fourThirds = 4F / 3F;
        private const float GlobeVolumeMultiplierToMinimizeIterations = 1.6F;
        private const float GlobeRadiusIterateMultiplierToMinimizeFormationRadius = 1.1F;

        #region Singleton Initialization

        private FormationFactory() {
            Initialize();
        }

        protected override void Initialize() {
            // WARNING: Donot use Instance or _instance in here as this is still part of Constructor
        }

        #endregion

        /// <summary>
        /// Generates the specified formation for the largest possible Starbase. 
        /// Returns the world space relative positions (to HQ) of the facilities that surround the HQ facility.
        /// Does NOT include the relative position of the HQElement which by definition is Vector3.zero.
        /// </summary>
        /// <param name="formation">The formation.</param>
        /// <param name="maxFormationRadius">The resulting unit formation radius.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public IList<Vector3> GenerateMaxStarbaseFormation(Formation formation, out float maxFormationRadius) {
            switch (formation) {
                //case Formation.Circle:
                //    return GenerateMaxCircleFormation(TempGameValues.LargestFacilityObstacleZoneRadius, TempGameValues.MaxFacilitiesPerBase, out maxFormationRadius);
                case Formation.Globe:
                    return GenerateMaxGlobeFormation(TempGameValues.LargestFacilityObstacleZoneRadius, TempGameValues.MaxFacilitiesPerBase, out maxFormationRadius);
                //case Formation.Wedge:
                case Formation.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(formation));
            }
        }

        /// <summary>
        /// Generates the specified formation for the largest possible Settlement. 
        /// Returns the world space relative positions (to HQ) of the facilities that surround the HQ facility.
        /// Does NOT include the relative position of the HQElement which by definition is Vector3.zero.
        /// </summary>
        /// <param name="formation">The formation.</param>
        /// <param name="maxFormationRadius">The resulting unit formation radius.</param>
        /// <returns></returns>
        public IList<Vector3> GenerateMaxSettlementFormation(Formation formation, out float maxFormationRadius) {
            //D.Assert(formation == Formation.Circle);
            return GenerateMaxCircleFormation(TempGameValues.LargestFacilityObstacleZoneRadius, TempGameValues.MaxFacilitiesPerBase, out maxFormationRadius);
        }

        /// <summary>
        /// Generates the specified formation for the largest possible Fleet. 
        /// Returns the world space relative positions (to HQ) of the ships that surround the HQ Flagship.
        /// Does NOT include the relative position of the HQElement which by definition is Vector3.zero.
        /// </summary>
        /// <param name="formation">The formation.</param>
        /// <param name="maxFormationRadius">The resulting unit formation radius.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public IList<Vector3> GenerateMaxFleetFormation(Formation formation, out float maxFormationRadius) {
            switch (formation) {
                //case Formation.Circle:
                //    return GenerateMaxCircleFormation(TempGameValues.FleetFormationStationRadius, TempGameValues.MaxShipsPerFleet, out maxFormationRadius);
                case Formation.Globe:
                    return GenerateMaxGlobeFormation(TempGameValues.FleetFormationStationRadius, TempGameValues.MaxShipsPerFleet, out maxFormationRadius);
                //case Formation.Wedge:
                //return GenerateMaxWedgeFormation(TempGameValues.FleetFormationStationRadius, TempGameValues.MaxShipsPerFleet, out maxFormationRadius);
                case Formation.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(formation));
            }
        }

        /// <summary>
        /// Generates a globular formation for a Unit. Returns a list of the world space relative position
        /// (offset relative to the position of the HQElement) of each formation station, 
        /// not including the HQElement's formation station.
        /// </summary>
        /// <param name="formationStationRadius">The radius of the formation station.</param>
        /// <param name="maxElements">The maximum number of elements.</param>
        /// <param name="maxFormationRadius">The resulting unit formation radius.</param>
        /// <returns></returns>
        private IList<Vector3> GenerateMaxGlobeFormation(float formationStationRadius, int maxElements, out float maxFormationRadius) {
            float formationStationVolume = _fourThirds * Mathf.PI * Mathf.Pow(formationStationRadius, 3F);
            float minPossibleGlobeVolume = maxElements * formationStationVolume / 0.6F; // from marbles in a spherical bowl, aka UnitSizeAndDistance.txt
            float estWorkableGlobeVolume = minPossibleGlobeVolume * GlobeVolumeMultiplierToMinimizeIterations;
            float estGlobeRadius = Mathf.Pow(estWorkableGlobeVolume / (_fourThirds * Mathf.PI), _oneThird);

            System.DateTime startTime = System.DateTime.UtcNow;
            int iterateCount = Constants.Zero;
            IList<Vector3> stationPositions;
            bool isPositioningSuccess = TryPositionSpheresWithinGlobe(estGlobeRadius, formationStationRadius, maxElements, out stationPositions);
            while (!isPositioningSuccess) {
                D.AssertException(iterateCount++ < 10, "{0}: Unable to generate globe formation. Largest Globe radius attempted = {1:0.#}.", GetType().Name, estGlobeRadius);
                // try again with a larger radius
                estGlobeRadius *= GlobeRadiusIterateMultiplierToMinimizeFormationRadius;
                isPositioningSuccess = TryPositionSpheresWithinGlobe(estGlobeRadius, formationStationRadius, maxElements, out stationPositions);
            }
            maxFormationRadius = estGlobeRadius + formationStationRadius;
            bool isHQRemoved = stationPositions.Remove(Vector3.zero);
            D.Assert(isHQRemoved);
            D.Log("{0}: Generating a Max Globe Formation took {1} iterations over {2:0.####} secs.",
                GetType().Name, iterateCount, (System.DateTime.UtcNow - startTime).TotalSeconds);
            return new List<Vector3>(stationPositions);
        }

        private bool TryPositionSpheresWithinGlobe(float globeRadius, float sphereRadius, int sphereCount, out IList<Vector3> spherePositions) {
            var hqSphere = new StationSphere(Vector3.zero, sphereRadius);
            IList<StationSphere> obstacleSpheres = new List<StationSphere>(sphereCount) { hqSphere };
            int sphereCountToPositionAroundHQSphere = sphereCount - Constants.One;
            int iterateCount = Constants.Zero;
            for (int i = 0; i < sphereCountToPositionAroundHQSphere; i++) {
                Vector3 candidateSpherePositionRelativeToHQSphere = UnityEngine.Random.insideUnitSphere * globeRadius;
                var candidateObstacleSphere = new StationSphere(candidateSpherePositionRelativeToHQSphere, sphereRadius);
                if (obstacleSpheres.All(os => !Intersects(os, candidateObstacleSphere))) {
                    // candidate doesn't intersect with any spheres already present
                    obstacleSpheres.Add(candidateObstacleSphere);
                    iterateCount = Constants.Zero;
                }
                else {
                    i--;
                    iterateCount++;
                    if (iterateCount > 100) {
                        //D.Log("{0}.TryPositionSpheresWithinGlobe iterate count exceed max.", GetType().Name);
                        spherePositions = new List<Vector3>(Constants.Zero);
                        return false;
                    }
                }
            }
            spherePositions = obstacleSpheres.Select(os => os.Center).ToList();
            return true;
        }

        /// <summary>
        /// Generates a circular formation for a Unit in the xz plane. Returns a list of the relative position
        /// (offset relative to the position of the HQElement) of each formation station, 
        /// not including the HQElement's formation station.
        /// </summary>
        /// <param name="formationStationRadius">The radius of the formation station.</param>
        /// <param name="maxElements">The maximum number of elements.</param>
        /// <param name="maxFormationRadius">The resulting unit formation radius.</param>
        /// <returns></returns>
        private IList<Vector3> GenerateMaxCircleFormation(float formationStationRadius, int maxElements, out float maxFormationRadius) {
            float formationStationDiameter = formationStationRadius * 2F;
            int reqdNumberOfPositionsOnCircle = maxElements - Constants.One;    // excluding HQ
            float circleCircumferenceFromLineSeqments = reqdNumberOfPositionsOnCircle * formationStationDiameter;
            float estReqdCircumference = circleCircumferenceFromLineSeqments * 1.2F;    // HACK
            float estCircleRadius = estReqdCircumference / (Mathf.PI * 2F);
            //float minCircleRadius = maxObstacleZoneRadius * 2F;   // HQ and an element on circle  // IMPROVE makes no sense
            float reqdCircleRadius = estCircleRadius;   // Mathf.Max(minCircleRadius, estCircleRadius);
            maxFormationRadius = reqdCircleRadius + formationStationRadius;

            Vector3[] localPositionsOnCircle = MyMath.UniformPointsOnCircle(reqdCircleRadius, reqdNumberOfPositionsOnCircle);
            var randomlyOrderedLocalPositionsOnCircle = localPositionsOnCircle.Shuffle();
            IList<Vector3> formationStationPositionsRelativeToHQ = new List<Vector3>(randomlyOrderedLocalPositionsOnCircle);
            return formationStationPositionsRelativeToHQ;
        }

        //private IList<Vector3> GenerateMaxWedgeFormation(float formationStationRadius, int maxElements, out float maxFormationRadius) {
        //    throw new NotImplementedException("{0} not yet implemented.".Inject(Formation.Wedge.GetEnumAttributeText()));
        //}

        private bool Intersects(StationSphere sourceSphere, StationSphere otherSphere) {
            return MyMath.DoSpheresIntersect(sourceSphere.Center, sourceSphere.Radius, otherSphere.Center, otherSphere.Radius);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Nested Classes

        private class StationSphere {

            public Vector3 Center { get; set; }

            public float Radius { get; private set; }

            public StationSphere(Vector3 center, float radius) {
                Center = center;
                Radius = radius;
            }
        }

        #endregion


    }
}


