// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OpeYield.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using CodeEnv.Master.Common.LocalResources;

    public class OpeYield {

        public float Organics { get; set; }
        public float Particulates { get; set; }
        public float Energy { get; set; }

        public OpeYield() : this(0, 0, 0) { }

        public OpeYield(float o, float p, float e) {
            Organics = o;
            Particulates = p;
            Energy = e;
        }

        public float GetYield(OpeResource opeResource) {
            switch (opeResource) {
                case OpeResource.Organics:
                    return Organics;
                case OpeResource.Particulates:
                    return Particulates;
                case OpeResource.Energy:
                    return Energy;
                case OpeResource.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(opeResource));
            }
        }

        public void SetYield(OpeResource opeResource, float value) {
            switch (opeResource) {
                case OpeResource.Organics:
                    Organics = value;
                    break;
                case OpeResource.Particulates:
                    Particulates = value;
                    break;
                case OpeResource.Energy:
                    Energy = value;
                    break;
                case OpeResource.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(opeResource));
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

