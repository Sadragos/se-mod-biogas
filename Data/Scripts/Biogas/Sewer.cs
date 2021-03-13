using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;

using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRage.Game.Entity;
using VRage.Voxels;
using SpaceEngineers.Game.ModAPI;

namespace Biogas
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CargoContainer), true, "Compost")]
    public class Compost : MyGameLogicComponent
    {
        bool _init = false;
        Sandbox.ModAPI.IMyTerminalBlock TerminalBlock;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();

            if (!_init)
            {
                TerminalBlock = Entity as Sandbox.ModAPI.IMyTerminalBlock;
                TerminalBlock.AppendingCustomInfo += AppendingCustomInfo;
                _init = true;
            }
            if (TerminalBlock.IsFunctional)
            {
                CompostGrid gridData = CompostDataStore.Get(TerminalBlock);
                if (gridData.LastScan == null || gridData.LastScan < DateTime.Now.AddSeconds(-10))
                {
                    gridData.Refresh();
                }
                float amount = MyUtils.GetRandomFloat(Config.Instance.OrganicPerOxyenfarmPerSecondMin * gridData.EffectiveFarms * 1.6f, Config.Instance.OrganicPerOxyenfarmPerSecondMax * gridData.EffectiveFarms * 1.6f);
                TerminalBlock.GetInventory().AddItems((VRage.MyFixedPoint)amount, new MyObjectBuilder_Ore() { SubtypeName = "Organic" });
                TerminalBlock.RefreshCustomInfo();
            }
        }

        void AppendingCustomInfo(Sandbox.ModAPI.IMyTerminalBlock block, StringBuilder stringBuilder)
        {
            try
            {
                CompostGrid gridData = CompostDataStore.Get(block);
                stringBuilder.Clear();
                stringBuilder
                    .Append("Collecting Organic\n")
                    .Append("- ")
                    .Append(gridData.OxygenFarms)
                    .Append(" working Farms\n");
                if (gridData.CropGrowers > 0)
                {
                    stringBuilder
                        .Append("- ")
                        .Append(gridData.CropGrowers)
                        .Append(" working Crop Grower\n");
                }
                if (gridData.Composts > 1)
                {
                    stringBuilder
                        .Append("- ")
                        .Append((gridData.Composts - 1))
                        .Append(" other Composters\n");
                }
                stringBuilder
                    .Append("\n")
                    .Append((Config.Instance.OrganicPerOxyenfarmPerSecondMin * gridData.EffectiveFarms * 60).ToString("0.00"))
                    .Append(" - ")
                    .Append((Config.Instance.OrganicPerOxyenfarmPerSecondMax * gridData.EffectiveFarms * 60).ToString("0.00"))
                    .Append(" Organic / Minute");
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("Biogas: ERROR " + e);
            }
        }
    }

    public class CompostDataStore
    {
        public static Dictionary<long, CompostGrid> Grids = new Dictionary<long, CompostGrid>();

        public static CompostGrid Get(Sandbox.ModAPI.IMyTerminalBlock block)
        {
            IMyCubeGrid grid = block.CubeGrid;
            MyLog.Default.WriteLine("Get Data for Cubegrid " + grid.EntityId);
            if (!Grids.ContainsKey(grid.EntityId))
            {
                Grids.Add(grid.EntityId, new CompostGrid()
                {
                    Grid = grid,
                    OxygenFarms = 0,
                    Composts = 0
                });
                MyLog.Default.WriteLine("New Entry");
            }
            return Grids[grid.EntityId];
        }
    }

    public class CompostGrid
    {
        List<IMySlimBlock> blocks = new List<IMySlimBlock>();
        public IMyCubeGrid Grid;
        public int Composts;
        public int OxygenFarms;
        public int CropGrowers;
        public DateTime LastScan;
        public float EffectiveFarms { get { return (float) (OxygenFarms + CropGrowers) / (float)Math.Max(1, Composts); } }

        public void Refresh()
        {
            blocks.Clear();
            Composts = 0;
            OxygenFarms = 0;
            CropGrowers = 0;
            Grid.GetBlocks(blocks, b => b.FatBlock != null);
            foreach(IMySlimBlock block in blocks)
            {
                if(block.FatBlock is Sandbox.ModAPI.IMyCargoContainer && block.FatBlock.BlockDefinition.SubtypeId.Equals("Compost"))
                {
                    Composts++;
                }
                else if (block.FatBlock is IMyOxygenFarm && (block.FatBlock as IMyOxygenFarm).GetOutput() > 0)
                {
                    OxygenFarms++;
                }
                else if (block.FatBlock is Sandbox.ModAPI.IMyAssembler && block.FatBlock.BlockDefinition.SubtypeId.Contains("Crop") && (block.FatBlock as Sandbox.ModAPI.IMyAssembler).IsWorking)
                {
                    CropGrowers++;
                }
            }
        }
    }
}