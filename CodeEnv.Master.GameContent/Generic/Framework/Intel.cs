// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Intel.cs
// Metadata describing the intelligence data known about a particular object.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Metadata describing the intelligence data known about a particular object.
    /// </summary>
    public class Intel : APropertyChangeTracking, IEquatable<Intel> {

        private IntelScope _scope;
        /// <summary>
        /// The comprehensiveness of our data on this object.
        /// </summary>
        public IntelScope Scope {
            get { return _scope; }
            private set { SetProperty<IntelScope>(ref _scope, value, "Scope"); }
        }

        private IntelSource _source;
        /// <summary>
        /// The current realtime source of Intel for this object.
        /// </summary>
        public IntelSource Source {
            get { return _source; }
            set { SetProperty<IntelSource>(ref _source, value, "Source", null, OnSourceChanging); }
        }


        private GameDate _dateStamp;
        /// <summary>
        /// The date of the last update to this intelligence brief. Typically
        /// set when Freshness changes from Realtime.
        /// </summary>
        public GameDate DateStamp {
            get { return _dateStamp; }
            private set { SetProperty<GameDate>(ref _dateStamp, value, "DateStamp"); }
        }

        public Intel(IntelScope scope, IntelSource source) {
            _scope = scope;
            _source = source;
            ProcessChange(source);
        }

        private void OnSourceChanging(IntelSource newSource) {
            ProcessChange(newSource);
        }

        /// <summary>
        /// Sets the DateStamp if called for and fixes any illegal conditions created by the change.
        /// </summary>
        /// <param name="newSource">The new source.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void ProcessChange(IntelSource newSource) {
            _dateStamp = null;
            switch (newSource) {
                case IntelSource.None:
                    if (Scope != IntelScope.None) {
                        DateStamp = new GameDate(GameDate.PresetDateSelector.Current);
                    }
                    break;
                case IntelSource.LongRangeSensors:
                    if (Scope < IntelScope.Aware) {
                        WarnIllegal(Scope, newSource);
                        Scope = IntelScope.Aware;
                    }
                    break;
                case IntelSource.MediumRangeSensors:
                    if (Scope < IntelScope.Minimal) {
                        WarnIllegal(Scope, newSource);
                        Scope = IntelScope.Minimal;
                    }
                    break;
                case IntelSource.ShortRangeSensors:
                    if (Scope < IntelScope.Moderate) {
                        WarnIllegal(Scope, newSource);
                        Scope = IntelScope.Moderate;
                    }
                    break;
                case IntelSource.InfoNet:
                    if (Scope < IntelScope.Comprehensive) {
                        WarnIllegal(Scope, newSource);
                        Scope = IntelScope.Comprehensive;
                    }
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(newSource));
            }
        }

        private void WarnIllegal(IntelScope scope, IntelSource source) {
            D.Warn("Illegal {0} state: Scope: {1}, Source: {2}.", typeof(Intel).Name, scope, source);
        }

        // Override object.Equals on reference types when you do not want your
        // reference type to obey reference semantics, as defined by System.Object.
        // Always override ValueType.Equals for your own Value Types.
        public override bool Equals(object right) {
            // TODO the containing class T must extend IEquatable<T>
            //       
            // See the full list of guidelines at
            //   http://go.microsoft.com/fwlink/?LinkID=85237  
            // and also the guidance for operator== at
            //   http://go.microsoft.com/fwlink/?LinkId=85238 aka
            // "Rarely override the operator==() when you create reference types as
            // the .NET Framework classes expect it to follow reference semantics for
            // all reference types. Always override the == operator for your own
            // Value Types. See Effective C#, Item 6.

            // No need to check 'this' for null as the CLR throws an exception before
            // calling any instance method through a null reference.
            if (object.ReferenceEquals(right, null)) {
                return false;
            }

            if (object.ReferenceEquals(this, right)) {
                return true;
            }

            if (this.GetType() != right.GetType()) {
                return false;
            }

            // now call IEquatable's Equals
            return this.Equals(right as Intel);
        }

        // Generally, do not override object.GetHashCode as object's version is reliable
        // although not efficient. You should override it IFF operator==() is redefined which
        // is rare. 
        // You should always override ValueType.GetHashCode and redefine ==() for your
        // value types. If the value type is used as a hash key, it must be immutable.
        // See Effective C# Item 7.
        public override int GetHashCode() {
            // TODO: write your implementation of GetHashCode() here
            return base.GetHashCode();
        }

        public override string ToString() {
            return string.Format("Intel Metadata [Scope: {0}, Source: {1}, DateStamp: {2} ].", Scope.GetName(), Source.GetName(), DateStamp.ToString());
        }

        #region IEquatable<Intel> Members

        public bool Equals(Intel other) {
            // TODO add your equality test here. Call the base class Equals only if the
            // base class version is not provided by System.Object or System.ValueType
            // as all that occurs is either a check for reference equality or content equality.
            return Scope == other.Scope && Source == other.Source && DateStamp == other.DateStamp;
        }

        #endregion

    }
}

