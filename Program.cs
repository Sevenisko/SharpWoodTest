using System;
using Sevenisko.SharpWood;
using System.IO;
using System.Linq;
using System.Timers;
using EventHandler = Sevenisko.SharpWood.Oakwood.EventHandler;
using Timer = System.Timers.Timer;
using System.Threading;

namespace Sevenisko.SharpWood.Test
{
    public class OakPlayerData
    {
        public int TpaID = -1;
    }

    class SWTestGamemode
    {
        public static void Loop(Action action)
        {
            while (true)
            {
                action();
            }
        }
        public static void Repeat(int count, Action action)
        {
            for (int i = 0; i < count; i++)
            {
                action();
            }
        }

        static EventHandler _handler;

        #region Initialization entry
        static void Main(string[] args)
        {
            OakwoodEvents.OnStart += OnGMStart;
            OakwoodEvents.OnStop += OnGMStop;
            OakwoodEvents.OnPlayerConnect += OnPlayerConnect;
            OakwoodEvents.OnPlayerDisconnect += OnPlayerDisconnect;
            OakwoodEvents.OnPlayerDeath += OnPlayerDeath;
            OakwoodEvents.OnPlayerChat += OnPlayerChat;
            OakwoodEvents.OnPlayerKeyDown += OnPlayerKey;
            OakwoodEvents.OnLog += OnConsoleLog;

            Thread oakwoodThread = new Thread(() => Oakwood.CreateClient("ipc://oakwood-inbound", "ipc://oakwood-outbound"));

            oakwoodThread.Start();

            OakwoodCommandSystem.RegisterEvent("unknownCommand", PlUnknownCmd);
        }

        #endregion

        #region Events
        static void OnGMStart()
        {
            Console.WriteLine("Gamemode has successfully started.");
            Oakwood.ConLog("Testing gamemode has been started.");
        }

        static void OnGMStop()
        {
            Oakwood.ConLog("Gamemode has been stopped.");
            Console.WriteLine("Gamemode has been stopped.");
        }

        private static void OnConsoleLog(DateTime time, string source, string message)
        {
            Console.WriteLine($"[{time.ToString("HH:mm:ss")} - {source}] {message}");
        }

        static void OnPlayerConnect(OakwoodPlayer player)
        {
            if (player.IsValid())
            {
                foreach (OakwoodPlayer p in Oakwood.Players)
                {
                    p.HUD.Message($"{player.Name} joined the game.", OakColor.White);
                }

                OakPlayerData data = new OakPlayerData();

                player.PlayerData = data;

                player.SpawnTempWeapons();

                player.Spawn(new OakVec3(-2136.182f, -5.768807f, -521.3138f), 90.0f);

                player.HUD.Announce("Welcome to SharpWood testing server!", 4.5f);
            }
        }

        private static void OnPlayerDisconnect(OakwoodPlayer player)
        {
            if (player.IsValid())
            {
                foreach (OakwoodPlayer p in Oakwood.Players)
                {
                    p.HUD.Message($"{player.Name} left the game.", OakColor.White);
                }
            }
        }

        private static void OnPlayerChat(OakwoodPlayer player, string message)
        {
            if (player.IsValid())
            {
                Oakwood.SendChatMessage($"[CHAT] {player.Name}: {message}");
                Oakwood.ConLog($"[CHAT] {player.Name}: {message}");
            }
        }

        private static void OnPlayerDeath(OakwoodPlayer player, OakwoodPlayer killer)
        {
            if (player.IsValid())
            {
                foreach (OakwoodPlayer p in Oakwood.Players)
                {
                    if(player == killer)
                    {
                        p.HUD.Message($"{player.Name} died.", OakColor.White);
                    }
                    else
                    {
                        p.HUD.Message($"{player.Name} was killed by {killer.Name}.", OakColor.White);
                    }
                }

                player.HUD.Announce("Wasted", 4.85f);

                Timer spawnTimer = new Timer();
                spawnTimer.Interval = 5000;
                spawnTimer.AutoReset = false;
                spawnTimer.Elapsed += (object sender, ElapsedEventArgs e) =>
                {
                    Timer waitTimer = new Timer();
                    player.HUD.Fade(OakwoodFade.FadeIn, 2500, OakColor.Black);
                    waitTimer.Interval = 2500;
                    waitTimer.AutoReset = false;
                    waitTimer.Elapsed += (object snd, ElapsedEventArgs ea) =>
                    {
                        player.SpawnTempWeapons();
                        player.Health = 200.0f;
                        player.Spawn(new OakVec3(-759.3801f, 13.24883f, 761.6967f), 180.0f);
                        player.HUD.Fade(OakwoodFade.FadeOut, 2500, OakColor.Black);
                    };
                    waitTimer.Start();
                };

                spawnTimer.Start();
            }
        }

        private static void PlUnknownCmd(object[] args)
        {
            OakwoodPlayer player = (OakwoodPlayer)args[0];
            string cmd = args[1].ToString();

            player.SendMessage($"[CMD] Command '{cmd}' was not found.");
        }

        private static void OnPlayerKey(OakwoodPlayer player, VirtualKey key)
        {
            // Do nothing
        }

        #endregion

        #region Commands
        [Command("getcar", "Gets current car information")]
        static void GetCurrentVehicle(OakwoodPlayer player, object[] args)
        {
            OakwoodVehicle vehicle = player.Vehicle;
            if (vehicle != null)
            {
                player.HUD.Message($"Vehicle ID: {vehicle.ID}", OakColor.White);
                player.HUD.Message($"{vehicle.Name}", OakColor.White);
            }
            else
            {
                player.HUD.Message("You're not inside any car!", OakColor.White);
            }
        }

        [Command("kill", "Kills you")]
        static void Kill(OakwoodPlayer player, object[] args)
        {
            foreach (OakwoodPlayer p in Oakwood.Players)
            {
                p.HUD.Message($"{player.Name} commited suicide.", OakColor.White);
            }
            player.HUD.Fade(OakwoodFade.FadeIn, 500, OakColor.Red);
            player.SpawnTempWeapons();
            player.Health = 200.0f;
            player.Spawn(new OakVec3(-759.3801f, 13.24883f, 761.6967f), 180.0f);
            player.HUD.Fade(OakwoodFade.FadeOut, 500, OakColor.Red);
        }

        [Command("warp", "Warps you to the place")]
        static void Warp(OakwoodPlayer player, object[] args)
        {
            if (args.Length == 0)
            {
                string List = "";

                foreach (string f in Directory.GetFiles(Environment.CurrentDirectory + @"\Warps"))
                {
                    List += $"{Path.GetFileNameWithoutExtension(f)}, ";
                }

                player.SendMessage($"[WARPS ({Directory.GetFiles(Environment.CurrentDirectory + @"\Warps").Length})] {List}");
                return;
            }

            string locName = args[0].ToString();

            string file = Environment.CurrentDirectory + @"\Warps\" + $"{locName}.txt";

            if (File.Exists(file))
            {
                player.HUD.Message($"Warping you to '{locName}'...", 0xFFFFFF);
                string[] pos = File.ReadAllText(file).Split(';');
                OakVec3 newPos = new OakVec3(float.Parse(pos[0]), float.Parse(pos[1]), float.Parse(pos[2]));
                OakVec3 newDir = new OakVec3(float.Parse(pos[3]), float.Parse(pos[4]), float.Parse(pos[5]));
                player.Position = newPos;
                player.Direction = newDir;
            }
            else
            {
                player.HUD.Message($"Warp '{locName}' doesn't exist!", 0xFF0000);
            }
        }

        [Command("delwarp", "Deletes a warp")]
        static void DeleteWarp(OakwoodPlayer player, object[] args)
        {
            if (args.Length == 0)
            {
                player.SendMessage("[USAGE] /deletewarp <locName>");
                return;
            }

            string locName = args[0].ToString();

            string file = Environment.CurrentDirectory + @"\Warps\" + $"{locName}.txt";

            if (File.Exists(file))
            {
                File.Delete(file);
                player.HUD.Message($"Warp '{locName}' deleted.", OakColor.White);
            }
            else
            {
                player.HUD.Message($"Warp '{locName}' doesn't exist!", OakColor.Red);
            }
        }

        [Command("updatewarp", "Updates a warp")]
        static void EditWarp(OakwoodPlayer player, object[] args)
        {
            if (args.Length == 0)
            {
                player.SendMessage("[USAGE] /updatewarp <locName>");
                return;
            }

            string locName = args[0].ToString();

            string file = Environment.CurrentDirectory + @"\Warps\" + $"{locName}.txt";

            if (!Directory.Exists(Path.GetDirectoryName(file)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file));
            }

            if (File.Exists(file))
            {
                OakVec3 playerPos = player.Position;
                OakVec3 playerDir = player.Direction;

                if (playerPos.x != 0 && playerPos.y != 0 && playerPos.z != 0 && playerDir.x != 0 && playerDir.z != 0)
                {
                    File.WriteAllText(file, $"{playerPos.x};{playerPos.y};{playerPos.z};{playerDir.x};{playerDir.y};{playerDir.z}");

                    player.HUD.Message($"Warp '{locName}' updated!", 0xFFFFFF);
                }
                else
                {
                    player.HUD.Message("Cannot create warp!", 0xFF0000);
                }
            }
            else
            {
                player.HUD.Message($"Warp '{locName}' doesn't exist!", OakColor.Red);
            }
        }

        [Command("createwarp", "Creates a warp")]
        static void CreateWarp(OakwoodPlayer player, object[] args)
        {
            if (args.Length == 0)
            {
                player.SendMessage("[USAGE] /createwarp <locName>");
                return;
            }

            string locName = args[0].ToString();

            string file = Environment.CurrentDirectory + @"\Warps\" + $"{locName}.txt";

            if (!Directory.Exists(Path.GetDirectoryName(file)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file));
            }

            if (!File.Exists(file))
            {
                OakVec3 playerPos = player.Position;
                OakVec3 playerDir = player.Direction;

                if (playerPos.x != 0 && playerPos.y != 0 && playerPos.z != 0 && playerDir.x != 0 && playerDir.z != 0)
                {
                    File.WriteAllText(file, $"{playerPos.x};{playerPos.y};{playerPos.z};{playerDir.x};{playerDir.y};{playerDir.z}");

                    player.HUD.Message($"Warp '{locName}' created!", 0xFFFFFF);
                }
                else
                {
                    player.HUD.Message("Cannot create warp!", 0xFF0000);
                }
            }
            else
            {
                player.HUD.Message($"Warp '{locName}' already exists!", OakColor.Red);
            }
        }

        [Command("clearchat", "Clears a chat")]
        static void ClearChatCommand(OakwoodPlayer player, object[] args)
        {
            for (int x = 0; x < 24; x++)
            {
                player.SendMessage("\n");
            }

            player.HUD.Message("Chat cleared.", 0xFFFFFF);
        }

        [Command("delcar", "Deletes a car")]
        static void DelCarCommand(OakwoodPlayer player, object[] args)
        {
            OakwoodVehicle car = player.Vehicle;

            if (car != null)
            {
                player.VehicleManipulation.Remove(car);
                car.Despawn();
                player.HUD.Message("Vehicle successfully removed.", 0xFFFFFF);
            }
            else
            {
                player.HUD.Message("You're not inside any vehicle!", 0xFF0000);
            }
        }

        [Command("heal", "Heals you")]
        static void HealCommand(OakwoodPlayer player, object[] args)
        {
            player.Health = 200.0f;
        }

        [Command("repair", "Repairs your car")]
        static void RepairCommand(OakwoodPlayer player, object[] args)
        {
            OakwoodVehicle car = player.Vehicle;

            if (car != null)
            {
                car.Repair();
                player.HUD.Message("Vehicle successfully repaired.", 0xFFFFFF);
            }
            else
            {
                player.HUD.Message("You're not inside any vehicle!", 0xFF0000);
            }
        }

        [Command("pm", "Sends a private message")]
        static void PmCommand(OakwoodPlayer player, object[] args)
        {
            if (args.Length == 0)
            {
                player.SendMessage("[USAGE] /pm <playerName> <message>");
                return;
            }

            string plName = args[0].ToString();

            object[] msg = args.Skip(1).ToArray();

            string message = "";

            foreach (object m in msg)
            {
                if (message == "")
                {
                    message = $"{m.ToString()} ";
                }
                else
                {
                    message += $"{m.ToString()} ";
                }
            }

            if (plName == player.Name)
            {
                player.SendMessage("[WARN] You can't just send PM to yourself!");
                return;
            }

            foreach (OakwoodPlayer p in Oakwood.Players)
            {
                if (plName == p.Name)
                {
                    player.SendMessage($"[Me -> {p.Name}] {message}");
                    p.SendMessage($"[{player.Name} -> Me] {message}");
                    return;
                }
            }

            player.SendMessage($"[WARN] Player '{plName}' was not found.");
        }

        [Command("tpa", "Sends a teleport request")]
        static void TpaCommand(OakwoodPlayer player, object[] args)
        {
            if (args.Length == 0)
            {
                player.SendMessage("[USAGE] /tpa <playerName>");
                return;
            }

            string plName = args[0].ToString();

            if (plName == player.Name)
            {
                player.SendMessage("[WARN] You can't just teleport to yourself!");
                return;
            }

            foreach (OakwoodPlayer p in Oakwood.Players)
            {
                if (plName == p.Name)
                {
                    ((OakPlayerData)p.PlayerData).TpaID = player.ID;

                    player.HUD.Message($"Sending teleport request to {p.Name}...", 0xFFFFFF);

                    p.SendMessage($"[TPA] Player {player.Name} wants to teleport to you:");
                    p.SendMessage($"  > Write '/tpaccept' to accept the request.");
                    p.SendMessage($"  > Or just ignore it, if you want to reject the request.");

                    return;
                }
            }

            player.SendMessage($"[WARN] Player '{plName}' was not found.");
        }

        [Command("tpaccept", "Accepts a teleport request")]
        static void TpacceptCommand(OakwoodPlayer player, object[] args)
        {
            if (((OakPlayerData)player.PlayerData).TpaID != -1)
            {
                foreach (OakwoodPlayer p in Oakwood.Players)
                {
                    if (p.ID == ((OakPlayerData)player.PlayerData).TpaID)
                    {
                        p.Position = player.Position;
                        p.HUD.Message("Your teleport request was accepted.", 0xFFFFFF);
                        ((OakPlayerData)player.PlayerData).TpaID = -1;
                        ((OakPlayerData)p.PlayerData).TpaID = -1;
                        return;
                    }
                }
            }

            player.SendMessage("[TPA] There's no teleport request.");
        }

        [Command("players", "Shows player list")]
        static void ShowPlayersCommand(OakwoodPlayer player, object[] args)
        {
            player.SendMessage("[INFO] Player list:");
            foreach (OakwoodPlayer pl in Oakwood.Players)
            {
                player.SendMessage($" > {pl.Name}#{pl.ID}");
            }
        }

        [Command("skin", "Sets your skin")]
        static void SetSkin(OakwoodPlayer player, object[] args)
        {
            if (args.Length == 0)
            {
                player.SendMessage("[USAGE] /skin <skinID>");
                return;
            }

            int skinID = int.Parse(args[0].ToString());

            OakwoodVehicle car = player.Vehicle;

            if (car == null)
            {
                if (skinID >= 0 && skinID < OakwoodResources.PlayerModels.Length - 1)
                {
                    player.SetModel(OakwoodResources.PlayerModels[skinID].Modelname);
                    player.HUD.Message("Skin successfully changed!", 0xFFFFFF);
                }
                else
                {
                    player.HUD.Message("Skin ID you provided is wrong!", 0xFCB603);
                }
            }
            else
            {
                player.HUD.Message("You can't be inside the vehicle!", 0xFF0000);
            }
        }

        [Command("getpos", "Gets your actual position")]
        static void GetPos(OakwoodPlayer player, object[] args)
        {
            OakVec3 pos = player.Position;
            player.HUD.Message( $"Actual position: [{pos.x}; {pos.y}; {pos.z}]", 0xFFFFFF);
        }

        [Command("getdir", "Gets your actual direction")]
        static void GetDir(OakwoodPlayer player, object[] args)
        {
            OakVec3 dir = player.Direction;
            player.HUD.Message($"Actual direction: [{dir.x * 360.0f}; {dir.y * 360.0f}; {dir.z * 360.0f}]", 0xFFFFFF);
        }

        [Command("loadloc", "Teleports you to loaded position")]
        static void LoadLoc(OakwoodPlayer player, object[] args)
        {
            if (args.Length == 0)
            {
                player.SendMessage("[USAGE] /loadloc <locName>");
                return;
            }

            string locName = args[0].ToString();

            string file = Environment.CurrentDirectory + @"\savedLocations\" + $"{locName}.txt";

            if (File.Exists(file))
            {
                string[] pos = File.ReadAllText(file).Split(';');
                OakVec3 newPos = new OakVec3(float.Parse(pos[0]), float.Parse(pos[1]), float.Parse(pos[2]));
                OakVec3 newDir = new OakVec3(float.Parse(pos[3]), float.Parse(pos[4]), float.Parse(pos[5]));
                player.Position = newPos;
                player.Direction = newDir;
                player.HUD.Message($"Teleported to '{locName}'!", 0xFFFFFF);
            }
            else
            {
                player.HUD.Message($"Location '{locName}' doesn't exist!", 0xFF0000);
            }
        }

        [Command("saveloc", "Saves your position into file")]
        static void SaveLoc(OakwoodPlayer player, object[] args)
        {
            if (args.Length == 0)
            {
                player.SendMessage("[USAGE] /saveloc <locName>");
                return;
            }

            string locName = args[0].ToString();

            string file = Environment.CurrentDirectory + @"\savedLocations\" + $"{locName}.txt";

            if (!Directory.Exists(Path.GetDirectoryName(file)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file));
            }

            OakVec3 playerPos = player.Position;
            OakVec3 playerDir = player.Direction;

            if (playerPos.x != 0 && playerPos.y != 0 && playerPos.z != 0 && playerDir.x != 0 && playerDir.z != 0)
            {
                File.WriteAllText(file, $"{playerPos.x};{playerPos.y};{playerPos.z};{playerDir.x};{playerDir.y};{playerDir.z}");

                player.HUD.Message($"Location '{locName}' saved!", 0xFFFFFF);
            }
            else
            {
                player.HUD.Message("Cannot save location!", 0xFF0000);
            }
        }

        [Command("car", "Spawns a car")]
        static void SpawnCar(OakwoodPlayer player, object[] args)
        {
            if (args.Length == 0)
            {
                player.SendMessage("[USAGE] /car <carID>");
                return;
            }

            int carID = int.Parse(args[0].ToString());

            if (carID >= 0 && carID < OakwoodResources.VehicleModels.Length - 1)
            {
                OakVec3 plPos = player.Position;

                OakVec3 plDir = player.Direction;

                OakwoodVehicle sCar = OakwoodVehicle.Spawn(OakwoodResources.VehicleModels[carID], plPos, plDir.x / 360.0f);

                if (sCar != null)
                {
                    player.VehicleManipulation.Put(sCar, VehicleSeat.FrontLeft);

                    player.HUD.Message("Car successfully spawned!", 0xFFFFFF);
                    return;
                }
                else
                {
                    player.HUD.Message("Cannot spawn car!", 0xFF0000);
                    return;
                }
            }
            else
            {
                player.HUD.Message("Car ID you provided is wrong!", 0xFCB603);
                return;
            }
        }

        [Command("test", "Testing command")]
        static void TestCommand(OakwoodPlayer player, object[] args)
        {
            player.SendMessage($"SharpWood testing command.");
        }

        [Command("spawn", "Respawns you")]
        static void SpawnCommand(OakwoodPlayer player, object[] args)
        {
            player.SpawnTempWeapons();

            player.Health = 200.0f;
            player.Spawn(new OakVec3(-1986.852539f, -5.089742f, 25.776871f), 180.0f);

            player.HUD.Fade(OakwoodFade.FadeIn, 500, 0xFFFFFF);
            player.HUD.Fade(OakwoodFade.FadeOut, 500, 0xFFFFFF);
        }

        #endregion
    }
}
