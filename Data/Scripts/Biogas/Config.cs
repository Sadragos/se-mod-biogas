using System;
using System.IO;
using System.Text;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Game;
using VRage.Utils;

namespace Biogas
{

    public class Config
    {

        public static MyConfig Instance;

        public static void Load()
        {
            // Load config xml
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage("BiogasConfig.xml", typeof(MyConfig)))
            {
                try
                {
                    TextReader reader = MyAPIGateway.Utilities.ReadFileInWorldStorage("BiogasConfig.xml", typeof(MyConfig));
                    var xmlData = reader.ReadToEnd();
                    Instance = MyAPIGateway.Utilities.SerializeFromXML<MyConfig>(xmlData);
                    reader.Dispose();
                    MyLog.Default.WriteLine("Biogas: found and loaded");
                }
                catch (Exception e)
                {
                    MyLog.Default.WriteLine("Biogas: loading failed, generating new Config");
                }
            }

            if (Instance == null)
            {
                MyLog.Default.WriteLine("Biogas: No Loot Config found, creating New");
                // Create default values
                Instance = new MyConfig()
                {
                    PoopChancePerSecond = 0.001f,
                    PoopMultiplierCrouch = 1.4f,
                    PoopMultiplierFly = 0.9f,
                    PoopMultiplierSit = 0.75f,
                    PoopMultiplierWalk = 1.2f,
                    PoopMultiplierSprint = 1.75f,
                    PoopSounds = false,
                    PoopAlwaysAt = 10,
                    PoopAmountPerSecondMin = 0.001f,
                    PoopAmountPerSecondMax = 0.01f,
                    PoopMultiplierToilet = 2f,
                    OrganicPerOxyenfarmPerSecondMin = 0.002f,
                    OrganicPerOxyenfarmPerSecondMax = 0.005f
            };
                Write();
            }
        }


        public static void Write()
        {
            if (Instance == null) return;

            try
            {
                MyLog.Default.WriteLine("Biogas: Serializing to XML... ");
                string xml = MyAPIGateway.Utilities.SerializeToXML<MyConfig>(Instance);
                MyLog.Default.WriteLine("Biogas: Writing to disk... ");
                TextWriter writer = MyAPIGateway.Utilities.WriteFileInWorldStorage("BiogasConfig.xml", typeof(MyConfig));
                writer.Write(xml);
                writer.Flush();
                writer.Close();
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("Biogas: Error saving XML!" + e.StackTrace);
            }
        }
    }
}