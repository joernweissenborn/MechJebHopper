using System;
using System.Diagnostics;
using MuMech;
using UnityEngine;

namespace MechJebHopper
{
    public class HopPilot : AutopilotModule
    {
        private readonly MechJebModuleRoverController _roverController;
        private readonly MechJebModuleLandingPredictions _landingPredictions;

        public static double CloseDistance => 1000;

        public Vector3 Current => core.vessel.ReferenceTransform.position;
        public double CurrentLatitude => core.vessel.mainBody.GetLatitude(Current);
        public double CurrentLongitude => core.vessel.mainBody.GetLongitude(Current);
        public Vector3 Target => core.vessel.targetObject?.GetTransform().position ?? Vector3.zero;
        public double TargetLatitude => core.vessel.mainBody.GetLatitude(Target);
        public double TargetLongitude => core.vessel.mainBody.GetLongitude(Target);
        public double TargetAltitude => core.vessel.mainBody.GetAltitude(Target);
        public double DistanceToTarget => Vector3d.Distance(Current, Target);
        public double ImpactDistanceToTarget => SurfaceDistance(PredictedImpact.latitude, PredictedImpact.longitude, TargetLatitude, TargetLongitude);

        public double Heading => MuUtils.ClampDegrees360(_roverController.HeadingToPos(Current, Target));
        public bool AdaptiveHeading { get; set; }
        public bool UseCorrectedHeading { get; set; }
        public double CorrectedHeading => MuUtils.ClampDegrees360(_roverController.HeadingToPos(Current, AdjustedTarget()));
        public bool PerformCourseCorrection = false;
        public double WantedHeading => UseCorrectedHeading ? CorrectedHeading : Heading;
        public EditableInt Angle = 45;
        public readonly EditableDouble MaxError = 20;
        public bool AscendOnly = false;

        public Vector3 CurrentRelPosition => core.vessel.mainBody.GetRelSurfacePosition(CurrentLatitude, CurrentLongitude, TargetAltitude);
        public Vector3 TargetRelPosition => core.vessel.mainBody.GetRelSurfacePosition(TargetLatitude, TargetLongitude, TargetAltitude);
        public Vector3 ImpactRelPosition => core.vessel.mainBody.GetRelSurfacePosition(PredictedImpact.latitude, PredictedImpact.longitude, TargetAltitude);
        public Vector3 RelDistanceToTargetVector => TargetRelPosition - CurrentRelPosition;
        public Vector3 RelDistanceToTargetNormalized => RelDistanceToTargetVector.normalized;
        public double RelDistance => RelDistanceToTargetVector.magnitude;
        public Vector3 RelDistanceToImpactVector => ImpactRelPosition - CurrentRelPosition;
        public double RelDistanceToImpact => Vector3.Dot(RelDistanceToImpactVector, RelDistanceToTargetNormalized);
        public double RelDistanceToImpactDelta => RelDistance - RelDistanceToImpact;

        private readonly Stopwatch _hopTimer;
        public double TimeSinceHop => _hopTimer?.Elapsed.TotalSeconds ?? 0;
        public double TimeToLand => _landingPredictions.Result?.endUT - Planetarium.GetUniversalTime() ?? 0;

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

        public HopPilot(MechJebCore core) : base(core)
        {
            _roverController = core.GetComputerModule<MechJebModuleRoverController>();
            _landingPredictions = core.GetComputerModule<MechJebModuleLandingPredictions>();
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
            _hopTimer.Stop();
            _hopTimer.Reset();
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

        private double SurfaceDistance(double lat1, double lon1, double lat2, double lon2)
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