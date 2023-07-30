using ExtensionMethods;
using GTA;
using GTA.Math;
using InworldV.Helper;
using System;
using System.Collections.Generic;

namespace InworldV.Missions
{
    [ScriptAttributes(NoDefaultInstance = true)]
    internal class BruciePodcastMission : PoliceMissionBase
    {

        private Vector3[] broRoganPos = new Vector3[]
        {
            new Vector3(-1940.723f, 359.998f, 93.358f), new Vector3(0f,0f,138.3263f)
        };

        private Vector3[] policePos = new Vector3[] {
            new Vector3(-1952.457f, 345.0499f, 90.67661f), new Vector3(0f,0f,105.0572f),
            new Vector3(-1955.142f, 350.1811f, 90.77228f), new Vector3(0f,0f,40.17044f),
            new Vector3(-1932.616f, 356.5288f, 93.75626f), new Vector3(0f,0f,-0.09149617f),
        };

        private Vector3[] tvPos = new Vector3[] {
            new Vector3(-1955.66f, 348.5575f, 90.66864f), new Vector3(0f,0f,-67.56017f),
            new Vector3(-1954.654f, 347.0895f, 90.64762f), new Vector3(0f,0f,-108.766f),
            new Vector3(-1955.231f, 347.7672f, 90.65141f), new Vector3(0f,0f,-92.21978f),
        };

        private Vector3[] tvVehiclePos = new Vector3[]
        {
            new Vector3(-1958.61f, 358.5299f, 91.25585f), new Vector3(0f,0f,-167.0483f)
        };

        private Vector3[] policeVehiclePos = new Vector3[]
        {
            new Vector3(-1952.792f, 335.005f, 90.22346f), new Vector3(0f,0f,-144.0838f)
        };

        private Blip blip;
        private Vehicle[] vehicles;
        private Ped bruciePed;
        private List<Ped> temporaryPed = new List<Ped>();
        private MissionStage currentStage = MissionStage.GO;
        private bool isFinished = false;

        enum MissionStage
        {
            GO,
            TALK,
            TALK_ACTION,
            ASK_FOLLOW_UP,
            ASK_FOLLOW_UP_ACTION,
            GO_CAR,
            GO_CAR_ACTION,
            NONE
        }

        public BruciePodcastMission()
        {
            Tick += TickMission;
        }
        public override void StartMission()
        {
            base.StartMission();
            ActorCharacter = "brucie";
            MessageQueue.Instance.AddHelpMessage(new HelpMessage("~y~10-13 ~w~| Gunshot reported. Proceed with caution.", 3000, true, false));
            this.missionState = MissionState.STARTED;
            PartnerMentionEventId = "podcaster_mission";
            SetupScene();
        }

        public override bool TryToConnectToActor(out Ped connected, out string characterId, out string playerName)
        {
            characterId = ActorCharacter;
            connected = bruciePed;
            playerName = "officer";
            if (bruciePed == null) return false;
            if (bruciePed.Position.DistanceTo(Game.Player.Character.Position) < 5)
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
            if (id.Contains("explain_attack"))
            {
                currentStage = MissionStage.ASK_FOLLOW_UP;
            }
            else if (id.Contains("mention_threats"))
            {
                currentStage = MissionStage.GO_CAR;
                if (blip != null)
                    blip.Delete();
            }
        }

        private void Stage()
        {
            if (currentStage == MissionStage.TALK)
            {
                bruciePed.Task.StandStill(-1);
                bruciePed.Task.TurnTo(Game.Player.Character, -1);
                bruciePed.Task.LookAt(Game.Player.Character, -1);
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Talk to ~y~Brucie~w~ to find out what happened", 3000, true, false));
                currentStage = MissionStage.NONE;
            }
            else if (currentStage == MissionStage.ASK_FOLLOW_UP)
            {
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Ask ~b~why~w~ they attacked him", 3000, true, false));
                currentStage = MissionStage.ASK_FOLLOW_UP_ACTION;
            }
            else if (currentStage == MissionStage.GO_CAR)
            {
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Go back to ~b~your car~w~ to finish collecting information", 3000, true, false));
                currentStage = MissionStage.GO_CAR_ACTION;
            }
        }

        private void SetupScene()
        {
            bruciePed = World.CreatePed(PedHash.Brucie2, broRoganPos[0]);
            bruciePed.Rotation = broRoganPos[1];
            bruciePed.CanBeTargetted = false;
            blip = bruciePed.AddBlip();
            blip.Color = BlipColor.Orange;
            blip.ShowRoute = true;
            bruciePed.Task.StandStill(-1);
            bruciePed.IsInvincible = true;
            string[] scenarios = new string[] { "CODE_HUMAN_POLICE_INVESTIGATE", "CODE_HUMAN_MEDIC_TIME_OF_DEATH", "WORLD_HUMAN_GUARD_STAND", "WORLD_HUMAN_COP_IDLES" };
            for (int i = 0; i < policePos.Length; i++)
            {
                Model policeModel = new Model(PedHash.Cop01SMY);
                Ped policePed = World.CreatePed(policeModel, policePos[i]);
                policePed.Rotation = policePos[i + 1];
                policePed.Task.StartScenario(scenarios.GetRandomElement(), 0);
                temporaryPed.Add(policePed);
                i++;
            }

            Model carModel = new Model(VehicleHash.Burrito3);
            Vehicle tvVan = World.CreateVehicle(carModel, tvVehiclePos[0]);
            tvVan.Rotation = tvVehiclePos[1];
            Model policeCar = new Model(VehicleHash.Police);
            Vehicle pCar = World.CreateVehicle(policeCar, policeVehiclePos[0]);
            pCar.Rotation = policeVehiclePos[1];
            vehicles = new Vehicle[2]
            {
                tvVan, pCar,
            };

            Ped tvPerson = World.CreatePed(PedHash.ReporterCutscene, tvPos[0]);
            tvPerson.Rotation = tvPos[1];
            temporaryPed.Add(tvPerson);
            Ped tvPerson2 = World.CreatePed(PedHash.ScreenWriter, tvPos[2]);
            tvPerson2.Rotation = tvPos[3];
            temporaryPed.Add(tvPerson2);
            tvPerson2.Task.StartScenario("WORLD_HUMAN_PAPARAZZI", 0);
            Ped tvPerson3 = World.CreatePed(PedHash.Bevhills01AFY, tvPos[4]);
            tvPerson3.Rotation = tvPos[5];
            tvPerson2.Task.StartScenario("WORLD_HUMAN_STAND_IMPATIENT", 0);
            temporaryPed.Add(tvPerson3);
        }
        bool isCloseToSubject = false;
        public override bool IsCloseToSubject => isCloseToSubject;

        public void TickMission(object sender, EventArgs e)
        {
            if (this.missionState == MissionState.STARTED)
            {
                Stage();

                // Make everyone wait
                foreach (var tPed in temporaryPed)
                {
                    if (tPed.IsAlive)
                    {
                        tPed.Task.StandStill(-1);
                        tPed.BlockPermanentEvents = true;
                    }
                }

                if (bruciePed != null)
                {
                    bruciePed.Task.StandStill(-1);
                    bruciePed.Task.TurnTo(Game.Player.Character, -1);
                    bruciePed.Task.LookAt(Game.Player.Character, -1);
                }

            }

            if (currentStage == MissionStage.GO)
            {
                if (bruciePed != null)
                {
                    var pos = Game.Player.Character.Position;
                    if (pos.DistanceTo(bruciePed.Position) < 10)
                    {
                        isCloseToSubject = true;
                        currentStage = MissionStage.TALK;
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
            foreach (var ped in temporaryPed)
            {
                ped.IsPersistent = false;
                ped.MarkAsNoLongerNeeded();
            }

            if (bruciePed != null)
            {
                bruciePed.IsPersistent = false;
                bruciePed.MarkAsNoLongerNeeded();
            }

            if (vehicles != null)
            {
                foreach (var vehicle in vehicles)
                {
                    vehicle.IsPersistent = false;
                    vehicle.MarkAsNoLongerNeeded();
                }
            }

            if (blip != null)
                blip.Delete();

            temporaryPed.Clear();
        }
    }
}
