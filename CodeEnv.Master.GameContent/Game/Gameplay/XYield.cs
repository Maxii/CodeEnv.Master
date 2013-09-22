// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: XYield.cs
// Data container holding XResource yields.
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

    /// <summary>
    /// Data container holding XResource yields.
    /// </summary>
    public class XYield : APropertyChangeTracking {

        public class XResourceValuePair {

            public XResource Resource { get; private set; }
            public float Value { get; internal set; }

            public XResourceValuePair(XResource xResource, float value) {
                Resource = xResource;
                Value = value;
            }
        }

        private float _special_1;
        public float Special_1 {
            get { return _special_1; }
            set {
                Arguments.ValidateNotNegative(value);
                SetProperty<float>(ref _special_1, value, "Special_1", null, OnSpecial_1Changing);
            }
        }

        private float _special_2;
        public float Special_2 {
            get { return _special_2; }
            set {
                Arguments.ValidateNotNegative(value);
                SetProperty<float>(ref _special_2, value, "Special_2", null, OnSpecial_2Changing);
            }
        }

        private float _special_3;
        public float Special_3 {
            get { return _special_3; }
            set {
                Arguments.ValidateNotNegative(value);
                SetProperty<float>(ref _special_3, value, "Special_3", null, OnSpecial_3Changing);
            }
        }

        private IDictionary<XResource, XResourceValuePair> resources = new Dictionary<XResource, XResourceValuePair>();

        public XYield(XResource xResource, float value)
            : this(new XResourceValuePair(xResource, value)) { }

        public XYield(params XResourceValuePair[] xResourcePairs) {
            AddResource(false, xResourcePairs);
        }

        private void OnSpecial_1Changing(float value) {
            ChangeValue(XResource.Special_1, value);
        }

        private void OnSpecial_2Changing(float value) {
            ChangeValue(XResource.Special_2, value);
        }

        private void OnSpecial_3Changing(float value) {
            ChangeValue(XResource.Special_3, value);
        }

        private void ChangeValue(XResource resource, float value) {
            if (value == Constants.ZeroF) {
                resources.Remove(resource);
                return;
            }
            if (resources.ContainsKey(resource)) {
                resources[resource].Value = value;
                return;
            }
            resources.Add(resource, new XResourceValuePair(resource, value));
        }

        public float GetYield(XResource xResource) {
            XResourceValuePair valuePair;
            if (resources.TryGetValue(xResource, out valuePair)) {
                return valuePair.Value;
            }
            D.Warn("{0} {1} is not present. Value of 0 returned.", typeof(XResource), xResource);
            return Constants.ZeroF;
        }

        /// <summary>
        /// Gets all XResourceValuePairs in this XYield. Can be empty.
        /// </summary>
        /// <returns></returns>
        public IList<XResourceValuePair> GetAllResources() {
            return resources.Values.ToList<XResourceValuePair>();
        }

        /// <summary>
        /// Adds the resource to the Yield.
        /// </summary>
        /// <param name="xResourcePairs">The resource pairs.</param>
        public void AddResource(params XResourceValuePair[] xResourcePairs) {
            AddResource(true, xResourcePairs);
        }

        /// <summary>
        /// Adds the resource.
        /// </summary>
        /// <param name="toBroadcast">if set to <c>true</c> the change in value will be communicated to subscribers of that change.</param>
        /// <param name="xResourcePairs">The resource pairs.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void AddResource(bool toBroadcast, params XResourceValuePair[] xResourcePairs) {
            foreach (var xPair in xResourcePairs) {
                float value = xPair.Value;
                Arguments.ValidateNotNegative(value);
                if (value == Constants.ZeroF) {
                    D.Warn("Attempting to add {0} {1} with a value of 0. Resource has not been added.", typeof(XResource), xPair.Resource);
                    continue;
                }

                XResource resource = xPair.Resource;
                if (resources.ContainsKey(resource)) {
                    value += resources[resource].Value;
                }
                switch (resource) {
                    case XResource.Special_1:
                        if (toBroadcast) {
                            Special_1 = value;
                        }
                        else {
                            _special_1 = value;
                            OnSpecial_1Changing(value);
                        }
                        break;
                    case XResource.Special_2:
                        if (toBroadcast) {
                            Special_2 = value;
                        }
                        else {
                            _special_2 = value;
                            OnSpecial_2Changing(value);
                        }
                        break;
                    case XResource.Special_3:
                        if (toBroadcast) {
                            Special_3 = value;
                        }
                        else {
                            _special_3 = value;
                            OnSpecial_3Changing(value);
                        }
                        break;
                    case XResource.None:
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(resource));
                }
            }
        }

        /// <summary>
        /// Sets the yield of the indicated resource. Setting a yield of zero removes the resource.
        /// </summary>
        /// <param name="xResource">The executable resource.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void SetYield(XResource xResource, float value) {
            switch (xResource) {
                case XResource.Special_1:
                    Special_1 = value;
                    break;
                case XResource.Special_2:
                    Special_2 = value;
                    break;
                case XResource.Special_3:
                    Special_3 = value;
                    break;
                case XResource.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(xResource));
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

