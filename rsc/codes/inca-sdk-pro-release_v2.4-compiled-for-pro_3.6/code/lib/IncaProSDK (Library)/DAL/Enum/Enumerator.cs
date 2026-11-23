using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Namespace qui contient l'ensemble d'enumerators qui font partie du modèle de données du projet INCA - ArcGIS Pro SDK
/// </summary>
namespace IGN.Inca.ArcGISPro.SDK.DAL.Enum
{
    /// <summary>
    /// Enumerator définissant les différents types de géométrie d'ArcGIS
    /// </summary>
    public enum ArcGISGeometryType
    {
        POINT = 0,
        MULTIPATCH = 1,
        MULTIPOINT = 2,
        POLYGON = 3,
        POLYLINE = 4
    }

    /// <summary>
    /// Enumerator définissant les différents types de toponymes à classifier
    /// </summary>
    public enum ToponymType
    {
        STRAIGHT = 0,
        CURVED = 1,
        UNKNOWN = 99
    }

    public enum MECCIMTextSymbolCalloutType
    {
        NONE = 0,
        SIMPLE_LINE = 1,
        BACKGROUND = 2
    }

    public enum ConvertingUtilTypes
    {
        BOOL = 0,
        SHORT = 1,
        INT = 2,
        DOUBLE = 3,
        DICTIONARY = 4
    }

    public enum MECSpreadWordsChoice
    {
        USE_DEFAULT_WORD_SPACING = 0,
        SPREAD_WORDS_UP_TO_A_FIXED_LIMIT = 1,
        SPREAD_WORDS_TO_FILL_FEATURE = 2
    }

    public enum MECSpreadCharactersChoice
    {
        USE_DEFAULT_LETTER_SPACING = 0,
        SPREAD_LETTERS_UP_TO_A_FIXED_LIMIT = 1,
        SPREAD_LETTERS_TO_FILL_FEATURE = 2
    }

    public enum MECAbbreviateTruncateChoice
    {
        NONE = 0,
        ABBREVIATION = 1,
        TRUNCATION = 2,
        ABBREVIATION_AND_TRUNCATION = 3
    }

    public enum MECRemoveDuplicateLabelsStrategyChoice
    {
        NEVER_REMOVE_LABEL = 0,
        THIN_DUPLICATE_LABELS = 1,
        CAN_REMOVE_OVERLAPPING_LABEL = 2
    }

    public enum MECMinimumFeatureSizePolygonBasedOnChoice
    {
        PERIMETER = 0,
        AREA = 1
    }

    public enum MECPointPlacementMeasureOffsetFromChoice
    {
        SIMPLIFIED_SYMBOL = 0,
        FEATURE_GEOMETRY = 1,
        EXACT_SYMBOL_OUTLINE = 2
    }

    public enum MECColorType
    {
        RGB = 0,
        CMYK = 1
    }
}
