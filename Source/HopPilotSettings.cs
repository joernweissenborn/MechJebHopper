using KSP.Localization;
using MuMech;
using UnityEngine;

namespace MechJebHopper
{
    public class HopPilotSettings : DisplayModule
    {

        private readonly HopPilot _hopPilot;

        public HopPilotSettings(MechJebCore core) : base(core)
        {
            ShowInEditor = false;
            ShowInFlight = true;
            _hopPilot = core.GetComputerModule<HopPilot>();
        }

        public override string GetName() => "Hop Pilot";

        public override GUILayoutOption[] WindowOptions() => new[] { GUILayout.Width(300), GUILayout.Height(300) };

        protected override void WindowGUI(int id)
        {
            GUILayout.BeginVertical();

            DrawTargetGui();

            core.node.autowarp = GUILayout.Toggle(core.node.autowarp, "Auto Warp");


            GuiUtils.SimpleTextBox("Launch Angle", _hopPilot.Angle, "°");
            _hopPilot.Angle = Mathf.Clamp(_hopPilot.Angle, 0, 90);

            GuiUtils.ToggledTextBox(ref _hopPilot.PerformCourseCorrection, "Course Correction", _hopPilot.MaxError, "Max Error[m]", width: 30);

            _hopPilot.AscendOnly = GUILayout.Toggle(_hopPilot.AscendOnly, "Ascend Only");


            GUILayout.FlexibleSpace();

            if (_hopPilot.enabled)
            {
                GuiUtils.SimpleLabel("Time Since Hop", GuiUtils.TimeToDHMS(_hopPilot.TimeSinceHop));
                GuiUtils.SimpleLabel("Time to Land", GuiUtils.TimeToDHMS(_hopPilot.TimeToLand));
                GuiUtils.SimpleLabel("Impact Disctance to Target", _hopPilot.ImpactDistanceToTarget.ToString("F2") + "m");
                GuiUtils.SimpleLabel("Step", _hopPilot.CurrentStep != null ? _hopPilot.CurrentStep.GetType().Name : "N/A");
                GuiUtils.SimpleLabel("Status", _hopPilot.status);

                if (GUILayout.Button("End Hop")) _hopPilot.EndHop();
            }
            else
            {
                if (GUILayout.Button("Hop to Target")) _hopPilot.Hop(this);
            }

            GUILayout.EndVertical();
            base.WindowGUI(id);
        }

        private void DrawTargetGui()
        {
            if (core.target.PositionTargetExists)
            {
                void MoveByMeter(ref EditableAngle angle, double distance, double Alt)
                {
                    double angularDelta = distance * UtilMath.Rad2Deg / (Alt + mainBody.Radius);
                    angle += angularDelta;
                }

                double asl = core.vessel.mainBody.TerrainAltitude(core.target.targetLatitude, core.target.targetLongitude);
                GUILayout.Label(Localizer.Format("#MechJeb_LandingGuidance_label1"));//Target coordinates:

                GUILayout.BeginHorizontal();
                core.target.targetLatitude.DrawEditGUI(EditableAngle.Direction.NS);
                if (GUILayout.Button("▲"))
                {
                    MoveByMeter(ref core.target.targetLatitude, 10, asl);
                }
                GUILayout.Label("10m");
                if (GUILayout.Button("▼"))
                {
                    MoveByMeter(ref core.target.targetLatitude, -10, asl);
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                core.target.targetLongitude.DrawEditGUI(EditableAngle.Direction.EW);
                if (GUILayout.Button("◄"))
                {
                    MoveByMeter(ref core.target.targetLongitude, -10, asl);
                }
                GUILayout.Label("10m");
                if (GUILayout.Button("►"))
                {
                    MoveByMeter(ref core.target.targetLongitude, 10, asl);
                }
                GUILayout.EndHorizontal();

                GuiUtils.SimpleLabel("Distance to Target", _hopPilot.DistanceToTarget.ToString("F2") + "m");

                GuiUtils.SimpleLabel("Heading", _hopPilot.Heading.ToString("F2") + "°");
                _hopPilot.AdaptiveHeading = GUILayout.Toggle(_hopPilot.AdaptiveHeading, "Adaptive Heading");
                _hopPilot.UseCorrectedHeading = GUILayout.Toggle(_hopPilot.UseCorrectedHeading, "Use Corrected Heading");
                if (_hopPilot.UseCorrectedHeading)
                {
                    GuiUtils.SimpleLabel("Corrected Heading", _hopPilot.CorrectedHeading.ToString("F2") + "°");
                    GuiUtils.SimpleLabel("Estimated Time of Flight", _hopPilot.EstimateTimeOfFlight().ToString("F2") + "s");
                }
            }
            else
            {
                if (GUILayout.Button(Localizer.Format("#MechJeb_LandingGuidance_button1")))//Enter target coordinates
                {
                    core.target.SetPositionTarget(mainBody, core.target.targetLatitude, core.target.targetLongitude);
                }
            }

            if (GUILayout.Button(Localizer.Format("#MechJeb_LandingGuidance_button2"))) core.target.PickPositionTargetOnMap();//Pick target on map

        }
    }
}