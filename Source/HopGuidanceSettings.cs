using MuMech;
using UnityEngine;

namespace Hopper
{
    public class HopGuidanceSettings : DisplayModule
    {

        private HopGuidanceAscendPilot _ascendPilot;

        public HopGuidanceSettings(MechJebCore core) : base(core)
        {
            ShowInEditor = false;
            ShowInFlight = true;
            _ascendPilot = core.GetComputerModule<HopGuidanceAscendPilot>();
        }

        public override string GetName() => "Hopping Guidance";
        
        public override GUILayoutOption[] WindowOptions() => new[] { GUILayout.Width(300), GUILayout.Height(300) };

        protected override void WindowGUI(int id)
        {
            GUILayout.BeginVertical();

            GUILayout.Label("Hopper Settings");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Current Latitude", GUILayout.ExpandWidth(true));
            GUILayout.Label(_ascendPilot.CurrentLatitude.ToString("F6"));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Current Longitude", GUILayout.ExpandWidth(true));
            GUILayout.Label(_ascendPilot.CurrentLongitude.ToString("F6"));
            GUILayout.EndHorizontal();

            GUILayout.Label("Target Position: " + _ascendPilot.Target.ToString("F2"));
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Target Latitude", GUILayout.ExpandWidth(true));
            GUILayout.Label(_ascendPilot.TargetLatitude.ToString("F6"));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Target Longitude", GUILayout.ExpandWidth(true));
            GUILayout.Label(_ascendPilot.TargetLongitude.ToString("F6"));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Adjusted Target Longitude", GUILayout.ExpandWidth(true));
            GUILayout.Label(_ascendPilot.AdjustedTargetLongitude.ToString("F6"));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Heading");
            GUILayout.Label(_ascendPilot.Heading.ToString("F2") + "°");
            GUILayout.EndVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Adjusted Heading");
            GUILayout.Label(_ascendPilot.AdjustedHeading().ToString("F2") + "°");
            GUILayout.EndHorizontal();

            _ascendPilot.UseCorrectedHeading = GUILayout.Toggle(_ascendPilot.UseCorrectedHeading, "Use Corrected Heading");
            _ascendPilot.AdaptiveHeading = GUILayout.Toggle(_ascendPilot.AdaptiveHeading, "Adaptive Heading");
            _ascendPilot.PerformCourseCorrection = GUILayout.Toggle(_ascendPilot.PerformCourseCorrection, "Perform Course Correction");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Current Impact Latitude", GUILayout.ExpandWidth(true));
            GUILayout.Label(_ascendPilot.PredictedImpact.latitude.ToString("F6"));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Current Impact Longitude", GUILayout.ExpandWidth(true));
            GUILayout.Label(_ascendPilot.PredictedImpact.longitude.ToString("F6"));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Distance to Target");
            GUILayout.Label(_ascendPilot.DistanceToTarget.ToString("F2") + "m");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Impact Disctance to Target");
            GUILayout.Label(_ascendPilot.ImpactDistanceToTarget.ToString("F2") + "m");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Estimated Time of Flight");
            GUILayout.Label(_ascendPilot.EstimateTimeOfFlight().ToString("F2") + "s");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("MAx Acceleration");
            GUILayout.Label(core.thrust.maxAcceleration + "m/s²");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Time Since Hop");
            GUILayout.Label(_ascendPilot.TimeSinceHop.ToString("F2") + "s");
            GUILayout.EndHorizontal();

            GUILayout.Label("Gravity: " + (core.vessel.mainBody.GeeASL * 9.81).ToString("F2") + " m/s²");

            GUILayout.Label("Status: " + _ascendPilot.status);

            GUILayout.Label("Enabled: " + _ascendPilot.enabled);

            if (GUILayout.Button("Hop to Target"))
            {
                _ascendPilot.Hop(this);
            }

            GUILayout.EndVertical();
            base.WindowGUI(id);
        }
    }
}