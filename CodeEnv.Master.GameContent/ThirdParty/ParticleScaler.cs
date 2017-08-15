// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ParticleScaler.cs
// Run-time particle system scaler.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Run-time particle system scaler.
    /// <see cref="http://forum.unity3d.com/threads/scaling-particle-systems-at-runtime.382345/"/>
    /// </summary>
    public static class ParticleScaler {

        public static ParticleScalerOptions defaultOptions = new ParticleScalerOptions();

        public static void ScaleByTransform(ParticleSystem particles, float scale, bool includeChildren = true) {
            var mainModule = particles.main;  // particles.scalingMode = ParticleSystemScalingMode.Local; Deprecated in Unity 5.5

            mainModule.scalingMode = ParticleSystemScalingMode.Local;
            particles.transform.localScale = particles.transform.localScale * scale;
            mainModule.gravityModifierMultiplier *= scale;    //particles.gravityModifier *= scale; Deprecated in Unity 5.5
            if (includeChildren) {
                var children = particles.GetComponentsInChildren<ParticleSystem>(includeInactive: true);    // my includeInactive addition
                for (var i = children.Length; i-- > 0;) {
                    if (children[i] == particles) {
                        continue;
                    }
                    var childMainModule = children[i].main;
                    childMainModule.scalingMode = ParticleSystemScalingMode.Local;   //children[i].scalingMode = ParticleSystemScalingMode.Local; Deprecated in Unity 5.5
                    children[i].transform.localScale = children[i].transform.localScale * scale;
                    childMainModule.gravityModifierMultiplier *= scale; // children[i].gravityModifier *= scale; Deprecated in Unity 5.5
                }
            }
        }

        public static void Scale(ParticleSystem particles, float scale, bool includeChildren = true, ParticleScalerOptions options = null) {
            ScaleSystem(particles, scale, false, options);
            if (includeChildren) {
                var children = particles.GetComponentsInChildren<ParticleSystem>(includeInactive: true);   // my includeInactive addition
                for (var i = children.Length; i-- > 0;) {
                    if (children[i] == particles) { continue; }
                    ScaleSystem(children[i], scale, true, options);
                }
            }
        }

        private static void ScaleSystem(ParticleSystem particles, float scale, bool scalePosition, ParticleScalerOptions options = null) {
            if (options == null) {
                options = defaultOptions;
            }
            if (scalePosition) {
                particles.transform.localPosition *= scale;
            }

            var mainModule = particles.main;
            mainModule.startSizeMultiplier *= scale;    //particles.startSize *= scale; Deprecated in Unity 5.5
            mainModule.gravityModifierMultiplier *= scale;  //particles.gravityModifier *= scale;   Deprecated in Unity 5.5
            mainModule.startSpeedMultiplier *= scale;   //particles.startSpeed *= scale;    Deprecated in Unity 5.5

            if (options.shape) {
                var shape = particles.shape;
                shape.radius *= scale;
                shape.scale *= scale;   //shape.box = shape.box * scale;    Deprecated in Unity 2017.1
            }

            // Currently disabled due to a bug in Unity 5.3.4. 
            // If any of these fields are using "Curves", the editor will shut down when they are modified.
            // If you're not using any curves, feel free to uncomment the following lines;
            // ************************************************************************************************
            if (options.velocity) {
                var vel = particles.velocityOverLifetime;
                vel.x = ScaleMinMaxCurve(vel.x, scale);
                vel.y = ScaleMinMaxCurve(vel.y, scale);
                vel.z = ScaleMinMaxCurve(vel.z, scale);
            }
            if (options.clampVelocity) {
                var clampVel = particles.limitVelocityOverLifetime;
                clampVel.limitX = ScaleMinMaxCurve(clampVel.limitX, scale);
                clampVel.limitY = ScaleMinMaxCurve(clampVel.limitY, scale);
                clampVel.limitZ = ScaleMinMaxCurve(clampVel.limitZ, scale);
            }
            if (options.force) {
                var force = particles.forceOverLifetime;
                force.x = ScaleMinMaxCurve(force.x, scale);
                force.y = ScaleMinMaxCurve(force.y, scale);
                force.z = ScaleMinMaxCurve(force.z, scale);
            }
        }
        // *****************************************************************************************************

        private static ParticleSystem.MinMaxCurve ScaleMinMaxCurve(ParticleSystem.MinMaxCurve curve, float scale) {
            curve.curveMultiplier *= scale; //curve.curveScalar *= scale; Deprecated in Unity 5.5
            curve.constantMin *= scale;
            curve.constantMax *= scale;
            ScaleCurve(curve.curveMin, scale);
            ScaleCurve(curve.curveMax, scale);
            return curve;
        }

        private static void ScaleCurve(AnimationCurve curve, float scale) {
            if (curve == null) { return; }
            for (int i = 0; i < curve.keys.Length; i++) { curve.keys[i].value *= scale; }
        }
    }

    public class ParticleScalerOptions {
        public bool shape = true;
        public bool velocity = true;
        public bool clampVelocity = true;
        public bool force = true;
    }
}

