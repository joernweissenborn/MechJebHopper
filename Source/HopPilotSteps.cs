using UnityEngine;
using MuMech;
using MuMech.Landing;
using System;

namespace MechJebHopper
{
    public class Ascend : AutopilotStep
    {

        private double _targetHeading;
        private readonly bool _startClose;

        private readonly HopPilot _hopPilot;
        public Ascend(MechJebCore core) : base(core)
        {
            _hopPilot = core.GetComputerModule<HopPilot>();
            _targetHeading = _hopPilot.WantedHeading;
            _startClose = _hopPilot.ImpactDistanceToTarget <= HopPilot.CloseDistance * 2;
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
                float targetThrottle = 1.0f;
                if (_hopPilot.RelDistanceToImpactDelta < HopPilot.CloseDistance) {
                    //Interpolate the throttle between 1 and 0.1 as we get closer to the target
                    targetThrottle = Mathf.Lerp(0.1f, 1f, (float)(_hopPilot.RelDistanceToImpactDelta / HopPilot.CloseDistance));
                }
                core.thrust.targetThrottle = targetThrottle;
            }
            status = $"Hopping at throttle {core.thrust.targetThrottle} at heading {_targetHeading:F3}°";
            return this;

        }

        public override AutopilotStep OnFixedUpdate()
        {
            if (_hopPilot.RelDistanceToImpactDelta <= 0)
            {
                core.thrust.ThrustOff();
                if (_hopPilot.PerformCourseCorrection) return new CourseCorrection(core);
                if (!_hopPilot.AscendOnly) return new CoastToApoapsis(core);
                _hopPilot.EndHop();
                return null;
            }
            return this;
        }

    }

    public class CourseCorrection : AutopilotStep
    {

        bool _courseCorrectionBurning = false;
        private readonly HopPilot _hopPilot;

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
                _courseCorrectionBurning = true;
            else if (core.attitude.attitudeAngleFromTarget() > 30)
                _courseCorrectionBurning = false;

            if (_courseCorrectionBurning)
            {
                const double TIMECONSTANT = 2.0;
                core.thrust.ThrustForDV(deltaV.magnitude, TIMECONSTANT);
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

        private readonly HopPilot _ascendPilot;
        public Land(MechJebCore core) : base(core)
        {
            _ascendPilot = core.GetComputerModule<HopPilot>();
        }

        public override AutopilotStep Drive(FlightCtrlState s)
        {
            AutopilotStep nextStep = base.Drive(s);
            if (vessel.LandedOrSplashed) _ascendPilot.EndHop();
            return nextStep;
        }
    }
}