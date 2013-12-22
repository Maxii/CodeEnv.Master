// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CombatStrength.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    public class CombatStrength {

        // TODO inherit from APropertyChangeTracker and add SetProperty() to these properties once I'm sure these will be what I use

        public float Beam_Offense { get; set; }

        public float Beam_Defense { get; set; }

        public float Missile_Offense { get; set; }

        public float Missile_Defense { get; set; }

        public float Particle_Offense { get; set; }

        public float Particle_Defense { get; set; }

        public float Combined {
            get {
                return Missile_Defense + Missile_Offense + Particle_Defense + Particle_Offense + Beam_Defense + Beam_Offense;
            }
        }

        /// <summary>
        /// Convenience constructor that makes a basic defensive CombatStrength.
        /// </summary>
        public CombatStrength() :
            this(0f, 1f, 0f, 1f, 0f, 1f) {
        }

        public CombatStrength(params float[] values) {
            D.Assert(values.Length == 6, "CombatStrength constructor incorrect.");
            Beam_Offense = values[0];
            Beam_Defense = values[1];
            Missile_Offense = values[2];
            Missile_Defense = values[3];
            Particle_Offense = values[4];
            Particle_Defense = values[5];
        }

        public void AddToTotal(CombatStrength cs) {
            Beam_Offense += cs.Beam_Offense;
            Beam_Defense += cs.Beam_Defense;
            Missile_Offense += cs.Missile_Offense;
            Missile_Defense += cs.Missile_Defense;
            Particle_Offense += cs.Particle_Offense;
            Particle_Defense += cs.Particle_Defense;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

