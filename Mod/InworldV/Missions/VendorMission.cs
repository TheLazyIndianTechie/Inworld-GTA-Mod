using GTA;
using GTA.Math;
using InworldV.Helper;
using System;

namespace InworldV.Missions
{
    [ScriptAttributes(NoDefaultInstance = true)]
    internal class VendorMission : PoliceMissionBase
    {
        // 0 hotdog, 1 beefy
        private Vector3[] vendorPlaces = new Vector3[]
        {
            new Vector3(-1220.359f, -1504.407f, 4.36391f), new Vector3(-0.04437036f,0.01215779f,118.6089f),
            new Vector3(-1268.3f, -1433.205f, 4.36796f), new Vector3(-5.643671E-06f,-7.582723E-06f,143.6998f)
        };

        private enum MissionStage
        {
            GO_AND_DECIDE,
            TALK,
            TALK_ACTION,
            PAY,
            PAY_ACTION,
            GO_CAR,
            GO_CAR_ACTION,
        };

        private Blip blip;
        private Ped vendorPed;
        public int VENDOR_TYPE = 0;
        private MissionStage currentStage = MissionStage.GO_AND_DECIDE;
        private bool isFinished = false;

        public VendorMission()
        {
        }

        public override void StartMission()
        {
            base.StartMission();

            if (VENDOR_TYPE == 0)
            {
                ActorCharacter = "carlos_morales";
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~g~Visit Chihuahua Vendor~w~ for hotdogs and coffee OR ~r~tell your partner~w~ you don't want to go", 13000, true, false));
            }
            else
            {
                ActorCharacter = "eddie_thompson";
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~g~Visit Beefy Bills Vendor~w~ for burgers and coffee OR ~r~tell your partner~w~ you don't want to go", 13000, true, false));
            }
            this.currentStage = MissionStage.GO_AND_DECIDE;
            this.missionState = MissionState.STARTED;
            SetupScene();

            Tick += TickMission;
        }

        public override bool TryToConnectToActor(out Ped connected, out string characterId, out string playerName)
        {
            playerName = "Officer";
            characterId = ActorCharacter;
            connected = vendorPed;
            if (vendorPed == null) return false;
            if (vendorPed.Position.DistanceTo(Game.Player.Character.Position) < 3)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void ProcessGameEvent(string id)
        {
            if (id.Contains("discussion_reject") || id.Contains("discussion_reject"))
            {
                isFinished = true;
                this.missionState = MissionState.FAILED;
                OnMissionStateChanged(MissionState.FAILED);
                return;
            }

            if (id.Contains("pay_"))
            {
                currentStage = MissionStage.GO_CAR;
            }
        }

        private void Stage()
        {
            if (currentStage == MissionStage.TALK)
            {
                vendorPed.Task.TurnTo(Game.Player.Character);
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Talk to vendor for purchase", 5000, true, false));
                currentStage = MissionStage.TALK_ACTION;
            }
            else if (currentStage == MissionStage.GO_CAR)
            {
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~You are done. Go back to your ~b~vehicle", 5000, true, false));
                currentStage = MissionStage.GO_CAR_ACTION;
            }
        }

        private void SetupScene()
        {
            PedGroup group = Game.Player.Character.PedGroup;
            var pos = VENDOR_TYPE == 0 ? vendorPlaces[0] : vendorPlaces[2];
            var rot = VENDOR_TYPE == 0 ? vendorPlaces[1] : vendorPlaces[3];
            vendorPed = World.CreatePed(PedHash.Strvend01SMM, pos);
            vendorPed.Rotation = rot;
            group.Add(vendorPed, false);
            vendorPed.Task.StandStill(-1);
            blip = vendorPed.AddBlip();
            blip.Color = BlipColor.Green2;
            blip.ShowRoute = true;
        }


        bool isCloseToSubject = false;
        public override bool IsCloseToSubject => isCloseToSubject;

        public void TickMission(object sender, EventArgs e)
        {
            if (this.missionState == MissionState.STARTED)
            {
                Stage();
                PedGroup group = Game.Player.Character.PedGroup;
                if (vendorPed != null)
                {
                    if (vendorPed.PedGroup != group) group.Add(vendorPed, false);

                    if (currentStage == MissionStage.GO_AND_DECIDE)
                    {
                        if (vendorPed.Position.DistanceTo(Game.Player.Character.Position) < 8)
                        {
                            isCloseToSubject = true;
                            currentStage = MissionStage.TALK;
                        }
                    }
                }
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

        public override bool IsFinished()
        {
            return isFinished;
        }

        public override void Cleanup()
        {
            if (vendorPed != null)
            {
                vendorPed.IsPersistent = false;
                vendorPed.MarkAsNoLongerNeeded();
            }

            if (blip != null)
                blip.Delete();
        }
    }
}
