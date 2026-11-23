using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.ComponentModel;

/// <summary>
/// Namespace qui contient l'ensemble d'outils communs et génériques 
/// dans le projet INCA - ArcGIS Pro SDK
/// </summary>
namespace IGN.Inca.ArcGISPro.SDK.Commons
{
    /// <summary>
    /// Classe qui facilite les manipulations avec les Enumerators
    /// </summary>
    public class EnumUtils
    {
        /// <summary>
        /// Gets the string value of the given Enum
        /// </summary>
        /// <param name="value">the Enum value</param>
        /// <returns>string Enum value</returns>
        public static string StringValueOf(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attributes.Length > 0)
            {
                return attributes[0].Description;
            }
            else
            {
                return value.ToString();
            }
        }

        public static object EnumValueOf(string value, Type enumType)
        {
            string[] names = Enum.GetNames(enumType);
            foreach (string name in names)
            {
                if (StringValueOf((Enum)Enum.Parse(enumType, name)).Equals(value))
                {
                    return Enum.Parse(enumType, name);
                }
            }

            throw new ArgumentException("The string is not a description or value of the specified enum.");
        }
    }
}
