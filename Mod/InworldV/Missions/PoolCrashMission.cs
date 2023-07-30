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
    internal class PoolCrashMission : PoliceMissionBase
    {
        private Vector3[] firemanPlaces = new Vector3[]
        {
            new Vector3(294.0876f, 464.3472f, 142.8854f), new Vector3(8.53783E-07f,9.341117E-08f,-117.8005f),
            new Vector3(293.9893f, 465.1588f, 142.7596f), new Vector3(8.539134E-07f,9.339075E-08f,179.3879f),
            new Vector3(293.0844f, 463.5705f, 142.7597f), new Vector3(8.443932E-07f,9.947219E-08f,-95.84446f)
        };
        private Vector3 perpPos = new Vector3(298.2492f, 453.917f, 142.8592f);
        private Vector3 crashedCar = new Vector3(298.9696f, 462.3476f, 141.1793f);

        private Blip blip;
        private Vehicle vehicle;
        private Ped perpPed;
        private List<Ped> temporaryPed = new List<Ped>();
        private int currentStage = -5;
        private bool isFinished = false, isTicketWriting = false;


        private int incomingMessageTime = 0;
        private bool isCharacterConnectionRequested = false;

        public override void TickIncomingMessage()
        {
            if (!isCharacterConnectionRequested) return;
            incomingMessageTime++;
            if (this.currentStage == 0)
            {
                if (incomingMessageTime >= 2)
                {
                    this.currentStage = 1;
                }
            }
        }

        public override void StartMission()
        {
            base.StartMission();
            ActorCharacter = "olivia_van_der_woodsen";
            MessageQueue.Instance.AddHelpMessage(new HelpMessage("~y~5-10 ~w~| Car crash reported at the specified address. Fire department is en route.", 5000, true, false));
            this.missionState = MissionState.STARTED;
            Function.Call(Hash.REQUEST_ANIM_DICT, "random@robbery");
            PartnerMentionEventId = "crash_mission_aftermath";
            SetupScene();
            Tick += TickMission;
            KeyDown += OnKeyDown;
        }

        public override bool TryToConnectToActor(out Ped connected, out string characterId, out string playerName)
        {
            characterId = ActorCharacter;
            connected = perpPed;
            playerName = "officer";
            if (perpPed == null) return false;
            if (perpPed.Position.DistanceTo(Game.Player.Character.Position) < 5)
            {
                isCharacterConnectionRequested = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        public override Ped GetArrestPed()
        {
            return perpPed;
        }

        public override void ProcessGameEvent(string id)
        {
            if (id.Contains("order_explain_situation"))
            {
                currentStage = 1;
            }
            else if (id.Contains("goal.complete.player_accepts_bribe"))
            {
                PartnerMentionEventId = "crash_mission_aftermath_bribe";
                currentStage = 2;
                MessageQueue.Instance.AddSubtitleMessage(new SubtitleMessage("~w~You earned ~g~$1000"));
            }
        }
        bool isCloseToSubject = false;
        public override bool IsCloseToSubject => isCloseToSubject;
        private void Stage()
        {
            if (currentStage == 0)
            {

                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Talk to ~y~suspect ~w~by pressing T. Ask her to explain what happened", 5000, true, false));
                currentStage = -1;
            }
            else if (currentStage == 1)
            {
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Press ~INPUT_CONTEXT~ ~b~to issue ticket~w~ OR ~g~talk~w~ with the ~y~suspect.", 5000, true, false));
                currentStage = -3;
            }
            else if (currentStage == 2)
            {
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Return back to your ~b~vehicle", 5000, true, false));
                currentStage = -2;
            }
            else if (currentStage == 3)
            {
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Press ~INPUT_CONTEXT~ again to ~b~finish issuing ticket", 5000, true, false));
            }
            else if (currentStage == -5)
            {
                if (Game.Player.Character.Position.DistanceTo(blip.Position) < 20)
                {
                    currentStage = 0;
                    isCloseToSubject = true;
                }
            }
        }

        private void SetupScene()
        {
            Model carModel = new Model(VehicleHash.Huntley);
            vehicle = World.CreateVehicle(carModel, crashedCar);
            PedGroup group = Game.Player.Character.PedGroup;

            blip = vehicle.AddBlip();
            blip.Color = BlipColor.Yellow5;
            blip.ShowRoute = true;

            for (int i = 0; i < firemanPlaces.Length; i = i + 2)
            {
                Vector3 position = firemanPlaces[i];
                Vector3 rotation = firemanPlaces[i + 1];
                Ped fireman = World.CreatePed(PedHash.Fireman01SMY, position);
                fireman.Rotation = rotation;
                fireman.Task.StandStill(-1);
                group.Add(fireman, false);
                temporaryPed.Add(fireman);
                Function.Call(Hash.TASK_TURN_PED_TO_FACE_ENTITY, fireman, vehicle, 52000);
            }
            perpPed = World.CreatePed(PedHash.Genhot01AFY, perpPos);
            perpPed.Task.PlayAnimation("random@robbery", "stand_worried_female", 100, -1, AnimationFlags.Loop | AnimationFlags.NotInterruptable | AnimationFlags.Secondary);
            perpPed.Task.StandStill(-1);
            perpPed.Task.TurnTo(vehicle);
            group.Add(perpPed, false);
        }

        public void TickMission(object sender, EventArgs e)
        {
            if (this.missionState == MissionState.STARTED)
            {
                Stage();
                PedGroup group = Game.Player.Character.PedGroup;
                if (perpPed != null)
                {
                    if (perpPed.PedGroup != group) group.Add(perpPed, false);
                }

                foreach (var ped in temporaryPed)
                {
                    if (ped.PedGroup != group) group.Add(ped, false);
                    ped.Task.TurnTo(vehicle);
                }
            }

            if (currentStage == -2)
            {
                if (Game.Player.Character.IsInVehicle(policeVehicle))
                {
                    isFinished = true;
                    this.missionState = MissionState.SUCCESS;
                    OnMissionStateChanged(MissionState.SUCCESS);
                }
            }
        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (currentStage == -3 || currentStage == 3)
            {
                if (e.KeyCode == Keys.E)
                {
                    if (!isTicketWriting)
                    {
                        WriteTicket();
                        currentStage = 3;
                    }
                    else
                    {
                        Game.Player.Character.Task.ClearAll();
                        isTicketWriting = false;
                        currentStage = 2;
                    }
                }
            }
        }

        private void WriteTicket()
        {
            isTicketWriting = true;
            Game.Player.Character.Task.StartScenario("CODE_HUMAN_MEDIC_TIME_OF_DEATH", 0);
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

            if (perpPed != null)
            {
                perpPed.IsPersistent = false;
                perpPed.MarkAsNoLongerNeeded();
            }

            if (vehicle != null)
            {
                vehicle.IsPersistent = false;
                vehicle.MarkAsNoLongerNeeded();
            }

            if (blip != null)
                blip.Delete();

            temporaryPed.Clear();
        }
    }

}
