// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AIconIDFactory.cs
//  Singleton. Abstract, generic base Factory that makes instances of AIconID, caches and reuses them.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Singleton. Abstract, generic base Factory that makes instances of AIconID, caches and reuses them. The reuse is critical as
    /// the object's equality comparer (same instance in memory) is used by the client of the factory to determine which icon is currently showing.
    /// </summary>
    /// <typeparam name="IconIDType">The type of the IconID.</typeparam>
    /// <typeparam name="DataType">The type of CommandData.</typeparam>
    /// <typeparam name="FactoryType">The type of the Factory.</typeparam>
    public abstract class AIconIDFactory<IconIDType, DataType, FactoryType> : AGenericSingleton<FactoryType>
        where IconIDType : AIconID
        where DataType : AUnitCmdItemData
        where FactoryType : class {

        private IDictionary<IconSection, IDictionary<IEnumerable<IconSelectionCriteria>, AIconID>> _idCache;

        protected override void Initialize() {
            _idCache = new Dictionary<IconSection, IDictionary<IEnumerable<IconSelectionCriteria>, AIconID>>();
        }

        public AIconID MakeInstance(DataType data) {
            IconSection section = GetIconSection();
            IconSelectionCriteria[] criteria = GetSelectionCriteria(data);
            return MakeInstance(section, data.Owner.Color, criteria);
        }

        private IconSelectionCriteria[] GetSelectionCriteria(DataType data) {
            IList<IconSelectionCriteria> criteria = new List<IconSelectionCriteria>();
            IntelCoverage humanPlayerIntelCoverage = data.GetHumanPlayerIntelCoverage();
            //D.Log("{0} found HumanPlayerIntelCoverage = {1}.", GetType().Name, humanPlayerIntelCoverage.GetName());
            switch (humanPlayerIntelCoverage) {
                case IntelCoverage.None:
                    // always returns None
                    criteria.Add(IconSelectionCriteria.None);
                    break;
                case IntelCoverage.Aware:
                    // always returns Unknown
                    criteria.Add(IconSelectionCriteria.Unknown);
                    break;
                case IntelCoverage.Minimal:
                    // always returns level 1-5
                    criteria.Add(GetCriteriaFromCategory(data));
                    break;
                case IntelCoverage.Moderate:
                case IntelCoverage.Comprehensive:
                    // always returns a comprehensive icon
                    criteria.Add(GetCriteriaFromCategory(data));
                    GetCriteriaFromComposition(data).ForAll(isc => criteria.Add(isc));
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(humanPlayerIntelCoverage));
            }
            return criteria.ToArray();
        }

        protected abstract IconSelectionCriteria GetCriteriaFromCategory(DataType data);

        protected abstract IEnumerable<IconSelectionCriteria> GetCriteriaFromComposition(DataType data);

        protected virtual IconSection GetIconSection() {
            return IconSection.Base;
        }

        /// <summary>
        /// Makes or retrieves an already made instance of IIcon based on the provided section and criteria. Clients should only
        /// use this method to make IIcon as they are compared against each other to determine equality.
        /// </summary>
        /// <param name="section">The section.</param>
        /// <param name="criteria">The criteria.</param>
        /// <returns></returns>
        private AIconID MakeInstance(IconSection section, GameColor color, params IconSelectionCriteria[] criteria) {
            AIconID iconID;
            if (!TryCheckCache(section, out iconID, criteria)) {
                iconID = (IconIDType)Activator.CreateInstance(typeof(IconIDType), section, criteria);
                RecordToCache(iconID, section, criteria);
            }
            (iconID as IconIDType).Color = color;
            return iconID;
        }

        private bool TryCheckCache(IconSection section, out AIconID iconID, params IconSelectionCriteria[] criteria) {
            IDictionary<IEnumerable<IconSelectionCriteria>, AIconID> criteriaCache;
            if (_idCache.TryGetValue(section, out criteriaCache)) {
                IList<IEnumerable<IconSelectionCriteria>> cacheCritieraList = criteriaCache.Keys.ToList();
                foreach (var criteriaSequenceInCache in cacheCritieraList) {
                    // Note: cannot use SequenceEquals as enums don't implement IComparable<T>, just IComparable
                    bool criteriaIsEqual = criteriaSequenceInCache.OrderBy(c => c).SequenceEqual(criteria.OrderBy(c => c));
                    if (criteriaIsEqual) {
                        IEnumerable<IconSelectionCriteria> criteriaKey = criteriaSequenceInCache;
                        iconID = criteriaCache[criteriaKey];
                        D.Log("A {0} has been reused from cache.", typeof(IconIDType).Name);
                        return true;
                    }
                }
            }
            iconID = null;
            return false;
        }

        private void RecordToCache(AIconID iconID, IconSection section, params IconSelectionCriteria[] criteria) {
            if (!_idCache.ContainsKey(section)) {
                _idCache.Add(section, new Dictionary<IEnumerable<IconSelectionCriteria>, AIconID>());
            }
            IDictionary<IEnumerable<IconSelectionCriteria>, AIconID> criteriaCache = _idCache[section];
            if (!criteriaCache.ContainsKey(criteria)) {
                criteriaCache.Add(criteria, iconID);
            }
        }

    }
}


