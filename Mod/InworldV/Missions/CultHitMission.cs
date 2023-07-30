using ExtensionMethods;
using GTA;
using GTA.Math;
using GTA.Native;
using InworldV.Helper;
using System;
using System.Collections.Generic;
using System.Timers;

namespace InworldV.Missions
{
    [ScriptAttributes(NoDefaultInstance = true)]
    internal class CultHitMission : PoliceMissionBase
    {

        enum MissionStage
        {
            KILL,
            KILL_ACTION,
            TALK,
            TALK_ACTION,
            CORNERED,
            CORNERED_ACTION,
            REVEAL,
            REVEAL_ACTION,
            NONE
        }

        private Vector3[] leaderPosition = new Vector3[] { new Vector3(-461.6105f, -1058.502f, 52.47615f), new Vector3(-1.805129E-08f, -2.487519E-10f, 45.60057f) };
        private Vector3[] goonPositions = new Vector3[] {
            new Vector3(-453.814f, -912.6782f, 29.39284f), new Vector3(-2.040793E-08f,0f,92.84556f),
            new Vector3(-448.8609f, -924.7563f, 29.39284f), new Vector3(-2.311943E-08f,-3.975693E-16f,99.38032f),
            new Vector3(-444.2224f, -905.524f, 29.39285f), new Vector3(-2.626757E-08f,0f,93.1919f),
            new Vector3(-451.3101f, -888.3243f, 29.39283f), new Vector3(-1.774248E-08f,0f,-5.111219f),
            new Vector3(-467.3159f, -927.1324f, 34.03624f), new Vector3(-1.774969E-08f,0f,90.63985f),
            new Vector3(-466.4733f, -904.9284f, 38.68372f), new Vector3(-1.774969E-08f,0f,95.93476f),
            new Vector3(-466.5798f, -925.9648f, 43.30981f), new Vector3(-1.775046E-08f,0f,-6.123837f),
            new Vector3(-451.598f, -892.9027f, 47.98392f), new Vector3(-1.757342E-08f,2.621731E-11f,42.72953f),
            new Vector3(-473.3066f, -1040.114f, 52.47617f), new Vector3(-1.805145E-08f,-2.492736E-10f,55.77367f),
            new Vector3(-488.9596f, -1032.783f, 52.47617f), new Vector3(-1.805298E-08f,-2.508391E-10f,-4.213536f),
            new Vector3(-487.7816f, -1010.527f, 52.47668f), new Vector3(-1.805333E-08f,-2.518427E-10f,10.97433f),
            new Vector3(-476.3221f, -991.6649f, 50.49024f), new Vector3(-1.809358E-08f,-3.158795E-10f,-105.7664f),
            new Vector3(-468.3f, -978.2965f, 48.20937f), new Vector3(-1.809357E-08f,-3.158247E-10f,-3.951346f),
            new Vector3(-468.0989f, -959.1507f, 47.98116f), new Vector3(-1.80934E-08f,-3.152669E-10f,7.306758f),
        };

        private Blip blip;
        private PedGroup cultGroup;
        private RelationshipGroup relGroup;
        private Ped leaderPed;
        private List<Ped> temporaryPed = new List<Ped>();
        private Dictionary<Ped, Blip> blipPairs = new Dictionary<Ped, Blip>();
        private MissionStage currentStage = MissionStage.NONE;
        private bool isFinished = false;
        private List<Ped> shootOrderedPeds = new List<Ped>();
        public Action<bool> TurnBackTrigger;
        public Action TeleportTrigger;
        private Timer timer;
        private int elapsedMs = 0;
        private int deferredTrigger = -1;
        private int incomingMessageTime = 0;
        private bool isTeleported = false;
        private bool isCharacterConnectionRequested = false;

        public CultHitMission()
        {
        }

        public override void TickIncomingMessage()
        {
            if (!isCharacterConnectionRequested) return;
            incomingMessageTime++;
            if (incomingMessageTime > 0 && !isTeleported)
            {
                isTeleported = true;
                // Teleport partner to back
                TeleportTrigger?.Invoke();
            }
        }

        public override void StartMission()
        {
            base.StartMission();
            timer = new Timer(500);
            timer.Elapsed += HandleTimerElapsed;
            timer.Start();

            ActorCharacter = "benjamin_steel";
            MessageQueue.Instance.AddHelpMessage(new HelpMessage("~r~5-19 ~w~| Dispatch, illegal activity and gunshot reported. Proceed with caution.", 5000, true, false));
            this.missionState = MissionState.STARTED;
            SetupScene();
            Tick += TickMission;
        }

        void HandleTimerElapsed(object sender, ElapsedEventArgs e)
        {
            elapsedMs += 500;
        }

        public override bool TryToConnectToActor(out Ped connected, out string characterId, out string playerName)
        {
            characterId = ActorCharacter;
            connected = leaderPed;
            playerName = "officer";
            if (leaderPed == null) return false;
            if (leaderPed.Position.DistanceTo(Game.Player.Character.Position) < 5)
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
            if (id.Contains("order_explain_reveal"))
            {
                currentStage = MissionStage.CORNERED;
            }
            else if (id.Contains("cornered"))
            {
                currentStage = MissionStage.REVEAL;
            }
            else if (id.Contains("reveal_partner_plan"))
            {
                currentStage = MissionStage.KILL;
            }
        }

        private void Stage()
        {
            if (currentStage == MissionStage.TALK)
            {
                AudioManager.Instance.StopSoundtrack();
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Talk to ~r~leader~w~ and ask to explain the cult and the plan", 5000, true, false));
                currentStage = MissionStage.TALK_ACTION;
                leaderPed.Task.StandStill(-1);
            }
            else if (currentStage == MissionStage.CORNERED)
            {
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Tell ~r~leader~w~ to give up and he cannot escape", 5000, true, false));
                currentStage = MissionStage.CORNERED_ACTION;
                leaderPed.Task.StandStill(-1);
            }
            else if (currentStage == MissionStage.REVEAL)
            {
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Ask about your ~r~partner.", 5000, true, false));
                currentStage = MissionStage.REVEAL_ACTION;
            }
            else if (currentStage == MissionStage.KILL)
            {
                currentStage = MissionStage.KILL_ACTION;
                deferredTrigger = elapsedMs + 3000;
            }
        }

        private Ped CreateCultMember(Vector3 spawnPosition, PedGroup cultGroup, RelationshipGroup relgroup)
        {
            PedHash[] hash = new PedHash[3] { PedHash.Acult01AMO, PedHash.Acult02AMO, PedHash.Acult02AMY };
            Model cultModel = new Model(hash.GetRandomElement());
            Ped cultMember = World.CreatePed(cultModel, spawnPosition);
            cultMember.Weapons.Give(WeaponHash.Machete, 1, true, true);
            cultMember.Weapons.Give(WeaponHash.VintagePistol, 50, true, true);
            cultMember.RelationshipGroup = relgroup;
            if (cultGroup.MemberCount % 3 == 0)
                cultMember.Weapons.Give(WeaponHash.AssaultRifle, 20, true, true);
            cultGroup.Add(cultMember, false);
            cultMember.Task.StandStill(-1);
            Function.Call(Hash.SET_PED_AS_GROUP_MEMBER, cultMember, cultGroup);
            return cultMember;
        }

        private void SetupScene()
        {
            CreateFanatics();
            CreateLeader();
            blip = World.CreateBlip(new Vector3(-522.993f, -963.8549f, 23.17476f));
            blip.Color = BlipColor.Red;
            blip.ShowRoute = true;
        }

        private void CreateLeader()
        {
            Vector3 leaderPos = leaderPosition[0];
            leaderPed = World.CreatePed(PedHash.MLCrisis01AMM, leaderPos);
            leaderPed.Rotation = leaderPosition[1];
            leaderPed.CanBeTargetted = false;
            leaderPed.IsInvincible = true;
            cultGroup.Add(leaderPed, true);
            leaderPed.Task.StandStill(-1);
            leaderPed.Task.Wait(-1);
            Function.Call(Hash.SET_PED_AS_GROUP_MEMBER, leaderPed, cultGroup);
        }

        private void ChangeLeaderStats()
        {
            leaderPed.Task.ClearAllImmediately();
            leaderPed.LeaveGroup();
            leaderPed.Weapons.Give(WeaponHash.APPistol, 50, true, true);
            leaderPed.CanBeTargetted = true;
            leaderPed.IsInvincible = false;
            // Depending on players actions like killing the unarmed people etc turn against him
            if (TurnBackTrigger != null)
            {
                TurnBackTrigger.Invoke(true);
            }
        }

        private void CreateFanatics()
        {
            cultGroup = new PedGroup();
            relGroup = World.AddRelationshipGroup("Cult");
            relGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Hate, true);
            Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(relGroup, Relationship.Hate, true);

            for (int i = 0; i < goonPositions.Length; i++)
            {
                Ped member = CreateCultMember(goonPositions[i], cultGroup, relGroup);
                member.Rotation = goonPositions[i + 1];
                i++;
                Blip bl = member.AddBlip();
                bl.Color = BlipColor.Red2;
                bl.ScaleX = 0.5f;
                bl.ScaleY = 0.5f;
                bl.ShowRoute = true;
                blipPairs.Add(member, bl);
                temporaryPed.Add(member);
                member.Task.StandStill(-1);
            }
        }

        private bool reachedDestination;

        public override bool IsCloseToSubject => true;

        public void TickMission(object sender, EventArgs e)
        {
            if (this.missionState == MissionState.STARTED)
            {
                if (leaderPed != null && (currentStage == MissionStage.NONE || currentStage == MissionStage.TALK || currentStage == MissionStage.TALK_ACTION))
                {
                    leaderPed.Task.StandStill(-1);
                }

                if (!reachedDestination && blip != null)
                {
                    if (Game.Player.Character.Position.DistanceTo(blip.Position) < 8)
                    {
                        AudioManager.Instance.StartSoundtrack(0);
                        reachedDestination = true;
                        blip.Delete();
                        blip = leaderPed.AddBlip();
                        blip.Color = BlipColor.Red;
                        blip.ShowRoute = true;
                    }
                }

                Stage();
                CheckConditions();
                CheckAndAttack();


                if (deferredTrigger != -1)
                {
                    if (elapsedMs >= deferredTrigger)
                    {
                        deferredTrigger = -1;
                        MessageQueue.Instance.AddHelpMessage(new HelpMessage("~r~Kill~w~ both of them", 5000, true, false));
                        // turn back and stuff
                        ChangeLeaderStats();
                    }
                }
            }
        }

        private bool triggeredTalk = false;
        private void CheckAndAttack()
        {
            var arrayCopy = temporaryPed.ToArray();
            foreach (var goon in arrayCopy)
            {
                if (goon.IsAlive)
                {
                    if (goon.Position.DistanceTo(Game.Player.Character.Position) < 5)
                    {
                        if (!shootOrderedPeds.Contains(goon))
                        {
                            goon.Task.ClearAllImmediately();
                            goon.Task.ShootAt(Game.Player.Character);
                            shootOrderedPeds.Add(goon);
                        }
                    }
                }
                else
                {
                    goon.MarkAsNoLongerNeeded();
                    temporaryPed.Remove(goon);
                    if (blipPairs.ContainsKey(goon))
                    {
                        blipPairs[goon].Delete();
                        blipPairs.Remove(goon);
                    }
                }
            }

            if (temporaryPed.Count == 0 && !triggeredTalk)
            {
                currentStage = MissionStage.TALK;
                triggeredTalk = true;
            }
        }

        private void CheckConditions()
        {
            if (isFinished) return;
            bool canFinish = true;
            foreach (var goon in temporaryPed)
            {
                if (goon != null)
                {
                    if (goon.IsAlive)
                    {
                        canFinish = false;
                        return;
                    }
                }
            }

            if (canFinish)
            {
                if (currentStage == MissionStage.KILL_ACTION)
                {
                    if (leaderPed != null && !leaderPed.IsAlive)
                    {
                        isFinished = true;
                        this.missionState = MissionState.SUCCESS;
                        OnMissionStateChanged(MissionState.SUCCESS);

                        this.OnMissionDialogueStateChanged(ActorCharacter, DialogueState.DISCONNECT);
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
            foreach (var ped in temporaryPed)
            {
                ped.IsPersistent = false;
                ped.MarkAsNoLongerNeeded();
            }

            foreach (var blipPair in blipPairs)
            {
                if (blipPair.Value != null)
                {
                    blipPair.Value.Delete();
                }
            }

            if (leaderPed != null)
            {
                leaderPed.IsPersistent = false;
                leaderPed.MarkAsNoLongerNeeded();
            }

            if (blip != null)
                blip.Delete();

            blipPairs.Clear();
            temporaryPed.Clear();
        }
    }
}
