using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreGraphics;

namespace SeekArc.Touch
{
    //
    //  EFCircularTrig.m
    //  
    //
    //  Created by Eliot Fowler on 12/3/13.
    //  Copyright (c) 2013 Eliot Fowler. All rights reserved.
    //



    /**
     *  Macro for converting radian degrees from cartesian reference (0 radians is along X axis) 
     *   to 'compass style' reference (0 radians is along Y axis (ie North on a compass)).
     *
     *  @param rad Radian degrees to convert from Cartesian reference
     *
     *  @return Radian Degrees in 'Compass' reference
     */
    //#define CartesianToCompass(rad) ( rad + M_PI/2 )
    ///**
    // *  Macro for converting radian degrees from 'compass style' reference (0 radians is along Y axis (ie North on a compass))
    // *   to cartesian reference (0 radians is along X axis).
    // *
    // *  @param rad Radian degrees to convert from 'Compass' reference
    // *
    // *  @return Radian Degrees in Cartesian reference
    // */
    //#define CompassToCartesian(rad) ( rad - M_PI/2 )
    //#define ToRad(deg) 		( (M_PI * (deg)) / 180.0 )
    //#define ToDeg(rad)		( (180.0 * (rad)) / M_PI )
    //#define SQR(x)			( (x) * (x) )

    //@implementation EFCircularTrig
    internal class Utils
    {
        private static nfloat SQR(nfloat x)
        {
            return x * x;
        }

		private static double CartesianToCompass(double rad)
		{
			return (rad + Math.PI / 2);
		}

        private static double CompassToCartesian(double rad)
        {
            return (rad - Math.PI / 2);
        }

        public static double angleRelativeToNorthFromPoint(CGPoint fromPoint, CGPoint toPoint)
        {
            CGPoint v = new CGPoint(toPoint.X - fromPoint.X, toPoint.Y - fromPoint.Y);
            var vmag = (nfloat)Math.Sqrt(SQR(v.X) + SQR(v.Y));
            v.X /= vmag;
            v.Y /= vmag;
            var cartesianRadians = Math.Atan2(v.Y, v.X);
		    // Need to convert from cartesian style radians to compass style
            double compassRadians = CartesianToCompass(cartesianRadians);
            if (compassRadians < 0)
            {
                compassRadians += 2 * Math.PI;
            }
            //NSAssert(compassRadians >= 0 && compassRadians <= 2*Math.PI, @"angleRelativeToNorth should be always positive");
            return ConvertToDegrees(compassRadians);
        }

        private static double ConvertToRadians(double angle)
        {
            return (Math.PI / 180) * angle;
        }


        public static double ConvertToDegrees(double val)
        {

            return val * (180.0 / Math.PI);

        }

        public static CGPoint pointOnRadius(nfloat radius, nfloat angleFromNorth)
        {
            //Get the point on the circle for this angle

            // Need to adjust from 'compass' style angle to cartesian angle
            var cartesianAngle = CompassToCartesian(ConvertToRadians(angleFromNorth));
            CGPoint result = new CGPoint();
			result.X = (nfloat)Math.Round(radius * Math.Cos(cartesianAngle));
            result.Y = (nfloat)Math.Round(radius * Math.Sin(cartesianAngle));

            return result;
        }

#pragma mark - Draw arcs

        public static void drawFilledCircleInContext(CGContext ctx, CGPoint center, nfloat radius)
        {
            ctx.FillEllipseInRect(new CGRect(center.X - (radius), center.Y - (radius), 2 * radius, 2 * radius));
        }

        public static void drawUnfilledCircleInContext(CGContext ctx, CGPoint center, nfloat radius, nfloat lineWidth)
        {
            drawUnfilledArcInContext(ctx, center, radius: radius, lineWidth: lineWidth, fromAngleFromNorth: 0, toAngleFromNorth: 360); // 0 - 360 is full circle
        }

        public static void drawUnfilledArcInContext(CGContext ctx, CGPoint center, nfloat radius, nfloat lineWidth, nfloat fromAngleFromNorth, nfloat toAngleFromNorth)
        {
            var cartesianFromAngle = (nfloat)CompassToCartesian(ConvertToRadians(fromAngleFromNorth));
            var cartesianToAngle = (nfloat)CompassToCartesian(ConvertToRadians(toAngleFromNorth));
            
            ctx.AddArc(center.X,   // arc start point x
                            center.Y,   // arc start point y
                            radius,     // arc radius from center
                            cartesianFromAngle, cartesianToAngle,
                            false); // iOS flips the y coordinate so anti-clockwise (specified here by 0) becomes clockwise (desired)!

            ctx.SetLineWidth(lineWidth);
            ctx.SetLineCap(CGLineCap.Butt);
            ctx.DrawPath(CGPathDrawingMode.Stroke);
        }

        public static nfloat DegreesForArcLength(nfloat arcLength, nfloat radius)
        {
            var totalCircumference = 2 * Math.PI * radius;

            var arcRatioToCircumference = arcLength / totalCircumference;

            return (nfloat)(360 * arcRatioToCircumference); // If arcLength is exactly half circumference, that is exactly half a circle in degrees
        }


        #region - Calculate radii of arcs with line widths
        /*
 *  For an unfilled arc.
 *
 *  Radius of outer arc (center to outside edge)  |          ---------
 *      = radius + 0.5 * lineWidth                |      +++++++++++++++
 *                                                |    /++/++++ --- ++++\++\
 *  Radius of inner arc (center to inside edge)   |   /++/++/         \++\++\
 *      = radius - (0.5 * lineWidth)              |  |++|++|     .     |++|++|
 *                                         outer edge^  ^-radius-^     ^inner edge
 *
 */
        public static nfloat outerRadiuOfUnfilledArcWithRadius(nfloat radius, nfloat lineWidth)
        {
            return radius + 0.5f * lineWidth;
        }

        public static nfloat innerRadiusOfUnfilledArcWithRadius(nfloat radius, nfloat lineWidth)
        {
            return radius - 0.5f * lineWidth;
        }
        #endregion

    }
}
