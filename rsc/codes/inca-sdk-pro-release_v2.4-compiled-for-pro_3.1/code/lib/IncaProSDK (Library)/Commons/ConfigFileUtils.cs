using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Mapping;
using IGN.Inca.ArcGISPro.SDK.DAL.Enum;
using System;
using System.Collections;
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
    /// Classe qui facilite l'interpretation des paramètres définis dans un fichier de 
    /// config (Excel) à travers d'un dictionnaire
    /// </summary>
    public class ConfigFileUtils
    {
        public Dictionary<string, string> MainDictionary { get; set; }
        public ConfigFileUtils(Dictionary<string, string> dict)
        {
            MainDictionary = dict;
        }

        /// <summary>
        /// Gets the string value from a given key parameter name
        /// </summary>
        /// <param name="key">the key parameter name</param>
        /// <returns>the string value, null if value not defined</returns>
        public string GetArgumentValue(string key)
        {
            if (this.MainDictionary.ContainsKey(key))
                return this.MainDictionary[key];
            else
                throw new ArgumentNullException(key, string.Format("Key (field): '{0}' doesn't exist.", key));
        }

        /// <summary>
        /// Converts the value from key to a specified type (bool, short, int, double)
        /// </summary>
        /// <param name="key">the key parameter name</param>
        /// <param name="type">the type of value we need to convert</param>
        /// <returns>the converted value, null if type wasn't implemented yet</returns>
        public object GetConvertedArgument(string key, ConvertingUtilTypes type)
        {
            var value = GetArgumentValue(key);

            try
            {
                if (value != null)
                {
                    if (type == ConvertingUtilTypes.BOOL)
                        return bool.Parse(value);
                    else if (type == ConvertingUtilTypes.SHORT)
                        return short.Parse(value);
                    else if (type == ConvertingUtilTypes.INT)
                        return int.Parse(value);
                    else if (type == ConvertingUtilTypes.DOUBLE)
                        return double.Parse(value);
                    else if (type == ConvertingUtilTypes.DICTIONARY)
                        return JsonUtils.ConvertJSONObjectToDict(value);                      
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new FormatException(string.Format("Problem converting : {0} field was expecting a {1}, got this value '{2}'",
                    key, EnumUtils.StringValueOf(type), value), ex);
                //throw new FormatException(ex.Message);
            }
        }

        /// <summary>
        /// Converts the value from key to a specified Enum 
        /// </summary>
        /// <param name="key">the key parameter name</param>
        /// <param name="enumerator">the enum we want to convert the value into</param>
        /// <returns>the converted enum value; null if value is not defined</returns>
        public object GetConvertedEnumArgument(string key, Enum enumerator)
        {
            var value = GetArgumentValue(key);

            try
            {
                return value != null ? System.Enum.Parse(enumerator.GetType(), value) : null;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(string.Format("The field '{0}' is an enum and the given value '{1}' doesn't exist in it (Enum type : '{2}')",
                    key, value, enumerator.GetType().FullName), ex);
            }
        }

        /// <summary>
        /// Converts the value from key to a specified Color found in the given style file 
        /// </summary>
        /// <param name="key">the key parameter name</param>
        /// <param name="stylx">the style file where we'll look for the specified color</param>
        /// <returns>the converted color; null if value is not defined</returns>
        public CIMColor GetConvertedColor(string key, StyleProjectItem stylx)
        {
            // getting the color name
            var colorName = GetArgumentValue(key);

            if (colorName == null)
                return null;

            // searching the color in the given stylx file
            var colorsFound = stylx.SearchColors(colorName);

            if (colorsFound.Count > 0)
                return colorsFound.FirstOrDefault().Color;
            else
                throw new Exception(string.Format("The color '{0}' was not found in stylx file {1}",
                   colorName, stylx.Path));
        }

        /// <summary>
        /// Searches by name a TextSymbol or MaplexLabelPlacement in a given style file and returns it if found
        /// </summary>
        /// <param name="key">the key parameter name</param>
        /// <param name="stylx">the style file where we'll look for the specified style item</param>
        /// <param name="type">the style item type we're looking for, only TextSymbol and MaplexLabelPlacement are implemented</param>
        /// <returns>the instance of the style item; null if value not defined or style type not implemented</returns>
        public CIMObject GetStyleItemFromGallery(string key, StyleProjectItem stylx, StyleItemType type)
        {
            // getting the text symbol name
            var styleItemName = GetArgumentValue(key);

            if (styleItemName == null)
                return null;

            try
            {
                if (type == StyleItemType.TextSymbol)
                    // searching the requested text symbol
                    return stylx.SearchSymbols(StyleItemType.TextSymbol, styleItemName).FirstOrDefault().Symbol.MakeSymbolReference();
                else if (type == StyleItemType.MaplexLabelPlacement)
                    // searching the requested label placement
                    return stylx.SearchLabelPlacements(StyleItemType.MaplexLabelPlacement, styleItemName).FirstOrDefault().LabelPlacement;
                else
                    // other cases not treated
                    return null;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("The style item '{0}' (type={1}) was not found in stylx file {2}",
                   styleItemName, type, stylx.Path), ex);
            }
        }

        /// <summary>
        /// Searches a layer by the name in the current active map
        /// </summary>
        /// <param name="key">the key parameter name</param>
        /// <returns>returns the layer if found in map; throws exception if not found</returns>
        public FeatureLayer GetConvertedFeatureLayer(string key)
        {
            // getting the layer name
            var layerName = GetArgumentValue(key);

            try
            {
                // searching the given feature layer in current aprx project
                var lyr = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault(x => x.Name == layerName);

                if (lyr == null) throw new Exception();

                return lyr;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("The feature layer '{0}' was not found in current active map",
                   layerName), ex);
            }
        }

        /// <summary>
        /// Prettifies the keys and values from dictionary if a print is needed
        /// </summary>
        /// <returns>Prettified dictionary key=value</returns>
        public string PrintPrettyDict()
        {
            var keysAndValues = string.Empty;

            foreach (var item in MainDictionary)
            {
                keysAndValues += string.Format("| {0}={1} |", item.Key, item.Value);
            }

            return keysAndValues;
        }
    }
}
