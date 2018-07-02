// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitMemberDesign.cs
// Abstract base design holding the stats of an element or command for a player.
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
    /// Abstract base design holding the stats of an element or command for a player.
    /// </summary>
    public abstract class AUnitMemberDesign {

        private const string DebugNameFormat = "{0}[{1}], Player = {2}, Status = {3}, ConstructionCost = {4:0.}, DesignLevel = {5}";
        private const string DesignNameFormat = "{0}_{1}";

        /// <summary>
        /// Calculates the construction cost required to refit from existingDesign to refitDesign.
        /// <remarks>Handled this way to bypass use of the MinimumRefitCost when the refitDesign chosen 
        /// is the same as the existingDesign or the SystemCreation_Default.</remarks>
        /// </summary>
        /// <param name="refitDesign">The design to refit too.</param>
        /// <param name="existingDesign">The existing design being refitted.</param>
        /// <returns></returns>
        public static float __CalcRefitConstructionCost(AUnitMemberDesign refitDesign, AUnitMemberDesign existingDesign) {
            if (refitDesign == existingDesign) {
                D.Assert(refitDesign.Player.IsUser);    // only user could pick same design or cancel DialogWindow
                return Constants.ZeroF;
            }
            if (refitDesign.Status == SourceAndStatus.SystemCreation_Default) {
                D.Assert(refitDesign is AUnitCmdModuleDesign);
                return Constants.ZeroF; // CmdModuleDesign to refit too is the free Default design
            }

            float refitCost = refitDesign.ConstructionCost - existingDesign.ConstructionCost;
            if (refitCost < refitDesign.MinConstructionRefitCost) {
                //D.Log("{0}.RefitCost {1:0.#} < Minimum {2:0.#}. Fixing.", DebugName, refitCost, refitDesign.MinimumRefitCost);
                refitCost = refitDesign.MinConstructionRefitCost;
            }
            return refitCost;
        }

        public virtual string DebugName {
            get {
                string designNameText = DesignName.IsNullOrEmpty() ? "Not yet named" : DesignName;
                return DebugNameFormat.Inject(GetType().Name, designNameText, Player.DebugName, Status.GetEnumAttributeText(), ConstructionCost, DesignLevel);
            }
        }

        public Player Player { get; private set; }

        /// <summary>
        /// The name of this design.
        /// <remarks>Typically in the format RootDesignName_DesignLevel (Cmd_1, Barracks1_1), except when DesignLevel is 0, 
        /// in which case it would be RootDesignName. The exception keeps the DesignName the same as the RootDesignName when a user enters
        /// the name which would be what a user expects. Auto-generated RootDesignNames typically have the format Cmd# or Barracks# 
        /// which makes the creation of unique RootDesignNames reliable.</remarks>
        /// </summary>
        public string DesignName { get; private set; }

        private string _rootDesignName;
        public string RootDesignName {
            get { return _rootDesignName; }
            set {
                if (_rootDesignName != value) {
                    _rootDesignName = value;
                    DesignName = value;
                    DesignLevel = Constants.Zero;
                }
            }
        }

        private SourceAndStatus _status;
        /// <summary>
        /// Indicates the source and status of the design.
        /// </summary>
        public SourceAndStatus Status {
            get { return _status; }
            set {
                D.AssertNotDefault((int)value);
                D.Assert(_status == SourceAndStatus.None || _status == SourceAndStatus.PlayerCreation_Current);
                _status = value;
            }
        }

        public abstract AtlasID ImageAtlasID { get; }

        public abstract string ImageFilename { get; }

        public int TotalReqdOptEquipmentSlots { get; private set; }

        /// <summary>
        /// The cost in units of production to construct this design from scratch.
        /// </summary>
        public float ConstructionCost { get; private set; }

        /// <summary>
        /// The financial cost to buyout the construction of this design from scratch.
        /// </summary>
        public decimal BuyoutCost { get; private set; }

        /// <summary>
        /// The maximum number of hit points in this design. A sum of ElementHull or CmdModule
        /// integrity with contributions from each piece of equipment.
        /// </summary>
        public float HitPoints { get; private set; }

        /// <summary>
        /// A value indicating how current this design is as compared to other designs of the same Type and RootDesignName.
        /// <remarks>The higher the value the more current it is. Values that are not the most current 
        /// indicate an obsolete design.</remarks>
        /// <remarks>Primary use is to make a unique DesignName (RootDesignName + DesignLevel, e.g. Frigate3). 
        /// A unique DesignName is useful for 1) during Debug when Creators need to find a Design designated by this 
        /// unique DesignName, and 2) during Play to indicate to the user how many versions of a design the
        /// AI or User has made.</remarks>
        /// </summary>
        public int DesignLevel { get; protected set; }

        /// <summary>
        /// The HullMountCategories that are supported in this design. Only EquipmentStats that can be mounted on one of these mount 
        /// categories can be added, removed and replaced in this design.
        /// <remarks>Some equipment is reqd in a design. These require no Mount. They are added through the constructor.</remarks>
        /// </summary>
        protected abstract OptionalEquipMountCategory[] SupportedOptionalMountCategories { get; }

        /// <summary>
        /// The minimum cost in units of production required to refit a UnitMember using this Design.
        /// <remarks>The actual construction cost required to refit using this Design is determined by 
        /// this class's static __CalcRefitConstructionCost method. This value is present so the algorithm used won't assign
        /// a refit cost below this minimum. Typically used when refitting a UnitMember to an older
        /// and/or obsolete Design whose cost is significantly less than what the current Element costs.</remarks>
        /// </summary>
        private float MinConstructionRefitCost { get { return ConstructionCost * TempGameValues.MinRefitConstructionCostFactor; } }

        protected IDictionary<OptionalEquipSlotID, AEquipmentStat> _optEquipLookupBySlotID;

        private IEnumerator<OptionalEquipSlotID> _optEquipStatsEnumerator;

        public AUnitMemberDesign(Player player) {
            Player = player;
            Status = SourceAndStatus.PlayerCreation_Current;
        }

        /// <summary>
        /// Initializes values and references and populates the design with the designs maximum number of empty mount slots
        /// in anticipation of adding EquipmentStats that utilize them.
        /// <remarks>Derived class constructors must call as last act of constructor.</remarks>
        /// </summary>
        protected void InitializeValuesAndReferences() {
            D.AssertNull(RootDesignName);
            _optEquipLookupBySlotID = new Dictionary<OptionalEquipSlotID, AEquipmentStat>();
            int slotNumber = Constants.One;
            foreach (var mountCat in SupportedOptionalMountCategories) {
                int maxOptEquipSlots = GetMaxOptionalEquipSlotsFor(mountCat);
                for (int i = 0; i < maxOptEquipSlots; i++) {
                    OptionalEquipSlotID slotID = new OptionalEquipSlotID(slotNumber, mountCat);
                    _optEquipLookupBySlotID.Add(slotID, null);
                    slotNumber++;
                }
            }
            TotalReqdOptEquipmentSlots = slotNumber - 1;
        }

        /// <summary>
        /// Increments DesignLevel and Name by replacing the existing DesignLevel with a higher value and adding it to the RootDesignName.
        /// </summary>
        /// <param name="increment">The increment.</param>
        public void IncrementDesignLevelAndName(int increment = Constants.One) {
            Utility.ValidateForContent(RootDesignName);
            DesignLevel += increment;
            string newDesignName = DesignNameFormat.Inject(RootDesignName, DesignLevel);
            //D.Log("{0}: Incrementing design name from {1} to {2}.", DebugName, DesignName, newDesignName);
            DesignName = newDesignName;
        }

        /// <summary>
        /// Returns <c>true</c> if there is another AEquipmentStat that has yet to be returned.
        /// All stats will eventually be returned including null stats.
        /// <remarks>10.28.17 Only acquires EquipmentStats that require EquipmentSlots, e.g. optional equipment.</remarks>
        /// <remarks>Done this way to avoid exposing _equipmentLookupBySlotID.</remarks>
        /// <remarks>Usage: while(GetEquipmentStat(out stat)) { doWorkOn(stat) }.</remarks>
        /// <remarks>Warning: Be sure to call ResetIterators() if you stop use of this method
        /// before it naturally completes by returning false.</remarks>
        /// </summary>
        /// <param name="slotID">The returned slotID.</param>
        /// <param name="equipStat">The returned equip stat.</param>
        /// <returns></returns>
        public bool TryGetNextOptEquipStat(out OptionalEquipSlotID slotID, out AEquipmentStat equipStat) {
            _optEquipStatsEnumerator = _optEquipStatsEnumerator ?? _optEquipLookupBySlotID.Keys.GetEnumerator();
            if (_optEquipStatsEnumerator.MoveNext()) {
                slotID = _optEquipStatsEnumerator.Current;
                equipStat = _optEquipLookupBySlotID[slotID];
                return true;
            }
            slotID = default(OptionalEquipSlotID);
            equipStat = null;
            _optEquipStatsEnumerator = null;
            return false;
        }

        /// <summary>
        /// Returns the EquipmentStats associated with the provided EquipmentCategory in the form of a collection
        /// of EquipmentSlotAndStatPairs.
        /// <remarks>None of the stats returned will be null.</remarks>
        /// <remarks>10.28.17 Only acquires EquipmentStats that require EquipmentSlots, e.g. optional equipment.</remarks>
        /// </summary>
        /// <param name="equipCat">The EquipmentCategory.</param>
        /// <returns></returns>
        public IEnumerable<EquipmentSlotAndStatPair> GetEquipmentSlotsAndStatsFor(EquipmentCategory equipCat) {
            IList<EquipmentSlotAndStatPair> slotsAndStats = new List<EquipmentSlotAndStatPair>();
            foreach (var slot in _optEquipLookupBySlotID.Keys) {
                var stat = _optEquipLookupBySlotID[slot];
                if (stat != null && stat.Category == equipCat) {
                    slotsAndStats.Add(new EquipmentSlotAndStatPair(slot, stat));
                }
            }
            return slotsAndStats;
        }

        /// <summary>
        /// Returns the EquipmentStats associated with the provided EquipmentCategory, organized in a lookup table by SlotID. 
        /// <remarks>None of the stats returned will be null.</remarks>
        /// <remarks>10.28.17 Only acquires EquipmentStats that require EquipmentSlots, e.g. does not acquire HullStats or EngineStats.</remarks>
        /// </summary>
        /// <param name="equipCat">The EquipmentCategory.</param>
        /// <returns></returns>
        public IDictionary<OptionalEquipSlotID, AEquipmentStat> GetEquipmentLookupFor(EquipmentCategory equipCat) {
            var lookup = new Dictionary<OptionalEquipSlotID, AEquipmentStat>();
            foreach (var slot in _optEquipLookupBySlotID.Keys) {
                var stat = _optEquipLookupBySlotID[slot];
                if (stat != null && stat.Category == equipCat) {
                    lookup.Add(slot, stat);
                }
            }
            return lookup;
        }

        /// <summary>
        /// Returns the AEquipmentStats associated with the provided EquipmentCategory.
        /// <remarks>None of the stats returned will be null.</remarks>
        /// <remarks>4.26.18 Only acquires optional EquipmentStats (those that require EquipmentSlots). Does not acquire 
        /// EquipmentStats that are reqd, e.g. HullStats, StlEngineStats, mandatory SensorStats, etc.</remarks>
        /// </summary>
        /// <param name="equipCat">The EquipmentCategory.</param>
        /// <returns></returns>
        public IEnumerable<AEquipmentStat> GetOptEquipStatsFor(EquipmentCategory equipCat) {
            return _optEquipLookupBySlotID.Values.Where(stat => stat != null && stat.Category == equipCat);
        }

        /// <summary>
        /// Returns <c>true</c> if this slotID is present in the design, false otherwise.
        /// Even if the slotID is present returning true, the returned stat can still be null.
        /// </summary>
        /// <param name="slotID">The slot identifier.</param>
        /// <param name="stat">The resulting stat which can be null.</param>
        /// <returns></returns>
        public bool TryGetOptEquipStat(OptionalEquipSlotID slotID, out AEquipmentStat stat) {
            if (_optEquipLookupBySlotID.ContainsKey(slotID)) {
                stat = _optEquipLookupBySlotID[slotID];
                return true;
            }
            stat = null;
            return false;
        }

        /// <summary>
        /// Returns the optional AEquipmentStat for this slotID which can be null. Will throw an error
        /// if the slotID is not present in this design.
        /// </summary>
        /// <param name="slotID">The slot identifier.</param>
        /// <returns></returns>
        public AEquipmentStat GetOptEquipStat(OptionalEquipSlotID slotID) {
            if (!_optEquipLookupBySlotID.ContainsKey(slotID)) {
                D.Error("{0} does not contain {1}.", DebugName, slotID.DebugName);
                return null;
            }
            return _optEquipLookupBySlotID[slotID];
        }

        public void Add(OptionalEquipSlotID slotID, AEquipmentStat optEquipStat) {
            Replace(slotID, optEquipStat);
        }

        /// <summary>
        /// Replaces the optional AEquipmentStat currently associated with slotID with the provided stat both of
        /// which can be null. Returns the replaced stat.
        /// </summary>
        /// <param name="slotID">The slot identifier.</param>
        /// <param name="optEquipStat">The AEquipmentStat for the optional equipment.</param>
        /// <returns></returns>
        public AEquipmentStat Replace(OptionalEquipSlotID slotID, AEquipmentStat optEquipStat) {
            AEquipmentStat replacedStat;
            if (!_optEquipLookupBySlotID.TryGetValue(slotID, out replacedStat)) {
                D.Error("{0} does not contain slotID {1}.", DebugName, slotID.DebugName);
                return null;
            }
            _optEquipLookupBySlotID[slotID] = optEquipStat;
            return replacedStat;
        }

        /// <summary>
        /// The iterator that makes TryGetNextOptEquipStat() work needs to be reset whenever the client terminates the 
        /// iteration before completing it. TryGetNextOptEquipStat has completed iterating when it returns false.
        /// </summary>
        public void ResetIterator() {
            _optEquipStatsEnumerator = null;
        }

        /// <summary>
        /// Returns <c>true</c> there is a slot available for an optional piece of equipment of the designated
        /// EquipmentCategory, <c>false</c> otherwise.
        /// </summary>
        /// <param name="cat">The cat.</param>
        /// <param name="slotID">The slot identifier.</param>
        /// <returns></returns>
        public bool TryGetEmptySlotIDFor(EquipmentCategory cat, out OptionalEquipSlotID slotID) {
            OptionalEquipMountCategory[] allowedMounts = cat.AllowedMounts();
            var availableCatSlotIDs = _optEquipLookupBySlotID.Keys.Where(slot => allowedMounts.Contains(slot.SupportedMount)
                                        && _optEquipLookupBySlotID[slot] == null);
            if (availableCatSlotIDs.Any()) {
                slotID = availableCatSlotIDs.First();
                return true;
            }
            slotID = default(OptionalEquipSlotID);
            return false;
        }

        /// <summary>
        /// Calculates and assigns a value to each Property that is dependent on the equipment added to the design.
        /// <remarks>Call after all EquipmentStats have been added.</remarks>
        /// </summary>
        public void AssignPropertyValues() {
            ConstructionCost = CalcConstructionCost();
            __ValidateConstructionCost();
            HitPoints = CalcHitPoints();
            BuyoutCost = (decimal)(ConstructionCost * TempGameValues.__ProductionCostBuyoutMultiplier * Player.BuyoutCostMultiplier);
        }

        protected virtual float CalcConstructionCost() {
            float cumConstructionCost = Constants.ZeroF;
            OptionalEquipSlotID unusedSlot;
            AEquipmentStat stat;
            while (TryGetNextOptEquipStat(out unusedSlot, out stat)) {
                if (stat != null) {
                    cumConstructionCost += stat.ConstructCost;
                }
            }
            return cumConstructionCost;
        }

        protected virtual float CalcHitPoints() {
            float cumHitPts = Constants.ZeroF;
            OptionalEquipSlotID unusedSlot;
            AEquipmentStat stat;
            while (TryGetNextOptEquipStat(out unusedSlot, out stat)) {
                if (stat != null) {
                    cumHitPts += stat.HitPoints;
                }
            }
            return cumHitPts;
        }

        /// <summary>
        /// Returns the maximum number of slots for optional equipment that this design is allowed for the provided OptionalEquipMountCategory.
        /// <remarks>Equipment that is required for a design is not included as they don't require slots.</remarks>
        /// </summary>
        /// <param name="mountCat">The OptionalEquipMountCategory.</param>
        /// <returns></returns>
        protected abstract int GetMaxOptionalEquipSlotsFor(OptionalEquipMountCategory mountCat);

        /// <summary>
        /// Returns <c>true</c> if the content is equal, excluding transient values like Name, Status and the iterator.
        /// <remarks>Warning: this is not the equivalent of Equals as Type equivalence is assumed and transient values
        /// are excluded.</remarks>
        /// </summary>
        /// <param name="oDesign">The other design.</param>
        /// <returns></returns>
        public bool HasEqualContent(AUnitMemberDesign oDesign) {
            if (!GetType().IsInstanceOfType(oDesign)) {
                D.Warn("{0}.HasEqualContent should not be used to compare against {1}, a design of a different Type!", DebugName, oDesign.DebugName);
                return false;
            }
            return IsNonOptionalStatContentEqual(oDesign) && AreOptionalStatsEqual(oDesign);
        }

        /// <summary>
        /// Returns <c>true</c> if the 'fixed' content of this design is equivalent, false otherwise.
        /// <remarks>Fixed content refers to all non-transient content of the design excluding optional EquipmentStats that are
        /// assigned to slots with SlotIDs. The equivalence of EquipmentStats assigned to slots is handled by AreOptStatsEqual()
        /// which is expensive and therefore handled after simpler equivalence is already found by this method.
        /// Transient content refers to fields like Name, Status and the iterator.</remarks>
        /// </summary>
        /// <param name="oDesign">The other design.</param>
        protected virtual bool IsNonOptionalStatContentEqual(AUnitMemberDesign oDesign) {
            return oDesign.Player == Player && oDesign.ConstructionCost.ApproxEquals(ConstructionCost) && oDesign.BuyoutCost.ApproxEquals(BuyoutCost)
                && oDesign.HitPoints.ApproxEquals(oDesign.HitPoints) && oDesign.DesignLevel == DesignLevel;
        }

        private bool AreOptionalStatsEqual(AUnitMemberDesign oDesign) {
            OptionalEquipSlotID slotID;
            AEquipmentStat aStat;
            while (TryGetNextOptEquipStat(out slotID, out aStat)) {
                AEquipmentStat bStat;
                if (oDesign.TryGetOptEquipStat(slotID, out bStat)) {
                    if (aStat == bStat) {
                        continue;
                    }
                }
                ResetIterator();
                return false;
            }
            while (oDesign.TryGetNextOptEquipStat(out slotID, out aStat)) {    // OPTIMIZE avoid second pass by comparing slotID sequence
                AEquipmentStat bStat;
                if (TryGetOptEquipStat(slotID, out bStat)) {
                    if (aStat == bStat) {
                        continue;
                    }
                }
                oDesign.ResetIterator();
                return false;
            }
            return true;
        }

        public sealed override string ToString() {
            return DebugName;
        }

        #region Debug

        [System.Diagnostics.Conditional("DEBUG")]
        private void __ValidateConstructionCost() {
            if (Status != SourceAndStatus.SystemCreation_Default && ConstructionCost.ApproxEquals(Constants.ZeroF)) {
                D.Warn("{0}.AssignPropertyValues() has completed with invalid construction cost.", DebugName);
            }
        }

        #endregion

        #region Nested Classes

        public class EquipmentSlotAndStatPair {

            private const string DebugNameFormat = "{0}[{1}, {2}]";

            public string DebugName { get { return DebugNameFormat.Inject(GetType().Name, SlotID.DebugName, Stat.Category.GetValueName()); } }

            public OptionalEquipSlotID SlotID { get; private set; }

            public AEquipmentStat Stat { get; private set; }

            public EquipmentSlotAndStatPair(OptionalEquipSlotID slotID, AEquipmentStat stat) {
                SlotID = slotID;
                Stat = stat;
            }

            public override string ToString() {
                return DebugName;
            }
        }

        /// <summary>
        /// Enum specifying the source and status of the design.
        /// </summary>
        public enum SourceAndStatus {

            None,

            /// <summary>
            /// The Design was created by the player and is not obsolete.
            /// </summary>
            [EnumAttribute("Current")]
            PlayerCreation_Current,

            /// <summary>
            /// The Design was created by the player but is obsolete.
            /// </summary>
            [EnumAttribute("Obsolete")]
            PlayerCreation_Obsolete,

            /// <summary>
            /// The Design was created by the system as a basic template for creating new designs.
            /// <remarks>The design is by definition, current, as only one exists for each type of design. 
            /// They are automatically replaced rather than obsoleted when new technology is researched that allows an upgrade.</remarks>
            /// <remarks>No unit member will ever be instantiated using a design with this SourceAndStatus.</remarks>
            /// </summary>
            [EnumAttribute("Template")]
            SystemCreation_Template,

            /// <summary>
            /// The Design was created by the system as a basic default design.
            /// <remarks>The design is by definition, current, as only one exists for each type of design. 
            /// They are replaced rather than obsoleted when new technology is researched that allows an upgrade.</remarks>
            /// <remarks>5.12.18 Currently only used with AUnitCmdModuleDesigns.</remarks>
            /// </summary>
            [EnumAttribute("Default")]
            SystemCreation_Default

        }

        #endregion

        #region Value-based Equality Archive

        ////public static bool operator ==(AUnitMemberDesign left, AUnitMemberDesign right) {
        ////    // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
        ////    if (ReferenceEquals(left, right)) { return true; }
        ////    if (((object)left == null) || ((object)right == null)) { return false; }
        ////    return left.Equals(right);
        ////}

        ////public static bool operator !=(AUnitMemberDesign left, AUnitMemberDesign right) {
        ////    return !(left == right);
        ////}

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
        ////        hash = hash * 31 + DesignName.GetHashCode();
        ////        hash = hash * 31 + RootDesignName.GetHashCode();
        ////        hash = hash * 31 + Status.GetHashCode();
        ////        hash = hash * 31 + ImageAtlasID.GetHashCode();
        ////        hash = hash * 31 + ImageFilename.GetHashCode();
        ////        hash = hash * 31 + TotalReqdEquipmentSlots.GetHashCode();
        ////        hash = hash * 31 + ConstructionCost.GetHashCode();
        ////        hash = hash * 31 + RefitBenefit.GetHashCode();
        ////        hash = hash * 31 + SupportedEquipmentCategories.GetHashCode();
        ////        hash = hash * 31 + _designNameCounter.GetHashCode();

        ////        OptionalEquipSlotID slotID;
        ////        AEquipmentStat eStat;
        ////        while (TryGetNextEquipmentStat(out slotID, out eStat)) {
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

        ////    AUnitMemberDesign oDesign = (AUnitMemberDesign)obj;
        ////    return oDesign.Player == Player && oDesign.DesignName == DesignName && oDesign.RootDesignName == RootDesignName
        ////        && oDesign.Status == Status && oDesign.ImageAtlasID == ImageAtlasID && oDesign.ImageFilename == ImageFilename
        ////        && oDesign.TotalReqdEquipmentSlots == TotalReqdEquipmentSlots && oDesign.ConstructionCost == ConstructionCost
        ////        && oDesign.RefitBenefit == RefitBenefit && oDesign.SupportedEquipmentCategories == SupportedEquipmentCategories
        ////        && oDesign._designNameCounter == _designNameCounter && AreStatsEqual(oDesign);
        ////}

        #endregion


    }
}

