using GTA;
using GTA.Math;
using GTA.Native;
using InworldV.Helper;
using System;

namespace InworldV.Missions
{
    [ScriptAttributes(NoDefaultInstance = true)]
    internal class ReportToSupervisor : PoliceMissionBase
    {
        private Vector3[] sergeantPos = new Vector3[] { new Vector3(446.9011f, -980.8807f, 30.68965f), new Vector3(3.246081E-05f, 1.705492E-06f, 177.1044f) };
        private Vector3 markerPos = new Vector3(457.8029f, -1008.894f, 27.29712f);
        private Blip blip;
        bool IsReporting = false;
        private Ped sergeantPed;
        private MissionStage currentStage = MissionStage.GO;
        private bool isFinished;
        public int DAY = 1;
        private bool startedReporting;

        enum MissionStage
        {
            GO,
            REPORT,
            REPORT_ACTION,
            TALK,
            TALK_ACTION,
            FINISH,
            FINISH_ACTION,
            NONE
        }


        public override void StartMission()
        {
            base.StartMission();
            ActorCharacter = "sergeant_alex_mercer";
            MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Report back to your ~b~captain.", 25000, true, false));
            this.missionState = MissionState.STARTED;
            SetupScene();
            Tick += TickMission;
        }

        public override bool TryToConnectToActor(out Ped connected, out string characterId, out string playerName)
        {
            characterId = ActorCharacter;
            connected = sergeantPed;
            playerName = "Mike";
            if (sergeantPed == null) return false;
            if (sergeantPed.Position.DistanceTo(Game.Player.Character.Position) < 5)
            {
                isCharacterConnectionRequested = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        private void UpdateSergeantLook(Ped ped)
        {
            Function.Call(Hash.SET_PED_COMPONENT_VARIATION, ped, 0, 2, 0, 0);
            Function.Call(Hash.SET_PED_COMPONENT_VARIATION, ped, 3, 1, 0, 0);
            Function.Call(Hash.SET_PED_COMPONENT_VARIATION, ped, 9, 1, 0, 0);
            Function.Call(Hash.SET_PED_COMPONENT_VARIATION, ped, 10, 1, 0, 0);
            Function.Call(Hash.SET_PED_PROP_INDEX, ped, 0, 0, 0, true);
            Function.Call(Hash.SET_PED_PROP_INDEX, ped, 1, 0, 0, true);
        }

        public override void ProcessGameEvent(string id)
        {
            if (id.Contains("greet"))
            {
                if (!startedReporting)
                {
                    currentStage = MissionStage.REPORT;
                    StartReporting();
                    startedReporting = true;
                }
            }
            else if (id.Contains("player_finish_reporting"))
            {
                currentStage = MissionStage.FINISH;
            }
        }


        bool isCloseToSubject = false;
        public override bool IsCloseToSubject => isCloseToSubject;

        private void Stage()
        {
            if (currentStage == MissionStage.REPORT)
            {
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Give report and finish reporting by saying this is all", 15000, true, false));
                currentStage = MissionStage.REPORT_ACTION;
            }
            else if (currentStage == MissionStage.TALK)
            {
                sergeantPed.Task.TurnTo(Game.Player.Character);
                sergeantPed.Task.LookAt(Game.Player.Character);
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Talk to ~b~captain~w~ to give report", 15000, true, false));
                currentStage = MissionStage.TALK_ACTION;
            }
            else if (currentStage == MissionStage.FINISH)
            {
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Go to ~b~marker~w~ to finish the day", 15000, true, false));
                currentStage = MissionStage.FINISH_ACTION;
                blip.Delete();
                blip = World.CreateBlip(markerPos);
                blip.Color = BlipColor.BlueDark;
            }
        }

        private void SetupScene()
        {
            sergeantPed = World.CreatePed(PedHash.Cop01SMY, sergeantPos[0]);
            sergeantPed.CanBeTargetted = false;
            sergeantPed.Task.StandStill(-1);
            UpdateSergeantLook(sergeantPed);
            blip = sergeantPed.AddBlip();
            blip.Color = BlipColor.Blue;
            blip.ShowRoute = true;
        }

        private int incomingMessageTime = 0;
        private bool isCharacterConnectionRequested = false;

        public override void TickIncomingMessage()
        {
            if (!isCharacterConnectionRequested) return;
            incomingMessageTime++;
            if (this.currentStage == MissionStage.TALK_ACTION || this.currentStage == MissionStage.REPORT_ACTION)
            {
                if (incomingMessageTime >= 7)
                {
                    this.currentStage = MissionStage.FINISH;
                }
            }
            else if (this.currentStage == MissionStage.TALK_ACTION)
            {
                if (!startedReporting && incomingMessageTime > 1)
                {
                    currentStage = MissionStage.REPORT;
                    StartReporting();
                    startedReporting = true;
                }
            }
        }

        public void TickMission(object sender, EventArgs e)
        {
            if (this.missionState == MissionState.STARTED)
            {
                Stage();
                if (sergeantPed != null)
                {
                    if (!IsReporting)
                    {
                        sergeantPed.Task.StandStill(-1);
                        sergeantPed.Task.TurnTo(Game.Player.Character, 100);
                        sergeantPed.Task.LookAt(Game.Player.Character, 100);
                    }

                    PedGroup group = Game.Player.Character.PedGroup;
                    if (sergeantPed.PedGroup != group) group.Add(sergeantPed, false);
                }
            }

            if (currentStage == MissionStage.GO)
            {
                if (Game.Player.Character.Position.DistanceTo(sergeantPed.Position) < 10)
                {
                    isCloseToSubject = true;
                    currentStage = MissionStage.TALK;
                }
            }

            if (isFinished) return;

            if (currentStage == MissionStage.FINISH_ACTION)
            {
                GTA.World.DrawMarker(MarkerType.VerticalCylinder, markerPos, new Vector3(), new Vector3(), new Vector3(1, 1, 1), System.Drawing.Color.Blue, true);
            }

            if (Game.Player.Character != null)
            {
                if (Game.Player.Character.Position.DistanceTo(markerPos) < 2)
                {
                    isFinished = true;
                    this.missionState = MissionState.SUCCESS;
                    OnMissionStateChanged(MissionState.SUCCESS);
                }
            }
        }
        private void StartReporting()
        {
            sergeantPed.Task.StartScenario("CODE_HUMAN_MEDIC_TIME_OF_DEATH", 0);
            IsReporting = true;
        }

        public override bool IsFinished()
        {
            return isFinished;
        }

        public override void Cleanup()
        {
            if (sergeantPed != null)
            {
                sergeantPed.MarkAsNoLongerNeeded();
                sergeantPed.Delete();
            }

            if (blip != null)
                blip.Delete();
        }
    }
}
