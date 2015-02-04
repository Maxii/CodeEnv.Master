// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AIconFactory.cs
//  Singleton. Abstract, generic base Factory that makes instances of IIcon, caches and reuses them.
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
    /// Singleton. Abstract, generic base Factory that makes instances of IIcon, caches and reuses them. The reuse is critical as
    /// the object's equality comparer (same instance in memory) is used by the client of the factory
    /// to determine which icon is currently showing.
    /// </summary>
    /// <typeparam name="IconType">The type of the Icon.</typeparam>
    /// <typeparam name="DataType">The type of CommandData.</typeparam>
    /// <typeparam name="FactoryType">The type of the Factory.</typeparam>
    public abstract class AIconFactory<IconType, DataType, FactoryType> : AGenericSingleton<FactoryType>
        where IconType : AIcon
        where DataType : ACommandData
        where FactoryType : class {

        private static IDictionary<IconSection, IDictionary<IEnumerable<IconSelectionCriteria>, IIcon>> _iconCache;

        protected override void Initialize() {
            _iconCache = new Dictionary<IconSection, IDictionary<IEnumerable<IconSelectionCriteria>, IIcon>>();
        }

        public IIcon MakeInstance(DataType data) {
            IconSection section = GetIconSection();
            IconSelectionCriteria[] criteria = GetSelectionCriteria(data);
            return MakeInstance(section, data.Owner.Color, criteria);
        }

        private IconSelectionCriteria[] GetSelectionCriteria(DataType data) {
            IList<IconSelectionCriteria> criteria = new List<IconSelectionCriteria>();
            switch (data.HumanPlayerIntelCoverage) {
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
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(data.HumanPlayerIntelCoverage));
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
        private IIcon MakeInstance(IconSection section, GameColor color, params IconSelectionCriteria[] criteria) {
            IIcon icon;
            if (!TryCheckCache(section, out icon, criteria)) {
                icon = (IconType)Activator.CreateInstance(typeof(IconType), section, criteria);
                RecordToCache(icon, section, criteria);
            }
            (icon as IconType).Color = color;
            return icon;
        }

        private bool TryCheckCache(IconSection section, out IIcon icon, params IconSelectionCriteria[] criteria) {
            IDictionary<IEnumerable<IconSelectionCriteria>, IIcon> criteriaCache;
            if (_iconCache.TryGetValue(section, out criteriaCache)) {
                IList<IEnumerable<IconSelectionCriteria>> cacheCritieraList = criteriaCache.Keys.ToList();
                foreach (var criteriaSequenceInCache in cacheCritieraList) {
                    // Note: cannot use SequenceEquals as enums don't implement IComparable<T>, just IComparable
                    bool criteriaIsEqual = criteriaSequenceInCache.OrderBy(c => c).SequenceEqual(criteria.OrderBy(c => c));
                    if (criteriaIsEqual) {
                        IEnumerable<IconSelectionCriteria> criteriaKey = criteriaSequenceInCache;
                        icon = criteriaCache[criteriaKey];
                        D.Log("A {0} has been reused from cache.", typeof(IconType).Name);
                        return true;
                    }
                }
            }
            icon = null;
            return false;
        }

        private void RecordToCache(IIcon icon, IconSection section, params IconSelectionCriteria[] criteria) {
            if (!_iconCache.ContainsKey(section)) {
                _iconCache.Add(section, new Dictionary<IEnumerable<IconSelectionCriteria>, IIcon>());
            }
            IDictionary<IEnumerable<IconSelectionCriteria>, IIcon> criteriaCache = _iconCache[section];
            if (!criteriaCache.ContainsKey(criteria)) {
                criteriaCache.Add(criteria, icon);
            }
        }

    }
}


