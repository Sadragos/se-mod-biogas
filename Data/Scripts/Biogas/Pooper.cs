using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace Biogas
{
    class Pooper
    {
        List<IMyPlayer> Players = new List<IMyPlayer>();
        MyObjectBuilder_Character character;
        Dictionary<long, PoopPlayer> Poopers = new Dictionary<long, PoopPlayer>();

        public void Go()
        {
            Players.Clear();
            MyAPIGateway.Players.GetPlayers(Players);

            foreach (IMyPlayer p in Players)
            {
                if (p.IsBot) continue;

                IMyEntity entity = p.Controller.ControlledEntity.Entity;
                entity = Utilities.GetCharacterEntity(entity);

                PoopPlayer pp;
                if (Poopers.ContainsKey(p.IdentityId))
                {
                    pp = Poopers[p.IdentityId];
                }
                else
                {
                    pp = new PoopPlayer() { PoopAmount = 0, Player = p };
                    Poopers.Add(p.IdentityId, pp);
                }

                float amount = MyUtils.GetRandomFloat(Config.Instance.PoopAmountPerSecondMin, Config.Instance.PoopAmountPerSecondMax);
                bool toilet = false;

                character = p.Character.GetObjectBuilder(false) as MyObjectBuilder_Character;

                switch (character.MovementState)
                {
                    case MyCharacterMovementEnum.Sitting:
                        IMyCubeBlock cb = p.Controller.ControlledEntity.Entity as IMyCubeBlock;
                        String seatmodel = cb.DefinitionDisplayNameText.ToLower();
                        if (seatmodel.Contains("toilet"))
                        {
                            amount *= Config.Instance.PoopMultiplierToilet;
                            toilet = true;
                        }
                        else
                        {
                            amount *= Config.Instance.PoopMultiplierSit;
                        }
                        break;
                    case MyCharacterMovementEnum.Flying:
                    case MyCharacterMovementEnum.Falling:
                        amount *= Config.Instance.PoopMultiplierFly;
                        break;
                    case MyCharacterMovementEnum.Crouching:
                    case MyCharacterMovementEnum.CrouchRotatingRight:
                    case MyCharacterMovementEnum.CrouchRotatingLeft:
                    case MyCharacterMovementEnum.CrouchWalking:
                    case MyCharacterMovementEnum.CrouchBackWalking:
                    case MyCharacterMovementEnum.CrouchStrafingLeft:
                    case MyCharacterMovementEnum.CrouchStrafingRight:
                    case MyCharacterMovementEnum.CrouchWalkingRightFront:
                    case MyCharacterMovementEnum.CrouchWalkingRightBack:
                    case MyCharacterMovementEnum.CrouchWalkingLeftFront:
                    case MyCharacterMovementEnum.CrouchWalkingLeftBack:
                        amount *= Config.Instance.PoopMultiplierCrouch;
                        break;
                    case MyCharacterMovementEnum.Walking:
                    case MyCharacterMovementEnum.BackWalking:
                    case MyCharacterMovementEnum.WalkStrafingLeft:
                    case MyCharacterMovementEnum.WalkStrafingRight:
                    case MyCharacterMovementEnum.WalkingRightFront:
                    case MyCharacterMovementEnum.WalkingRightBack:
                    case MyCharacterMovementEnum.WalkingLeftFront:
                    case MyCharacterMovementEnum.WalkingLeftBack:
                        amount *= Config.Instance.PoopMultiplierWalk;
                        break;
                    case MyCharacterMovementEnum.Running:
                    case MyCharacterMovementEnum.Backrunning:
                    case MyCharacterMovementEnum.RunStrafingLeft:
                    case MyCharacterMovementEnum.RunStrafingRight:
                    case MyCharacterMovementEnum.RunningRightFront:
                    case MyCharacterMovementEnum.RunningRightBack:
                    case MyCharacterMovementEnum.RunningLeftBack:
                    case MyCharacterMovementEnum.RunningLeftFront:
                    case MyCharacterMovementEnum.Sprinting:
                    case MyCharacterMovementEnum.Jump:
                        amount *= Config.Instance.PoopMultiplierSprint;
                        break;
                    case MyCharacterMovementEnum.Died:
                        amount = 0f;
                        break;
                }

                pp.PoopAmount += (float)Math.Round(amount, 3);
                //MyLog.Default.WriteLine("Biogas: Added " + amount + " to " + p.DisplayName + " -> " + pp.PoopAmount);
                if ((toilet && pp.PoopAmount >= 0.1f) || MyUtils.GetRandomFloat(0, 1) <= Config.Instance.PoopChancePerSecond || pp.PoopAmount >= Config.Instance.PoopAlwaysAt)
                {
                    IMyInventory inventory = entity.GetInventory();
                    inventory.AddItems((VRage.MyFixedPoint)Math.Round(pp.PoopAmount, 3), new MyObjectBuilder_Ore() { SubtypeName = "Organic" });
                    pp.PoopAmount = 0;
                    //MyLog.Default.WriteLine("Biogas: " + p.DisplayName + " pooped");
                    if (Config.Instance.PoopSounds)
                    {
                        MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("Fart" + MyUtils.GetRandomInt(0, 5), p.GetPosition());
                    }
                }

            }
        }
    }

    class PoopPlayer
    {
        public IMyPlayer Player;
        public float PoopAmount;
    }
}
