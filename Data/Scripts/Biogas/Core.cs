using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Timers;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

//using Sandbox.ModAPI.Ingame;

namespace Biogas
{
    [MySessionComponentDescriptor(
        MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation | MyUpdateOrder.Simulation )]
    public class Core : MySessionComponentBase
    {
        // Declarations
        private static readonly string version = "v1.0";

        private static bool _initialized;

        public const ushort CLIENT_ID = 1258;
        public const ushort SERVER_ID = 1259;

        private static int interval = 0;

        private Pooper Poop;

        override public void SaveData()
        {
            Config.Write();
        }

        // Initializers
        private void Initialize( )
        {
            AddMessageHandler( );
            if (!MyAPIGateway.Multiplayer.IsServer) return;
            Config.Load();
            Poop = new Pooper();
        }
        
        // CLIENT hat eine Chatnachricht eingegeben. Prüfe, ob es ein Befehl ist
        public void HandleMessageEntered( string messageText, ref bool sendToOthers )
        {
            byte[] data = null;
            MyLog.Default.WriteLine("HandleMessageEntered " + messageText);

            if (messageText.StartsWith("/biogas"))
            {
                data = Utilities.MessageToBytes(new MessageData()
                {
                    SteamId = MyAPIGateway.Session.Player.SteamUserId,
                    Message = messageText.Replace("/biogas", "").Trim()
                });
                sendToOthers = false;
            }

            if (data != null)
            {
                MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                {
                    MyAPIGateway.Multiplayer.SendMessageToServer(SERVER_ID, data);
                });
            }

        }

        // CLIENT hat Daten vom Server Erhalten. Entweder Chatnachricht oder Dialog
        public void HandleServerData( byte[ ] data )
        {
            MyLog.Default.WriteLine( string.Format( "Received Server Data: {0} bytes", data.Length ) );
            if ( MyAPIGateway.Multiplayer.IsServer && MyAPIGateway.Session.LocalHumanPlayer == null )
                return;

            MessageData item = Utilities.BytesToMessage(data);

            if (item == null)
                return;

            if ( item.Type.Equals("chat") )
            {
                MyAPIGateway.Utilities.ShowMessage(item.Sender, item.Message);
            } else if(item.Type.Equals("dialog"))
            {
                Dialog(item.Message, item.DialogTitle);
            }
        }

        // SERVER hat Daten vom Spieler erhalten - Vermutlich ein Befehl
        public void HandlePlayerData(byte[] data)
        {
            MyLog.Default.WriteLine(string.Format("Received Player Data: {0} bytes", data.Length));
            MessageData request = Utilities.BytesToMessage(data);
            if (request == null)
                return;

            if (request.Message.Equals("reload"))
            {
                MyLog.Default.WriteLine("Reload Command");
                IMyPlayer player = Utilities.GetPlayer(request.SteamId);
                if (player == null || player.PromoteLevel.CompareTo(MyPromoteLevel.Admin) < 0)
                    return;
                Config.Load();
                Utilities.ShowChatMessage("Config reloaded", player.IdentityId);

            }
        }

        // Zeigt dem Spieler eine Dialog an
        public static void Dialog( string message, string title = null )
        {
            if (title == null) title = "Biogas";
            MyAPIGateway.Utilities.ShowMissionScreen(title, "", "", message.Replace("|", "\n\r"), null, "OK");
        }

        public void AddMessageHandler( )
        {
            //register all our events and stuff
            MyAPIGateway.Utilities.MessageEntered += HandleMessageEntered;
            MyAPIGateway.Multiplayer.RegisterMessageHandler( CLIENT_ID, HandleServerData );
            MyAPIGateway.Multiplayer.RegisterMessageHandler( SERVER_ID, HandlePlayerData );
        }

        public void RemoveMessageHandler( )
        {
            //unregister them when the game is closed
            MyAPIGateway.Utilities.MessageEntered -= HandleMessageEntered;
            MyAPIGateway.Multiplayer.UnregisterMessageHandler( CLIENT_ID, HandleServerData );
            MyAPIGateway.Multiplayer.UnregisterMessageHandler( SERVER_ID, HandlePlayerData );
        }

        public void  UpdateBeforeEverySecond()
        {
            // UpdatePoop Stuff
            Poop.Go();
        }

        // Overrides
        public override void UpdateBeforeSimulation( )
        {
            try
            {
                if ( MyAPIGateway.Session == null)
                    return;

                // Run the init
                if ( !_initialized )
                {
                    _initialized = true;
                    Initialize( );
                    return;
                }

                if (MyAPIGateway.Multiplayer.IsServer && interval++ >= 60)
                {
                    interval = 0;
                    UpdateBeforeEverySecond();
                }

            }
            catch ( Exception ex )
            {
                MyLog.Default.WriteLine( string.Format( "UpdateBeforeSimulation(): {0}", ex ) );
            }
        }

        

        public override void UpdateAfterSimulation( )
        {
        }

        protected override void UnloadData( )
        {
            try
            {
                RemoveMessageHandler( );
            }
            catch
            {
            }

            base.UnloadData( );
        }
    }
}