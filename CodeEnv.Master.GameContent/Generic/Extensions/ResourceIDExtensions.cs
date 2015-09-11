// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ResourceIDExtensions.cs
// Extension methods for ResourceID values.
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
    /// Extension methods for ResourceID values.
    /// </summary>
    public static class ResourceIDExtensions {

        private static ResourceIDXmlPropertyReader _xmlReader = ResourceIDXmlPropertyReader.Instance;

        public static string GetImageFilename(this ResourceID resourceID) {
            switch (resourceID) {
                case ResourceID.Organics:
                    return _xmlReader.OrganicsImageFilename;
                case ResourceID.Particulates:
                    return _xmlReader.ParticulatesImageFilename;
                case ResourceID.Energy:
                    return _xmlReader.EnergyImageFilename;
                case ResourceID.Titanium:
                    return _xmlReader.TitaniumImageFilename;
                case ResourceID.Duranium:
                    return _xmlReader.DuraniumImageFilename;
                case ResourceID.Unobtanium:
                    return _xmlReader.UnobtaniumImageFilename;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(resourceID));
            }
        }

        public static AtlasID GetImageAtlasID(this ResourceID resourceID) {
            switch (resourceID) {
                case ResourceID.Organics:
                    return Enums<AtlasID>.Parse(_xmlReader.OrganicsImageAtlasID);
                case ResourceID.Particulates:
                    return Enums<AtlasID>.Parse(_xmlReader.ParticulatesImageAtlasID);
                case ResourceID.Energy:
                    return Enums<AtlasID>.Parse(_xmlReader.EnergyImageAtlasID);
                case ResourceID.Titanium:
                    return Enums<AtlasID>.Parse(_xmlReader.TitaniumImageAtlasID);
                case ResourceID.Duranium:
                    return Enums<AtlasID>.Parse(_xmlReader.DuraniumImageAtlasID);
                case ResourceID.Unobtanium:
                    return Enums<AtlasID>.Parse(_xmlReader.UnobtaniumImageAtlasID);
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(resourceID));
            }
        }

        public static string GetIconFilename(this ResourceID resourceID) {
            switch (resourceID) {
                case ResourceID.Organics:
                    return _xmlReader.OrganicsIconFilename;
                case ResourceID.Particulates:
                    return _xmlReader.ParticulatesIconFilename;
                case ResourceID.Energy:
                    return _xmlReader.EnergyIconFilename;
                case ResourceID.Titanium:
                    return _xmlReader.TitaniumIconFilename;
                case ResourceID.Duranium:
                    return _xmlReader.DuraniumIconFilename;
                case ResourceID.Unobtanium:
                    return _xmlReader.UnobtaniumIconFilename;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(resourceID));
            }
        }

        public static AtlasID GetIconAtlasID(this ResourceID resourceID) {
            switch (resourceID) {
                case ResourceID.Organics:
                    return Enums<AtlasID>.Parse(_xmlReader.OrganicsIconAtlasID);
                case ResourceID.Particulates:
                    return Enums<AtlasID>.Parse(_xmlReader.ParticulatesIconAtlasID);
                case ResourceID.Energy:
                    return Enums<AtlasID>.Parse(_xmlReader.EnergyIconAtlasID);
                case ResourceID.Titanium:
                    return Enums<AtlasID>.Parse(_xmlReader.TitaniumIconAtlasID);
                case ResourceID.Duranium:
                    return Enums<AtlasID>.Parse(_xmlReader.DuraniumIconAtlasID);
                case ResourceID.Unobtanium:
                    return Enums<AtlasID>.Parse(_xmlReader.UnobtaniumIconAtlasID);
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(resourceID));
            }
        }

        public static string GetResourceDescription(this ResourceID resourceID) {
            switch (resourceID) {
                case ResourceID.Organics:
                    return _xmlReader.OrganicsDescription;
                case ResourceID.Particulates:
                    return _xmlReader.ParticulatesDescription;
                case ResourceID.Energy:
                    return _xmlReader.EnergyDescription;
                case ResourceID.Titanium:
                    return _xmlReader.TitaniumDescription;
                case ResourceID.Duranium:
                    return _xmlReader.DuraniumDescription;
                case ResourceID.Unobtanium:
                    return _xmlReader.UnobtaniumDescription;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(resourceID));
            }
        }

        public static ResourceCategory GetResourceCategory(this ResourceID resourceID) {
            // currently don't see a reason to externalize this
            switch (resourceID) {
                case ResourceID.Organics:
                case ResourceID.Particulates:
                case ResourceID.Energy:
                    return ResourceCategory.Common;
                case ResourceID.Titanium:
                case ResourceID.Duranium:
                case ResourceID.Unobtanium:
                    return ResourceCategory.Strategic;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(resourceID));
            }
        }

        #region Nested Classes

        /// <summary>
        /// Parses ResourceID.xml used to provide externalized values for the ResourceID enum.
        /// </summary>
        private sealed class ResourceIDXmlPropertyReader : AEnumXmlPropertyReader<ResourceIDXmlPropertyReader> {

            #region Organics

            private string _organicsImageFilename;
            public string OrganicsImageFilename {
                get {
                    CheckValuesInitialized();
                    return _organicsImageFilename;
                }
                private set { _organicsImageFilename = value; }
            }

            private string _organicsImageAtlasID;
            public string OrganicsImageAtlasID {
                get {
                    CheckValuesInitialized();
                    return _organicsImageAtlasID;
                }
                private set { _organicsImageAtlasID = value; }
            }

            private string _organicsIconFilename;
            public string OrganicsIconFilename {
                get {
                    CheckValuesInitialized();
                    return _organicsIconFilename;
                }
                private set { _organicsIconFilename = value; }
            }

            private string _organicsIconAtlasID;
            public string OrganicsIconAtlasID {
                get {
                    CheckValuesInitialized();
                    return _organicsIconAtlasID;
                }
                private set { _organicsIconAtlasID = value; }
            }

            private string _organicsDescription;
            public string OrganicsDescription {
                get {
                    CheckValuesInitialized();
                    return _organicsDescription;
                }
                private set { _organicsDescription = value; }
            }

            #endregion

            #region Particulates

            private string _particulatesImageFilename;
            public string ParticulatesImageFilename {
                get {
                    CheckValuesInitialized();
                    return _particulatesImageFilename;
                }
                private set { _particulatesImageFilename = value; }
            }

            private string _particulatesImageAtlasID;
            public string ParticulatesImageAtlasID {
                get {
                    CheckValuesInitialized();
                    return _particulatesImageAtlasID;
                }
                private set { _particulatesImageAtlasID = value; }
            }

            private string _particulatesIconFilename;
            public string ParticulatesIconFilename {
                get {
                    CheckValuesInitialized();
                    return _particulatesIconFilename;
                }
                private set { _particulatesIconFilename = value; }
            }

            private string _particulatesIconAtlasID;
            public string ParticulatesIconAtlasID {
                get {
                    CheckValuesInitialized();
                    return _particulatesIconAtlasID;
                }
                private set { _particulatesIconAtlasID = value; }
            }

            private string _particulatesDescription;
            public string ParticulatesDescription {
                get {
                    CheckValuesInitialized();
                    return _particulatesDescription;
                }
                private set { _particulatesDescription = value; }
            }

            #endregion

            #region Energy

            private string _energyImageFilename;
            public string EnergyImageFilename {
                get {
                    CheckValuesInitialized();
                    return _energyImageFilename;
                }
                private set { _energyImageFilename = value; }
            }

            private string _energyImageAtlasID;
            public string EnergyImageAtlasID {
                get {
                    CheckValuesInitialized();
                    return _energyImageAtlasID;
                }
                private set { _energyImageAtlasID = value; }
            }

            private string _energyIconFilename;
            public string EnergyIconFilename {
                get {
                    CheckValuesInitialized();
                    return _energyIconFilename;
                }
                private set { _energyIconFilename = value; }
            }

            private string _energyIconAtlasID;
            public string EnergyIconAtlasID {
                get {
                    CheckValuesInitialized();
                    return _energyIconAtlasID;
                }
                private set { _energyIconAtlasID = value; }
            }

            private string _energyDescription;
            public string EnergyDescription {
                get {
                    CheckValuesInitialized();
                    return _energyDescription;
                }
                private set { _energyDescription = value; }
            }

            #endregion

            #region Titanium

            private string _titaniumImageFilename;
            public string TitaniumImageFilename {
                get {
                    CheckValuesInitialized();
                    return _titaniumImageFilename;
                }
                private set { _titaniumImageFilename = value; }
            }

            private string _titaniumImageAtlasID;
            public string TitaniumImageAtlasID {
                get {
                    CheckValuesInitialized();
                    return _titaniumImageAtlasID;
                }
                private set { _titaniumImageAtlasID = value; }
            }

            private string _titaniumIconFilename;
            public string TitaniumIconFilename {
                get {
                    CheckValuesInitialized();
                    return _titaniumIconFilename;
                }
                private set { _titaniumIconFilename = value; }
            }

            private string _titaniumIconAtlasID;
            public string TitaniumIconAtlasID {
                get {
                    CheckValuesInitialized();
                    return _titaniumIconAtlasID;
                }
                private set { _titaniumIconAtlasID = value; }
            }

            private string _titaniumDescription;
            public string TitaniumDescription {
                get {
                    CheckValuesInitialized();
                    return _titaniumDescription;
                }
                private set { _titaniumDescription = value; }
            }

            #endregion

            #region Duranium

            private string _duraniumImageFilename;
            public string DuraniumImageFilename {
                get {
                    CheckValuesInitialized();
                    return _duraniumImageFilename;
                }
                private set { _duraniumImageFilename = value; }
            }

            private string _duraniumImageAtlasID;
            public string DuraniumImageAtlasID {
                get {
                    CheckValuesInitialized();
                    return _duraniumImageAtlasID;
                }
                private set { _duraniumImageAtlasID = value; }
            }

            private string _duraniumIconFilename;
            public string DuraniumIconFilename {
                get {
                    CheckValuesInitialized();
                    return _duraniumIconFilename;
                }
                private set { _duraniumIconFilename = value; }
            }

            private string _duraniumIconAtlasID;
            public string DuraniumIconAtlasID {
                get {
                    CheckValuesInitialized();
                    return _duraniumIconAtlasID;
                }
                private set { _duraniumIconAtlasID = value; }
            }

            private string _duraniumDescription;
            public string DuraniumDescription {
                get {
                    CheckValuesInitialized();
                    return _duraniumDescription;
                }
                private set { _duraniumDescription = value; }
            }

            #endregion

            #region Unobtanium

            private string _unobtaniumImageFilename;
            public string UnobtaniumImageFilename {
                get {
                    CheckValuesInitialized();
                    return _unobtaniumImageFilename;
                }
                private set { _unobtaniumImageFilename = value; }
            }

            private string _unobtaniumImageAtlasID;
            public string UnobtaniumImageAtlasID {
                get {
                    CheckValuesInitialized();
                    return _unobtaniumImageAtlasID;
                }
                private set { _unobtaniumImageAtlasID = value; }
            }

            private string _unobtaniumIconFilename;
            public string UnobtaniumIconFilename {
                get {
                    CheckValuesInitialized();
                    return _unobtaniumIconFilename;
                }
                private set { _unobtaniumIconFilename = value; }
            }

            private string _unobtaniumIconAtlasID;
            public string UnobtaniumIconAtlasID {
                get {
                    CheckValuesInitialized();
                    return _unobtaniumIconAtlasID;
                }
                private set { _unobtaniumIconAtlasID = value; }
            }

            private string _unobtaniumDescription;
            public string UnobtaniumDescription {
                get {
                    CheckValuesInitialized();
                    return _unobtaniumDescription;
                }
                private set { _unobtaniumDescription = value; }
            }

            #endregion

            /// <summary>
            /// The type of the enum being supported by this class.
            /// </summary>
            protected override Type EnumType { get { return typeof(ResourceID); } }

            private ResourceIDXmlPropertyReader() {
                Initialize();
            }

            public override string ToString() {
                return new ObjectAnalyzer().ToString(this);
            }

        }

        #endregion
    }
}

