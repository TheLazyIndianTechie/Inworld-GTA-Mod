using GTA;
using GTA.Math;
using GTA.Native;
using InworldV.Helper;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace InworldV.Missions
{
    [ScriptAttributes(NoDefaultInstance = true)]
    internal class PartnerSelectionMission : PoliceMissionBase
    {
        private enum MissionStage
        {
            SELECT,
            SELECT_ACTION,
            GO_CAR,
            GO_CAR_ACTION,
            NONE
        }

        private readonly string partner1 = "emily_martinez", partner2 = "tony_russo", partner3 = "frank_thompson";
        private Vector3[] policeLocations = new Vector3[] {
            new Vector3(421.7809f, -1010.716f, 29.09797f), new Vector3(3.61966E-10f,1.696474E-09f,176.7261f),
            new Vector3(439.1223f, -1012.64f, 28.59421f), new Vector3(3.622901E-10f,1.696116E-09f,130.7742f),
            new Vector3(457.8597f, -1024.634f, 28.41791f), new Vector3(3.618802E-10f,1.696131E-09f,57.77071f) };
        private Vector3 spawn = new Vector3(457.8029f, -1008.894f, 28.29712f);
        private List<Blip> blips = new List<Blip>();
        private MissionStage currentStage = MissionStage.NONE;
        private bool isFinished = false;
        private int selectedPed = -1;
        public Action<Ped, int> PartnerSelected;
        public Ped partner1Ped, partner2Ped, partner3Ped;

        private void SetupPartners()
        {
            Model companionMaleModel = new Model(PedHash.Cop01SMY);
            Model companionFemaleModel = new Model(PedHash.Cop01SFY);

            Vector3 spawnPosition = policeLocations[0];
            partner1Ped = World.CreatePed(companionFemaleModel, spawnPosition);
            partner1Ped.IsInvincible = true;
            partner1Ped.Weapons.Give(WeaponHash.PistolMk2, 150, false, true);

            spawnPosition = policeLocations[2];
            partner2Ped = World.CreatePed(companionMaleModel, spawnPosition);
            partner2Ped.IsInvincible = true;
            partner2Ped.Weapons.Give(WeaponHash.PistolMk2, 150, false, true);

            spawnPosition = policeLocations[4];
            partner3Ped = World.CreatePed(companionMaleModel, spawnPosition);
            partner3Ped.IsInvincible = true;
            partner3Ped.Weapons.Give(WeaponHash.PistolMk2, 150, false, true);


            PedGroup group = Game.Player.Character.PedGroup;
            Function.Call(Hash.SET_PED_AS_GROUP_MEMBER, partner1Ped, group);
            Blip partner1blip = partner1Ped.AddBlip();
            partner1Ped.Position = policeLocations[0];
            partner1Ped.Rotation = policeLocations[1];
            partner1blip.Name = "Emily";
            partner1blip.Color = BlipColor.BlueDark;
            partner1blip.ShowsFriendIndicator = true;
            blips.Add(partner1blip);

            Function.Call(Hash.SET_PED_AS_GROUP_MEMBER, partner2Ped, group);
            Blip partner2blip = partner2Ped.AddBlip();
            partner2Ped.Position = policeLocations[2];
            partner2Ped.Rotation = policeLocations[3];
            partner2blip.Color = BlipColor.BlueDark;
            partner2blip.Name = "Tony";
            partner2blip.ShowsFriendIndicator = true;
            blips.Add(partner2blip);

            Function.Call(Hash.SET_PED_AS_GROUP_MEMBER, partner3Ped, group);
            Blip partner3blip = partner3Ped.AddBlip();
            partner3Ped.Position = policeLocations[4];
            partner3Ped.Rotation = policeLocations[5];
            partner3blip.Color = BlipColor.BlueDark;
            partner3blip.Name = "Frank";
            partner3blip.ShowsFriendIndicator = true;
            blips.Add(partner3blip);

            Helper.SceneHelper.SetupCompanionPed(partner1Ped, 1);
            Helper.SceneHelper.SetupCompanionPed(partner2Ped, 2);
            Helper.SceneHelper.SetupCompanionPed(partner3Ped, 3);
        }

        public override void StartMission()
        {
            base.StartMission();
            MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Approach available ~b~partners ~w~and press ~INPUT_CONTEXT~ to select your partner.", 25000, true, false));
            this.missionState = MissionState.STARTED;
            this.currentStage = MissionStage.SELECT_ACTION;
            SetupScene();
            Interval = 50;
            Tick += TickMission;
            KeyDown += OnKeyDown;
        }

        public override bool TryToConnectToActor(out Ped connected, out string characterId, out string playerName)
        {
            characterId = ActorCharacter;
            connected = null;
            playerName = "Mike";
            Vector3 positionPlayer = Game.Player.Character.Position;
            if (partner1Ped == null || partner2Ped == null || partner3Ped == null) return false;
            if (positionPlayer.DistanceTo2D(partner1Ped.Position) < 5)
            {
                characterId = partner1;
                connected = partner1Ped;
                return true;
            }
            else if (positionPlayer.DistanceTo2D(partner2Ped.Position) < 5)
            {
                characterId = partner2;
                connected = partner2Ped;
                return true;
            }
            else if (positionPlayer.DistanceTo2D(partner3Ped.Position) < 5)
            {
                characterId = partner3;
                connected = partner3Ped;
                return true;
            }
            return false;
        }

        int engaging = -1;
        private void Stage()
        {
            try
            {
                if (currentStage == MissionStage.SELECT_ACTION)
                {
                    Vector3 positionPlayer = Game.Player.Character.Position;
                    if (positionPlayer.DistanceTo2D(partner1Ped.Position) < 5)
                    {
                        if (engaging != 1)
                        {
                            MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Press T to talk to ~b~Emily, ~w~you can press E to select her as partner", 5000, true, false));
                            engaging = 1;

                            partner1Ped.Task.TurnTo(Game.Player.Character, -1);
                            partner1Ped.Task.LookAt(Game.Player.Character, -1);
                        }
                    }
                    else if (positionPlayer.DistanceTo2D(partner2Ped.Position) < 5)
                    {
                        if (engaging != 2)
                        {
                            MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Press T to talk to ~b~Tony, ~w~you can press E to select him as partner", 5000, true, false));
                            engaging = 2;

                            partner2Ped.Task.TurnTo(Game.Player.Character, -1);
                            partner2Ped.Task.LookAt(Game.Player.Character, -1);
                        }
                    }
                    else if (positionPlayer.DistanceTo2D(partner3Ped.Position) < 5)
                    {
                        if (engaging != 3)
                        {
                            MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Press T to talk to ~b~Frank, ~w~you can press E to select him as partner", 5000, true, false));
                            engaging = 3;

                            partner3Ped.Task.TurnTo(Game.Player.Character, -1);
                            partner3Ped.Task.LookAt(Game.Player.Character, -1);
                        }

                    }
                }
                else if (currentStage == MissionStage.GO_CAR)
                {
                    MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Go to your ~b~car~w~ to start your day, your partner will follow you once you are in", 5000, true, false));
                    currentStage = MissionStage.GO_CAR_ACTION;
                }
            }
            catch
            {

            }
        }

        private void TeleportPlayer()
        {
            Game.Player.Character.Position = spawn;
        }

        private void SetupScene()
        {
            TeleportPlayer();
            SetupPartners();
        }

        private void MakePedWait(Ped partnerPed)
        {
            if (partnerPed != null)
            {
                PedGroup group = Game.Player.Character.PedGroup;
                if (partnerPed.PedGroup != group) group.Add(partnerPed, false);
                partnerPed.Task.StandStill(-1);
                partnerPed.Task.TurnTo(Game.Player.Character, 100);
                partnerPed.Task.LookAt(Game.Player.Character, 100);
            }
        }

        public void TickMission(object sender, EventArgs e)
        {
            if (this.missionState == MissionState.STARTED)
            {
                Stage();

                MakePedWait(partner1Ped);
                MakePedWait(partner2Ped);
                MakePedWait(partner3Ped);
            }

            if (currentStage == MissionStage.GO_CAR_ACTION)
            {
                if (Game.Player.Character.IsInVehicle(policeVehicle))
                {
                    isFinished = true;
                    this.missionState = MissionState.SUCCESS;
                    OnMissionStateChanged(MissionState.SUCCESS);
                }
            }
        }

        public override bool IsCloseToSubject => true;

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (currentStage == MissionStage.SELECT_ACTION)
            {
                if (e.KeyCode == Keys.E)
                {
                    // Select person in front of you and show go to car to finish the mission
                    Vector3 positionPlayer = Game.Player.Character.Position;
                    if (!(partner1Ped == null || partner2Ped == null || partner3Ped == null))
                    {
                        if (positionPlayer.DistanceTo2D(partner1Ped.Position) < 5)
                        {
                            selectedPed = 1;
                            currentStage = MissionStage.GO_CAR;

                            PartnerSelected.Invoke(partner1Ped, 1);
                        }
                        else if (positionPlayer.DistanceTo2D(partner2Ped.Position) < 5)
                        {
                            selectedPed = 2;
                            currentStage = MissionStage.GO_CAR;

                            PartnerSelected.Invoke(partner2Ped, 2);
                        }
                        else if (positionPlayer.DistanceTo2D(partner3Ped.Position) < 5)
                        {
                            selectedPed = 3;
                            currentStage = MissionStage.GO_CAR;

                            PartnerSelected.Invoke(partner3Ped, 3);
                        }
                    }
                }
            }
        }

        public override bool IsFinished()
        {
            return isFinished;
        }

        public override void Cleanup()
        {
            foreach (var blip in blips)
            {
                if (blip != null)
                    blip.Delete();
            }


            if (selectedPed != 1)
            {
                partner1Ped.MarkAsNoLongerNeeded();
                partner1Ped.Delete();
            }

            if (selectedPed != 2)
            {
                partner2Ped.MarkAsNoLongerNeeded();
                partner2Ped.Delete();
            }


            if (selectedPed != 3)
            {
                partner3Ped.MarkAsNoLongerNeeded();
                partner3Ped.Delete();
            }

        }
    }
}
