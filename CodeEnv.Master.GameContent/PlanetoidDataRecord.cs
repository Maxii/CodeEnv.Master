// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetoidDataRecord.cs
// Immutable record of planetoid data for a specific level of IntelCoverage.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Immutable record of planetoid data for a specific level of IntelCoverage.
    /// </summary>
    public struct PlanetoidDataRecord {

        public IntelCoverage IntelCoverage { get; private set; }

        public string Name { get; private set; }

        public string ParentName { get; private set; }

        public Player Owner { get; private set; }

        public Topography Topography { get; private set; }      // useful? always in system...

        public Vector3 Position { get; private set; }

        public float MaxHitPoints { get; private set; }

        public float CurrentHitPoints { get; private set; }

        public float Health { get; private set; }

        public CombatStrength DefensiveStrength { get; private set; }

        public float Mass { get; private set; }

        public PlanetoidCategory Category { get; private set; }

        public int Capacity { get; private set; }

        public OpeYield Resources { get; private set; }

        public XYield SpecialResources { get; private set; }


        public PlanetoidDataRecord(IntelCoverage intelCoverage, APlanetoidData data)
            : this() {
            IntelCoverage = intelCoverage;
            RecordData(intelCoverage, data);
        }

        private void RecordData(IntelCoverage intelCoverage, APlanetoidData data) {
            switch (intelCoverage) {
                case IntelCoverage.Comprehensive:
                    CurrentHitPoints = data.CurrentHitPoints;
                    Health = data.Health;

                    goto case IntelCoverage.Moderate;
                case IntelCoverage.Moderate:
                    MaxHitPoints = data.MaxHitPoints;
                    DefensiveStrength = data.DefensiveStrength;
                    Mass = data.Mass;
                    Capacity = data.Capacity;
                    Resources = data.Resources;
                    SpecialResources = data.SpecialResources;

                    goto case IntelCoverage.Minimal;
                case IntelCoverage.Minimal:
                    Owner = new Player(data.Owner); // FIXME this is really a different player with the same attributes
                    Topography = data.Topography;
                    Position = data.Position;
                    Category = data.Category;

                    goto case IntelCoverage.Aware;
                case IntelCoverage.Aware:
                    Name = data.Name;
                    ParentName = data.ParentName;

                    goto case IntelCoverage.None;
                case IntelCoverage.None:
                    // nothing
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(intelCoverage));
            }

            //Name = data.Name;
            //ParentName = data.ParentName;
            //Owner = new Player(data.Owner);
            //Topography = data.Topography;
            //Position = data.Position;
            //Countermeasures = new List<Countermeasure>(data.Countermeasures.Count);
            //foreach (var cm in data.Countermeasures) {  // can't use ForAll()
            //    Countermeasures.Add(new Countermeasure(cm));
            //}
            //MaxHitPoints = data.MaxHitPoints;
            //CurrentHitPoints = data.CurrentHitPoints;
            //Health = data.Health;
            //DefensiveStrength = data.DefensiveStrength;
            //Mass = data.Mass;
            //Category = data.Category;
            //Capacity = data.Capacity;
            //Resources = data.Resources;
            //SpecialResources = data.SpecialResources;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

