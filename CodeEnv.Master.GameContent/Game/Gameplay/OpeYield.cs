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

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Data container holding Organic, Particulate and Energy yields.
    /// </summary>
    public class OpeYield : APropertyChangeTracking {

        public class OpeResourceValuePair {

            public OpeResource Resource { get; private set; }
            public float Value { get; internal set; }

            public OpeResourceValuePair(OpeResource opeResource, float value) {
                Resource = opeResource;
                Value = value;
            }
        }

        private float _organics;
        public float Organics {
            get { return _organics; }
            set {
                Arguments.ValidateNotNegative(value);
                SetProperty<float>(ref _organics, value, "Organics", null, OnOrganicsChanging);
            }
        }

        private float _particulates;
        public float Particulates {
            get { return _particulates; }
            set {
                Arguments.ValidateNotNegative(value);
                SetProperty<float>(ref _particulates, value, "Particulates", null, OnParticulatesChanging);
            }
        }

        private float _energy;
        public float Energy {
            get { return _energy; }
            set {
                Arguments.ValidateNotNegative(value);
                SetProperty<float>(ref _energy, value, "Energy", null, OnEnergyChanging);
            }
        }

        private IDictionary<OpeResource, OpeResourceValuePair> resources = new Dictionary<OpeResource, OpeResourceValuePair>();

        public OpeYield() : this(0F, 0F, 0F) { }

        public OpeYield(float organics, float particulates, float energy)
            : this(new OpeResourceValuePair(OpeResource.Organics, organics), new OpeResourceValuePair(OpeResource.Particulates, particulates),
            new OpeResourceValuePair(OpeResource.Energy, energy)) { }

        private OpeYield(OpeResourceValuePair organics, OpeResourceValuePair particulates, OpeResourceValuePair energy) {
            Arguments.ValidateNotNegative(organics.Value); Arguments.ValidateNotNegative(particulates.Value); Arguments.ValidateNotNegative(energy.Value);
            resources.Add(organics.Resource, organics);
            _organics = organics.Value;
            resources.Add(particulates.Resource, particulates);
            _particulates = particulates.Value;
            resources.Add(energy.Resource, energy);
            _energy = energy.Value;
        }

        private void OnOrganicsChanging(float value) {
            resources[OpeResource.Organics].Value = value;
        }

        private void OnParticulatesChanging(float value) {
            resources[OpeResource.Particulates].Value = value;
        }

        private void OnEnergyChanging(float value) {
            resources[OpeResource.Energy].Value = value;
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

        /// <summary>
        /// Sets the yield of the indicated resource.
        /// </summary>
        /// <param name="opeResource">The ope resource.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.NotImplementedException"></exception>
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

