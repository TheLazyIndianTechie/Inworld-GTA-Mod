using GTA;
using GTA.Math;
using InworldV.Helper;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace InworldV.Missions
{
    [ScriptAttributes(NoDefaultInstance = true)]
    internal class QueenMission : PoliceMissionBase
    {
        private Vector3 queenPos = new Vector3(-430.9923f, 1133.533f, 325.9046f);
        private Vector3[] queenWaypoint = new Vector3[2] { new Vector3(-430.9923f, 1133.533f, 325.9046f), new Vector3(-438.9611f, 1136.808f, 325.9046f) };

        private Blip blip;
        private Vehicle vehicle;
        private Ped queenPed;
        private List<Ped> temporaryPed = new List<Ped>();
        private MissionStage currentStage = MissionStage.GO;
        private bool isFinished = false, isTicketWriting = false;

        enum MissionStage
        {
            GO,
            TALK,
            TALK_ACTION,
            DECIDE,
            DECIDE_ACTION,
            TICKET,
            GO_CAR,
            GO_CAR_ACTION,
            END_TICKET,
            NONE
        }

        public QueenMission()
        {
        }
        public override void StartMission()
        {
            base.StartMission();
            ActorCharacter = "edna_quirke";
            MessageQueue.Instance.AddHelpMessage(new HelpMessage("~y~5-12 ~w~| Dispatch, a public disturbance is reported.", 3000, true, false));
            this.missionState = MissionState.STARTED;
            SetupScene();

            Tick += TickMission;
            KeyDown += OnKeyDown;
        }

        public override bool TryToConnectToActor(out Ped connected, out string characterId, out string playerName)
        {
            characterId = ActorCharacter;
            connected = queenPed;
            playerName = "Loyal Subject";
            if (queenPed == null) return false;
            if (queenPed.Position.DistanceTo(Game.Player.Character.Position) < 5)
            {
                isCharacterConnectionRequested = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void ProcessGameEvent(string id)
        {
            if (id.Contains("provide_explanation_goal"))
            {
                currentStage = MissionStage.DECIDE;
            }
        }

        bool isCloseToSubject = false;
        public override bool IsCloseToSubject => isCloseToSubject;

        private void Stage()
        {
            if (currentStage == MissionStage.TALK)
            {
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Talk to ~y~suspect ~w~ on the field and ask to explain what is happening", 3000, true, false));
                currentStage = MissionStage.NONE;
            }
            else if (currentStage == MissionStage.DECIDE)
            {
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Press ~INPUT_CONTEXT~ ~b~to issue ticket OR walk back to your ~b~car ~w~and leave it be.", 3000, true, false));
                currentStage = MissionStage.DECIDE_ACTION;
            }
            else if (currentStage == MissionStage.TICKET)
            {
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Press ~INPUT_CONTEXT~ again to ~b~finish issuing ticket", 3000, true, false));
                currentStage = MissionStage.END_TICKET;
            }
            else if (currentStage == MissionStage.GO_CAR)
            {
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Go back to ~b~your car", 5000, true, false));
                currentStage = MissionStage.GO_CAR_ACTION;
            }
        }

        private int incomingMessageTime = 0;
        private bool isCharacterConnectionRequested;
        public override void TickIncomingMessage()
        {
            if (!isCharacterConnectionRequested) return;

            incomingMessageTime++;
            if (this.currentStage == MissionStage.NONE)
            {
                if (incomingMessageTime >= 3)
                {
                    this.currentStage = MissionStage.DECIDE;
                }
            }
        }

        private void SetupScene()
        {
            queenPed = World.CreatePed(PedHash.MrsThornhill, queenPos);
            queenPed.CanBeTargetted = false;

            blip = queenPed.AddBlip();
            blip.Color = BlipColor.Yellow2;
            blip.ShowRoute = true;
        }

        public void TickMission(object sender, EventArgs e)
        {
            if (this.missionState == MissionState.STARTED)
            {
                Stage();

                if (queenPed != null)
                {
                    PedGroup group = Game.Player.Character.PedGroup;
                    if (queenPed.PedGroup != group) group.Add(queenPed, false);
                    queenPed.Task.StandStill(-1);


                    if (currentStage == MissionStage.GO)
                    {
                        if (Game.Player.Character.Position.DistanceTo(queenPed.Position) < 10)
                        {
                            isCloseToSubject = true;
                            currentStage = MissionStage.TALK;
                        }
                    }
                }
            }

            if (currentStage == MissionStage.GO_CAR_ACTION || currentStage == MissionStage.DECIDE_ACTION)
            {
                if (Game.Player.Character.IsInVehicle(policeVehicle))
                {
                    if (!didWriteTicket)
                        PartnerMentionEventId = "queen_mission_aftermath_noticket";
                    isFinished = true;
                    this.missionState = MissionState.SUCCESS;
                    OnMissionStateChanged(MissionState.SUCCESS);
                }
            }
        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (currentStage == MissionStage.DECIDE_ACTION || currentStage == MissionStage.END_TICKET)
            {
                if (e.KeyCode == Keys.E)
                {
                    if (!isTicketWriting)
                    {
                        WriteTicket();
                        currentStage = MissionStage.TICKET;
                    }
                    else
                    {
                        Game.Player.Character.Task.ClearAllImmediately();
                        isTicketWriting = false;
                        currentStage = MissionStage.GO_CAR;
                    }
                }
            }
        }

        private bool didWriteTicket;
        private void WriteTicket()
        {
            didWriteTicket = true;
            PartnerMentionEventId = "queen_mission_aftermath_ticket";
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

            if (queenPed != null)
            {
                queenPed.IsPersistent = false;
                queenPed.MarkAsNoLongerNeeded();
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
