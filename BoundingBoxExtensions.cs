using System;
using Android.Gms.Maps.Model;

namespace DroidMapping
{
    public static class BoundingBoxExtensions
    {
        public static LatLngBounds GetBoundingBox(this LatLng point, double radius)
        {            
            // Bounding box surrounding the point at given coordinates,
            var lat = Deg2rad(point.Latitude);
            var lon = Deg2rad(point.Longitude);
            var halfSide = (radius/2);

            // Radius of Earth at given latitude
            var earthRadius = WGS84EarthRadius(lat);

            var latMin = lat - halfSide / earthRadius;
            var latMax = lat + halfSide / earthRadius;
            var lonMin = lon - halfSide / earthRadius;
            var lonMax = lon + halfSide / earthRadius;

            return new LatLngBounds(
                new LatLng(Rad2deg(latMin), Rad2deg(lonMin)),
                new LatLng(Rad2deg(latMax), Rad2deg(lonMax)));
        }

// ---------------------------------------------------------------------------------------------------------------------------------- //

        static double Deg2rad(double degrees)
        {
            return Math.PI * degrees / 180.0;
        }

// ---------------------------------------------------------------------------------------------------------------------------------- //

        static double Rad2deg(double radians)
        {
            return 180.0 * radians / Math.PI;
        }

// ---------------------------------------------------------------------------------------------------------------------------------- //

        // Earth radius at a given latitude, according to the WGS-84 ellipsoid [m]
        static double WGS84EarthRadius(double lat)
        {
            // http://en.wikipedia.org/wiki/Earth_radius
            const double WGS84_a = 6378137.0; // Major semiaxis [m]
            const double WGS84_b = 6356752.3; // Minor semiaxis [m]
            var An = WGS84_a * WGS84_a * Math.Cos(lat);
            var Bn = WGS84_b * WGS84_b * Math.Sin(lat);
            var Ad = WGS84_a * Math.Cos(lat);
            var Bd = WGS84_b * Math.Sin(lat);
            return Math.Sqrt((An*An + Bn*Bn) / (Ad*Ad + Bd*Bd));
        }

// ---------------------------------------------------------------------------------------------------------------------------------- //
    }
}

