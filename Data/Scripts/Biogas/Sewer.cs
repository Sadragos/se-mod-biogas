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
                float playerAmount = 0;
                if (gridData.LastScan == null || gridData.LastScan < DateTime.Now.AddSeconds(-5))
                {
                    gridData.Refresh();

                    foreach (IMyIdentity identity in gridData.PlayersOnToilet)
                    {
                        IMyPlayer player = Utilities.IdentityToPlayer(identity);
                        if (player == null || player.Character == null || !player.Character.HasInventory) continue;
                        IMyInventory inven = player.Character.GetInventory();

                        for (int i = inven.ItemCount - 1; i >= 0; i--)
                        {
                            VRage.Game.ModAPI.Ingame.MyInventoryItem? item = inven.GetItemAt(i);
                            if (!item.HasValue) continue;
                            if (item.Value.Type.ToString().Contains("Ore/Organic"))
                            {
                                playerAmount += (float) item.Value.Amount;
                                inven.RemoveItemsAt(i);
                            }
                        }
                    }
                }
                float amount = playerAmount + MyUtils.GetRandomFloat(Config.Instance.OrganicPerOxyenfarmPerSecondMin * gridData.EffectiveFarms * 1.6f, Config.Instance.OrganicPerOxyenfarmPerSecondMax * gridData.EffectiveFarms * 1.6f);
                TerminalBlock.GetInventory().AddItems((VRage.MyFixedPoint)amount, new MyObjectBuilder_Ore() { SubtypeName = "Organic" });
                TerminalBlock.RefreshCustomInfo();
            }
        }

        void AppendingCustomInfo(Sandbox.ModAPI.IMyTerminalBlock block, StringBuilder stringBuilder)
        {
            try
            {
                CompostGrid gridData = CompostDataStore.Get(block);
                if (gridData.OxygenFarms > 0)
                {
                    stringBuilder
                    .Append("Collecting Organic\n")
                    .Append("- ")
                    .Append(gridData.OxygenFarms)
                    .Append(" working Farms\n");
                }
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
                    .Append(" Organic / Minute from Farms");

                if (gridData.PlayersOnToilet.Count > 0)
                {
                    stringBuilder.Append("\n\nEngineers on Toilet\n");
                    foreach (IMyIdentity id in gridData.PlayersOnToilet)
                    {
                        stringBuilder.Append("- ").Append(id.DisplayName).Append("\n");
                    }

                }
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
        public List<IMyIdentity> PlayersOnToilet = new List<IMyIdentity>();
        public DateTime LastScan;
        public float EffectiveFarms { get { return (float) (OxygenFarms + CropGrowers) / (float)Math.Max(1, Composts); } }

        public void Refresh()
        {
            blocks.Clear();
            PlayersOnToilet.Clear();
            Composts = 0;
            OxygenFarms = 0;
            CropGrowers = 0;
            Grid.GetBlocks(blocks, b => b.FatBlock != null);
            LastScan = DateTime.Now;
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
                else if (block.FatBlock is Sandbox.ModAPI.IMyCockpit 
                    && block.FatBlock.DefinitionDisplayNameText.ToLower().Contains("toilet"))
                {
                    Sandbox.ModAPI.IMyCockpit cockpit = block.FatBlock as Sandbox.ModAPI.IMyCockpit;
                    if (!cockpit.IsUnderControl) continue;
                    long controller = cockpit.ControllerInfo.ControllingIdentityId;
                    IMyIdentity identity = Utilities.CubeBlockBuiltByToIdentity(controller);
                    if (identity != null)
                    {
                        PlayersOnToilet.Add(identity);
                    }
                }
            }
        }
    }
}