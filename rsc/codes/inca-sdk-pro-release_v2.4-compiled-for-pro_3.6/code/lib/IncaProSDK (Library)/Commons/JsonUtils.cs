using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Namespace qui contient l'ensemble d'outils communs et génériques 
/// dans le projet INCA - ArcGIS Pro SDK
/// </summary>
namespace IGN.Inca.ArcGISPro.SDK.Commons
{
    /// <summary>
    /// Classe qui gère quelques traitements génériques lié au format JSON
    /// </summary>
    public class JsonUtils
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Méthode qui transforme un JSON array en une Liste de chaînes, si le JSON 
        /// envoyé n'est pas valide, la méthode retourne null
        /// </summary>
        /// <param name="jsonArrayParam">Une chaîne qui représente un JSON array</param>
        /// <returns>Une liste de chaînes ou null si le JSON n'est pas valide</returns>
        public static IEnumerable<string> ConvertJSONArrayToList(string jsonArrayParam)
        {
            var list = new List<string>();
            try
            {
                list = JArray.Parse(jsonArrayParam.ToString()).ToObject<List<string>>(); // parse as array  
            }
            catch (JsonReaderException)
            {
                log.Error(string.Format("The given Json is not valid : {0}", jsonArrayParam));
                list = null;
            }

            return list;
        }

        /// <summary>
        /// Méthode qui transforme un JSON object en un dictionnaire de chaînes, si le JSON 
        /// envoyé n'est pas valide, la méthode retourne null
        /// </summary>
        /// <param name="jsonObjetcParam">Une chaîne qui représente un JSON object</param>
        /// <returns>Un dictionnaire de chaînes ou null si le JSON n'est pas valide</returns>
        public static Dictionary<string, string> ConvertJSONObjectToDict(string jsonObjectParam)
        {
            var dict = new Dictionary<string, string>();

            try
            {
                dict = JObject.Parse(jsonObjectParam.ToString()).ToObject<Dictionary<string, string>>();
            }
            catch (JsonReaderException)
            {
                dict = null;
                throw new Exception(string.Format("The given Json is not valid : {0}", jsonObjectParam));             
            }

            return dict;
        }
    }
}
