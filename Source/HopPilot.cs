using System;
using System.Diagnostics;
using MuMech;
using UnityEngine;

namespace Hopper
{
    public class HopPilot : AutopilotModule
    {
        private readonly MechJebModuleRoverController _roverController;
        private readonly MechJebModuleLandingPredictions _landingPredictions;

        public static double CloseDistance => 1000;

        public Vector3 Current => core.vessel.ReferenceTransform.position;
        public double CurrentLatitude => core.vessel.mainBody.GetLatitude(Current);
        public double CurrentLongitude => core.vessel.mainBody.GetLongitude(Current);
        public double CurrentAltitude => core.vessel.mainBody.GetAltitude(Current);
        public Vector3 Target => core.vessel.targetObject?.GetTransform().position ?? Vector3.zero;
        public double TargetLatitude => core.vessel.mainBody.GetLatitude(Target);
        public double TargetLongitude => core.vessel.mainBody.GetLongitude(Target);
        public double TargetAltitude => core.vessel.mainBody.GetAltitude(Target);
        public double AdjustedTargetLongitude => core.vessel.mainBody.GetLongitude(AdjustedTarget());
        public double DistanceToTarget => Vector3d.Distance(Current, Target);
        public bool UseCorrectedHeading { get; set; }
        public bool AdaptiveHeading { get; set; }
        public bool PerformCourseCorrection = false;
        public double WantedHeading => UseCorrectedHeading ? AdjustedHeading() : Heading;
        public readonly EditableInt Angle = 45;
        public readonly EditableDouble MaxError = 20;
        public readonly EditableDouble ImpactDelta = 0;
        public bool AscendOnly = false;

        // a timer to keep track of the time since starting the hop
        private readonly Stopwatch _hopTimer;
        public double TimeSinceHop => _hopTimer?.Elapsed.TotalSeconds ?? 0;
        public double TimeToLand => _landingPredictions.Result != null ? _landingPredictions.Result.endUT - Planetarium.GetUniversalTime() : 0;

        public double Heading => _roverController.HeadingToPos(Current, Target);
        public AbsoluteVector PredictedImpact
        {
            get
            {
                if (_landingPredictions.Result != null)
                {
                    return _landingPredictions.Result.endPosition;
                }
                return new AbsoluteVector { latitude = CurrentLatitude, longitude = CurrentLongitude };
            }
        }

        public double ImpactDistanceToTarget => surfaceDistance(PredictedImpact.latitude, PredictedImpact.longitude, TargetLatitude, TargetLongitude);

        public HopPilot(MechJebCore core) : base(core)
        {
            _roverController = core.GetComputerModule<MechJebModuleRoverController>();
            if (_roverController == null)
            {
                throw new Exception("[HopGuidanceAscendPilot] Rover controller not found.");
            }
            _landingPredictions = core.GetComputerModule<MechJebModuleLandingPredictions>();
            if (_landingPredictions == null)
            {
                throw new Exception("[HopGuidanceAscendPilot] Landing predictions not found.");
            }
            _hopTimer = new Stopwatch();
        }

        public override void OnModuleEnabled()
        {
            //core.attitude.users.Add(this);
            core.thrust.users.Add(this);
            _landingPredictions.users.Add(this);
        }

        public override void OnModuleDisabled()
        {
            core.thrust.ThrustOff();
            core.thrust.users.Remove(this);
            core.attitude.attitudeDeactivate();
            core.attitude.users.Remove(this);
            _landingPredictions.users.Remove(this);
        }

        public void Hop(object controller)
        {
            users.Add(controller);
            _hopTimer.Start();
            setStep(new Ascend(core));
        }

        public void EndHop()
        {
            _hopTimer.Stop();
            users.Clear();
            setStep(null);
        }

        public double EstimateTimeOfFlight()
        {
            double gravity = core.vessel.mainBody.GeeASL * 9.81; // m/s^2
            double velocity = Math.Sqrt(gravity * DistanceToTarget); // m/s
            return 2 * velocity / (gravity * Math.Sqrt(2)); // seconds
        }

        public double EstimateTimeOfFlight2()
        {
            double mu = core.vessel.mainBody.gravParameter; // Gravitational parameter (m^3/s^2)
            double radiusPlanet = core.vessel.mainBody.Radius; // meters

            double periapsis = radiusPlanet + CurrentAltitude; // meters
            double apoapsis = periapsis + (DistanceToTarget * Math.Tan(Angle * UtilMath.Deg2Rad)); // meters

            double semiMajorAxis = (periapsis + apoapsis) / 2; // meters

            return Math.PI * Math.Sqrt(Math.Pow(semiMajorAxis, 3) / mu); // seconds
        }

        public Vector3 AdjustedTarget()
        {
            double rotationPeriod = core.vessel.mainBody.rotationPeriod; // seconds
            double timeOfFlight = EstimateTimeOfFlight(); // seconds

            Vector3 currentRadialVector = Current - core.vessel.mainBody.position;
            Vector3 targetRadialVector = Target - core.vessel.mainBody.position;

            double planetRotationAngle = 360 * timeOfFlight / rotationPeriod; // degrees
            Vector3 rotationAxis = core.vessel.mainBody.transform.up;
            Quaternion planetRotation = Quaternion.AngleAxis((float)planetRotationAngle, rotationAxis);

            Vector3 currentRadialVectorRotated = planetRotation * currentRadialVector;
            Vector3 targetRadialVectorRotated = planetRotation * targetRadialVector;

            Vector3 relativeMovement = targetRadialVectorRotated - currentRadialVectorRotated;

            return Target + relativeMovement;
        }
        public double AdjustedHeading() => _roverController.HeadingToPos(Current, AdjustedTarget());

        private double surfaceDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Haversine formula to calculate the distance between two points on the surface of a sphere
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double radius = core.vessel.mainBody.Radius; // radius of the celestial body
            return radius * c; // distance in meters
        }
    }

}