using System;
using System.Collections.Generic;
using static CitizenFX.Core.Native.API;
using CitizenFX.Core;

namespace VoiceGPS_FiveM.Server
{
    public class ServerScript : BaseScript
    {
        public ServerScript()
        {
            RegisterCommand("vgps", new Action<int, List<object>, string>((source, arguments, raw) =>
            {
                var pl = new PlayerList();
                var player = pl[source];

                TriggerClientEvent(player, "vgps:toggleVGPS");
            }), false);
            
            //RegisterCommand("vgpsvol", new Action<int, List<object>, string>((source, arguments, raw) =>
            //{
            //    var pl = new PlayerList();
            //    var player = pl[source];

            //    TriggerClientEvent(player, "vgps:adjustVolume", arguments);
            //}), false);
        }
    }
}