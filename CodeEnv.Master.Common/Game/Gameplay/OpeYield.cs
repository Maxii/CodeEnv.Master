// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OpeYield.cs
// Data container holding Organic, Particulate and Energy yields.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common.LocalResources;


    /// <summary>
    /// Data container holding Organic, Particulate and Energy yields.
    /// </summary>
    public class OpeYield : APropertyChangeTracking {

        public class OpeResourceValuePair {

            public OpeResource Resource { get; private set; }
            public float Value { get; private set; }

            public OpeResourceValuePair(OpeResource opeResource, float value) {
                Resource = opeResource;
                Value = value;
            }
        }

        private IDictionary<OpeResource, OpeResourceValuePair> resources = new Dictionary<OpeResource, OpeResourceValuePair>();

        public OpeYield() : this(0F, 0F, 0F) { }

        public OpeYield(float organics, float particulates, float energy)
            : this(new OpeResourceValuePair(OpeResource.Organics, organics), new OpeResourceValuePair(OpeResource.Particulates, particulates),
            new OpeResourceValuePair(OpeResource.Energy, energy)) { }

        public OpeYield(params OpeResourceValuePair[] opeResourcePairs) {
            foreach (var opePair in opeResourcePairs) {
                resources.Add(opePair.Resource, opePair);
            }
        }

        public float GetYield(OpeResource opeResource) {
            OpeResourceValuePair valuePair;
            if (resources.TryGetValue(opeResource, out valuePair)) {
                return valuePair.Value;
            }
            D.Error("{0} {1} should be present but is not. Value of 0 returned.", typeof(OpeResource), opeResource);
            return Constants.ZeroF;
        }

        public IList<OpeResourceValuePair> GetAllResources() {
            return resources.Values.ToList<OpeResourceValuePair>();
        }

        public void ChangeYieldValue(OpeResource opeResource, float value) {
            if (!resources.Remove(opeResource)) {
                D.Error("{0} {1} should be present but is not. New Yield value was added.", typeof(OpeResource), opeResource);
            }
            resources.Add(opeResource, new OpeResourceValuePair(opeResource, value));
            // TODO raise OpeResourceValueChanged event
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

