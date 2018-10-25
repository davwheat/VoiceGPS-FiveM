using System;
using System.Reflection;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using System.Threading.Tasks;
using CitizenFX.Core.UI;

namespace VoiceGPS_FiveM.Client
{
    public class ClientScript : BaseScript
    {
        private static Ped _playerPed;
        private bool _justPlayed1000M, _justPlayed200M, _justPlayedFollowRoad, _justPlayedImmediate,  _playedStartDriveAudio, _justPlayedRecalc, _voiceGpsEnabled, _welcomeShowed;
        private bool _justPlayedArrived = true;
        private int _lastDirection;

        // User editable variables

        // Default volume (between 0.1 and 1.0)
        private double _audioVolume = 0.7;

        private const Control ToggleVGPSKeybind = Control.DropAmmo; // F10
        private const bool IsKeybindEnabled = true;

        private const bool IsCommandEnabled = true;


        public ClientScript()
        {
            Chat("VGPS Loaded");
            EventHandlers.Add("vgps:toggleVGPS",
                IsCommandEnabled
                    ? new Action(ToggleVgps)
                    : new Action(() => ShowNotification("~r~The command has been disabled. Please use the set keybind instead.")));

            // UNCOMMENT BELOW LINE TO REMOVE THE WELCOME MESSAGE
            //_welcomeShowed = true;

            Tick += OnTick;


        }

        private async Task OnTick()
        {
            _playerPed = GetPlayerPed();
            
            if (!_welcomeShowed)
            {
                ShowNotification("~b~VoiceGPS~w~ | ~o~by github.com/davwheat");
                ShowNotification("~b~To enable GPS speech, type ~o~/vgps");
                _welcomeShowed = true;
            }

            if (IsKeybindEnabled && Game.IsControlJustReleased(1, ToggleVGPSKeybind))
            {
                ToggleVgps();
            }

            if (_playerPed.IsInVehicle() && _voiceGpsEnabled)
            {
                var blip = World.GetWaypointBlip();

                if (!Game.IsWaypointActive || blip == null)
                {
                    if (_playedStartDriveAudio && !_justPlayedArrived)
                    {
                        PlayAudio("arrived");
                        _justPlayedArrived = true;
                        await Delay(2000);
                    }
#if DEBUG
                    //Chat("a");
#endif

                    _playedStartDriveAudio = false;

                    await Delay(1000);
                    return;
                }

                _justPlayedArrived = false;
                if (!_playedStartDriveAudio)
                {
#if DEBUG
                    //Chat("b");
#endif
                    PlayAudio("start");
                    await Delay(2600);
                    _playedStartDriveAudio = true;
                    return;
                }

                _justPlayedArrived = false;

                var directionInfo = GenerateDirectionsToCoord(blip.Position);

                var dist = (int)Math.Round(directionInfo.Item3);
                var dir = directionInfo.Item1;

                if (dir > 8 || dir < 0)
                {
                    await Delay(1000);
                    return;
                }

#if DEBUG
                //ShowNotification(DirectionToString(dir));
#endif

                //0 : You arrived at your destination
                //1 : Going The Wrong wAY...recalculating
                //2: Follow this lane and wait for more instructions
                //3: On the next intersection, turn left. (distance on p6)
                //4: On the next intersection, turn right. (distance on p6)
                //5: On the next intersection, go straight. (distance on p6)
                //6: Take the next return to the left. (distance on p6)
                //7: Take the next return to the right. (distance on p6)
                //8: NO idea...similar to 6 in some way

                if (_lastDirection != dir)
                {
                    _justPlayed200M = false;
                    _justPlayedImmediate = false;
                    _justPlayed1000M = false;
                }

                if (dist > 175 && dist < 300 && !_justPlayed200M && dir != 5)
                {
                    PlayAudio("200m");
                    _justPlayed200M = true;
                    await Delay(2300);
                    PlayDirectionAudio(dir);
                }

                if (dist > 500 && dist < 1000 && !_justPlayed1000M && dir != 5)
                {
                    PlayAudio("1000m");
                    _justPlayed1000M = true;
                    await Delay(2300);
                    PlayDirectionAudio(dir);
                }

                if (!_justPlayedImmediate && dist < 55 && dist > 20 && dir != 5)
                {
                    _justPlayedImmediate = true;
                    PlayDirectionAudio(dir);
                }
                else if (dist < 20 && dir != 5)
                {
                    _lastDirection = 0;
                    _justPlayed1000M = false;
                    _justPlayed200M = false;
                    _justPlayedImmediate = true;
                }

                if (dir == 2 && !_justPlayedFollowRoad && _lastDirection != 5)
                {
                    _justPlayedFollowRoad = true;
                    //PlayAudio("continue");
                }
                else if (dir != 2)
                {
                    _justPlayedFollowRoad = false;
                }

                if (dir == 1 && !_justPlayedRecalc)
                {
                    _justPlayedRecalc = true;
                    PlayAudio("recalculating");
                    await Delay(3000);
                }
                else if (dir != 1)
                {
                    _justPlayedRecalc = false;
                }

                _lastDirection = dir;
                _justPlayedArrived = true;

                //ShowNotification(DirectionToString(dir));
            }

        }

        private string DirectionToString(int direction)
        {
            switch (direction)
            {
                default:
                    return $"Unknown ({direction})";
                case 0:
                    return $"You have arrived (0)";
                case 1:
                    return $"Recalculating (1)";
                case 2:
                    return $"Follow the road (2)";
                case 3:
                    return $"Left at next junction (3)";
                case 4:
                    return $"Right at next junction (4)";
                case 5:
                    return $"Straight at next junction (5)";
                case 6:
                    return $"Keep left (6)";
                case 7:
                    return $"Keep right (7)";
                case 8:
                    return $"Unknown (8)";
            }
        }

        private void PlayDirectionAudio(int dir)
        {
            //Chat("Attempting to play sound.");
            switch (dir)
            {
                default:
                    // Anything NOT known
                    Chat(dir.ToString());
                    break;

                case 6:
                    // Not 100%
                    PlayAudio("keepleft");
                    break;

                case 7:
                    // Not 100%
                    PlayAudio("keepright");
                    break;

                case 3:
                    // Turn left at next intersection
                    PlayAudio("turnleft");
                    break;

                case 4:
                    // Turn right at next intersection
                    PlayAudio("turnright");
                    break;

                case 5:
                    // Straight ahead at next intersection
                    // Played way too much
                    //PlayAudio("straight");
                    break;

                case 1:
                    // Driver went wrong way -- remaking route
                    PlayAudio("recalculating");
                    break;
            }
        }

        void ToggleVgps()
        {
            _voiceGpsEnabled = !_voiceGpsEnabled;

            if (!_voiceGpsEnabled)
            {
                _lastDirection = 0;
                _justPlayed200M = _justPlayedArrived = _justPlayedFollowRoad =
                    _justPlayedImmediate = _justPlayedRecalc = _justPlayed1000M = false;
            }

            ShowNotification(_voiceGpsEnabled ? "Voice GPS ~g~ENABLED" : "Voice GPS ~r~DISABLED");
        }

        public Tuple<int, float, float> GenerateDirectionsToCoord(Vector3 position)
        {
            OutputArgument f4 = new OutputArgument(), f5 = new OutputArgument(), f6 = new OutputArgument();

            Function.Call<int>(Hash.GENERATE_DIRECTIONS_TO_COORD, position.X, position.Y, position.Z, true, f4, f5, f6);

            return new Tuple<int, float, float>(
                f4.GetResult<int>(),
                f5.GetResult<float>(),
                f6.GetResult<float>()
            );
        }

        public Ped GetPlayerPed()
        {
            return Game.PlayerPed;

            //var ped = new OutputArgument();
            //try
            //{
            //    Function.Call<Ped>(Hash.GET_PLAYER_PED, -1, ped);
            //    return ped.GetResult<Ped>();
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e);
            //    return null;
            //}
        }

        private void Chat(string msg) =>
            TriggerEvent("chatMessage", "VoiceGPS", new[] { 255, 255, 255 }, msg);

        private void ShowNotification(string msg, bool blinking = false) =>
            Screen.ShowNotification(msg, blinking);


        private void PlayAudio(string filename)
        {
            var json =
                $"{{\"type\":\"playGPSSound\",\"audioFile\":\"{filename}\",\"volume\":{_audioVolume}}}";
#if DEBUG
            Debug.WriteLine(json);
#endif

            API.SendNuiMessage(json);
        }
    }
}
