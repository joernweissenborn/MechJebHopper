using UnityEngine;
using MuMech;
using MuMech.Landing;

namespace Hopper
{
    public class AscendStep : AutopilotStep
    {

        private double _targetHeading;
        private double _lastDistance;
        private bool _closeToTarget;
        private double _maxAcceleration;

        private HopGuidanceAscendPilot _ascendPilot;
        public AscendStep(MechJebCore core, double targetHeading) : base(core)
        {
            _targetHeading = targetHeading;
            _ascendPilot = core.GetComputerModule<HopGuidanceAscendPilot>();
            _lastDistance = _ascendPilot.ImpactDistanceToTarget;
            _maxAcceleration = core.thrust.maxAcceleration;
        }

        public override AutopilotStep Drive(FlightCtrlState s)
        {
            if (_ascendPilot.AdaptiveHeading) _targetHeading = _ascendPilot.AdjustedHeading();
            Quaternion attitude = Quaternion.AngleAxis((float)_ascendPilot.AdjustedHeading(), Vector3.up)
                                    * Quaternion.AngleAxis(-(float)45, Vector3.right)
                                    * Quaternion.AngleAxis(-(float)0, Vector3.forward);
            AttitudeReference reference = AttitudeReference.SURFACE_NORTH;
            core.attitude.attitudeTo(attitude, reference, null, true, true, false);
            //core.attitude.attitudeTo(_targetHeading, 45, 0, null, true, true, false, true);

            double impactDistance = _ascendPilot.ImpactDistanceToTarget;
            if (impactDistance > _lastDistance + 5)
            {
                core.thrust.ThrustOff();
                if (_ascendPilot.PerformCourseCorrection) return new CourseCorrectionStep(core);
                return new CoastStep(core);
            }
            _lastDistance = impactDistance;
            core.thrust.targetThrottle = impactDistance < 2000 ? 0.1F : 1F;
            status = $"Hopping at throttle {core.thrust.targetThrottle} at heading {_targetHeading}°";
            return this;

        }
    }

    public class CourseCorrectionStep : AutopilotStep
    {

        bool courseCorrectionBurning = false;
        private HopGuidanceAscendPilot _ascendPilot;

        public CourseCorrectionStep(MechJebCore core) : base(core)
        {
            _ascendPilot = core.GetComputerModule<HopGuidanceAscendPilot>();
        }

        public override AutopilotStep Drive(FlightCtrlState s)
        {

            if (_ascendPilot.ImpactDistanceToTarget < 50) return new CoastStep(core);

            Vector3d deltaV = core.landing.ComputeCourseCorrection(true);

            status = "Performing course correction of about " + deltaV.magnitude.ToString("F1")  + " m/s";

            core.attitude.attitudeTo(deltaV.normalized, AttitudeReference.INERTIAL, null);

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

    public class CoastStep : AutopilotStep
    {
        public CoastStep(MechJebCore core) : base(core)
        {
        }

        public override AutopilotStep OnFixedUpdate()
        {
            if (orbit.timeToAp > orbit.timeToPe) return new FinalDescent(core);
            core.thrust.ThrustOff();
            core.warp.WarpRegularAtRate(100);
            status = "Coasting to apoapsis in " + orbit.timeToAp.ToString("F1") + "s";
            return this;
        }
    }
}