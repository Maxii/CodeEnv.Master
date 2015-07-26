// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SpeciesFactory.cs
// Singleton. Factory that makes SpeciesStat instances from values externally acquired via Xml.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    ///  Singleton. Factory that makes SpeciesStat instances from values externally acquired via Xml.
    /// </summary>
    public class SpeciesFactory : AXmlReader<SpeciesFactory> {

        private string _speciesTagName = "Species";
        private string _speciesAttributeTagName = "SpeciesName";
        private string _speciesPluralNameTagName = "PluralName";
        private string _speciesDescriptionTagName = "Description";
        private string _speciesImageAtlasIDTagName = "ImageAtlasID";
        private string _speciesImageFilenameTagName = "ImageFilename";
        private string _speciesSensorRangeMultiplierTagName = "SensorRangeMultiplier";
        private string _speciesWeaponRangeMultiplierTagName = "WeaponRangeMultiplier";
        private string _speciesWeaponReloadPeriodMultiplierTagName = "WeaponReloadPeriodMultiplier";

        protected override string XmlFilename { get { return "SpeciesValues"; } }

        private IDictionary<Species, SpeciesStat> _statCache;

        private SpeciesFactory() {
            Initialize();
        }

        protected override void InitializeValuesAndReferences() {
            base.InitializeValuesAndReferences();
            _statCache = new Dictionary<Species, SpeciesStat>();
        }

        public SpeciesStat MakeInstance(Species species) {
            SpeciesStat stat;
            if (!_statCache.TryGetValue(species, out stat)) {
                stat = CreateStat(species);
                _statCache.Add(species, stat);
            }
            return stat;
        }

        private SpeciesStat CreateStat(Species species) {
            SpeciesStat stat = default(SpeciesStat);
            var speciesNodes = _xElement.Elements(_speciesTagName);
            foreach (var speciesNode in speciesNodes) {
                var speciesAttribute = speciesNode.Attribute(_speciesAttributeTagName);
                string speciesName = speciesAttribute.Value;

                if (species.GetValueName() == speciesName) {
                    // found the right species node
                    string pluralName = speciesNode.Element(_speciesPluralNameTagName).Value;
                    string description = speciesNode.Element(_speciesDescriptionTagName).Value;
                    AtlasID atlasID = Enums<AtlasID>.Parse(speciesNode.Element(_speciesImageAtlasIDTagName).Value);
                    string filename = speciesNode.Element(_speciesImageFilenameTagName).Value;
                    float sensorRangeMultiplier = float.Parse(speciesNode.Element(_speciesSensorRangeMultiplierTagName).Value);
                    float weaponRangeMultiplier = float.Parse(speciesNode.Element(_speciesWeaponRangeMultiplierTagName).Value);
                    float weaponReloadPeriodMultiplier = float.Parse(speciesNode.Element(_speciesWeaponReloadPeriodMultiplierTagName).Value);
                    stat = new SpeciesStat(species, pluralName, description, atlasID, filename, sensorRangeMultiplier, weaponRangeMultiplier, weaponReloadPeriodMultiplier);
                    break;
                }
            }
            D.Assert(!stat.Equals(default(SpeciesStat)));
            return stat;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}


