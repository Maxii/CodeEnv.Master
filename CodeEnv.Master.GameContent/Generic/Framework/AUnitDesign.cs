// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitDesign.cs
// Abstract base class holding the design of an element or command for a player.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using Common;

    /// <summary>
    /// Abstract base class holding the design of an element or command for a player.
    /// </summary>
    public abstract class AUnitDesign {

        private const string DebugNameFormat = "{0}[{1}]";

        private const string DesignNameFormat = "{0}_{1}";

        public string DebugName {
            get {
                string designNameText = DesignName.IsNullOrEmpty() ? "Not yet named" : DesignName;
                return DebugNameFormat.Inject(GetType().Name, designNameText);
            }
        }

        public Player Player { get; private set; }

        public string DesignName { get; private set; }

        private string _rootDesignName;
        public string RootDesignName {
            get { return _rootDesignName; }
            set {
                if (_rootDesignName != value) {
                    _rootDesignName = value;
                    DesignName = value;
                    _designNameCounter = Constants.Zero;
                }
            }
        }

        /// <summary>
        /// Indicates the source and status of the design.
        /// </summary>
        public SourceAndStatus Status { get; set; }

        public abstract AtlasID ImageAtlasID { get; }

        public abstract string ImageFilename { get; }

        public int TotalReqdEquipmentSlots { get; private set; }

        [System.Obsolete]
        public IEnumerable<PassiveCountermeasureStat> PassiveCmStats {
            get {
                var keys = _equipLookupBySlotID.Keys.Where(key => key.Category == EquipmentCategory.PassiveCountermeasure
                && _equipLookupBySlotID[key] != null);
                IList<PassiveCountermeasureStat> stats = new List<PassiveCountermeasureStat>();
                foreach (var key in keys) {
                    stats.Add(_equipLookupBySlotID[key] as PassiveCountermeasureStat);
                }
                return stats;
            }
        }

        /// <summary>
        /// The Equipment Categories that can be added, removed and replaced in this design.
        /// <remarks>Some equipment can be reqd in a design and are added through the constructor.</remarks>
        /// </summary>
        protected abstract EquipmentCategory[] SupportedEquipmentCategories { get; }

        protected IDictionary<EquipmentSlotID, AEquipmentStat> _equipLookupBySlotID;
        protected int _designNameCounter = Constants.Zero;

        private IEnumerator<EquipmentSlotID> _statsEnumerator1;
        private IEnumerator<EquipmentSlotID> _statsEnumerator2;

        public AUnitDesign(Player player) {
            Player = player;
            Status = SourceAndStatus.Player_Current;
            __ValidateEquipmentCategorySequence();
        }

        /// <summary>
        /// Increments DesignName by adding the next ascending int value to the RootDesignName.
        /// </summary>
        public void IncrementDesignName() {
            Utility.ValidateForContent(RootDesignName);
            _designNameCounter++;
            string newDesignName = DesignNameFormat.Inject(RootDesignName, _designNameCounter);
            //D.Log("{0}: Incrementing design name from {1} to {2}.", DebugName, DesignName, newDesignName);
            DesignName = newDesignName;
        }

        /// <summary>
        /// Initializes values and references.
        /// <remarks>Derived class constructors must call as last act of constructor.</remarks>
        /// </summary>
        protected void InitializeValuesAndReferences() {
            D.AssertNull(RootDesignName);
            _equipLookupBySlotID = new Dictionary<EquipmentSlotID, AEquipmentStat>();
            int slotNumber = Constants.One;
            foreach (var cat in SupportedEquipmentCategories) {
                int maxCatSlots = GetMaxEquipmentSlotsFor(cat);
                for (int i = 0; i < maxCatSlots; i++) {
                    EquipmentSlotID slotID = new EquipmentSlotID(slotNumber, cat);
                    _equipLookupBySlotID.Add(slotID, null);
                    slotNumber++;
                }
            }
            TotalReqdEquipmentSlots = slotNumber - 1;
        }

        /// <summary>
        /// Returns <c>true</c> if there is another AEquipmentStat that has yet to be returned.
        /// All stats will eventually be returned including null stats.
        /// <remarks>Done this way to avoid exposing _equipmentLookupBySlotID.</remarks>
        /// <remarks>Usage: while(GetEquipmentStat(out stat)) { doWorkOn(stat) }.</remarks>
        /// <remarks>Warning: Be sure to call ResetIterators() if you stop use of this method
        /// before it naturally completes by returning false.</remarks>
        /// </summary>
        /// <param name="slotID">The returned slotID.</param>
        /// <param name="equipStat">The returned equip stat.</param>
        /// <returns></returns>
        public bool GetNextEquipmentStat(out EquipmentSlotID slotID, out AEquipmentStat equipStat) {
            _statsEnumerator1 = _statsEnumerator1 ?? _equipLookupBySlotID.Keys.GetEnumerator();
            if (_statsEnumerator1.MoveNext()) {
                slotID = _statsEnumerator1.Current;
                equipStat = _equipLookupBySlotID[slotID];
                return true;
            }
            slotID = default(EquipmentSlotID);
            equipStat = null;
            _statsEnumerator1 = null;
            return false;
        }

        /// <summary>
        /// Returns <c>true</c> if there is another AEquipmentStat of the provided EquipmentCategory that has yet to be returned.
        /// All stats of the provided EquipmentCategory will eventually be returned including null stats.
        /// <remarks>Done this way to avoid exposing _equipmentLookupBySlotID.</remarks>
        /// <remarks>Usage: while(GetEquipmentStat(out stat)) { doWorkOn(stat) }.</remarks>
        /// <remarks>Warning: Be sure to call ResetIterators() if you stop use of this method
        /// before it naturally completes by returning false.</remarks>
        /// </summary>
        /// <param name="equipCat">The EquipmentCategory of the desired stats.</param>
        /// <param name="slotID">The returned slotID.</param>
        /// <param name="equipStat">The returned equip stat.</param>
        /// <returns></returns>
        public bool GetNextEquipmentStat(EquipmentCategory equipCat, out EquipmentSlotID slotID, out AEquipmentStat equipStat) {
            D.Assert(SupportedEquipmentCategories.Contains(equipCat), equipCat.GetValueName());

            _statsEnumerator2 = _statsEnumerator2 ?? _equipLookupBySlotID.Keys.GetEnumerator();
            while (_statsEnumerator2.MoveNext()) {
                slotID = _statsEnumerator2.Current;
                if (slotID.Category == equipCat) {
                    equipStat = _equipLookupBySlotID[slotID];
                    return true;
                }
            }
            slotID = default(EquipmentSlotID);
            equipStat = null;
            _statsEnumerator2 = null;
            return false;
        }

        /// <summary>
        /// Returns <c>true</c> if there is another AEquipmentStat of the provided EquipmentCategory that has yet to be returned.
        /// All stats of the provided EquipmentCategory will eventually be returned including null stats.
        /// <remarks>Done this way to avoid exposing _equipmentLookupBySlotID.</remarks>
        /// <remarks>Usage: while(GetEquipmentStat(out stat)) { doWorkOn(stat) }.</remarks>
        /// <remarks>Warning: Be sure to call ResetIterators() if you stop use of this method
        /// before it naturally completes by returning false.</remarks>
        /// </summary>
        /// <param name="equipCat">The EquipmentCategory of the desired stats.</param>
        /// <param name="equipStat">The returned equip stat.</param>
        /// <returns></returns>
        public bool GetNextEquipmentStat(EquipmentCategory equipCat, out AEquipmentStat equipStat) {
            EquipmentSlotID unusedSlotID;
            return (GetNextEquipmentStat(equipCat, out unusedSlotID, out equipStat));
        }

        public AEquipmentStat GetEquipmentStat(EquipmentSlotID slotID) {
            return _equipLookupBySlotID[slotID];
        }

        public void Add(EquipmentSlotID slotID, AEquipmentStat stat) {
            Replace(slotID, stat);
        }

        /// <summary>
        /// Replaces the stat currently associated with slotID with the provided stat both of
        /// which can be null. Returns the replaced stat.
        /// </summary>
        /// <param name="slotID">The slot identifier.</param>
        /// <param name="stat">The stat.</param>
        /// <returns></returns>
        public AEquipmentStat Replace(EquipmentSlotID slotID, AEquipmentStat stat) {
            AEquipmentStat replacedStat;
            if (!_equipLookupBySlotID.TryGetValue(slotID, out replacedStat)) {
                D.Error("{0} does not contain slotID {1}.", DebugName, slotID.DebugName);
                return null;
            }
            _equipLookupBySlotID[slotID] = stat;
            return replacedStat;
        }

        /// <summary>
        /// The iterators that make GetNextEquipment() work need to be reset whenever the client terminates the 
        /// iteration before completing it. GetNextEquipment has completed iterating when it returns false.
        /// </summary>
        public void ResetIterators() {
            _statsEnumerator1 = null;
            _statsEnumerator2 = null;
        }

        public bool TryGetEmptySlotIDFor(EquipmentCategory cat, out EquipmentSlotID slotID) {
            var availableCatSlotIDs = _equipLookupBySlotID.Keys.Where(slot => slot.Category == cat && _equipLookupBySlotID[slot] == null);
            if (availableCatSlotIDs.Any()) {
                slotID = availableCatSlotIDs.First();
                return true;
            }
            slotID = default(EquipmentSlotID);
            return false;
        }

        /// <summary>
        /// Returns the maximum number of AEquipmentStat slots that this design is allowed for the provided EquipmentCategory.
        /// <remarks>AEquipmentStats that are required for a design are not included. These are typically added via the constructor.</remarks>
        /// </summary>
        /// <param name="equipCat">The equip cat.</param>
        /// <returns></returns>
        protected abstract int GetMaxEquipmentSlotsFor(EquipmentCategory equipCat);

        public sealed override string ToString() {
            return DebugName;
        }

        #region Debug

        protected virtual void __ValidateEquipmentCategorySequence() { }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Enum specifying the source and status of the design.
        /// </summary>
        public enum SourceAndStatus {

            None,

            /// <summary>
            /// The Design originated with the player and is not obsolete.
            /// </summary>
            Player_Current,

            /// <summary>
            /// The Design originated with the player but is obsolete.
            /// </summary>
            Player_Obsolete,

            /// <summary>
            /// The Design was originated by the system as an empty template for creating designs.
            /// </summary>
            System_CreationTemplate
        }

        #endregion

        #region Value-based Equality Archive

        /// <summary>
        /// Returns a hash code for this instance.
        /// See "Page 254, C# 4.0 in a Nutshell."
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        ////public override int GetHashCode() {
        ////    unchecked { // http://dobrzanski.net/2010/09/13/csharp-gethashcode-cause-overflowexception/
        ////        int hash = 17;  // 17 = some prime number
        ////        hash = hash * 31 + Player.GetHashCode(); // 31 = another prime number
        ////        hash = hash * 31 + Category.GetHashCode();
        ////        hash = hash * 31 + TotalReqdEquipmentSlots.GetHashCode();

        ////        EquipmentSlotID slotID;
        ////        AEquipmentStat eStat;
        ////        while (GetNextEquipmentStat(out slotID, out eStat)) {
        ////            hash = hash * 31 + slotID.GetHashCode();
        ////            int eStatHash = (eStat == null) ? 0 : eStat.GetHashCode();
        ////            hash = hash * 31 + eStatHash;
        ////        }
        ////        return hash;
        ////    }
        ////}

        ////public override bool Equals(object obj) {
        ////    if (obj == null) { return false; }
        ////    if (ReferenceEquals(obj, this)) { return true; }
        ////    if (obj.GetType() != GetType()) { return false; }

        ////    AUnitDesign oDesign = (AUnitDesign)obj;
        ////    D.AssertNull(_statsEnumerator1);
        ////    D.AssertNull(_statsEnumerator2);
        ////    D.AssertNull(oDesign._statsEnumerator1);
        ////    D.AssertNull(oDesign._statsEnumerator2);

        ////    if (oDesign.Player != Player || oDesign.Category != Category || oDesign.TotalReqdEquipmentSlots != TotalReqdEquipmentSlots) {
        ////        return false;
        ////    }
        ////    EquipmentSlotID slotID;
        ////    AEquipmentStat eStat;
        ////    while (GetNextEquipmentStat(out slotID, out eStat)) {
        ////        if (oDesign.GetEquipmentStat(slotID) != eStat) {
        ////            ResetIterators();
        ////            return false;
        ////        }
        ////    }
        ////    while (oDesign.GetNextEquipmentStat(out slotID, out eStat)) {
        ////        if (GetEquipmentStat(slotID) != eStat) {
        ////            oDesign.ResetIterators();
        ////            return false;
        ////        }
        ////    }
        ////    return true;
        ////}

        ////protected void __ValidateHashCodesEqual(object obj) {
        ////    D.AssertEqual(GetHashCode(), obj.GetHashCode(), "{0} HashCode != {1} HashCode.".Inject(DebugName, obj.ToString()));
        ////}

        #endregion


    }
}

