using GTA;
using GTA.Math;
using GTA.Native;
using System;

namespace InworldV.Agent
{
    internal class Companion
    {
        internal enum Directive
        {
            FOLLOW,
            HOLD,
            DRIVE,
            COVER,
        }

        internal enum Order
        {
            Arrest,
            CallBackup
        }

        private Directive _currentDirective;
        public Directive CurrentDirective
        {
            set
            {
                if (!Character.IsInVehicle())
                    Character.Task.ClearAllImmediately();
                _currentDirective = value;
            }
            get { return _currentDirective; }
        }

        public Ped Character;
        public bool IsEnemy = false;
        private Vehicle shop;
        private bool isTaskEnterTriggered, isTaskFollowTriggered, isTaskHoldTriggered, isTakeCoverTriggered;

        public bool IsAlive => Character != null && Character.IsAlive && Character.Health > 0;

        public void SetShop(Vehicle shop) { this.shop = shop; }

        public Ped CreateCompanion(int type = 1)
        {
            Model companionModel = new Model(type == 1 ? PedHash.Cop01SFY : PedHash.Cop01SMY);
            Vector3 spawnPosition = Game.Player.Character.GetOffsetPosition(new Vector3(0f, 3f, 0f));
            Character = World.CreatePed(companionModel, spawnPosition);
            while (Character == null)
            {
                Character = World.CreatePed(companionModel, spawnPosition);
            }

            Character.IsInvincible = true;
            Character.Task.FollowToOffsetFromEntity(Game.Player.Character, new Vector3(0, -1, 0), 10, -1, 100, true);
            Character.Weapons.Give(WeaponHash.PistolMk2, 150, false, true);
            Helper.SceneHelper.SetupCompanionPed(Character, type);
            isTaskFollowTriggered = false;
            isTaskEnterTriggered = false;
            SetRelationship();
            return Character;
        }

        public Ped CreateCompanion(Ped ped)
        {
            Character = ped;
            Character.IsInvincible = true;
            Character.Task.FollowToOffsetFromEntity(Game.Player.Character, new Vector3(0, -1, 0), 10, -1, 100, true);
            Character.Weapons.Give(WeaponHash.PistolMk2, 150, false, true);
            SetRelationship();
            isTaskFollowTriggered = false;
            isTaskEnterTriggered = false;
            return Character;
        }

        private void SetRelationship()
        {
            PedGroup group = Game.Player.Character.PedGroup;
            group.Add(this.Character, false);
        }

        public void ProcessDirective(string directiveEvent)
        {
            if (IsEnemy) return;
            if (directiveEvent == string.Empty) return;

            if (directiveEvent.Contains("complete.order_follow'"))
            {
                CurrentDirective = Directive.FOLLOW;
                isTaskFollowTriggered = false;
                isTaskEnterTriggered = false;
            }
            else if (directiveEvent.Contains("complete.order_stop"))
            {
                CurrentDirective = Directive.HOLD;
                isTaskHoldTriggered = false;
            }
            else if (directiveEvent.Contains("complete.order_cover"))
            {
                CurrentDirective = Directive.COVER;
                isTakeCoverTriggered = false;
            }
        }

        public void RemoveCompanion()
        {
            if (this.Character != null)
            {
                this.Character.IsPersistent = false;
                this.Character.Task.ClearAll();
                this.Character.MarkAsNoLongerNeeded();
                this.Character.IsInvincible = false;
                this.Character.Delete();
            }
        }

        public void Follow()
        {
            if (Game.Player.Character == null) return;
            if (Character == null) return;
            isTaskHoldTriggered = false;
            isTakeCoverTriggered = false;

            if (Game.Player.Character.IsInVehicle() && !Character.IsInVehicle())
            {
                isTaskFollowTriggered = false;
                if (!isTaskEnterTriggered)
                {
                    isTaskEnterTriggered = true;
                    Character.Task.ClearAllImmediately();
                    Character.BlockPermanentEvents = true;
                    Character.Task.EnterVehicle(shop, VehicleSeat.RightFront, -1, 10, EnterVehicleFlags.ResumeIfInterupted);
                }
            }
            else if (!Game.Player.Character.IsInVehicle() && Character.IsInVehicle())
            {
                isTaskEnterTriggered = false;
                if (!isTaskFollowTriggered)
                {
                    isTaskFollowTriggered = true;
                    if (Character.IsInVehicle())
                        Character.Task.LeaveVehicle();

                    Character.BlockPermanentEvents = false;
                    Character.Task.FollowToOffsetFromEntity(Game.Player.Character, new Vector3(0, -1, 0), 10, -1, 100, true);
                }
            }
        }

        public void Hold()
        {
            if (isTaskHoldTriggered) return;
            if (Character.IsInVehicle())
            {
                Character.Task.ClearAll();
            }
            else
            {
                Character.Task.ClearAllImmediately();
            }
            Character.Task.StandStill(-1);
            isTaskHoldTriggered = true;
            isTaskEnterTriggered = false;
            isTaskFollowTriggered = false;
            isTakeCoverTriggered = false;
        }

        public void TakeCover()
        {
            if (isTakeCoverTriggered) return;
            if (Character.IsInVehicle())
            {
                Character.Task.ClearAll();
            }
            else
            {
                Character.Task.ClearAllImmediately();
            }
            var pos = this.Character.Position;
            Function.Call(Hash.TASK_SEEK_COVER_FROM_POS, Character, pos.X, pos.Y, pos.Z, -1, 0);
            isTakeCoverTriggered = true;
            isTaskHoldTriggered = false;
            isTaskEnterTriggered = false;
            isTaskFollowTriggered = false;
        }

        private void SetGroupIfNotTurnedBack()
        {
            PedGroup group = Game.Player.Character.PedGroup;
            if (Character.PedGroup != group) group.Add(Character, false);
        }

        public void Think()
        {
            if (IsEnemy) return;
            try
            {
                if (Character != null && Character.Health > 0)
                {
                    SetGroupIfNotTurnedBack();

                    if (CurrentDirective == Directive.FOLLOW)
                    {
                        Follow();
                    }
                    else if (CurrentDirective == Directive.HOLD)
                    {
                        Hold();
                    }
                    else if (CurrentDirective == Directive.COVER)
                    {
                        TakeCover();
                    }
                }
            }
            catch (Exception e)
            {

            }
        }
    }

}
