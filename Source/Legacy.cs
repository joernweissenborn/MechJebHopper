using System;
using MuMech;  // MechJeb namespace
using UnityEngine;

namespace Hopper
{
    // This will ensure the mod only runs in flight scenarios
    //[KSPAddon(KSPAddon.Startup.Flight, false)]
    public class HopperController : MonoBehaviour
    {
        private Rect _windowRect;
        private MechJebCore _mechJebCore;
        private MechJebModuleSmartASS _smartASS;
        private MechJebModuleLandingAutopilot _landingAutopilot;
        private HopManager _hopManager;
        private bool _isAscending;
        private bool _inReach;
        private double _lastDistance;
        private double _deltaDistance;

        // Runs when the Flight scene is loaded
        void Start()
        {
            Debug.Log("[HopperController] Flight scene loaded. Initializing...");

            _windowRect = new Rect(100, 100, 200, 300);

            // Get the MechJebCore for the active vessel
            _mechJebCore = GetMechJebCoreForActiveVessel();

            if (_mechJebCore == null)
            {
                Debug.LogError("[HopperController] MechJeb is not installed on the active vessel.");
                return;
            }

            //_mechJebCore.AddComputerModule(new HopGuidanceAscendPilot(_mechJebCore));
            //_mechJebCore.AddComputerModule(new HopGuidanceSettings(_mechJebCore));

            

            _smartASS = GetSmartASSModule();
            if (_smartASS == null)
            {
                Debug.LogError("[HopperController] MechJeb SmartASS module not found.");
            }

            _landingAutopilot = GetLandingAutopilotModule();
            if (_landingAutopilot == null)
            {
                Debug.LogError("[HopperController] MechJeb Landing Autopilot module not found.");
            }

            _hopManager = new HopManager();
        }

        void OnGUI()
        {
            GUI.skin = HighLogic.Skin;

            _windowRect = GUILayout.Window(0, _windowRect, Window, "Hopper Controller");
        }

        private void Window(int id)
        {
            _hopManager.Update();

            DriveAscend();

            GUILayout.BeginVertical();
            GUILayout.Label("Current Position: " + _hopManager.CurrentPosition.ToString("F2"));
            if (_hopManager.HasTarget)
            {
                GUILayout.Label("Target Position: " + _hopManager.TargetPosition.ToString("F2"));
                GUILayout.Label("Distance: " + _hopManager.Distance().ToString("F2") + "m");
                GUILayout.Label("Heading: " + _hopManager.Heading.ToString("F2") + "°");
                GUILayout.Label("Need Apoapsis: " + _hopManager.Apoapsis.ToString("F2") + "m");
                GUILayout.Label("Current Apoapsis: " + CurrentApoapsis().ToString("F2") + "m");
                GUILayout.Label("Impact Position: " + ImpactPosition().ToString("F2"));
                GUILayout.Label("Impact Distance: " + ImpactDistance().ToString("F2") + "m");
                GUILayout.Label("Is Ascending: " + _isAscending);
                GUILayout.Label("Delta Distance: " + _deltaDistance.ToString("F5") + "m");

                if (GUILayout.Button("Hop To Target"))
                {
                    SetupSmartAssForAscend();
                    _isAscending = true;
                }
            }
            else
            {
                GUILayout.Label("No target selected.");
            }

            GUILayout.EndVertical();
        }

        private void SetupSmartAssForAscend()
        {
            if (_smartASS == null) return;

            _smartASS.mode = MechJebModuleSmartASS.Mode.SURFACE;
            _smartASS.target = MechJebModuleSmartASS.Target.SURFACE;
            _smartASS.srfHdg = _hopManager.Heading;
            _smartASS.srfPit = 45;
            _smartASS.Engage();
            _mechJebCore.attitude.SetAxisControl(true, true, false);
        }

        private Vector3 ImpactPosition()
        {
            if (!_landingAutopilot.PredictionReady) return Vector3.zero;
            return _landingAutopilot.Prediction.WorldEndPosition();
        }
        private double ImpactDistance()
        {
            //calculate surface distance to impact point
            if (!_landingAutopilot.PredictionReady) return 0;
            CelestialBody body = FlightGlobals.ActiveVessel.mainBody;

            // Convert latitude and longitude to radians
            double lat1 = _landingAutopilot.Prediction.endPosition.latitude * Math.PI / 180;
            double lon1 = _landingAutopilot.Prediction.endPosition.longitude * Math.PI / 180;
            double lat2 = body.GetLatitude(_hopManager.TargetPosition) * Math.PI / 180;
            double lon2 = body.GetLongitude(_hopManager.TargetPosition) * Math.PI / 180;

            // Use the Haversine formula to calculate the great-circle distance
            double dLat = lat2 - lat1;
            double dLon = lon2 - lon1;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            // Calculate the distance
            double distance = body.Radius * c;

            return distance;

        }

        private void SetThrottle(float throttle) => FlightInputHandler.state.mainThrottle = throttle;
        private double CurrentApoapsis() => FlightGlobals.ActiveVessel.orbit.ApA;

        private void DriveAscend()
        {
            if (!_isAscending) return;
            double distance = ImpactDistance();
            _deltaDistance = _lastDistance - distance;
            _lastDistance = distance;
            if (distance < 1000) _inReach = true;
            if (distance > 1000 && _inReach)
            {
                _isAscending = false;
                _inReach = false;
                SetThrottle(0);
            }
            else
            {
                SetThrottle(distance > 5000 ? 0.5f : 0.1F);
            }
        }

        // This method retrieves the MechJebCore instance from the active vessel
        private MechJebCore GetMechJebCoreForActiveVessel()
        {
            if (FlightGlobals.ActiveVessel == null)
            {
                Debug.LogError("[HopperController] No active vessel.");
                return null;
            }

            Debug.Log("[HopperController] Retrieving MechJebCore from the active vessel.");

            // Iterate through all parts of the active vessel
            foreach (Part part in FlightGlobals.ActiveVessel.parts)
            {
                // Check if any part has the MechJebCore module
                foreach (PartModule pm in part.Modules)
                {
                    if (pm is MechJebCore mechJebCore)
                    {
                        Debug.Log("[HopperController] MechJebCore found on the active vessel.");
                        return mechJebCore;
                    }
                }
            }
            Debug.LogError("[HopperController] MechJebCore not found on the active vessel.");
            return null;
        }

        private MechJebModuleSmartASS GetSmartASSModule()
        {
            if (_mechJebCore == null) return null;

            return _mechJebCore.GetComputerModule<MechJebModuleSmartASS>();
        }

        private MechJebModuleLandingAutopilot GetLandingAutopilotModule()
        {
            if (_mechJebCore == null) return null;

            return _mechJebCore.GetComputerModule<MechJebModuleLandingAutopilot>();
        }
    }
}