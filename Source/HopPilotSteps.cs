using UnityEngine;
using MuMech;
using MuMech.Landing;
using System;

namespace MechJebHopper
{
    public class Ascend : AutopilotStep
    {

        private double _targetHeading;
        private double _lastDistance;
        private readonly bool _startClose;
        private readonly double _halfDistance;

        private readonly HopPilot _hopPilot;
        public Ascend(MechJebCore core) : base(core)
        {
            _hopPilot = core.GetComputerModule<HopPilot>();
            _lastDistance = _hopPilot.ImpactDistanceToTarget;
            _targetHeading = _hopPilot.WantedHeading;
            _startClose = _lastDistance <= HopPilot.CloseDistance * 2;
            _halfDistance = _lastDistance / 2;
        }

        public override AutopilotStep Drive(FlightCtrlState s)
        {
            if (_hopPilot.AdaptiveHeading) _targetHeading = _hopPilot.WantedHeading;
            Quaternion attitude = Quaternion.AngleAxis((float)_targetHeading, Vector3.up)
                                    * Quaternion.AngleAxis(-(float)_hopPilot.Angle, Vector3.right);
            AttitudeReference reference = AttitudeReference.SURFACE_NORTH;
            core.attitude.attitudeTo(attitude, reference, _hopPilot, true, true, false);

            if (_startClose)
            {
                double maxTwr = vesselState.thrustAvailable / (vesselState.mass * mainBody.GeeASL * 9.81);
                core.thrust.targetThrottle = (float)Math.Min(1, 4 / maxTwr);
            }
            else
            {
                double impactDistance = _hopPilot.ImpactDistanceToTarget;
                core.thrust.targetThrottle = impactDistance < HopPilot.CloseDistance ? 0.1F : 1F;
            }
            status = $"Hopping at throttle {core.thrust.targetThrottle} at heading {_targetHeading}°";
            return this;

        }

        public override AutopilotStep OnFixedUpdate()
        {

            double impactDistance = _hopPilot.ImpactDistanceToTarget;
            double deltaDistance = impactDistance - _lastDistance;
            if (impactDistance < _halfDistance && deltaDistance > _hopPilot.ImpactDelta)
            {
                //Debug.Log("[Hopper] Impact distance increasing, aborting hop.");
                //Debug.Log("[Hopper] Distance: " + impactDistance);
                //Debug.Log("[Hopper] Delta: " + deltaDistance);
                //Debug.Log("[Hopper] Last distance: " + _lastDistance);
                //Debug.Log("[Hopper] Half distance: " + _halfDistance);
                core.thrust.ThrustOff();
                if (_hopPilot.PerformCourseCorrection) return new CourseCorrection(core);
                if (!_hopPilot.AscendOnly) return new CoastToApoapsis(core);
                //Debug.Log("[Hopper] Ending hop.");
                _hopPilot.EndHop();
                return null;
            }
            _lastDistance = impactDistance;
            return this;
        }

    }

    public class CourseCorrection : AutopilotStep
    {

        bool courseCorrectionBurning = false;
        private HopPilot _hopPilot;

        public CourseCorrection(MechJebCore core) : base(core)
        {
            _hopPilot = core.GetComputerModule<HopPilot>();
        }

        public override AutopilotStep Drive(FlightCtrlState s)
        {

            if (_hopPilot.ImpactDistanceToTarget < _hopPilot.MaxError) return new CoastToApoapsis(core);

            Vector3d deltaV = core.landing.ComputeCourseCorrection(true);

            status = "Performing course correction of about " + deltaV.magnitude.ToString("F1") + " m/s";

            core.attitude.attitudeTo(deltaV.normalized, AttitudeReference.INERTIAL, _hopPilot);

            if (core.attitude.attitudeAngleFromTarget() < 2)
                courseCorrectionBurning = true;
            else if (core.attitude.attitudeAngleFromTarget() > 30)
                courseCorrectionBurning = false;

            if (courseCorrectionBurning)
            {
                const double timeConstant = 2.0;
                core.thrust.ThrustForDV(deltaV.magnitude, timeConstant);
            }
            else
            {
                core.thrust.targetThrottle = 0;
            }

            return this;
        }
    }

    public class CoastToApoapsis : AutopilotStep
    {
        public CoastToApoapsis(MechJebCore core) : base(core)
        {
        }

        public override AutopilotStep OnFixedUpdate()
        {
            if (orbit.timeToAp > orbit.timeToPe)
            {
                core.warp.MinimumWarp();
                return new Land(core);
            }
            core.thrust.ThrustOff();
            if (core.node.autowarp) core.warp.WarpRegularAtRate(100);
            status = "Coasting to apoapsis in " + orbit.timeToAp.ToString("F1") + "s";
            return this;
        }
    }

    public class Land : FinalDescent
    {

        private HopPilot _ascendPilot;
        public Land(MechJebCore core) : base(core)
        {
            _ascendPilot = core.GetComputerModule<HopPilot>();
        }

        public override AutopilotStep Drive(FlightCtrlState s)
        {
            var nextStep = base.Drive(s);
            if (vessel.LandedOrSplashed) _ascendPilot.EndHop();
            return nextStep;
        }
    }
}