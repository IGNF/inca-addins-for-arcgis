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
    /// qui initialise un objet de type CIMMaplexLabelPlacementProperties
    /// </summary>
    public class MECMaplexLabelPlacementBO
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public MECMaplexLabelPlacementBO() { }

        public string LabelPlacementName { get; set; }
        public CIMMaplexLabelPlacementProperties CIMMaplexLabelPlacementProps { get; set; }

        public static Task<MECMaplexLabelPlacementBO> CreateMECLabelPlacementBO(Dictionary<string, string> valuesFromExcel, StyleProjectItem stylxFile)
        {
            return QueuedTask.Run<MECMaplexLabelPlacementBO>(() =>
            {
                MECMaplexLabelPlacementBO newMECLabelPlacementBO = null;
                ConfigFileUtils dictTools = new ConfigFileUtils(valuesFromExcel);

                try
                {
                    // ~~~~~~~~~~~
                    //  LABEL_PLACEMENT
                    // ~~~~~~~~~~~
                    var labelPlacement = dictTools.GetArgumentValue("LABEL_PLACEMENT");

                    // this field is mandatory
                    if (string.IsNullOrWhiteSpace(labelPlacement))
                        throw new ArgumentNullException("'LABEL_PLACEMENT' field is empty");

                    // start creating a new instance of "MECTextSymbolBO"
                    newMECLabelPlacementBO = new MECMaplexLabelPlacementBO
                    {
                        LabelPlacementName = labelPlacement
                    };

                    // initialize a CIMTextSymbol object through the factory (with Pro SDK default values)
                    var aCIMMaplexLabelPlacementProps = new CIMMaplexLabelPlacementProperties();
                    
                    // ~~~~~~~~~~~~~~
                    //  FEATURE_TYPE
                    // ~~~~~~~~~~~~~~
                    var featureType = dictTools.GetConvertedEnumArgument("FEATURE_TYPE", LabelFeatureType.Point);
                    if (featureType != null) aCIMMaplexLabelPlacementProps.FeatureType = (LabelFeatureType)featureType;

                    // ############################################################
                    // #################### PLACEMENT - STARTS ####################
                    if (aCIMMaplexLabelPlacementProps.FeatureType == LabelFeatureType.Point)
                    {
                        // ~~~~~~~~~~~~~~~~~~
                        //  POINT_PLACEMENT_METHOD
                        // ~~~~~~~~~~~~~~~~~~
                        var pointPlacementMethod = dictTools.GetConvertedEnumArgument("POINT_PLACEMENT_METHOD", MaplexPointPlacementMethod.AroundPoint);
                        if (pointPlacementMethod != null) aCIMMaplexLabelPlacementProps.PointPlacementMethod = (MaplexPointPlacementMethod)pointPlacementMethod;

                        // ~~~~~~~~~~~~
                        //  CAN_SHIFT_POINT_LABEL
                        // ~~~~~~~~~~~~
                        var canShiftPointLabel = dictTools.GetConvertedArgument("CAN_SHIFT_POINT_LABEL", ConvertingUtilTypes.BOOL);
                        if (canShiftPointLabel != null) aCIMMaplexLabelPlacementProps.CanShiftPointLabel = (bool)canShiftPointLabel;

                        // ~~~~~~~~~~~~~~~~~~~~
                        //  PRIMARY_OFFSET_PT
                        // ~~~~~~~~~~~~~~~~~~~~
                        var primaryOffset = dictTools.GetConvertedArgument("PRIMARY_OFFSET_PT", ConvertingUtilTypes.DOUBLE);
                        if (primaryOffset != null) aCIMMaplexLabelPlacementProps.PrimaryOffset = (double)primaryOffset;

                        // ~~~~~~~~~~~~~~~~~~
                        //  PRIMARY_OFFSET_UNIT_PT
                        // ~~~~~~~~~~~~~~~~~~
                        var primaryOffsetUnit = dictTools.GetConvertedEnumArgument("PRIMARY_OFFSET_UNIT_PT", MaplexUnit.Map);
                        if (primaryOffsetUnit != null) aCIMMaplexLabelPlacementProps.PrimaryOffsetUnit = (MaplexUnit)primaryOffsetUnit;

                        // ~~~~~~~~~~~~~~~~~~
                        //  SECONDARY_OFFSET_PT
                        // ~~~~~~~~~~~~~~~~~~
                        var secondaryOffset = dictTools.GetConvertedArgument("SECONDARY_OFFSET_PT", ConvertingUtilTypes.DOUBLE);
                        if (secondaryOffset != null) aCIMMaplexLabelPlacementProps.SecondaryOffset = (double)secondaryOffset;

                        // ~~~~~~~~~~~~~~~~~~
                        //  MEASURE_OFFSET_FROM_PT
                        // ~~~~~~~~~~~~~~~~~~
                        var pointPlacementMeasureOffsetFromChoice = dictTools.GetConvertedEnumArgument("MEASURE_OFFSET_FROM_PT", MECPointPlacementMeasureOffsetFromChoice.SIMPLIFIED_SYMBOL);

                        if (pointPlacementMeasureOffsetFromChoice != null)
                        {
                            var choice = (MECPointPlacementMeasureOffsetFromChoice)pointPlacementMeasureOffsetFromChoice;

                            // /!\ special field /!\ In PRO there's a select box having the values "SIMPLIFIED_SYMBOL", "FEATURE_GEOMETRY" 
                            //                       and "EXACT_SYMBOL_OUTLINE" and the SDK has 2 props for this : "IsOffsetFromFeatureGeometry"
                            //                       and "UseExactSymbolOutline", this has been managed in the code below
                            if (MECPointPlacementMeasureOffsetFromChoice.SIMPLIFIED_SYMBOL == choice)
                            {
                                aCIMMaplexLabelPlacementProps.IsOffsetFromFeatureGeometry = false;
                            }
                            else if (MECPointPlacementMeasureOffsetFromChoice.FEATURE_GEOMETRY == choice)
                            {
                                aCIMMaplexLabelPlacementProps.IsOffsetFromFeatureGeometry = true;
                            }
                            else if (MECPointPlacementMeasureOffsetFromChoice.EXACT_SYMBOL_OUTLINE == choice)
                            {
                                aCIMMaplexLabelPlacementProps.UseExactSymbolOutline = true;
                            }
                        }

                        //
                        // POINT_EXTERNAL_ZONE_PRIORITIES
                        //
                        aCIMMaplexLabelPlacementProps.PointExternalZonePriorities = new CIMMaplexExternalZonePriorities();

                        // ~~~~~~~~~~~~~~~~~~
                        //  CENTER_PT
                        // ~~~~~~~~~~~~~~~~~~
                        var center = dictTools.GetConvertedArgument("CENTER_PT", ConvertingUtilTypes.INT);

                        // ~~~~~~~~~~~~~~~~~~
                        //  ABOVE_CENTER_PT
                        // ~~~~~~~~~~~~~~~~~~
                        var aboveCenter = dictTools.GetConvertedArgument("ABOVE_CENTER_PT", ConvertingUtilTypes.INT);

                        // ~~~~~~~~~~~~~~~~~~
                        //  ABOVE_RIGHT_PT
                        // ~~~~~~~~~~~~~~~~~~
                        var aboveRight = dictTools.GetConvertedArgument("ABOVE_RIGHT_PT", ConvertingUtilTypes.INT);

                        // ~~~~~~~~~~~~~~~~~~
                        //  CENTER_RIGHT_PT
                        // ~~~~~~~~~~~~~~~~~~
                        var centerRight = dictTools.GetConvertedArgument("CENTER_RIGHT_PT", ConvertingUtilTypes.INT);

                        // ~~~~~~~~~~~~~~~~~~
                        //  BELOW_RIGHT_PT
                        // ~~~~~~~~~~~~~~~~~~
                        var belowRight = dictTools.GetConvertedArgument("BELOW_RIGHT_PT", ConvertingUtilTypes.INT);

                        // ~~~~~~~~~~~~~~~~~~
                        //  BELOW_CENTER_PT
                        // ~~~~~~~~~~~~~~~~~~
                        var belowCenter = dictTools.GetConvertedArgument("BELOW_CENTER_PT", ConvertingUtilTypes.INT);

                        // ~~~~~~~~~~~~~~~~~~
                        //  BELOW_LEFT_PT
                        // ~~~~~~~~~~~~~~~~~~
                        var belowLeft = dictTools.GetConvertedArgument("BELOW_LEFT_PT", ConvertingUtilTypes.INT);

                        // ~~~~~~~~~~~~~~~~~~
                        //  CENTER_LEFT_PT
                        // ~~~~~~~~~~~~~~~~~~
                        var centerLeft = dictTools.GetConvertedArgument("CENTER_LEFT_PT", ConvertingUtilTypes.INT);

                        // ~~~~~~~~~~~~~~~~~~
                        //  ABOVE_LEFT_PT
                        // ~~~~~~~~~~~~~~~~~~
                        var aboveLeft = dictTools.GetConvertedArgument("ABOVE_LEFT_PT", ConvertingUtilTypes.INT);

                        // if ALL zone priorities have been filled
                        if (center != null && aboveCenter != null && aboveRight != null && 
                            centerRight != null && belowRight != null && belowCenter != null && 
                            belowLeft != null && centerLeft != null && aboveLeft != null)
                        {
                            aCIMMaplexLabelPlacementProps.PointExternalZonePriorities = new CIMMaplexExternalZonePriorities
                            {
                                Center = (int)center,
                                AboveCenter = (int)aboveCenter,
                                AboveRight = (int)aboveRight,
                                CenterRight = (int)centerRight,
                                BelowRight = (int)belowRight,
                                BelowCenter = (int)belowCenter,
                                BelowLeft = (int)belowLeft,
                                CenterLeft = (int)centerLeft,
                                AboveLeft = (int)aboveLeft
                            };

                            // very important to set this property to TRUE, if not the parameters above are not updated in Pro
                            aCIMMaplexLabelPlacementProps.EnablePointPlacementPriorities = true;
                        }
                    }

                    if (aCIMMaplexLabelPlacementProps.FeatureType == LabelFeatureType.Line)
                    {
                        // ~~~~~~~~~~~~~~~~~~
                        //  LINE_FEATURE_TYPE
                        // ~~~~~~~~~~~~~~~~~~
                        var lineFeatureType = dictTools.GetConvertedEnumArgument("LINE_FEATURE_TYPE", MaplexLineFeatureType.General);
                        if (lineFeatureType != null) aCIMMaplexLabelPlacementProps.LineFeatureType = (MaplexLineFeatureType)lineFeatureType;

                        // ~~~~~~~~~~~~
                        //  ALLOW_STRADDLE_STACKING
                        // ~~~~~~~~~~~~
                        var allowStraddleStacking = dictTools.GetConvertedArgument("ALLOW_STRADDLE_STACKING", ConvertingUtilTypes.BOOL);
                        if (allowStraddleStacking != null) aCIMMaplexLabelPlacementProps.AllowStraddleStacking = (bool)allowStraddleStacking;

                        // ~~~~~~~~~~~~~~~~~~
                        //  LINE_PLACEMENT_METHOD
                        // ~~~~~~~~~~~~~~~~~~
                        var linePlacementMethod = dictTools.GetConvertedEnumArgument("LINE_PLACEMENT_METHOD", MaplexLinePlacementMethod.CenteredHorizontalOnLine);
                        if (linePlacementMethod != null) aCIMMaplexLabelPlacementProps.LinePlacementMethod = (MaplexLinePlacementMethod)linePlacementMethod;

                        //
                        // OFFSET ALONG LINE PROPERTIES
                        //
                        var moalp = new CIMMaplexOffsetAlongLineProperties();

                        // ~~~~~~~~~~~~~~~~~~
                        //  PLACEMENT_METHOD
                        // ~~~~~~~~~~~~~~~~~~
                        var placementMethod = dictTools.GetConvertedEnumArgument("PLACEMENT_METHOD", MaplexOffsetAlongLineMethod.BestPositionAlongLine);
                        if (placementMethod != null) moalp.PlacementMethod = (MaplexOffsetAlongLineMethod)placementMethod;

                        // ~~~~~~~~~~~~~~~~~~
                        //  LABEL_ANCHOR_POINT
                        // ~~~~~~~~~~~~~~~~~~
                        var labelAnchorPoint = dictTools.GetConvertedEnumArgument("LABEL_ANCHOR_POINT", MaplexLabelAnchorPoint.CenterOfLabel);
                        if (labelAnchorPoint != null) moalp.LabelAnchorPoint = (MaplexLabelAnchorPoint)labelAnchorPoint;

                        // ~~~~~~~~~~
                        //  DISTANCE
                        // ~~~~~~~~~~
                        var distance = dictTools.GetConvertedArgument("DISTANCE", ConvertingUtilTypes.DOUBLE);
                        if (distance != null) moalp.Distance = (double)distance;

                        // ~~~~~~~~~~~~~~~~~~
                        //  DISTANCE_UNIT
                        // ~~~~~~~~~~~~~~~~~~
                        var distanceUnit = dictTools.GetConvertedEnumArgument("DISTANCE_UNIT", MaplexUnit.Map);
                        if (distanceUnit != null) moalp.DistanceUnit = (MaplexUnit)distanceUnit;

                        // ~~~~~~~~~~
                        //  TOLERANCE
                        // ~~~~~~~~~~
                        var tolerance = dictTools.GetConvertedArgument("TOLERANCE", ConvertingUtilTypes.DOUBLE);
                        if (tolerance != null) moalp.Tolerance = (double)tolerance;

                        // ~~~~~~~~~~~~
                        //  USE_LINE_DIRECTION
                        // ~~~~~~~~~~~~
                        var useLineDirection = dictTools.GetConvertedArgument("USE_LINE_DIRECTION", ConvertingUtilTypes.BOOL);
                        if (useLineDirection != null) moalp.UseLineDirection = (bool)useLineDirection;

                        // assign values
                        aCIMMaplexLabelPlacementProps.OffsetAlongLineProperties = moalp;

                        // ~~~~~~~~~~~~
                        //  ENABLE_SECONDARY_OFFSET
                        // ~~~~~~~~~~~~
                        var enableSecondaryOffset = dictTools.GetConvertedArgument("ENABLE_SECONDARY_OFFSET", ConvertingUtilTypes.BOOL);
                        if (enableSecondaryOffset != null) aCIMMaplexLabelPlacementProps.EnableSecondaryOffset = (bool)enableSecondaryOffset;

                        // ~~~~~~~~~~~~
                        //  SECONDARY_OFFSET_MINIMUM
                        // ~~~~~~~~~~~~
                        var secondaryOffsetMinimum = dictTools.GetConvertedArgument("SECONDARY_OFFSET_MINIMUM", ConvertingUtilTypes.DOUBLE);
                        if (secondaryOffsetMinimum != null) aCIMMaplexLabelPlacementProps.SecondaryOffsetMinimum = (double)secondaryOffsetMinimum;

                        // ~~~~~~~~~~~~~~~~~~
                        //  PRIMARY_OFFSET_UNIT_LN
                        // ~~~~~~~~~~~~~~~~~~
                        var primaryOffsetUnit = dictTools.GetConvertedEnumArgument("PRIMARY_OFFSET_UNIT_LN", MaplexUnit.Map);
                        if (primaryOffsetUnit != null) aCIMMaplexLabelPlacementProps.PrimaryOffsetUnit = (MaplexUnit)primaryOffsetUnit;

                        // ~~~~~~~~~~~~
                        //  SECONDARY_OFFSET_MAXIMUM
                        // ~~~~~~~~~~~~
                        var secondaryOffsetMaximum = dictTools.GetConvertedArgument("SECONDARY_OFFSET_MAXIMUM", ConvertingUtilTypes.DOUBLE);
                        if (secondaryOffsetMaximum != null) aCIMMaplexLabelPlacementProps.SecondaryOffsetMaximum = (double)secondaryOffsetMaximum;

                        // ~~~~~~~~~~~~
                        //  IS_OFFSET_FROM_FEATURE_GEOMETRY_LN
                        // ~~~~~~~~~~~~
                        var isOffsetFromFeatureGeometry = dictTools.GetConvertedArgument("IS_OFFSET_FROM_FEATURE_GEOMETRY_LN", ConvertingUtilTypes.BOOL);
                        if (isOffsetFromFeatureGeometry != null) aCIMMaplexLabelPlacementProps.IsOffsetFromFeatureGeometry = (bool)isOffsetFromFeatureGeometry;

                        // ~~~~~~~~~~~~
                        //  PRIMARY_OFFSET_LN
                        // ~~~~~~~~~~~~
                        var primaryOffset = dictTools.GetConvertedArgument("PRIMARY_OFFSET_LN", ConvertingUtilTypes.DOUBLE);
                        if (primaryOffset != null) aCIMMaplexLabelPlacementProps.PrimaryOffset = (double)primaryOffset;

                        // ~~~~~~~~~~~~~~~~~~
                        //  CONSTRAIN_OFFSET
                        // ~~~~~~~~~~~~~~~~~~
                        var constrainOffset = dictTools.GetConvertedEnumArgument("CONSTRAIN_OFFSET", MaplexConstrainOffset.NoConstraint);
                        if (constrainOffset != null) aCIMMaplexLabelPlacementProps.ConstrainOffset = (MaplexConstrainOffset)constrainOffset;

                        // ~~~~~~~~~~~~
                        //  CAN_PLACE_LABEL_ON_TOP_OF_FEATURE
                        // ~~~~~~~~~~~~
                        var canPlaceLabelOnTopOfFeature = dictTools.GetConvertedArgument("CAN_PLACE_LABEL_ON_TOP_OF_FEATURE", ConvertingUtilTypes.BOOL);
                        if (canPlaceLabelOnTopOfFeature != null) aCIMMaplexLabelPlacementProps.CanPlaceLabelOnTopOfFeature = (bool)canPlaceLabelOnTopOfFeature;

                        // ~~~~~~~~~~~~
                        //  CAN_REDUCE_LEADING
                        // ~~~~~~~~~~~~
                        var canReduceLeading = dictTools.GetConvertedArgument("CAN_REDUCE_LEADING", ConvertingUtilTypes.BOOL);
                        if (canReduceLeading != null) aCIMMaplexLabelPlacementProps.CanReduceLeading = (bool)canReduceLeading;

                        // ~~~~~~~~~~~~
                        //  CAN_FLIP_STACKED_STREET_LABEL
                        // ~~~~~~~~~~~~
                        var canFlipStackedStreetLabel = dictTools.GetConvertedArgument("CAN_FLIP_STACKED_STREET_LABEL", ConvertingUtilTypes.BOOL);
                        if (canFlipStackedStreetLabel != null) aCIMMaplexLabelPlacementProps.CanFlipStackedStreetLabel = (bool)canFlipStackedStreetLabel;

                        // ~~~~~~~~~~~~
                        //  MINIMUM_END_OF_STREET_CLEARANCE
                        // ~~~~~~~~~~~~
                        var minimumEndOfStreetClearance = dictTools.GetConvertedArgument("MINIMUM_END_OF_STREET_CLEARANCE", ConvertingUtilTypes.DOUBLE);
                        if (minimumEndOfStreetClearance != null) aCIMMaplexLabelPlacementProps.MinimumEndOfStreetClearance = (double)minimumEndOfStreetClearance;

                        // ~~~~~~~~~~~~
                        //  PREFERRED_END_OF_STREET_CLEARANCE
                        // ~~~~~~~~~~~~
                        var preferredEndOfStreetClearance = dictTools.GetConvertedArgument("PREFERRED_END_OF_STREET_CLEARANCE", ConvertingUtilTypes.DOUBLE);
                        if (preferredEndOfStreetClearance != null) aCIMMaplexLabelPlacementProps.PreferredEndOfStreetClearance = (double)preferredEndOfStreetClearance;

                        // ~~~~~~~~~~~~~~~~~~
                        //  CONTOUR_ALIGNMENT_TYPE
                        // ~~~~~~~~~~~~~~~~~~
                        var contourAlignmentType = dictTools.GetConvertedEnumArgument("CONTOUR_ALIGNMENT_TYPE", MaplexContourAlignmentType.Uphill);
                        if (contourAlignmentType != null) aCIMMaplexLabelPlacementProps.ContourAlignmentType = (MaplexContourAlignmentType)contourAlignmentType;

                        // ~~~~~~~~~~~~
                        //  CONTOUR_MAXIMUM_ANGLE
                        // ~~~~~~~~~~~~
                        var contourMaximumAngle = dictTools.GetConvertedArgument("CONTOUR_MAXIMUM_ANGLE", ConvertingUtilTypes.INT);
                        if (contourMaximumAngle != null) aCIMMaplexLabelPlacementProps.ContourMaximumAngle = (int)contourMaximumAngle;

                        // ~~~~~~~~~~~~
                        //  CAN_FLIP_STACKED_STREET_LABEL
                        // ~~~~~~~~~~~~
                        var placeLabelsInLaddersBool = dictTools.GetConvertedArgument("PLACE_LABELS_IN_LADDERS", ConvertingUtilTypes.BOOL);
                        // /!\ special field /!\ In PRO there's a checkbox called "Place labels in ladders", but in the SDK this is stored
                        //                       in the enum "ContourLadderType" having 3 possible values: "None", "Straight" and "Curved".
                        //                       Where FALSE matches with "None" and TRUE matches "Straight" and "Curved".
                        //
                        //                       *Note*: By default "Straight" is always used, but on the other hand the value "Curved" 
                        //                               is never used (so far...)

                        if (placeLabelsInLaddersBool != null)
                        {
                            if ((bool)placeLabelsInLaddersBool)
                                aCIMMaplexLabelPlacementProps.ContourLadderType = MaplexContourLadderType.Straight;
                            else
                                aCIMMaplexLabelPlacementProps.ContourLadderType = MaplexContourLadderType.None;
                        }
                    }

                    if (aCIMMaplexLabelPlacementProps.FeatureType == LabelFeatureType.Polygon)
                    {
                        // ~~~~~~~~~~~~~~~~~~
                        //  POLYGON_FEATURE_TYPE
                        // ~~~~~~~~~~~~~~~~~~
                        var polygonFeatureType = dictTools.GetConvertedEnumArgument("POLYGON_FEATURE_TYPE", MaplexPolygonFeatureType.General);
                        if (polygonFeatureType != null) aCIMMaplexLabelPlacementProps.PolygonFeatureType = (MaplexPolygonFeatureType)polygonFeatureType;

                        // ~~~~~~~~~~~~
                        //  AVOID_POLYGON_HOLES
                        // ~~~~~~~~~~~~
                        var avoidPolygonHoles = dictTools.GetConvertedArgument("AVOID_POLYGON_HOLES", ConvertingUtilTypes.BOOL);
                        if (avoidPolygonHoles != null) aCIMMaplexLabelPlacementProps.AvoidPolygonHoles = (bool)avoidPolygonHoles;

                        // ~~~~~~~~~~~~~~~~~~
                        //  POLYGON_PLACEMENT_METHOD
                        // ~~~~~~~~~~~~~~~~~~
                        var polygonPlacementMethod = dictTools.GetConvertedEnumArgument("POLYGON_PLACEMENT_METHOD", MaplexPolygonPlacementMethod.HorizontalInPolygon);
                        if (polygonPlacementMethod != null) aCIMMaplexLabelPlacementProps.PolygonPlacementMethod = (MaplexPolygonPlacementMethod)polygonPlacementMethod;

                        // ~~~~~~~~~~~~
                        //  PREFER_HORIZONTAL_PLACEMENT
                        // ~~~~~~~~~~~~
                        var preferHorizontalPlacement = dictTools.GetConvertedArgument("PREFER_HORIZONTAL_PLACEMENT", ConvertingUtilTypes.BOOL);
                        if (preferHorizontalPlacement != null) aCIMMaplexLabelPlacementProps.PreferHorizontalPlacement = (bool)preferHorizontalPlacement;

                        // ~~~~~~~~~~~~
                        //  CAN_PLACE_LABEL_OUTSIDE_POLYGON
                        // ~~~~~~~~~~~~
                        var canPlaceLabelOutsidePolygon = dictTools.GetConvertedArgument("CAN_PLACE_LABEL_OUTSIDE_POLYGON", ConvertingUtilTypes.BOOL);
                        if (canPlaceLabelOutsidePolygon != null) aCIMMaplexLabelPlacementProps.CanPlaceLabelOutsidePolygon = (bool)canPlaceLabelOutsidePolygon;

                        // ~~~~~~~~~~~~
                        //  ENABLE_POLYGON_FIXED_POSITION
                        // ~~~~~~~~~~~~
                        var enablePolygonFixedPosition = dictTools.GetConvertedArgument("ENABLE_POLYGON_FIXED_POSITION", ConvertingUtilTypes.BOOL);
                        if (enablePolygonFixedPosition != null) aCIMMaplexLabelPlacementProps.EnablePolygonFixedPosition = (bool)enablePolygonFixedPosition;

                        // ~~~~~~~~~~~~
                        //  PRIMARY_OFFSET_PG
                        // ~~~~~~~~~~~~
                        var primaryOffset = dictTools.GetConvertedArgument("PRIMARY_OFFSET_PG", ConvertingUtilTypes.DOUBLE);
                        if (primaryOffset != null) aCIMMaplexLabelPlacementProps.PrimaryOffset = (double)primaryOffset;

                        // ~~~~~~~~~~~~~~~~~~
                        //  PRIMARY_OFFSET_UNIT_PG
                        // ~~~~~~~~~~~~~~~~~~
                        var primaryOffsetUnit = dictTools.GetConvertedEnumArgument("PRIMARY_OFFSET_UNIT_PG", MaplexUnit.Map);
                        if (primaryOffsetUnit != null) aCIMMaplexLabelPlacementProps.PrimaryOffsetUnit = (MaplexUnit)primaryOffsetUnit;

                        // ~~~~~~~~~~~~
                        //  SECONDARY_OFFSET_PG
                        // ~~~~~~~~~~~~
                        var secondaryOffset = dictTools.GetConvertedArgument("SECONDARY_OFFSET_PG", ConvertingUtilTypes.DOUBLE);
                        if (secondaryOffset != null) aCIMMaplexLabelPlacementProps.SecondaryOffset = (double)secondaryOffset;

                        // ~~~~~~~~~~~~
                        //  IS_OFFSET_FROM_FEATURE_GEOMETRY_PG
                        // ~~~~~~~~~~~~
                        var isOffsetFromFeatureGeometry = dictTools.GetConvertedArgument("IS_OFFSET_FROM_FEATURE_GEOMETRY_PG", ConvertingUtilTypes.BOOL);
                        if (isOffsetFromFeatureGeometry != null) aCIMMaplexLabelPlacementProps.IsOffsetFromFeatureGeometry = (bool)isOffsetFromFeatureGeometry;

                        //
                        // POLYGON_INTERNAL_ZONE_PRIORITIES
                        //
                        aCIMMaplexLabelPlacementProps.PolygonInternalZones = new CIMMaplexInternalZonePriorities();

                        // ~~~~~~~~~~~~~~~~~~
                        //  CENTER_INT_PG
                        // ~~~~~~~~~~~~~~~~~~
                        var center = dictTools.GetConvertedArgument("CENTER_INT_PG", ConvertingUtilTypes.INT);

                        // ~~~~~~~~~~~~~~~~~~
                        //  ABOVE_CENTER_INT_PG
                        // ~~~~~~~~~~~~~~~~~~
                        var aboveCenter = dictTools.GetConvertedArgument("ABOVE_CENTER_INT_PG", ConvertingUtilTypes.INT);

                        // ~~~~~~~~~~~~~~~~~~
                        //  ABOVE_RIGHT_INT_PG
                        // ~~~~~~~~~~~~~~~~~~
                        var aboveRight = dictTools.GetConvertedArgument("ABOVE_RIGHT_INT_PG", ConvertingUtilTypes.INT);

                        // ~~~~~~~~~~~~~~~~~~
                        //  CENTER_RIGHT_INT_PG
                        // ~~~~~~~~~~~~~~~~~~
                        var centerRight = dictTools.GetConvertedArgument("CENTER_RIGHT_INT_PG", ConvertingUtilTypes.INT);

                        // ~~~~~~~~~~~~~~~~~~
                        //  BELOW_RIGHT_INT_PG
                        // ~~~~~~~~~~~~~~~~~~
                        var belowRight = dictTools.GetConvertedArgument("BELOW_RIGHT_INT_PG", ConvertingUtilTypes.INT);

                        // ~~~~~~~~~~~~~~~~~~
                        //  BELOW_CENTER_INT_PG
                        // ~~~~~~~~~~~~~~~~~~
                        var belowCenter = dictTools.GetConvertedArgument("BELOW_CENTER_INT_PG", ConvertingUtilTypes.INT);

                        // ~~~~~~~~~~~~~~~~~~
                        //  BELOW_LEFT_INT_PG
                        // ~~~~~~~~~~~~~~~~~~
                        var belowLeft = dictTools.GetConvertedArgument("BELOW_LEFT_INT_PG", ConvertingUtilTypes.INT);

                        // ~~~~~~~~~~~~~~~~~~
                        //  CENTER_LEFT_INT_PG
                        // ~~~~~~~~~~~~~~~~~~
                        var centerLeft = dictTools.GetConvertedArgument("CENTER_LEFT_INT_PG", ConvertingUtilTypes.INT);

                        // ~~~~~~~~~~~~~~~~~~
                        //  ABOVE_LEFT_INT_PG
                        // ~~~~~~~~~~~~~~~~~~
                        var aboveLeft = dictTools.GetConvertedArgument("ABOVE_LEFT_INT_PG", ConvertingUtilTypes.INT);

                        // if ALL zone priorities have been filled
                        if (center != null && aboveCenter != null && aboveRight != null && centerRight != null && belowRight != null &&
                            belowCenter != null && belowLeft != null && centerLeft != null && aboveLeft != null)
                        {
                            aCIMMaplexLabelPlacementProps.PolygonInternalZones = new CIMMaplexInternalZonePriorities
                            {
                                Center = (int)center,
                                AboveCenter = (int)aboveCenter,
                                AboveRight = (int)aboveRight,
                                CenterRight = (int)centerRight,
                                BelowRight = (int)belowRight,
                                BelowCenter = (int)belowCenter,
                                BelowLeft = (int)belowLeft,
                                CenterLeft = (int)centerLeft,
                                AboveLeft = (int)aboveLeft
                            };
                        }

                        //
                        // POLYGON_EXTERNAL_ZONE_PRIORITIES
                        //
                        aCIMMaplexLabelPlacementProps.PolygonExternalZones = new CIMMaplexExternalZonePriorities();

                        // ~~~~~~~~~~~~~~~~~~
                        //  ABOVE_CENTER_EXT_PG
                        // ~~~~~~~~~~~~~~~~~~
                        aboveCenter = dictTools.GetConvertedArgument("ABOVE_CENTER_EXT_PG", ConvertingUtilTypes.INT);

                        // ~~~~~~~~~~~~~~~~~~
                        //  ABOVE_RIGHT_EXT_PG
                        // ~~~~~~~~~~~~~~~~~~
                        aboveRight = dictTools.GetConvertedArgument("ABOVE_RIGHT_EXT_PG", ConvertingUtilTypes.INT);

                        // ~~~~~~~~~~~~~~~~~~
                        //  CENTER_RIGHT_EXT_PG
                        // ~~~~~~~~~~~~~~~~~~
                        centerRight = dictTools.GetConvertedArgument("CENTER_RIGHT_EXT_PG", ConvertingUtilTypes.INT);

                        // ~~~~~~~~~~~~~~~~~~
                        //  BELOW_RIGHT_EXT_PG
                        // ~~~~~~~~~~~~~~~~~~
                        belowRight = dictTools.GetConvertedArgument("BELOW_RIGHT_EXT_PG", ConvertingUtilTypes.INT);

                        // ~~~~~~~~~~~~~~~~~~
                        //  BELOW_CENTER_EXT_PG
                        // ~~~~~~~~~~~~~~~~~~
                        belowCenter = dictTools.GetConvertedArgument("BELOW_CENTER_EXT_PG", ConvertingUtilTypes.INT);

                        // ~~~~~~~~~~~~~~~~~~
                        //  BELOW_LEFT_EXT_PG
                        // ~~~~~~~~~~~~~~~~~~
                        belowLeft = dictTools.GetConvertedArgument("BELOW_LEFT_EXT_PG", ConvertingUtilTypes.INT);

                        // ~~~~~~~~~~~~~~~~~~
                        //  CENTER_LEFT_EXT_PG
                        // ~~~~~~~~~~~~~~~~~~
                        centerLeft = dictTools.GetConvertedArgument("CENTER_LEFT_EXT_PG", ConvertingUtilTypes.INT);

                        // ~~~~~~~~~~~~~~~~~~
                        //  ABOVE_LEFT_EXT_PG
                        // ~~~~~~~~~~~~~~~~~~
                        aboveLeft = dictTools.GetConvertedArgument("ABOVE_LEFT_EXT_PG", ConvertingUtilTypes.INT);

                        // if ALL zone priorities have been filled
                        if (aboveCenter != null && aboveRight != null && centerRight != null && belowRight != null &&
                            belowCenter != null && belowLeft != null && centerLeft != null && aboveLeft != null)
                        {
                            aCIMMaplexLabelPlacementProps.PolygonExternalZones = new CIMMaplexExternalZonePriorities
                            {
                                AboveCenter = (int)aboveCenter,
                                AboveRight = (int)aboveRight,
                                CenterRight = (int)centerRight,
                                BelowRight = (int)belowRight,
                                BelowCenter = (int)belowCenter,
                                BelowLeft = (int)belowLeft,
                                CenterLeft = (int)centerLeft,
                                AboveLeft = (int)aboveLeft
                            };
                        }

                        // ~~~~~~~~~~~~
                        //  BOUNDARY_LABELING_ALLOW_SINGLE_SIDED
                        // ~~~~~~~~~~~~
                        var boundaryLabelingAllowSingleSided = dictTools.GetConvertedArgument("BOUNDARY_LABELING_ALLOW_SINGLE_SIDED", ConvertingUtilTypes.BOOL);
                        if (boundaryLabelingAllowSingleSided != null) aCIMMaplexLabelPlacementProps.BoundaryLabelingAllowSingleSided = (bool)boundaryLabelingAllowSingleSided;

                        // ~~~~~~~~~~~~
                        //  BOUNDARY_LABELING_SINGLE_SIDED_ON_LINE
                        // ~~~~~~~~~~~~
                        var boundaryLabelingSingleSidedOnLine = dictTools.GetConvertedArgument("BOUNDARY_LABELING_SINGLE_SIDED_ON_LINE", ConvertingUtilTypes.BOOL);
                        if (boundaryLabelingSingleSidedOnLine != null) aCIMMaplexLabelPlacementProps.BoundaryLabelingSingleSidedOnLine = (bool)boundaryLabelingSingleSidedOnLine;

                        // ~~~~~~~~~~~~
                        //  BOUNDARY_LABELING_ALLOW_HOLES
                        // ~~~~~~~~~~~~
                        var boundaryLabelingAllowHoles = dictTools.GetConvertedArgument("BOUNDARY_LABELING_ALLOW_HOLES", ConvertingUtilTypes.BOOL);
                        if (boundaryLabelingAllowHoles != null) aCIMMaplexLabelPlacementProps.BoundaryLabelingAllowHoles = (bool)boundaryLabelingAllowHoles;
                    }
                    // #################### PLACEMENT - ENDS ####################
                    // ##########################################################


                    // ##############################################################
                    // #################### ORIENTATION - STARTS ####################
                    // ~~~~~~~~~~~~~~~~~~
                    //  GRATICULE_ALIGNMENT_TYPE
                    // ~~~~~~~~~~~~~~~~~~
                    var graticuleAlignmentType = dictTools.GetConvertedEnumArgument("GRATICULE_ALIGNMENT_TYPE", MaplexGraticuleAlignmentType.Straight);
                    if (graticuleAlignmentType != null) aCIMMaplexLabelPlacementProps.GraticuleAlignmentType = (MaplexGraticuleAlignmentType)graticuleAlignmentType;

                    if (aCIMMaplexLabelPlacementProps.FeatureType == LabelFeatureType.Line)
                    {
                        // ~~~~~~~~~~~~
                        //  ALIGN_LABEL_TO_LINE_DIRECTION
                        // ~~~~~~~~~~~~
                        var alignLabelToLineDirection = dictTools.GetConvertedArgument("ALIGN_LABEL_TO_LINE_DIRECTION", ConvertingUtilTypes.BOOL);
                        if (alignLabelToLineDirection != null) aCIMMaplexLabelPlacementProps.AlignLabelToLineDirection = (bool)alignLabelToLineDirection;
                    }
                    // #################### ORIENTATION - ENDS ####################
                    // ############################################################
                    
                    // ###########################################################
                    // #################### ROTATION - STARTS ####################
                    if (aCIMMaplexLabelPlacementProps.FeatureType == LabelFeatureType.Point)
                    {
                        // disable the RotationProperties in order to avoid disabling the Placement properties
                        aCIMMaplexLabelPlacementProps.RotationProperties = new CIMMaplexRotationProperties() { Enable = false };

                        // ~~~~~~~~~~~~
                        //  ROTATION_FIELD
                        // ~~~~~~~~~~~~
                        // /!\ special field : under observation /!\
                        var rotationField = dictTools.GetArgumentValue("ROTATION_FIELD");
                        if (rotationField != null) aCIMMaplexLabelPlacementProps.RotationProperties.RotationField = rotationField == "None" ? null : rotationField;

                        // ~~~~~~~~~~~~
                        //  ADDITIONAL_ANGLE
                        // ~~~~~~~~~~~~
                        var additionalAngle = dictTools.GetConvertedArgument("ADDITIONAL_ANGLE", ConvertingUtilTypes.INT);
                        if (additionalAngle != null) aCIMMaplexLabelPlacementProps.RotationProperties.AdditionalAngle = (int)additionalAngle;

                        // ~~~~~~~~~~~~
                        //  ROTATION_TYPE
                        // ~~~~~~~~~~~~
                        var rotationType = dictTools.GetConvertedEnumArgument("ROTATION_TYPE", MaplexLabelRotationType.Geographic);
                        if (rotationType != null) aCIMMaplexLabelPlacementProps.RotationProperties.RotationType = (MaplexLabelRotationType)rotationType;

                        // ~~~~~~~~~~~~
                        //  ALIGNMENT_TYPE
                        // ~~~~~~~~~~~~
                        var alignmentType = dictTools.GetConvertedEnumArgument("ALIGNMENT_TYPE", MaplexRotationAlignmentType.Straight);
                        if (alignmentType != null) aCIMMaplexLabelPlacementProps.RotationProperties.AlignmentType = (MaplexRotationAlignmentType)alignmentType;

                        // ~~~~~~~~~~~~
                        //  PERPENDICULAR_TO_ANGLE
                        // ~~~~~~~~~~~~
                        // /!\ special field /!\ In PRO there's only a checkbox "Keep label upright (may flip)", in th SDK there are 2 
                        //                       Booleans "PerpendicularToAngle" and "AlignLabelToAngle", only one can be true.
                        var perpendicularToAngle = dictTools.GetConvertedArgument("PERPENDICULAR_TO_ANGLE", ConvertingUtilTypes.BOOL);

                        if (perpendicularToAngle != null)
                        {
                            // checkbox "Keep label upright (may flip)"
                            aCIMMaplexLabelPlacementProps.RotationProperties.PerpendicularToAngle = (bool)perpendicularToAngle;
                            aCIMMaplexLabelPlacementProps.RotationProperties.AlignLabelToAngle = !(bool)perpendicularToAngle;
                        }
                    }
                    // #################### ROTATION - ENDS ####################
                    // #########################################################
                    
                    // ##############################################################################
                    // #################### LEADER_LINE_ANCHOR_POINTS - STARTS ######################
                    if (aCIMMaplexLabelPlacementProps.FeatureType == LabelFeatureType.Polygon)
                    {
                        // ~~~~~~~~~~~~
                        //  POLYGON_ANCHOR_POINT_TYPE
                        // ~~~~~~~~~~~~
                        var polygonAnchorPointType = dictTools.GetConvertedEnumArgument("POLYGON_ANCHOR_POINT_TYPE", MaplexAnchorPointType.GeometricCenter);
                        if (polygonAnchorPointType != null) aCIMMaplexLabelPlacementProps.PolygonAnchorPointType = (MaplexAnchorPointType)polygonAnchorPointType;
                    }
                    // #################### LEADER_LINE_ANCHOR_POINTS - ENDS ######################
                    // ############################################################################

                    // ##################################################################
                    // #################### SPREAD LABELS - STARTS ######################
                    if (aCIMMaplexLabelPlacementProps.FeatureType == LabelFeatureType.Line
                        || aCIMMaplexLabelPlacementProps.FeatureType == LabelFeatureType.Polygon)
                    {
                        // Note: les colonnes "SPREAD_WORDS" et "SPREAD_CHARACTERS" sont traitées de façon particulière
                        //       car dans PRO elles sont des enums mais dans le SDK on peut constater qu'elles sont de 
                        //       type bool. Alors, pour simplifier les choses pour les utilisateurs on a créé les enums :
                        //       "MECSpreadWordsChoice" et "MECSpreadCharactersChoice" (comme dans PRO) et en fonction 
                        //       de la sélection, on applique les paramètres qu'il faut dans le SDK.

                        // ~~~~~~~~~~~~
                        //  SPREAD_WORDS
                        // ~~~~~~~~~~~~
                        var spreadWordsChoice = dictTools.GetConvertedEnumArgument("SPREAD_WORDS", MECSpreadWordsChoice.USE_DEFAULT_WORD_SPACING);

                        if (spreadWordsChoice != null)
                        {
                            var choice = (MECSpreadWordsChoice)spreadWordsChoice;

                            if (choice == MECSpreadWordsChoice.USE_DEFAULT_WORD_SPACING)
                            {
                                aCIMMaplexLabelPlacementProps.SpreadWords = false;
                                aCIMMaplexLabelPlacementProps.MaximumWordSpacing = 0;
                            }
                            else if (choice == MECSpreadWordsChoice.SPREAD_WORDS_UP_TO_A_FIXED_LIMIT)
                            {
                                aCIMMaplexLabelPlacementProps.SpreadWords = true;

                                // ~~~~~~~~~~~~
                                //  MAXIMUM_WORD_SPACING
                                // ~~~~~~~~~~~~
                                var maximumWordSpacing = dictTools.GetConvertedArgument("MAXIMUM_WORD_SPACING", ConvertingUtilTypes.DOUBLE);

                                if (maximumWordSpacing != null)
                                {
                                    if ((double)maximumWordSpacing < 0)
                                        throw new ArgumentException("Max value must be greater than zero", "MAXIMUM_WORD_SPACING");

                                    aCIMMaplexLabelPlacementProps.MaximumWordSpacing = (double)maximumWordSpacing;
                                }
                            }
                            else if (choice == MECSpreadWordsChoice.SPREAD_WORDS_TO_FILL_FEATURE)
                            {
                                aCIMMaplexLabelPlacementProps.SpreadWords = true;
                                aCIMMaplexLabelPlacementProps.MaximumWordSpacing = 0;
                            }
                        }

                        // ~~~~~~~~~~~~
                        //  SPREAD_CHARACTERS
                        // ~~~~~~~~~~~~
                        var spreadCharactersChoice = dictTools.GetConvertedEnumArgument("SPREAD_CHARACTERS", MECSpreadCharactersChoice.USE_DEFAULT_LETTER_SPACING);

                        if (spreadCharactersChoice != null)
                        {
                            var choice = (MECSpreadCharactersChoice)spreadCharactersChoice;

                            if (choice == MECSpreadCharactersChoice.USE_DEFAULT_LETTER_SPACING)
                            {
                                aCIMMaplexLabelPlacementProps.SpreadCharacters = false;
                                aCIMMaplexLabelPlacementProps.MaximumCharacterSpacing = 0;
                            }
                            else if (choice == MECSpreadCharactersChoice.SPREAD_LETTERS_UP_TO_A_FIXED_LIMIT)
                            {
                                aCIMMaplexLabelPlacementProps.SpreadCharacters = true;

                                // ~~~~~~~~~~~~
                                //  MAXIMUM_CHARACTER_SPACING
                                // ~~~~~~~~~~~~
                                var maximumCharacterSpacing = dictTools.GetConvertedArgument("MAXIMUM_CHARACTER_SPACING", ConvertingUtilTypes.DOUBLE);

                                if (maximumCharacterSpacing != null)
                                {
                                    if ((double)maximumCharacterSpacing < 0)
                                        throw new ArgumentException("Max value must be greater than zero", "MAXIMUM_CHARACTER_SPACING");


                                    aCIMMaplexLabelPlacementProps.MaximumCharacterSpacing = (double)maximumCharacterSpacing;
                                }
                            }
                            else if (choice == MECSpreadCharactersChoice.SPREAD_LETTERS_TO_FILL_FEATURE)
                            {
                                aCIMMaplexLabelPlacementProps.SpreadCharacters = true;
                                aCIMMaplexLabelPlacementProps.MaximumCharacterSpacing = 0;
                            }
                        }
                    }
                    // #################### SPREAD LABELS - ENDS ########################
                    // ##################################################################

                    // ######################################################################################
                    // #################### STACK (Label Stacking Properties) - STARTS ######################
                    // ~~~~~~~~~~~~
                    //  CAN_STACK_LABEL
                    // ~~~~~~~~~~~~
                    var canStackLabel = dictTools.GetConvertedArgument("CAN_STACK_LABEL", ConvertingUtilTypes.BOOL);
                    if (canStackLabel != null) aCIMMaplexLabelPlacementProps.CanStackLabel = (bool)canStackLabel;

                    if (aCIMMaplexLabelPlacementProps.CanStackLabel)
                    {
                        var lsp = new CIMMaplexLabelStackingProperties();

                        // ~~~~~~~~~~~~
                        //  STACK_ALIGNMENT
                        // ~~~~~~~~~~~~
                        var stackAlignment = dictTools.GetConvertedEnumArgument("STACK_ALIGNMENT", MaplexStackingAlignment.ChooseBest);
                        if (stackAlignment != null) lsp.StackAlignment = (MaplexStackingAlignment)stackAlignment;

                        //
                        // SEPARATORS 
                        //
                        var separators = new List<CIMMaplexStackingSeparator>();

                        // ~~~~~~~~~~~~
                        //  SEPARATOR_1
                        // ~~~~~~~~~~~~
                        var separator = dictTools.GetArgumentValue("SEPARATOR_1");

                        // ~~~~~~~~~~~~
                        //  VISIBLE_1
                        // ~~~~~~~~~~~~
                        var visible = dictTools.GetConvertedArgument("VISIBLE_1", ConvertingUtilTypes.BOOL);

                        // ~~~~~~~~~~~~
                        //  SPLIT_FORCED_1
                        // ~~~~~~~~~~~~
                        var splitForced = dictTools.GetConvertedArgument("SPLIT_FORCED_1", ConvertingUtilTypes.BOOL);

                        // ~~~~~~~~~~~~
                        //  SPLIT_AFTER_1
                        // ~~~~~~~~~~~~
                        var splitAfter = dictTools.GetConvertedArgument("SPLIT_AFTER_1", ConvertingUtilTypes.BOOL);

                        var ss = BuildSeparator(separator, visible, splitForced, splitAfter);
                        if (ss != null) separators.Add(ss);

                        // ~~~~~~~~~~~~
                        //  SEPARATOR_2
                        // ~~~~~~~~~~~~
                        separator = dictTools.GetArgumentValue("SEPARATOR_2");

                        // ~~~~~~~~~~~~
                        //  VISIBLE_2
                        // ~~~~~~~~~~~~
                        visible = dictTools.GetConvertedArgument("VISIBLE_2", ConvertingUtilTypes.BOOL);

                        // ~~~~~~~~~~~~
                        //  SPLIT_FORCED_2
                        // ~~~~~~~~~~~~
                        splitForced = dictTools.GetConvertedArgument("SPLIT_FORCED_2", ConvertingUtilTypes.BOOL);

                        // ~~~~~~~~~~~~
                        //  SPLIT_AFTER_2
                        // ~~~~~~~~~~~~
                        splitAfter = dictTools.GetConvertedArgument("SPLIT_AFTER_2", ConvertingUtilTypes.BOOL);

                        ss = BuildSeparator(separator, visible, splitForced, splitAfter);
                        if (ss != null) separators.Add(ss);

                        // ~~~~~~~~~~~~
                        //  SEPARATOR_3
                        // ~~~~~~~~~~~~
                        separator = dictTools.GetArgumentValue("SEPARATOR_3");

                        // ~~~~~~~~~~~~
                        //  VISIBLE_3
                        // ~~~~~~~~~~~~
                        visible = dictTools.GetConvertedArgument("VISIBLE_3", ConvertingUtilTypes.BOOL);

                        // ~~~~~~~~~~~~
                        //  SPLIT_FORCED_3
                        // ~~~~~~~~~~~~
                        splitForced = dictTools.GetConvertedArgument("SPLIT_FORCED_3", ConvertingUtilTypes.BOOL);

                        // ~~~~~~~~~~~~
                        //  SPLIT_AFTER_3
                        // ~~~~~~~~~~~~
                        splitAfter = dictTools.GetConvertedArgument("SPLIT_AFTER_3", ConvertingUtilTypes.BOOL);

                        ss = BuildSeparator(separator, visible, splitForced, splitAfter);
                        if (ss != null) separators.Add(ss);

                        // assign separators if not empty
                        if (separators.Count > 0)
                            lsp.Separators = separators.ToArray();

                        // ~~~~~~~~~~~~
                        //  MAXIMUM_NUMBER_OF_LINES
                        // ~~~~~~~~~~~~
                        var maximumNumberOfLines = dictTools.GetConvertedArgument("MAXIMUM_NUMBER_OF_LINES", ConvertingUtilTypes.INT);
                        if (maximumNumberOfLines != null) lsp.MaximumNumberOfLines = (int)maximumNumberOfLines;

                        // ~~~~~~~~~~~~
                        //  MINIMUM_NUMBER_OF_CHARS_PER_LINE
                        // ~~~~~~~~~~~~
                        var minimumNumberOfCharsPerLine = dictTools.GetConvertedArgument("MINIMUM_NUMBER_OF_CHARS_PER_LINE", ConvertingUtilTypes.INT);
                        if (minimumNumberOfCharsPerLine != null) lsp.MinimumNumberOfCharsPerLine = (int)minimumNumberOfCharsPerLine;

                        // ~~~~~~~~~~~~
                        //  MAXIMUM_NUMBER_OF_CHARS_PER_LINE
                        // ~~~~~~~~~~~~
                        var maximumNumberOfCharsPerLine = dictTools.GetConvertedArgument("MAXIMUM_NUMBER_OF_CHARS_PER_LINE", ConvertingUtilTypes.INT);
                        if (maximumNumberOfCharsPerLine != null) lsp.MaximumNumberOfCharsPerLine = (int)maximumNumberOfCharsPerLine;

                        // assign values
                        aCIMMaplexLabelPlacementProps.LabelStackingProperties = lsp;
                    }
                    // #################### STACK (Label Stacking Properties) - ENDS ########################
                    // ######################################################################################
                    
                    // ############################################################
                    // #################### OVERRUN - STARTS ######################
                    if (aCIMMaplexLabelPlacementProps.FeatureType == LabelFeatureType.Line
                        || aCIMMaplexLabelPlacementProps.FeatureType == LabelFeatureType.Polygon)
                    {
                        // ~~~~~~~~~~~~
                        //  MAXIMUM_LABEL_OVERRUN
                        // ~~~~~~~~~~~~
                        var maximumLabelOverrun = dictTools.GetConvertedArgument("MAXIMUM_LABEL_OVERRUN", ConvertingUtilTypes.DOUBLE);
                        if (maximumLabelOverrun != null) aCIMMaplexLabelPlacementProps.MaximumLabelOverrun = (double)maximumLabelOverrun;

                        // ~~~~~~~~~~~~
                        //  MAXIMUM_LABEL_OVERRUN_UNIT
                        // ~~~~~~~~~~~~
                        var maximumLabelOverrunUnit = dictTools.GetConvertedEnumArgument("MAXIMUM_LABEL_OVERRUN_UNIT", MaplexUnit.Map);
                        if (maximumLabelOverrunUnit != null) aCIMMaplexLabelPlacementProps.MaximumLabelOverrunUnit = (MaplexUnit)maximumLabelOverrunUnit;

                        if (aCIMMaplexLabelPlacementProps.FeatureType == LabelFeatureType.Polygon)
                        {
                            // ~~~~~~~~~~~~
                            //  ALLOW_ASYMMETRIC_OVERRUN
                            // ~~~~~~~~~~~~
                            var allowAsymmetricOverrun = dictTools.GetConvertedArgument("ALLOW_ASYMMETRIC_OVERRUN", ConvertingUtilTypes.BOOL);
                            if (allowAsymmetricOverrun != null) aCIMMaplexLabelPlacementProps.AllowAsymmetricOverrun = (bool)allowAsymmetricOverrun;
                        }
                    }
                    // #################### OVERRUN - ENDS ########################
                    // ############################################################

                    // ################################################################
                    // #################### REDUCE SIZE - STARTS ######################
                    // ~~~~~~~~~~~~
                    //  CAN_REDUCE_FONT_SIZE
                    // ~~~~~~~~~~~~
                    var canReduceFontSize = dictTools.GetConvertedArgument("CAN_REDUCE_FONT_SIZE", ConvertingUtilTypes.BOOL);
                    if (canReduceFontSize != null) aCIMMaplexLabelPlacementProps.CanReduceFontSize = (bool)canReduceFontSize;

                    if (aCIMMaplexLabelPlacementProps.CanReduceFontSize)
                    {
                        // ~~~~~~~~~~~~
                        //  FONT_HEIGHT_REDUCTION_LIMIT
                        // ~~~~~~~~~~~~
                        var fontHeightReductionLimit = dictTools.GetConvertedArgument("FONT_HEIGHT_REDUCTION_LIMIT", ConvertingUtilTypes.DOUBLE);
                        if (fontHeightReductionLimit != null) aCIMMaplexLabelPlacementProps.FontHeightReductionLimit = (double)fontHeightReductionLimit;

                        // ~~~~~~~~~~~~
                        //  FONT_HEIGHT_REDUCTION_STEP
                        // ~~~~~~~~~~~~
                        var fontHeightReductionStep = dictTools.GetConvertedArgument("FONT_HEIGHT_REDUCTION_STEP", ConvertingUtilTypes.DOUBLE);
                        if (fontHeightReductionStep != null) aCIMMaplexLabelPlacementProps.FontHeightReductionStep = (double)fontHeightReductionStep;

                        // ~~~~~~~~~~~~
                        //  FONT_WIDTH_REDUCTION_LIMIT
                        // ~~~~~~~~~~~~
                        var fontWidthReductionLimit = dictTools.GetConvertedArgument("FONT_WIDTH_REDUCTION_LIMIT", ConvertingUtilTypes.DOUBLE);
                        if (fontWidthReductionLimit != null) aCIMMaplexLabelPlacementProps.FontWidthReductionLimit = (double)fontWidthReductionLimit;

                        // ~~~~~~~~~~~~
                        //  FONT_WIDTH_REDUCTION_STEP
                        // ~~~~~~~~~~~~
                        var fontWidthReductionStep = dictTools.GetConvertedArgument("FONT_WIDTH_REDUCTION_STEP", ConvertingUtilTypes.DOUBLE);
                        if (fontWidthReductionStep != null) aCIMMaplexLabelPlacementProps.FontWidthReductionStep = (double)fontWidthReductionStep;
                    }
                    // #################### REDUCE SIZE - ENDS ########################
                    // ################################################################

                    // ################################################################
                    // #################### ABBREVIATE - STARTS #######################
                    // ~~~~~~~~~~~~
                    //  ABBREVIATE_TRUNCATE_CHOICE
                    // ~~~~~~~~~~~~
                    var abbreviateTruncateChoice = dictTools.GetConvertedEnumArgument("ABBREVIATE_TRUNCATE_CHOICE", MECAbbreviateTruncateChoice.NONE);

                    if (abbreviateTruncateChoice != null)
                    {
                        if ((MECAbbreviateTruncateChoice)abbreviateTruncateChoice == MECAbbreviateTruncateChoice.NONE)
                        {
                            aCIMMaplexLabelPlacementProps.CanAbbreviateLabel = false;
                            aCIMMaplexLabelPlacementProps.CanTruncateLabel = false;
                        }
                        else if ((MECAbbreviateTruncateChoice)abbreviateTruncateChoice == MECAbbreviateTruncateChoice.ABBREVIATION)
                        {
                            aCIMMaplexLabelPlacementProps.CanAbbreviateLabel = true;
                            aCIMMaplexLabelPlacementProps.CanTruncateLabel = false;
                        }
                        else if ((MECAbbreviateTruncateChoice)abbreviateTruncateChoice == MECAbbreviateTruncateChoice.TRUNCATION)
                        {
                            aCIMMaplexLabelPlacementProps.CanAbbreviateLabel = false;
                            aCIMMaplexLabelPlacementProps.CanTruncateLabel = true;
                        }
                        else if ((MECAbbreviateTruncateChoice)abbreviateTruncateChoice == MECAbbreviateTruncateChoice.ABBREVIATION_AND_TRUNCATION)
                        {
                            aCIMMaplexLabelPlacementProps.CanAbbreviateLabel = true;
                            aCIMMaplexLabelPlacementProps.CanTruncateLabel = true;
                        }
                    }

                    if (aCIMMaplexLabelPlacementProps.CanAbbreviateLabel)
                    {
                        // ~~~~~~~~~~~~
                        //  DICTIONARY_NAME
                        // ~~~~~~~~~~~~
                        var dictionaryName = dictTools.GetArgumentValue("DICTIONARY_NAME");
                        if (dictionaryName != null) aCIMMaplexLabelPlacementProps.DictionaryName = dictionaryName;
                    }

                    if (aCIMMaplexLabelPlacementProps.CanTruncateLabel)
                    {
                        // Potential bug identified : if "MECAbbreviateTruncateChoice.TRUNCATION" => the truncation panel doesn't show
                        // "CanAbbreviateLabel" property has to be set to TRUE in order to get the truncation panel to appear.
                        aCIMMaplexLabelPlacementProps.CanAbbreviateLabel = true;

                        // ~~~~~~~~~~~~
                        //  TRUNCATION_MINIMUM_LENGTH
                        // ~~~~~~~~~~~~
                        var truncationMinimumLength = dictTools.GetConvertedArgument("TRUNCATION_MINIMUM_LENGTH", ConvertingUtilTypes.INT);
                        if (truncationMinimumLength != null) aCIMMaplexLabelPlacementProps.TruncationMinimumLength = (int)truncationMinimumLength;

                        // ~~~~~~~~~~~~
                        //  TRUNCATION_MARKER_CHARACTER
                        // ~~~~~~~~~~~~
                        var truncationMarkerCharacter = dictTools.GetArgumentValue("TRUNCATION_MARKER_CHARACTER");
                        if (truncationMarkerCharacter != null) aCIMMaplexLabelPlacementProps.TruncationMarkerCharacter = truncationMarkerCharacter;

                        // ~~~~~~~~~~~~
                        //  TRUNCATION_PREFERRED_CHARACTERS
                        // ~~~~~~~~~~~~
                        var truncationPreferredCharacters = dictTools.GetArgumentValue("TRUNCATION_PREFERRED_CHARACTERS");
                        if (truncationPreferredCharacters != null) aCIMMaplexLabelPlacementProps.TruncationPreferredCharacters = truncationPreferredCharacters;
                    }
                    // #################### ABBREVIATE - ENDS #########################
                    // ################################################################

                    // ################################################################
                    // #################### KEY NUMBER - STARTS #######################
                    // ~~~~~~~~~~~~
                    //  KEY_NUMBER_GROUP_NAME
                    // ~~~~~~~~~~~~
                    var keyNumberGroupName = dictTools.GetArgumentValue("KEY_NUMBER_GROUP_NAME");
                    if (keyNumberGroupName != null) aCIMMaplexLabelPlacementProps.KeyNumberGroupName = keyNumberGroupName;
                    // #################### KEY NUMBER - ENDS #########################
                    // ################################################################

                    // ################################################################
                    // #################### STRATEGY ORDER - STARTS ###################
                    var sp = new CIMMaplexStrategyPriorities();

                    // ~~~~~~~~~~~~
                    //  STACKING
                    // ~~~~~~~~~~~~
                    var stacking = dictTools.GetConvertedArgument("STACKING", ConvertingUtilTypes.INT);
                    if (stacking != null) sp.Stacking = (int)stacking;

                    // ~~~~~~~~~~~~
                    //  OVERRUN
                    // ~~~~~~~~~~~~
                    var overrun = dictTools.GetConvertedArgument("OVERRUN", ConvertingUtilTypes.INT);
                    if (overrun != null) sp.Overrun = (int)overrun;

                    // ~~~~~~~~~~~~
                    //  FONT_COMPRESSION
                    // ~~~~~~~~~~~~
                    var fontCompression = dictTools.GetConvertedArgument("FONT_COMPRESSION", ConvertingUtilTypes.INT);
                    if (fontCompression != null) sp.FontCompression = (int)fontCompression;

                    // ~~~~~~~~~~~~
                    //  FONT_REDUCTION
                    // ~~~~~~~~~~~~
                    var fontReduction = dictTools.GetConvertedArgument("FONT_REDUCTION", ConvertingUtilTypes.INT);
                    if (fontReduction != null) sp.FontReduction = (int)fontReduction;

                    // ~~~~~~~~~~~~
                    //  ABBREVIATION
                    // ~~~~~~~~~~~~
                    var abbreviation = dictTools.GetConvertedArgument("ABBREVIATION", ConvertingUtilTypes.INT);
                    if (abbreviation != null) sp.Abbreviation = (int)abbreviation;

                    // assign values
                    aCIMMaplexLabelPlacementProps.StrategyPriorities = sp;
                    // #################### STRATEGY ORDER - ENDS #####################
                    // ################################################################

                    // #########################################################################
                    // #################### REMOVE DUPLICATE LABELS - STARTS ###################
                    // ~~~~~~~~~~~~
                    //  REMOVE_DUPLICATE_LABELS_STRATEGY
                    // ~~~~~~~~~~~~
                    var removeDuplicateLabelsStrategyChoice = dictTools.GetConvertedEnumArgument("REMOVE_DUPLICATE_LABELS_STRATEGY", MECRemoveDuplicateLabelsStrategyChoice.NEVER_REMOVE_LABEL);

                    if (removeDuplicateLabelsStrategyChoice != null)
                    {
                        var choice = (MECRemoveDuplicateLabelsStrategyChoice)removeDuplicateLabelsStrategyChoice;
                        if (choice == MECRemoveDuplicateLabelsStrategyChoice.NEVER_REMOVE_LABEL)
                        {
                            aCIMMaplexLabelPlacementProps.NeverRemoveLabel = true;
                        }
                        else if (choice == MECRemoveDuplicateLabelsStrategyChoice.THIN_DUPLICATE_LABELS)
                        {
                            aCIMMaplexLabelPlacementProps.ThinDuplicateLabels = true;

                            // ~~~~~~~~~~~~
                            //  THINNING_DISTANCE
                            // ~~~~~~~~~~~~
                            var thinningDistance = dictTools.GetConvertedArgument("THINNING_DISTANCE", ConvertingUtilTypes.DOUBLE);
                            if (thinningDistance != null) aCIMMaplexLabelPlacementProps.ThinningDistance = (double)thinningDistance;

                            // ~~~~~~~~~~~~
                            //  THINNING_DISTANCE_UNIT
                            // ~~~~~~~~~~~~
                            var thinningDistanceUnit = dictTools.GetConvertedEnumArgument("THINNING_DISTANCE_UNIT", MaplexUnit.Map);
                            if (thinningDistanceUnit != null) aCIMMaplexLabelPlacementProps.ThinningDistanceUnit = (MaplexUnit)thinningDistanceUnit;
                        }
                        else if (choice == MECRemoveDuplicateLabelsStrategyChoice.CAN_REMOVE_OVERLAPPING_LABEL)
                        {
                            // Potential bug identified : the props "NeverRemoveLabel" and "ThinDuplicateLabels" 
                            // have to be set to TRUE in order to "CanRemoveOverlappingLabel" prop to be TRUE
                            aCIMMaplexLabelPlacementProps.NeverRemoveLabel = true;
                            aCIMMaplexLabelPlacementProps.ThinDuplicateLabels = true;

                            aCIMMaplexLabelPlacementProps.CanRemoveOverlappingLabel = true;
                        }
                    }
                    // #################### REMOVE DUPLICATE LABELS - ENDS #####################
                    // #########################################################################

                    // ~~~~~~~~~~~~
                    //  REMOVE_AMBIGUOUS_LABELS_STRATEGY
                    // ~~~~~~~~~~~~
                    var removeAmbiguousLabelsStrategyChoice = dictTools.GetConvertedEnumArgument("REMOVE_AMBIGUOUS_LABELS_STRATEGY", MaplexRemoveAmbiguousLabelsType.All);
                    if (removeAmbiguousLabelsStrategyChoice != null) aCIMMaplexLabelPlacementProps.RemoveAmbiguousLabels = (MaplexRemoveAmbiguousLabelsType)removeAmbiguousLabelsStrategyChoice;

                    // ########################################################
                    // #################### REPEAT - STARTS ###################
                    if (aCIMMaplexLabelPlacementProps.FeatureType == LabelFeatureType.Line
                        || aCIMMaplexLabelPlacementProps.FeatureType == LabelFeatureType.Polygon)
                    {
                        // ~~~~~~~~~~~~
                        //  MINIMUM_REPETITION_INTERVAL
                        // ~~~~~~~~~~~~
                        var minimumRepetitionInterval = dictTools.GetConvertedArgument("MINIMUM_REPETITION_INTERVAL", ConvertingUtilTypes.DOUBLE);
                        if (minimumRepetitionInterval != null) aCIMMaplexLabelPlacementProps.MinimumRepetitionInterval = (double)minimumRepetitionInterval;

                        // PATCH: "RepeatLabel" prop needs to be TRUE in order to synchronize "MinimumRepetitionInterval" prop with the UI
                        if (aCIMMaplexLabelPlacementProps.MinimumRepetitionInterval > 0)
                            aCIMMaplexLabelPlacementProps.RepeatLabel = true;

                        // ~~~~~~~~~~~~
                        //  REPETITION_INTERVAL_UNIT
                        // ~~~~~~~~~~~~
                        var repetitionIntervalUnit = dictTools.GetConvertedEnumArgument("REPETITION_INTERVAL_UNIT", MaplexUnit.Map);
                        if (repetitionIntervalUnit != null) aCIMMaplexLabelPlacementProps.RepetitionIntervalUnit = (MaplexUnit)repetitionIntervalUnit;

                        if (aCIMMaplexLabelPlacementProps.FeatureType == LabelFeatureType.Line)
                        {
                            // ~~~~~~~~~~~~
                            //  PREFER_LABEL_NEAR_MAP_BORDER
                            // ~~~~~~~~~~~~
                            var preferLabelNearMapBorder = dictTools.GetConvertedArgument("PREFER_LABEL_NEAR_MAP_BORDER", ConvertingUtilTypes.BOOL);
                            if (preferLabelNearMapBorder != null) aCIMMaplexLabelPlacementProps.PreferLabelNearMapBorder = (bool)preferLabelNearMapBorder;

                            // ~~~~~~~~~~~~
                            //  PREFER_LABEL_NEAR_MAP_BORDER_CLEARANCE
                            // ~~~~~~~~~~~~
                            var preferLabelNearMapBorderClearance = dictTools.GetConvertedArgument("PREFER_LABEL_NEAR_MAP_BORDER_CLEARANCE", ConvertingUtilTypes.DOUBLE);
                            if (preferLabelNearMapBorderClearance != null) aCIMMaplexLabelPlacementProps.PreferLabelNearMapBorderClearance = (double)preferLabelNearMapBorderClearance;

                            // ~~~~~~~~~~~~
                            //  PREFER_LABEL_NEAR_JUNCTION
                            // ~~~~~~~~~~~~
                            var preferLabelNearJunction = dictTools.GetConvertedArgument("PREFER_LABEL_NEAR_JUNCTION", ConvertingUtilTypes.BOOL);
                            if (preferLabelNearJunction != null) aCIMMaplexLabelPlacementProps.PreferLabelNearJunction = (bool)preferLabelNearJunction;

                            // ~~~~~~~~~~~~
                            //  PREFER_LABEL_NEAR_JUNCTION_CLEARANCE
                            // ~~~~~~~~~~~~
                            var preferLabelNearJunctionClearance = dictTools.GetConvertedArgument("PREFER_LABEL_NEAR_JUNCTION_CLEARANCE", ConvertingUtilTypes.DOUBLE);
                            if (preferLabelNearJunctionClearance != null) aCIMMaplexLabelPlacementProps.PreferLabelNearJunctionClearance = (double)preferLabelNearJunctionClearance;
                        }
                    }
                    // #################### REPEAT - ENDS #####################
                    // ########################################################

                    // ########################################################
                    // #################### BUFFER - STARTS ###################
                    // ~~~~~~~~~~~~
                    //  LABEL_BUFFER
                    // ~~~~~~~~~~~~
                    var labelBuffer = dictTools.GetConvertedArgument("LABEL_BUFFER", ConvertingUtilTypes.INT);
                    if (labelBuffer != null) aCIMMaplexLabelPlacementProps.LabelBuffer = (int)labelBuffer;

                    // ~~~~~~~~~~~~
                    //  IS_LABEL_BUFFER_HARD_CONSTRAINT
                    // ~~~~~~~~~~~~
                    var isLabelBufferHardConstraint = dictTools.GetConvertedArgument("IS_LABEL_BUFFER_HARD_CONSTRAINT", ConvertingUtilTypes.BOOL);
                    if (isLabelBufferHardConstraint != null) aCIMMaplexLabelPlacementProps.IsLabelBufferHardConstraint = (bool)isLabelBufferHardConstraint;
                    // #################### BUFFER - ENDS #####################
                    // ########################################################

                    // ######################################################################
                    // #################### MINIMUM FEATURE SIZE - STARTS ###################
                    if (aCIMMaplexLabelPlacementProps.FeatureType == LabelFeatureType.Line
                        || aCIMMaplexLabelPlacementProps.FeatureType == LabelFeatureType.Polygon)
                    {
                        // ~~~~~~~~~~~~
                        //  MINIMUM_SIZE_FOR_LABELING
                        // ~~~~~~~~~~~~
                        var minimumSizeForLabeling = dictTools.GetConvertedArgument("MINIMUM_SIZE_FOR_LABELING", ConvertingUtilTypes.DOUBLE);
                        if (minimumSizeForLabeling != null) aCIMMaplexLabelPlacementProps.MinimumSizeForLabeling = (double)minimumSizeForLabeling;

                        // ~~~~~~~~~~~~
                        //  MINIMUM_FEATURE_SIZE_UNIT
                        // ~~~~~~~~~~~~
                        var minimumFeatureSizeUnit = dictTools.GetConvertedEnumArgument("MINIMUM_FEATURE_SIZE_UNIT", MaplexUnit.Map);
                        if (minimumFeatureSizeUnit != null) aCIMMaplexLabelPlacementProps.MinimumFeatureSizeUnit = (MaplexUnit)minimumFeatureSizeUnit;

                        if (aCIMMaplexLabelPlacementProps.FeatureType == LabelFeatureType.Polygon)
                        {
                            // ~~~~~~~~~~~~
                            //  MINIMUM_SIZE_BASED_ON
                            // ~~~~~~~~~~~~
                            var minimumFeatureSizePolygonBasedOn = dictTools.GetConvertedEnumArgument("MINIMUM_SIZE_BASED_ON", MECMinimumFeatureSizePolygonBasedOnChoice.PERIMETER);

                            if (minimumFeatureSizePolygonBasedOn != null)
                            {
                                if ((MECMinimumFeatureSizePolygonBasedOnChoice)minimumFeatureSizePolygonBasedOn == MECMinimumFeatureSizePolygonBasedOnChoice.AREA)
                                    aCIMMaplexLabelPlacementProps.IsMinimumSizeBasedOnArea = true;
                                else
                                    aCIMMaplexLabelPlacementProps.IsMinimumSizeBasedOnArea = false;
                            }
                        }
                    }
                    // #################### MINIMUM FEATURE SIZE - ENDS #####################
                    // ######################################################################

                    // #################################################################
                    // #################### LINE CONNECTION - STARTS ###################
                    if (aCIMMaplexLabelPlacementProps.FeatureType == LabelFeatureType.Line)
                    {
                        // ~~~~~~~~~~~~
                        //  ENABLE_CONNECTION
                        // ~~~~~~~~~~~~
                        var enableConnection = dictTools.GetConvertedArgument("ENABLE_CONNECTION", ConvertingUtilTypes.BOOL);
                        if (enableConnection != null) aCIMMaplexLabelPlacementProps.EnableConnection = (bool)enableConnection;

                        if (aCIMMaplexLabelPlacementProps.EnableConnection)
                        {
                            // ~~~~~~~~~~~~
                            //  CONNECTION_TYPE
                            // ~~~~~~~~~~~~
                            var connectionType = dictTools.GetConvertedEnumArgument("CONNECTION_TYPE", MaplexConnectionType.MinimizeLabels);
                            if (connectionType != null) aCIMMaplexLabelPlacementProps.ConnectionType = (MaplexConnectionType)connectionType;
                        }
                        else
                        {
                            // ~~~~~~~~~~~~
                            //  MULTI_PART_OPTION
                            // ~~~~~~~~~~~~
                            var multiPartOption = dictTools.GetConvertedEnumArgument("MULTI_PART_OPTION", MaplexMultiPartOption.OneLabelPerFeature);
                            if (multiPartOption != null) aCIMMaplexLabelPlacementProps.MultiPartOption = (MaplexMultiPartOption)multiPartOption;
                        }
                    }
                    // #################### LINE CONNECTION - ENDS #####################
                    // #################################################################

                    // ####################################################################
                    // #################### LABEL LARGEST PART - STARTS ###################
                    if (aCIMMaplexLabelPlacementProps.FeatureType == LabelFeatureType.Polygon)
                    {
                        // ~~~~~~~~~~~~
                        //  LABEL_LARGEST_POLYGON
                        // ~~~~~~~~~~~~
                        var labelLargestPolygon = dictTools.GetConvertedArgument("LABEL_LARGEST_POLYGON", ConvertingUtilTypes.BOOL);
                        if (labelLargestPolygon != null) aCIMMaplexLabelPlacementProps.LabelLargestPolygon = (bool)labelLargestPolygon;
                    }
                    // #################### LABEL LARGEST PART - ENDS #####################
                    // ####################################################################

                    // ################################################################
                    // #################### FEATURE WEIGHT - STARTS ###################
                    // ~~~~~~~~~~~~
                    //  FEATURE_WEIGHT
                    // ~~~~~~~~~~~~
                    var featureWeight = dictTools.GetConvertedArgument("FEATURE_WEIGHT", ConvertingUtilTypes.INT);
                    if (featureWeight != null) aCIMMaplexLabelPlacementProps.FeatureWeight = (int)featureWeight;

                    if (aCIMMaplexLabelPlacementProps.FeatureType == LabelFeatureType.Polygon)
                    {
                        // ~~~~~~~~~~~~
                        //  POLYGON_BOUNDARY_WEIGHT
                        // ~~~~~~~~~~~~
                        var polygonBoundaryWeight = dictTools.GetConvertedArgument("POLYGON_BOUNDARY_WEIGHT", ConvertingUtilTypes.INT);
                        if (polygonBoundaryWeight != null) aCIMMaplexLabelPlacementProps.PolygonBoundaryWeight = (int)polygonBoundaryWeight;
                    }
                    // #################### FEATURE WEIGHT - ENDS #####################
                    // ################################################################

                    // ###################################################################
                    // #################### BACKGROUND LABELS - STARTS ###################
                    // ~~~~~~~~~~~~
                    //  BACKGROUND_LABEL
                    // ~~~~~~~~~~~~
                    var backgroundLabel = dictTools.GetConvertedArgument("BACKGROUND_LABEL", ConvertingUtilTypes.BOOL);
                    if (backgroundLabel != null) aCIMMaplexLabelPlacementProps.BackgroundLabel = (bool)backgroundLabel;
                    // #################### BACKGROUND LABELS - ENDS #####################
                    // ###################################################################

                    // #################################################################
                    // #################### UNPLACED LABELS - STARTS ###################
                    // ~~~~~~~~~~~~
                    //  NEVER_REMOVE_LABEL
                    // ~~~~~~~~~~~~
                    var neverRemoveLabel = dictTools.GetConvertedArgument("NEVER_REMOVE_LABEL", ConvertingUtilTypes.BOOL);
                    if (neverRemoveLabel != null) aCIMMaplexLabelPlacementProps.NeverRemoveLabel = (bool)neverRemoveLabel;
                    // #################### UNPLACED LABELS - ENDS #####################
                    // #################################################################
                    
                    // Assign the CIMMaplexLabelPlacementProperties instance to the new MECLabelPlacementBO.CIMMaplexLabelPlacementProperties property
                    newMECLabelPlacementBO.CIMMaplexLabelPlacementProps = aCIMMaplexLabelPlacementProps;
                }
                catch (Exception ex)
                {
                    string key = null;
                    if (newMECLabelPlacementBO != null) key = newMECLabelPlacementBO.LabelPlacementName;

                    log.Error(string.Format("LABEL_PLACEMENT KEY='{0}'", key), ex);
                    newMECLabelPlacementBO = null;
                }

                return newMECLabelPlacementBO;
            });
        }

        /// <summary>
        /// Helper method to build a separator
        /// </summary>
        /// <param name="separator"></param>
        /// <param name="visible"></param>
        /// <param name="splitForced"></param>
        /// <param name="splitAfter"></param>
        /// <returns></returns>
        protected static CIMMaplexStackingSeparator BuildSeparator(string separator, object visible, object splitForced, object splitAfter)
        {
            if (separator != null)
            {
                CIMMaplexStackingSeparator ss = new CIMMaplexStackingSeparator()
                {
                    // UI control to manage spaces in English and French
                    Separator = separator.Trim().ToUpperInvariant().Contains("SPACE") ? " " : separator
                };

                if (visible != null) ss.Visible = (bool)visible;

                if (splitForced != null) ss.SplitForced = (bool)splitForced;

                if (splitAfter != null) ss.SplitAfter = (bool)splitAfter;

                return ss;
            }

            return null;
        }
    }
}
