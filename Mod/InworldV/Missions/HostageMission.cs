using GTA;
using GTA.Math;
using GTA.Native;
using InworldV.Helper;
using System;
using System.Collections.Generic;

namespace InworldV.Missions
{
    [ScriptAttributes(NoDefaultInstance = true)]
    internal class HostageMission : PoliceMissionBase
    {
        private Vector3[] positions = new Vector3[] { new Vector3(294.4901f, 203.9191f, 104.3727f), new Vector3(-0.0002154737f,0f,-173.217f),
            new Vector3(297.8109f, 183.8136f, 104.1835f), new Vector3(292.3878f, 185.4283f, 104.312f), new Vector3(296.7399f, 179.8829f, 104.2081f) };
        private Vector3[] vehicleLocs = new Vector3[]
        {
            new Vector3(281.1418f, 183.7561f, 104.5006f), new Vector3(299.5618f, 174.0842f, 103.9786f)
        };

        enum MissionStage
        {
            GO,
            TALK,
            TALK_ACTION,
            TALK_MIME,
            TALK_MIME_ACTION,
            KILL,
            KILL_ACTION,
            GO_CAR,
            GO_CAR_ACTION,
            NONE
        }

        private MissionStage currentStage = MissionStage.NONE;
        Blip blip;
        Vehicle[] vehicles = new Vehicle[] { };
        Ped hostage, captor;
        List<Ped> temporaryPed = new List<Ped>();
        private bool isFinished;

        public HostageMission()
        {
        }
        public override void StartMission()
        {
            base.StartMission();
            this.ActorCharacter = "lucas_blackwood";
            MessageQueue.Instance.AddHelpMessage(new HelpMessage("~r~10-39 ~w~| Suspect is holding a hostage and refusing to negotiate.", 25000, true, false));
            this.missionState = MissionState.STARTED;
            this.currentStage = MissionStage.GO;

            Function.Call(Hash.REQUEST_ANIM_DICT, "amb@code_human_cower@male@base");
            Function.Call(Hash.REQUEST_ANIM_DICT, "misssagrab_inoffice");
            CreateHostageScenario();

            Tick += TickMission;
        }

        public override void ProcessGameEvent(string id)
        {
            if (currentStage == MissionStage.TALK_MIME_ACTION)
            {
                if (currentStage != MissionStage.GO_CAR_ACTION)
                    currentStage = MissionStage.GO_CAR;
            }
            else
            {
                if (id.Contains("player_rejects_solution"))
                {
                    PartnerMentionEventId = "hostage_mission_killed_mimegg";
                    KillHostage();
                }
                else if (id.Contains("_accepts_solution"))
                {
                    PartnerMentionEventId = "hostage_mission_agreed";
                    Surrender();
                }
            }
        }

        private void Surrender()
        {
            captor.Weapons.Drop();
            hostage.Task.ClearAllImmediately();
            captor.Task.ClearAllImmediately();
            captor.Task.PlayAnimation("random@arrests", "kneeling_arrest_idle", 8f, -4, -1, AnimationFlags.NotInterruptable | AnimationFlags.Loop, 0);
            hostage.Task.GoTo(positions[0]);
            currentStage = MissionStage.TALK_MIME;
        }

        private void KillHostage()
        {
            // Kill hostage
            currentStage = MissionStage.KILL;
            captor.Task.ClearAllImmediately();
            hostage.Task.ClearAllImmediately();
            captor.Task.ShootAt(hostage);
        }


        public override bool TryToConnectToActor(out Ped connected, out string characterId, out string playerName)
        {
            if (currentStage == MissionStage.TALK_MIME || currentStage == MissionStage.TALK_MIME_ACTION)
            {
                characterId = "oliver_bellamy";
                connected = hostage;
                playerName = "Officer";
            }
            else
            {
                playerName = "Cop";
                characterId = ActorCharacter;
                connected = captor;
            }

            if (connected == null) return false;
            if (connected.Position.DistanceTo(Game.Player.Character.Position) < 5)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void Stage()
        {
            if (currentStage == MissionStage.TALK)
            {
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Talk to ~r~captor~w~ or ~r~eliminate ~w~the captor", 5000, true, false));
                currentStage = MissionStage.TALK_ACTION;
                captor.CanBeTargetted = true;
            }
            else if (currentStage == MissionStage.TALK_MIME)
            {
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Talk to ~g~mime~w~ and ask if he is hurt", 5000, true, false));
                currentStage = MissionStage.TALK_MIME_ACTION;
                hostage.IsInvincible = true;
                hostage.CanBeTargetted = false;
                hostage.Task.StandStill(-1);
                hostage.Task.PlayAnimation("amb@code_human_cower@male@base", "base", 8f, -4, -1, AnimationFlags.NotInterruptable | AnimationFlags.Loop, 0);
            }
            else if (currentStage == MissionStage.KILL)
            {
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Eliminate the ~r~murderer", 5000, true, false));
                currentStage = MissionStage.KILL_ACTION;
                captor.CanBeTargetted = true;
                this.OnMissionDialogueStateChanged(ActorCharacter, DialogueState.DISCONNECT);
            }
            else if (currentStage == MissionStage.GO_CAR)
            {
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Units will take care rest. Go back to ~b~your car", 5000, true, false));
                currentStage = MissionStage.GO_CAR_ACTION;

                if (hostage == null || !hostage.IsAlive)
                {
                    PartnerMentionEventId = "hostage_mission_killed_mimegg";
                }
            }
        }

        public override Ped GetArrestPed()
        {
            return captor;
        }

        private void CreateHostageScenario()
        {
            Vector3 spawnPosition = positions[0];
            hostage = World.CreatePed(PedHash.MimeSMY, spawnPosition);
            captor = World.CreatePed(PedHash.Acult01AMM, spawnPosition);

            blip = hostage.AddBlip();
            blip.Color = BlipColor.Red;
            blip.ShowRoute = true;

            Vector3 rotation = positions[1];
            hostage.Rotation = rotation;
            captor.Rotation = rotation;
            captor.Position = hostage.Position;
            hostage.CanBeTargetted = false;
            captor.CanBeTargetted = false;


            hostage.Task.StandStill(-1);
            captor.Task.StandStill(-1);
            captor.Weapons.Give(WeaponHash.Pistol, 20, true, true);
            captor.Weapons.Select(WeaponHash.Pistol, true);

            // Create surrounding police
            Model policeModel = new Model(PedHash.Cop01SMY);
            Vector3 policeSpawn = positions[2];
            Ped police1 = World.CreatePed(policeModel, policeSpawn);
            police1.Weapons.Give(WeaponHash.Pistol, 150, true, true);
            temporaryPed.Add(police1);

            policeModel = new Model(PedHash.Cop01SMY);
            policeSpawn = positions[3];
            Ped police2 = World.CreatePed(policeModel, policeSpawn);
            police2.Weapons.Give(WeaponHash.Pistol, 150, true, true);
            temporaryPed.Add(police2);

            policeModel = new Model(PedHash.Cop01SMY);
            policeSpawn = positions[4];
            Ped police3 = World.CreatePed(policeModel, policeSpawn);
            police3.Weapons.Give(WeaponHash.Pistol, 150, true, true);
            temporaryPed.Add(police3);


            foreach (var ped in temporaryPed)
            {
                ped.Task.TurnTo(captor);
                ped.Task.AimAt(captor, -1);
                ped.BlockPermanentEvents = true;
            }

            Vehicle vehicle = World.CreateVehicle(VehicleHash.Police3, vehicleLocs[0]);
            Vehicle vehicle2 = World.CreateVehicle(VehicleHash.Police, vehicleLocs[1]);
            vehicle.IsSirenActive = true;
            vehicle2.IsSirenActive = true;
            vehicles = new Vehicle[2] { vehicle, vehicle2 };


            string animDict = "misssagrab_inoffice";
            hostage.Task.PlayAnimation(animDict, "hostage_loop_mrk", 8f, -4, -1, AnimationFlags.NotInterruptable | AnimationFlags.Loop, 0);
            captor.Task.PlayAnimation(animDict, "hostage_loop", 8f, -4, -1, AnimationFlags.NotInterruptable | AnimationFlags.Loop, 0);
        }

        public void TickMission(object sender, EventArgs e)
        {
            if (this.missionState == MissionState.STARTED)
            {
                TickStageParameters();

                if (currentStage == MissionStage.GO_CAR_ACTION)
                {
                    if (Game.Player.Character.IsInVehicle(policeVehicle))
                    {
                        isFinished = true;
                        this.missionState = MissionState.SUCCESS;
                        OnMissionStateChanged(MissionState.SUCCESS);
                    }
                }

                PedGroup group = Game.Player.Character.PedGroup;
                foreach (var ped in temporaryPed)
                {
                    if (captor != null)
                    {
                        ped.Task.AimAt(captor, -1);
                    }
                    if (ped.PedGroup != group) group.Add(ped, false);
                }
            }
        }

        bool isCloseToSubject = false;
        public override bool IsCloseToSubject => isCloseToSubject;

        private void TickStageParameters()
        {
            if (currentStage == MissionStage.GO)
            {
                if (captor != null)
                {
                    var pos = Game.Player.Character.Position;
                    if (pos.DistanceTo(captor.Position) < 10)
                    {
                        currentStage = MissionStage.TALK;
                        isCloseToSubject = true;
                        string animDict = "misssagrab_inoffice";
                        hostage.Task.PlayAnimation(animDict, "hostage_loop_mrk", 8f, -4, -1, AnimationFlags.NotInterruptable | AnimationFlags.Loop, 0);
                        captor.Task.PlayAnimation(animDict, "hostage_loop", 8f, -4, -1, AnimationFlags.NotInterruptable | AnimationFlags.Loop, 0);
                    }
                }
            }
            else if (currentStage == MissionStage.KILL_ACTION)
            {
                if (captor != null && !captor.IsAlive)
                {
                    this.OnMissionDialogueStateChanged(ActorCharacter, DialogueState.DISCONNECT);
                    if (hostage != null && hostage.IsAlive)
                    {
                        PartnerMentionEventId = "hostage_mission_killed_unharmed";
                        currentStage = MissionStage.TALK_MIME;
                        //hostage.Task.ClearAllImmediately();
                        //hostage.Task.GoTo(positions[0]);
                    }
                    else
                    {
                        currentStage = MissionStage.GO_CAR;
                    }
                }
            }
            else if (currentStage == MissionStage.TALK_ACTION)
            {
                if (hostage != null && hostage.IsAlive)
                {
                    if (!captor.IsAlive)
                    {
                        //hostage.Task.ClearAllImmediately();
                        //hostage.Task.GoTo(positions[0]);
                        currentStage = MissionStage.TALK_MIME;
                        PartnerMentionEventId = "hostage_mission_killed_unharmed";
                    }
                }
                else
                {
                    currentStage = MissionStage.GO_CAR;
                }
            }

            Stage();
        }

        public override bool IsFinished()
        {
            return isFinished;
        }

        public override void Cleanup()
        {
            foreach (var ped in temporaryPed)
            {
                ped.IsPersistent = false;
                ped.MarkAsNoLongerNeeded();
            }

            if (captor != null)
            {
                captor.IsPersistent = false;
                captor.MarkAsNoLongerNeeded();
            }

            if (hostage != null)
            {
                hostage.IsPersistent = false;
                hostage.MarkAsNoLongerNeeded();
            }

            foreach (var vehicle in vehicles)
            {
                if (vehicle != null)
                {
                    vehicle.IsPersistent = false;
                    vehicle.MarkAsNoLongerNeeded();
                }
            }

            if (blip != null)
                blip.Delete();

            vehicles.Clone();
            temporaryPed.Clear();
        }
    }
}
