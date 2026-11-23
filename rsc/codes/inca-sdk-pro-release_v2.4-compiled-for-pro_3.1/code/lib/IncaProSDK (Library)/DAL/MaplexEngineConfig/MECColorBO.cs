using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using IGN.Inca.ArcGISPro.SDK.Commons;
using IGN.Inca.ArcGISPro.SDK.DAL.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Namespace qui contient le modèle de données dont l'Add-In "MaplexEngineConfigAddIn" a besoin
/// </summary>
namespace IGN.Inca.ArcGISPro.SDK.DAL.MaplexEngineConfig
{
    /// <summary>
    /// Classe qui aide à la lecture des paramètres provenant du fichier de config et
    /// qui initialise un objet de type CIMColor
    /// </summary>
    class MECColorBO
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public MECColorBO() { }

        public string ColorName { get; set; }
        public string ColorKey { get; set; }
        public MECColorType ColorType { get; set; }
        public CIMColor CIMColor { get; set; }

        public static Task<MECColorBO> CreateMECColorBO(Dictionary<string, string> valuesFromExcel, StyleProjectItem stylxFile)
        {
            return QueuedTask.Run<MECColorBO>(() =>
            {
                MECColorBO newMECColorBO = null;
                ConfigFileUtils dictTools = new ConfigFileUtils(valuesFromExcel);

                try
                {
                    // ~~~~~~~~~~~
                    //  COLOR_NAME
                    // ~~~~~~~~~~~
                    var colorName = dictTools.GetArgumentValue("COLOR_NAME");

                    // this field is mandatory
                    if (string.IsNullOrWhiteSpace(colorName)) throw new ArgumentNullException("The mandatory field 'COLOR_NAME' is empty");

                    // start creating a new instance of "MECTextSymbolBO"
                    newMECColorBO = new MECColorBO
                    {
                        ColorName = colorName
                    };

                    // ~~~~~~~~~~~
                    //  COLOR_KEY
                    // ~~~~~~~~~~~
                    var colorKey = dictTools.GetArgumentValue("COLOR_KEY");

                    // this field is mandatory
                    if (string.IsNullOrWhiteSpace(colorKey)) throw new ArgumentNullException("The mandatory field 'COLOR_KEY' is empty");

                    newMECColorBO.ColorKey = colorKey;

                    // ~~~~~~~~~~~
                    //  TYPE
                    // ~~~~~~~~~~~
                    var type = dictTools.GetConvertedEnumArgument("TYPE", MECColorType.RGB);

                    // this field is mandatory
                    if (type == null) throw new ArgumentNullException("The mandatory field 'TYPE' is empty");

                    newMECColorBO.ColorType = (MECColorType)type;

                    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    //  T (Transparency)
                    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    var transparency = dictTools.GetConvertedArgument("T", ConvertingUtilTypes.DOUBLE);

                    // this field is mandatory
                    if (transparency == null) throw new ArgumentNullException("The mandatory field 'T' is empty");

                    // #####################################################################
                    // ########## START - Reserve percentage transparency value ############

                    transparency = 100 - (double)transparency;                

                    // ########## END - Reserve percentage transparency value ############
                    // ###################################################################

                    CIMColor aCIMColor = null;

                    if (newMECColorBO.ColorType == MECColorType.RGB)
                    {
                        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                        //  R (Red)
                        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                        var red = dictTools.GetConvertedArgument("R", ConvertingUtilTypes.DOUBLE);

                        // this field is mandatory
                        if (red == null) throw new ArgumentNullException("The mandatory field 'R' is empty");

                        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                        //  G (Green)
                        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                        var green = dictTools.GetConvertedArgument("G", ConvertingUtilTypes.DOUBLE);

                        // this field is mandatory
                        if (green == null) throw new ArgumentNullException("The mandatory field 'G' is empty");

                        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                        //  B (Blue)
                        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                        var blue = dictTools.GetConvertedArgument("B", ConvertingUtilTypes.DOUBLE);

                        // this field is mandatory
                        if (blue == null) throw new ArgumentNullException("The mandatory field 'B' is empty");

                        aCIMColor = CIMColor.CreateRGBColor((double)red, (double)green, (double)blue, (double)transparency);
                    }
                    else if (newMECColorBO.ColorType == MECColorType.CMYK)
                    {
                        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                        //  C (Cyan)
                        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                        var cyan = dictTools.GetConvertedArgument("C", ConvertingUtilTypes.DOUBLE);

                        // this field is mandatory
                        if (cyan == null) throw new ArgumentNullException("The mandatory field 'C' is empty");

                        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                        //  M (Magenta)
                        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                        var magenta = dictTools.GetConvertedArgument("M", ConvertingUtilTypes.DOUBLE);

                        // this field is mandatory
                        if (magenta == null) throw new ArgumentNullException("The mandatory field 'M' is empty");

                        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                        //  Y (Yellow)
                        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                        var yellow = dictTools.GetConvertedArgument("Y", ConvertingUtilTypes.DOUBLE);

                        // this field is mandatory
                        if (yellow == null) throw new ArgumentNullException("The mandatory field 'Y' is empty");

                        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                        //  K (Black)
                        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                        var black = dictTools.GetConvertedArgument("K", ConvertingUtilTypes.DOUBLE);

                        // this field is mandatory
                        if (black == null) throw new ArgumentNullException("The mandatory field 'K' is empty");

                        aCIMColor = CIMColor.CreateCMYKColor((double)cyan, (double)magenta, (double)yellow, (double)black, (double)transparency);
                    }

                    // Assign the CIMColor instance to the new MECColorBO.CIMColor property
                    newMECColorBO.CIMColor = aCIMColor;
                }
                catch (Exception ex)
                {
                    string key = null;
                    if (newMECColorBO != null) key = newMECColorBO.ColorName;

                    log.Error(string.Format("COLOR NAME='{0}'", key), ex);
                    newMECColorBO = null;
                }

                return newMECColorBO;
            });
        }
    }
}
