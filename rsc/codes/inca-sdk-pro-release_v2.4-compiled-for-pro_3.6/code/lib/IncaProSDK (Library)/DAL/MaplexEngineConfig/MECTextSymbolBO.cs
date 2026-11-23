using ArcGIS.Core.CIM;
using ArcGIS.Core.Internal.CIM;
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
    /// qui initialise un objet de type CIMTextSymbol
    /// </summary>
    public class MECTextSymbolBO
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public MECTextSymbolBO() { }

        public string TextSymbolName { get; set; }
        public CIMTextSymbol CIMTextSymbol { get; set; }

        public static Task<MECTextSymbolBO> CreateMECTextSymbolBO(Dictionary<string, string> valuesFromExcel, StyleProjectItem stylxFile)
        {
            return QueuedTask.Run<MECTextSymbolBO>(() =>
            {
                MECTextSymbolBO newMECTextSymbolBO = null;
                ConfigFileUtils dictTools = new ConfigFileUtils(valuesFromExcel);

                try
                {
                    // ~~~~~~~~~~~
                    //  TEXT_SYMBOL
                    // ~~~~~~~~~~~
                    var textSymbol = dictTools.GetArgumentValue("TEXT_SYMBOL");

                    // this field is mandatory
                    if (string.IsNullOrWhiteSpace(textSymbol))
                        throw new ArgumentNullException("'TEXT_SYMBOL' field is empty");

                    // start creating a new instance of "MECTextSymbolBO"
                    newMECTextSymbolBO = new MECTextSymbolBO
                    {
                        TextSymbolName = textSymbol
                    };

                    // initialize a CIMTextSymbol object through the factory (with Pro SDK default values)
                    var aCIMTextSymbol = SymbolFactory.Instance.ConstructTextSymbol();

                    // ###############################################################
                    // #################### APPEARANCE - STARTS ######################
                    // ~~~~~~~~~~~~~~~~~~
                    //  FONT_FAMILY_NAME
                    // ~~~~~~~~~~~~~~~~~~
                    var fontFamilyName = dictTools.GetArgumentValue("FONT_FAMILY_NAME");
                    if (fontFamilyName != null) aCIMTextSymbol.FontFamilyName = fontFamilyName;

                    // ~~~~~~~~~~~~
                    //  FONT_STYLE
                    // ~~~~~~~~~~~~
                    var fontStyleName = dictTools.GetArgumentValue("FONT_STYLE");
                    if (fontStyleName != null) aCIMTextSymbol.FontStyleName = fontStyleName;

                    // ~~~~~~~~~~~~~~~~~~~~
                    //  HEIGHT (FONT SIZE)
                    // ~~~~~~~~~~~~~~~~~~~~
                    var height = dictTools.GetConvertedArgument("HEIGHT", ConvertingUtilTypes.DOUBLE);
                    if (height != null) aCIMTextSymbol.Height = (double)height;

                    // 
                    //  FILL
                    // 

                    // ~~~~~~~~~~~~
                    //  FILL_STYLE
                    // ~~~~~~~~~~~~
                    var fillStyle = dictTools.GetConvertedEnumArgument("FILL_STYLE", SimpleFillStyle.Null) ?? SimpleFillStyle.Solid;

                    // ~~~~~~~~~~~~
                    //  FONT_COLOR
                    // ~~~~~~~~~~~~
                    var fontColor = dictTools.GetConvertedColor("FONT_COLOR", stylxFile) ?? ColorFactory.Instance.BlackRGB;

                    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    //  FONT_OUTLINE_STROKE_COLOR
                    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    var fontOutlineStrokeColor = dictTools.GetConvertedColor("FONT_OUTLINE_STROKE_COLOR", stylxFile) ?? ColorFactory.Instance.BlackRGB;

                    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    //  FONT_OUTLINE_STROKE_WIDTH
                    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    var fontOutlineStrokeWidth = dictTools.GetConvertedArgument("FONT_OUTLINE_STROKE_WIDTH", ConvertingUtilTypes.DOUBLE) ?? 0.0;

                    var fillCIMStroke = new CIMSolidStroke
                    {
                        Width = (double)fontOutlineStrokeWidth,
                        Color = fontOutlineStrokeColor
                    };
                    aCIMTextSymbol.Symbol = SymbolFactory.Instance.ConstructPolygonSymbol(fontColor, (SimpleFillStyle)fillStyle, fillCIMStroke);

                    // ~~~~~~~~~~~
                    //  UNDERLINE
                    // ~~~~~~~~~~~
                    var underline = dictTools.GetConvertedArgument("UNDERLINE", ConvertingUtilTypes.BOOL);
                    if (underline != null) aCIMTextSymbol.Underline = (bool)underline;

                    // ~~~~~~~~~~~~~~~
                    //  STRIKETHROUGH
                    // ~~~~~~~~~~~~~~~
                    var strikethrough = dictTools.GetConvertedArgument("STRIKETHROUGH", ConvertingUtilTypes.BOOL);
                    if (strikethrough != null) aCIMTextSymbol.Strikethrough = (bool)strikethrough;

                    // ~~~~~~~~~~~
                    //  TEXT_CASE
                    // ~~~~~~~~~~~
                    var textCase = dictTools.GetConvertedEnumArgument("TEXT_CASE", TextCase.Normal);
                    if (textCase != null) aCIMTextSymbol.TextCase = (TextCase)textCase;

                    // ~~~~~~~~~~~~~~
                    //  FONT_EFFECTS
                    // ~~~~~~~~~~~~~~
                    var fontEffects = dictTools.GetConvertedEnumArgument("FONT_EFFECTS", FontEffects.Normal);
                    if (fontEffects != null) aCIMTextSymbol.FontEffects = (FontEffects)fontEffects;
                    // #################### APPEARANCE - ENDS ########################
                    // ###############################################################

                    // #############################################################
                    // #################### POSITION - STARTS ######################
                    // ~~~~~~~~~~~~~~~~~~~~~~
                    //  HORIZONTAL_ALIGNMENT
                    // ~~~~~~~~~~~~~~~~~~~~~~
                    var horizontalAlignment = dictTools.GetConvertedEnumArgument("HORIZONTAL_ALIGNMENT", HorizontalAlignment.Center);
                    if (horizontalAlignment != null) aCIMTextSymbol.HorizontalAlignment = (HorizontalAlignment)horizontalAlignment;

                    // ~~~~~~~~~~~~~~~~~~~~
                    //  VERTICAL_ALIGNMENT
                    // ~~~~~~~~~~~~~~~~~~~~
                    var verticalAlignment = dictTools.GetConvertedEnumArgument("VERTICAL_ALIGNMENT", VerticalAlignment.Center);
                    if (verticalAlignment != null) aCIMTextSymbol.VerticalAlignment = (VerticalAlignment)verticalAlignment;

                    // ~~~~~~~~~~
                    //  OFFSET_X
                    // ~~~~~~~~~~
                    var offsetX = dictTools.GetConvertedArgument("OFFSET_X", ConvertingUtilTypes.DOUBLE);
                    if (offsetX != null) aCIMTextSymbol.OffsetX = (double)offsetX;

                    // ~~~~~~~~~~
                    //  OFFSET_Y
                    // ~~~~~~~~~~
                    var offsetY = dictTools.GetConvertedArgument("OFFSET_Y", ConvertingUtilTypes.DOUBLE);
                    if (offsetY != null) aCIMTextSymbol.OffsetY = (double)offsetY;
                    // #################### POSITION - ENDS ########################
                    // #############################################################

                    // #############################################################
                    // #################### ROTATION - STARTS ######################
                    // ~~~~~~~~~~~~~~~~
                    //  ROTATION_ANGLE
                    // ~~~~~~~~~~~~~~~~
                    var angle = dictTools.GetConvertedArgument("ROTATION_ANGLE", ConvertingUtilTypes.DOUBLE);
                    if (angle != null) aCIMTextSymbol.Angle = (double)angle;
                    // #################### ROTATION - ENDS ########################
                    // #############################################################

                    // #########################################################
                    // #################### HALO - STARTS ######################
                    // ~~~~~~~~~~~~
                    //  HALO_STYLE
                    // ~~~~~~~~~~~~
                    var haloStyle = dictTools.GetConvertedEnumArgument("HALO_STYLE", SimpleFillStyle.Null) ?? SimpleFillStyle.Null;

                    // ~~~~~~~~~~~~
                    //  HALO_COLOR
                    // ~~~~~~~~~~~~
                    var haloColor = dictTools.GetConvertedColor("HALO_COLOR", stylxFile);

                    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    //  HALO_OUTLINE_STROKE_COLOR
                    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    var haloOutlineStrokeColor = dictTools.GetConvertedColor("HALO_OUTLINE_STROKE_COLOR", stylxFile);

                    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    //  HALO_OUTLINE_STROKE_WIDTH
                    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    var haloOutlineStrokeWidth = dictTools.GetConvertedArgument("HALO_OUTLINE_STROKE_WIDTH", ConvertingUtilTypes.DOUBLE) ?? 0.0;

                    var haloCIMStroke = new CIMSolidStroke
                    {
                        Width = (double)haloOutlineStrokeWidth,
                        Color = haloOutlineStrokeColor
                    };
                    if ((SimpleFillStyle)haloStyle != SimpleFillStyle.Null)
                        aCIMTextSymbol.HaloSymbol = SymbolFactory.Instance.ConstructPolygonSymbol(haloColor, (SimpleFillStyle)haloStyle, haloCIMStroke);

                    // ~~~~~~~~~~~
                    //  HALO_SIZE
                    // ~~~~~~~~~~~
                    var haloSize = dictTools.GetConvertedArgument("HALO_SIZE", ConvertingUtilTypes.DOUBLE);
                    if (haloSize != null) aCIMTextSymbol.HaloSize = (double)haloSize;
                    // #################### HALO - ENDS ########################
                    // #########################################################

                    // ###########################################################
                    // #################### SHADOW - STARTS ######################
                    // ~~~~~~~~~~~~~~
                    //  SHADOW_COLOR
                    // ~~~~~~~~~~~~~~
                    aCIMTextSymbol.ShadowColor = dictTools.GetConvertedColor("SHADOW_COLOR", stylxFile);

                    // ~~~~~~~~~~~~~~~~~
                    //  SHADOW_OFFSET_X
                    // ~~~~~~~~~~~~~~~~~
                    var shadowOffsetX = dictTools.GetConvertedArgument("SHADOW_OFFSET_X", ConvertingUtilTypes.DOUBLE);
                    if (shadowOffsetX != null) aCIMTextSymbol.ShadowOffsetX = (double)shadowOffsetX;

                    // ~~~~~~~~~~~~~~~~~
                    //  SHADOW_OFFSET_Y
                    // ~~~~~~~~~~~~~~~~~
                    var shadowOffsetY = dictTools.GetConvertedArgument("SHADOW_OFFSET_Y", ConvertingUtilTypes.DOUBLE);
                    if (shadowOffsetY != null) aCIMTextSymbol.ShadowOffsetY = (double)shadowOffsetY;
                    // #################### SHADOW - ENDS ########################
                    // ###########################################################

                    // ############################################################
                    // #################### CALLOUT - STARTS ######################
                    // ~~~~~~~~~~~~~~
                    //  CALLOUT_TYPE
                    // ~~~~~~~~~~~~~~
                    var calloutType = (MECCIMTextSymbolCalloutType)(dictTools.GetConvertedEnumArgument("CALLOUT_TYPE", MECCIMTextSymbolCalloutType.NONE) ?? MECCIMTextSymbolCalloutType.NONE);

                    if (calloutType == MECCIMTextSymbolCalloutType.NONE)
                        aCIMTextSymbol.Callout = null;
                    else if (calloutType == MECCIMTextSymbolCalloutType.SIMPLE_LINE)
                    {
                        // ~~~~~~~~~~~~~~~~~~~~
                        //  CALLOUT_LINE_STYLE
                        // ~~~~~~~~~~~~~~~~~~~~
                        var calloutLineStyle = dictTools.GetConvertedEnumArgument("CALLOUT_LINE_STYLE", SimpleLineStyle.Null) ?? SimpleLineStyle.Solid;

                        // ~~~~~~~~~~~~~~~~~~~~
                        //  CALLOUT_LINE_COLOR
                        // ~~~~~~~~~~~~~~~~~~~~
                        var calloutLineColor = dictTools.GetConvertedColor("CALLOUT_LINE_COLOR", stylxFile) ?? ColorFactory.Instance.BlackRGB;

                        // ~~~~~~~~~~~~~~~~~~~~
                        //  CALLOUT_LINE_WIDTH
                        // ~~~~~~~~~~~~~~~~~~~~
                        var calloutLineWidth = dictTools.GetConvertedArgument("CALLOUT_LINE_WIDTH", ConvertingUtilTypes.DOUBLE) ?? 1.0;

                        // ~~~~~~~~~~~~~~~~~~~~~~~~~~
                        //  CALLOUT_LEADER_TOLERANCE
                        // ~~~~~~~~~~~~~~~~~~~~~~~~~~
                        var calloutLeaderTolerance = dictTools.GetConvertedArgument("CALLOUT_LEADER_TOLERANCE", ConvertingUtilTypes.DOUBLE);

                        // ~~~~~~~~~~~~~~~~~~~
                        //  CALLOUT_AUTO_SNAP
                        // ~~~~~~~~~~~~~~~~~~~
                        var calloutAutoSnap = dictTools.GetConvertedArgument("CALLOUT_AUTO_SNAP", ConvertingUtilTypes.BOOL) ?? false;

                        // Build a line symbol
                        var lineSymbol = SymbolFactory.Instance.ConstructLineSymbol(calloutLineColor, (double)calloutLineWidth, (SimpleLineStyle)calloutLineStyle);

                        // Create a simple line callout
                        var lineCalloutSymbol = new CIMSimpleLineCallout()
                        {
                            // Assign the line symbol to the callout
                            LineSymbol = lineSymbol,
                            // Automatically snap the leader to the text
                            AutoSnap = (bool)calloutAutoSnap
                        };

                        // Assign the callout to the text symbol
                        aCIMTextSymbol.Callout = lineCalloutSymbol;

                        // And finally, set the callout leader tolerance if defined
                        if (calloutLeaderTolerance != null)
                            aCIMTextSymbol.Callout.LeaderTolerance = (double)calloutLeaderTolerance;
                    }
                    else if (calloutType == MECCIMTextSymbolCalloutType.BACKGROUND)
                    {
                        // ~~~~~~~~~~~~~~~~~~~~
                        //  CALLOUT_LINE_STYLE
                        // ~~~~~~~~~~~~~~~~~~~~
                        var calloutLineStyle = dictTools.GetConvertedEnumArgument("CALLOUT_LINE_STYLE", SimpleLineStyle.Null) ?? SimpleLineStyle.Null;

                        // ~~~~~~~~~~~~~~~~~~~~
                        //  CALLOUT_LINE_COLOR
                        // ~~~~~~~~~~~~~~~~~~~~
                        var calloutLineColor = dictTools.GetConvertedColor("CALLOUT_LINE_COLOR", stylxFile) ?? ColorFactory.Instance.BlackRGB;

                        // ~~~~~~~~~~~~~~~~~~~~
                        //  CALLOUT_LINE_WIDTH
                        // ~~~~~~~~~~~~~~~~~~~~
                        var calloutLineWidth = dictTools.GetConvertedArgument("CALLOUT_LINE_WIDTH", ConvertingUtilTypes.DOUBLE) ?? 1.0;

                        // ~~~~~~~~~~~~~~~~~~~~~~~~~~
                        //  CALLOUT_LEADER_TOLERANCE
                        // ~~~~~~~~~~~~~~~~~~~~~~~~~~
                        var calloutLeaderTolerance = dictTools.GetConvertedArgument("CALLOUT_LEADER_TOLERANCE", ConvertingUtilTypes.DOUBLE) ?? 0.0;

                        // ~~~~~~~~~~~~~~~~~~~
                        //  CALLOUT_FILL_STYLE
                        // ~~~~~~~~~~~~~~~~~~~
                        var calloutFillStyle = dictTools.GetConvertedEnumArgument("CALLOUT_FILL_STYLE", SimpleFillStyle.Null) ?? SimpleFillStyle.Null;

                        // ~~~~~~~~~~~~~~~~~~~
                        //  CALLOUT_FILL_COLOR
                        // ~~~~~~~~~~~~~~~~~~~
                        var calloutFillColor = dictTools.GetConvertedColor("CALLOUT_FILL_COLOR", stylxFile) ?? ColorFactory.Instance.WhiteRGB;

                        // ~~~~~~~~~~~~~~~~~~~
                        //  CALLOUT_OUTLINE_COLOR
                        // ~~~~~~~~~~~~~~~~~~~
                        var calloutOutlineColor = dictTools.GetConvertedColor("CALLOUT_OUTLINE_COLOR", stylxFile) ?? ColorFactory.Instance.BlackRGB;

                        // ~~~~~~~~~~~~~~~~~~~
                        //  CALLOUT_OUTLINE_WIDTH
                        // ~~~~~~~~~~~~~~~~~~~
                        var calloutOutlineWidth = dictTools.GetConvertedArgument("CALLOUT_OUTLINE_WIDTH", ConvertingUtilTypes.DOUBLE) ?? 0.0;

                        // ~~~~~~~~~~~~~~~~~~~
                        //  CALLOUT_ACCENT_STYLE
                        // ~~~~~~~~~~~~~~~~~~~
                        var calloutAccentStyle = dictTools.GetConvertedEnumArgument("CALLOUT_ACCENT_STYLE", SimpleLineStyle.Null) ?? SimpleLineStyle.Null;

                        // ~~~~~~~~~~~~~~~~~~~
                        //  CALLOUT_ACCENT_COLOR
                        // ~~~~~~~~~~~~~~~~~~~
                        var calloutAccentColor = dictTools.GetConvertedColor("CALLOUT_ACCENT_COLOR", stylxFile) ?? ColorFactory.Instance.BlackRGB; 

                        // ~~~~~~~~~~~~~~~~~~~
                        //  CALLOUT_ACCENT_WIDTH
                        // ~~~~~~~~~~~~~~~~~~~
                        var calloutAccentWidth = dictTools.GetConvertedArgument("CALLOUT_ACCENT_WIDTH", ConvertingUtilTypes.DOUBLE) ?? 1.0;

                        // ~~~~~~~~~~~~~~~~~~~
                        //  CALLOUT_LEADER_STYLE
                        // ~~~~~~~~~~~~~~~~~~~
                        var calloutLeaderStyle = dictTools.GetConvertedEnumArgument("CALLOUT_LEADER_STYLE", LeaderLineStyle.Base) ?? LeaderLineStyle.Base;

                        // ~~~~~~~~~~~~~~~~~~~
                        //  CALLOUT_GAP
                        // ~~~~~~~~~~~~~~~~~~~
                        var calloutGap = dictTools.GetConvertedArgument("CALLOUT_GAP", ConvertingUtilTypes.DOUBLE) ?? 0.0;

                        // ~~~~~~~~~~~~~~~~~~~
                        //  CALLOUT_LEFT_MARGIN
                        // ~~~~~~~~~~~~~~~~~~~
                        var calloutLeftMargin = dictTools.GetConvertedArgument("CALLOUT_LEFT_MARGIN", ConvertingUtilTypes.DOUBLE) ?? 5.0;

                        // ~~~~~~~~~~~~~~~~~~~
                        //  CALLOUT_RIGHT_MARGIN
                        // ~~~~~~~~~~~~~~~~~~~
                        var calloutRightMargin = dictTools.GetConvertedArgument("CALLOUT_RIGHT_MARGIN", ConvertingUtilTypes.DOUBLE) ?? 5.0;

                        // ~~~~~~~~~~~~~~~~~~~
                        //  CALLOUT_TOP_MARGIN
                        // ~~~~~~~~~~~~~~~~~~~
                        var calloutTopMargin = dictTools.GetConvertedArgument("CALLOUT_TOP_MARGIN", ConvertingUtilTypes.DOUBLE) ?? 5.0;

                        // ~~~~~~~~~~~~~~~~~~~
                        //  CALLOUT_BOTTOM_MARGIN
                        // ~~~~~~~~~~~~~~~~~~~
                        var calloutBottomMargin = dictTools.GetConvertedArgument("CALLOUT_BOTTOM_MARGIN", ConvertingUtilTypes.DOUBLE) ?? 5.0;

                        // Create and assign a background callout
                        var backgroundCalloutSymbol = new CIMBackgroundCallout();

                        // Build and assign a line symbol
                        if ((SimpleLineStyle)calloutLineStyle != SimpleLineStyle.Null)
                            backgroundCalloutSymbol.LeaderLineSymbol = SymbolFactory.Instance.ConstructLineSymbol(calloutLineColor, (double)calloutLineWidth, (SimpleLineStyle)calloutLineStyle);

                        // Build and assign a solid fill polygon symbol
                        if ((SimpleFillStyle)calloutFillStyle != SimpleFillStyle.Null)
                        {
                            var outline = SymbolFactory.Instance.ConstructStroke(calloutOutlineColor, (double)calloutOutlineWidth);
                            backgroundCalloutSymbol.BackgroundSymbol = SymbolFactory.Instance.ConstructPolygonSymbol(calloutFillColor, (SimpleFillStyle)calloutFillStyle, (CIMStroke)outline);
                        }

                        // Build and assign an accent bar
                        if ((SimpleLineStyle)calloutAccentStyle != SimpleLineStyle.Null)
                            backgroundCalloutSymbol.AccentBarSymbol = SymbolFactory.Instance.ConstructLineSymbol(calloutAccentColor, (double)calloutAccentWidth, (SimpleLineStyle)calloutAccentStyle);

                        //Set margins for the callout
                        var calloutMargin = new CIMTextMargin
                        {
                            Left = (double)calloutLeftMargin,
                            Right = (double)calloutRightMargin,
                            Top = (double)calloutTopMargin,
                            Bottom = (double)calloutBottomMargin
                        };

                        // Assign the other properties to the callout
                        backgroundCalloutSymbol.Gap = (double)calloutGap;
                        backgroundCalloutSymbol.LeaderTolerance = (double)calloutLeaderTolerance;
                        backgroundCalloutSymbol.LineStyle = (LeaderLineStyle)calloutLeaderStyle;
                        backgroundCalloutSymbol.Margin = calloutMargin;

                        // Assign the callout to the text symbol
                        aCIMTextSymbol.Callout = backgroundCalloutSymbol;
                    }
                    // #################### CALLOUT - ENDS ########################
                    // ############################################################

                    // ###############################################################
                    // #################### FORMATTING - STARTS ######################
                    // ~~~~~~~~~~~~~~
                    //  WORD_SPACING
                    // ~~~~~~~~~~~~~~
                    var wordSpacing = dictTools.GetConvertedArgument("WORD_SPACING", ConvertingUtilTypes.DOUBLE);
                    if (wordSpacing != null) aCIMTextSymbol.WordSpacing = (double)wordSpacing;

                    // ~~~~~~~~~~~~~~~~
                    //  LETTER_SPACING
                    // ~~~~~~~~~~~~~~~~
                    var letterSpacing = dictTools.GetConvertedArgument("LETTER_SPACING", ConvertingUtilTypes.DOUBLE);
                    if (letterSpacing != null) aCIMTextSymbol.LetterSpacing = (double)letterSpacing;

                    // ~~~~~~~~~~~~~~
                    //  LETTER_WIDTH
                    // ~~~~~~~~~~~~~~
                    var letterWidth = dictTools.GetConvertedArgument("LETTER_WIDTH", ConvertingUtilTypes.DOUBLE);
                    if (letterWidth != null) aCIMTextSymbol.LetterWidth = (double)letterWidth;

                    // ~~~~~~~~~~~~~~~
                    //  LINE_GAP_TYPE
                    // ~~~~~~~~~~~~~~~
                    var lineGapType = dictTools.GetConvertedEnumArgument("LINE_GAP_TYPE", LineGapType.Exact);
                    if (lineGapType != null) aCIMTextSymbol.LineGapType = (LineGapType)lineGapType;

                    // ~~~~~~~~~~
                    //  LINE_GAP
                    // ~~~~~~~~~~
                    var lineGap = dictTools.GetConvertedArgument("LINE_GAP", ConvertingUtilTypes.DOUBLE);
                    if (lineGap != null) aCIMTextSymbol.LineGap = (double)lineGap;

                    // ~~~~~~~~~~~~
                    //  FLIP_ANGLE
                    // ~~~~~~~~~~~~
                    var flipAngle = dictTools.GetConvertedArgument("FLIP_ANGLE", ConvertingUtilTypes.DOUBLE);
                    if (flipAngle != null) aCIMTextSymbol.FlipAngle = (double)flipAngle;

                    // ~~~~~~~~~~~
                    //  LIGATURES
                    // ~~~~~~~~~~~
                    var ligatures = dictTools.GetConvertedArgument("LIGATURES", ConvertingUtilTypes.BOOL);
                    if (ligatures != null) aCIMTextSymbol.Ligatures = (bool)ligatures;

                    // ~~~~~~~~~
                    //  KERNING
                    // ~~~~~~~~~
                    var kerning = dictTools.GetConvertedArgument("KERNING", ConvertingUtilTypes.BOOL);
                    if (kerning != null) aCIMTextSymbol.Kerning = (bool)kerning;

                    // ~~~~~~~~~~~~~~~~~~~~~~~~~
                    //  DRAW_GLYPHS_AS_GEOMETRY
                    // ~~~~~~~~~~~~~~~~~~~~~~~~~
                    var drawGlyphsAsGeometry = dictTools.GetConvertedArgument("DRAW_GLYPHS_AS_GEOMETRY", ConvertingUtilTypes.BOOL);
                    if (drawGlyphsAsGeometry != null) aCIMTextSymbol.DrawGlyphsAsGeometry = (bool)drawGlyphsAsGeometry;

                    // ~~~~~~~~~~~~~~~~~~~~~~~
                    //  EXTRAPOLATE_BASELINES
                    // ~~~~~~~~~~~~~~~~~~~~~~~
                    var extrapolateBaselines = dictTools.GetConvertedArgument("EXTRAPOLATE_BASELINES", ConvertingUtilTypes.BOOL);
                    if (extrapolateBaselines != null) aCIMTextSymbol.ExtrapolateBaselines = (bool)extrapolateBaselines;

                    // ~~~~~~~~~~~~~~~
                    //  GLYPH_HINTING
                    // ~~~~~~~~~~~~~~~
                    var hinting = dictTools.GetConvertedEnumArgument("GLYPH_HINTING", GlyphHinting.None);
                    if (hinting != null) aCIMTextSymbol.Hinting = (GlyphHinting)hinting;

                    // ~~~~~~~~~~~~~~~~~~~~
                    //  COMPATIBILITY_MODE
                    // ~~~~~~~~~~~~~~~~~~~~
                    var compatibilityMode = dictTools.GetConvertedArgument("COMPATIBILITY_MODE", ConvertingUtilTypes.BOOL);
                    if (compatibilityMode != null) aCIMTextSymbol.CompatibilityMode = (bool)compatibilityMode;
                    // #################### FORMATTING - ENDS ########################
                    // ###############################################################

                    // ##############################################################
                    // #################### PARAGRAPH - STARTS ######################
                    // ~~~~~~~~~~~~~~~~~~~
                    //  DRAW_SOFT_HYPHENS
                    // ~~~~~~~~~~~~~~~~~~~
                    var drawSoftHyphen = dictTools.GetConvertedArgument("DRAW_SOFT_HYPHENS", ConvertingUtilTypes.BOOL);
                    if (drawSoftHyphen != null) aCIMTextSymbol.DrawSoftHyphen = (bool)drawSoftHyphen;

                    // ~~~~~~~~~~~~~~~~~~~
                    //  INDENT_FIRST_LINE
                    // ~~~~~~~~~~~~~~~~~~~
                    var indentFirstLine = dictTools.GetConvertedArgument("INDENT_FIRST_LINE", ConvertingUtilTypes.DOUBLE);
                    if (indentFirstLine != null) aCIMTextSymbol.IndentFirstLine = (double)indentFirstLine;

                    // ~~~~~~~~~~~~~~~
                    //  INDENT_BEFORE
                    // ~~~~~~~~~~~~~~~
                    var indentBefore = dictTools.GetConvertedArgument("INDENT_BEFORE", ConvertingUtilTypes.DOUBLE);
                    if (indentBefore != null) aCIMTextSymbol.IndentBefore = (double)indentBefore;

                    // ~~~~~~~~~~~~~~
                    //  INDENT_AFTER
                    // ~~~~~~~~~~~~~~
                    var indentAfter = dictTools.GetConvertedArgument("INDENT_AFTER", ConvertingUtilTypes.DOUBLE);
                    if (indentAfter != null) aCIMTextSymbol.IndentAfter = (double)indentAfter;
                    // #################### PARAGRAPH - ENDS ########################
                    // ##############################################################

                    // #########################################################################
                    // #################### INTERNATIONALIZATION - STARTS ######################
                    // ~~~~~~~~~~~~~~~~
                    //  TEXT_DIRECTION
                    // ~~~~~~~~~~~~~~~~
                    var textDirection = dictTools.GetConvertedEnumArgument("TEXT_DIRECTION", TextReadingDirection.LTR);
                    if (textDirection != null) aCIMTextSymbol.TextDirection = (TextReadingDirection)textDirection;

                    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    //  VERTICAL_GLYPH_ORIENTATION
                    // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    var verticalGlyphOrientation = dictTools.GetConvertedEnumArgument("VERTICAL_GLYPH_ORIENTATION", VerticalGlyphOrientation.Right);
                    if (verticalGlyphOrientation != null) aCIMTextSymbol.VerticalGlyphOrientation = (VerticalGlyphOrientation)verticalGlyphOrientation;

                    // ~~~~~~~~~~~~~~~~~~~
                    //  BLOCK_PROGRESSION
                    // ~~~~~~~~~~~~~~~~~~~
                    var blockProgression = dictTools.GetConvertedEnumArgument("BLOCK_PROGRESSION", BlockProgression.TTB);
                    if (blockProgression != null) aCIMTextSymbol.BlockProgression = (BlockProgression)blockProgression;

                    // ~~~~~~~~~~~~~~~~~~~~~~~~~
                    //  REGION_AND_LANGUAGE_ISO
                    // ~~~~~~~~~~~~~~~~~~~~~~~~~
                    aCIMTextSymbol.LanguageISO = dictTools.GetArgumentValue("REGION_AND_LANGUAGE_ISO") ?? aCIMTextSymbol.LanguageISO;

                    // ~~~~~~~~~~~~~~~
                    //  FONT_ENCODING
                    // ~~~~~~~~~~~~~~~
                    var fontEncoding = dictTools.GetConvertedEnumArgument("FONT_ENCODING", FontEncoding.MSSymbol);
                    if (fontEncoding != null) aCIMTextSymbol.FontEncoding = (FontEncoding)fontEncoding;
                    // #################### INTERNATIONALIZATION - ENDS ########################
                    // #########################################################################

                    // Assign the CIMTextSymbol instance to the new MECTextSymbolBO.CIMTextSymbol property
                    newMECTextSymbolBO.CIMTextSymbol = aCIMTextSymbol;
                }
                catch (Exception ex)
                {
                    string key = null;
                    if (newMECTextSymbolBO != null) key = newMECTextSymbolBO.TextSymbolName;

                    log.Error(string.Format("TEXT_SYMBOL KEY='{0}'", key), ex);
                    newMECTextSymbolBO = null;
                }

                return newMECTextSymbolBO;
            });
        }
    }
}
