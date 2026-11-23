using System.Globalization;
using System.Resources;

namespace IncaEditing
{
    /// <summary>
    /// Class in charge of managing the culture resources depending on the current UI culture ("CurrentUICulture")
    /// </summary>
    public class CultureResourcesManager
    {
        /// <summary>
        /// Culture dictionary 
        /// </summary>
        public ResourceSet dictLanguage;

        /// <summary>
        /// Init dictionary in the constructor
        /// </summary>
        private CultureResourcesManager() { LoadResourcesDict(); }

        private static CultureResourcesManager _instance;

        public static CultureResourcesManager GetInstance()
        {
            if (_instance == null)
            {
                _instance = new CultureResourcesManager();
            }
            return _instance;
        }

        /// <summary>
        /// Method in charge of initializing the dictionary with the current UI culture
        /// </summary>
        private void LoadResourcesDict()
        {
            CultureInfo culture = CultureInfo.CurrentUICulture;

            if (culture.TwoLetterISOLanguageName == "fr")
            {
                dictLanguage = DictionaryFR.ResourceManager.GetResourceSet(culture, true, true);
            }
            else if (culture.TwoLetterISOLanguageName == "es")
            {
                dictLanguage = DictionaryES.ResourceManager.GetResourceSet(culture, true, true);
            }
            else
            {
                // English ("en") by default 
                dictLanguage = DictionaryEN.ResourceManager.GetResourceSet(culture, true, true);
            }
        }
    }
}
