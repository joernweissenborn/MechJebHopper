using System;
using UnityEngine;

namespace Hopper
{
    public class HopManager
    {
        public Vector3d TargetPosition { get; set; }
        public Vector3d CurrentPosition { get; set; }

        public bool HasTarget => FlightGlobals.fetch.VesselTarget != null;

        public double Apoapsis
        {
            get
            {
                CelestialBody body = FlightGlobals.ActiveVessel.mainBody;
                double radius = body.Radius + FlightGlobals.ActiveVessel.altitude;
                double gravParameter = body.gravParameter;

                double hopVelocity = Math.Sqrt(gravParameter / radius) * Math.Sqrt(Distance() / (2 * Math.PI * radius));

                double semiMajorAxis = 1 / ((2 / radius) - Math.Pow(hopVelocity, 2) / gravParameter);

                double apoapsis = semiMajorAxis * 2 - radius;

                return apoapsis;
            }
        }

        public double Distance() => Vector3d.Distance(CurrentPosition, TargetPosition);

        public double Heading
        {
            get
            {
                CelestialBody body = FlightGlobals.ActiveVessel.mainBody;
                // Convert current and target world positions to lat/lon on the celestial body
                double currentLat = body.GetLatitude(CurrentPosition);
                double currentLon = body.GetLongitude(CurrentPosition);
                double targetLat = body.GetLatitude(TargetPosition);
                double targetLon = body.GetLongitude(TargetPosition);

                // Convert latitudes and longitudes from degrees to radians
                double lat1 = currentLat * Math.PI / 180;
                double lon1 = currentLon * Math.PI / 180;
                double lat2 = targetLat * Math.PI / 180;
                double lon2 = targetLon * Math.PI / 180;

                // Calculate the difference in longitude
                double deltaLon = lon2 - lon1;

                // Calculate the bearing using the spherical trigonometry formula
                double y = Math.Sin(deltaLon) * Math.Cos(lat2);
                double x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(deltaLon);

                // Calculate the bearing in radians and then convert to degrees
                double bearing = Math.Atan2(y, x) * (180 / Math.PI);

                // Normalize the bearing to 0-360 degrees
                bearing = (bearing + 360) % 360;

                return bearing;
            }
        }

        public HopManager()
        {
        }

        public void Update()
        {
            UpdateCurrentPosition();
            UpdateTargetPosition();
        }

        private void UpdateCurrentPosition()
        {
            CurrentPosition = FlightGlobals.ActiveVessel.GetWorldPos3D();
        }

        private void UpdateTargetPosition()
        {
            if (FlightGlobals.fetch.VesselTarget != null)
            {
                TargetPosition = FlightGlobals.fetch.VesselTarget.GetVessel().GetWorldPos3D();
            }
        }
    }

}