using ExtensionMethods;
using GTA;
using GTA.Math;
using InworldV.Helper;
using System;
using System.Collections.Generic;

namespace InworldV.Missions
{
    [ScriptAttributes(NoDefaultInstance = true)]
    internal class LiquorSmugglingMission : PoliceMissionBase
    {
        private Vector3[] goons = new Vector3[8]
        {
            new Vector3(-1268.825f, -826.3788f, 17.09917f), new Vector3(-3.493119E-05f,-8.696957E-06f,101.9958f),
            new Vector3(-1267.601f, -823.2741f, 17.09917f), new Vector3(-3.493515E-05f,-8.694904E-06f,-6.607692f),
            new Vector3(-1281.055f, -826.3003f, 17.09918f), new Vector3(-3.494143E-05f,-8.693854E-06f,15.74733f),
            new Vector3(-1283.366f, -824.1352f, 17.10768f), new Vector3(-3.494075E-05f,-8.693602E-06f,9.493263f)
        };

        private Vector3[] vehiclePosition = new Vector3[2]
        {
            new Vector3(-1274.304f, -809.9395f, 16.63912f), new Vector3(0.07493573f,-9.377295f,124.2747f)
        };

        private Vector3[] bossPosition = new Vector3[2]
        {
            new Vector3(-1269.068f, -824.6492f, 17.09915f), new Vector3(-3.493398E-05f,-8.705864E-06f,70.32026f)
        };

        private Blip blip;
        private Vehicle vehicle;
        private Ped mafiaBossPed;
        private List<Ped> goonPeds = new List<Ped>();
        private Dictionary<Ped, Blip> goonBlips = new Dictionary<Ped, Blip>();
        private MissionStage currentStage = MissionStage.GO;
        private bool isFinished = false;
        private float elapsedMs;

        enum MissionStage
        {
            GO,
            TALK,
            TALK_ACTION,
            DECIDE,
            DECIDE_ACTION,
            ELIMINATE,
            ELIMINATE_ACTION,
            GO_CAR,
            GO_CAR_ACTION,
            NONE
        }

        public LiquorSmugglingMission()
        {
        }

        public override void StartMission()
        {
            base.StartMission();
            ActorCharacter = "vincent_the_viper_moretti";
            MessageQueue.Instance.AddHelpMessage(new HelpMessage("~o~8-22 ~w~| A caller reported a suspicious van and possible smuggling activity. Need a unit to investigate and report back", 5000, true, false));
            this.missionState = MissionState.STARTED;
            SetupScene();
            Tick += TickMission;
        }

        void HandleTimerElapsed()
        {
            elapsedMs += (Game.LastFrameTime * 1000);
        }

        public override bool TryToConnectToActor(out Ped connected, out string characterId, out string playerName)
        {
            characterId = ActorCharacter;
            connected = mafiaBossPed;
            playerName = "Officer";
            if (mafiaBossPed == null) return false;
            if (mafiaBossPed.Position.DistanceTo(Game.Player.Character.Position) < 5)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private float attackTimeout = -1;
        private bool AreTheyHostile = false;
        public override void ProcessGameEvent(string id)
        {
            if (id.Contains("player_accepts_bribe"))
            {
                PartnerMentionEventId = "smuggle_mission_bribe";
                currentStage = MissionStage.GO_CAR;
            }
            else if (id.Contains("player_reject_bribe"))
            {
                attackTimeout = elapsedMs;
            }
            else if (id.Contains("boss_explain_illegal_activities"))
            {
                currentStage = MissionStage.DECIDE;
            }
        }

        private void StartAttackingPlayer()
        {
            AreTheyHostile = true;
            AudioManager.Instance.StopEverythingAbruptly();
            this.OnMissionDialogueStateChanged(ActorCharacter, DialogueState.DISCONNECT);
            var relgroup = World.AddRelationshipGroup("attacker");
            relgroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Hate, true);
            PedGroup pg = new PedGroup();
            var player = Game.Player.Character;
            foreach (var goon in goonPeds)
            {
                goon.Task.ClearAllImmediately();
                pg.Add(goon, false);
                goon.Task.ShootAt(player);
                goon.IsEnemy = true;
                goon.RelationshipGroup = relgroup;
                Blip bl = goon.AddBlip();
                bl.Color = BlipColor.Red2;
                bl.ScaleX = 0.5f;
                bl.ScaleY = 0.5f;
                goonBlips.Add(goon, bl);
            }
            mafiaBossPed.IsEnemy = true;
            mafiaBossPed.LeaveGroup();
            mafiaBossPed.Task.ClearAllImmediately();
            mafiaBossPed.Task.ShootAt(player);
            mafiaBossPed.RelationshipGroup = relgroup;
            pg.Add(mafiaBossPed, true);
            Blip blBoss = mafiaBossPed.AddBlip();
            blBoss.Color = BlipColor.Red2;
            blBoss.ScaleX = 0.5f;
            blBoss.ScaleY = 0.5f;
            goonBlips.Add(mafiaBossPed, blBoss);
            blip.Delete();
            blip = null;
        }

        private void Stage()
        {
            if (attackTimeout != -1)
            {
                if (elapsedMs - attackTimeout > 3500)
                {
                    attackTimeout = -1;
                    currentStage = MissionStage.ELIMINATE;
                    StartAttackingPlayer();
                }
            }

            if (currentStage == MissionStage.TALK)
            {
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Talk to ~y~suspect ~w~ and find out what is happening", 5000, true, false));
                currentStage = MissionStage.NONE;
                mafiaBossPed.Task.StandStill(-1);
            }
            else if (currentStage == MissionStage.DECIDE)
            {
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Convince the suspect to stop his illegitimate activity.", 5000, true, false));
                currentStage = MissionStage.DECIDE_ACTION;
                mafiaBossPed.Task.StandStill(-1);
            }
            else if (currentStage == MissionStage.ELIMINATE)
            {
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~r~Eliminate~w~ the threats.", 5000, true, false));
                currentStage = MissionStage.ELIMINATE_ACTION;
                PartnerMentionEventId = "smuggle_mission";
            }
            else if (currentStage == MissionStage.GO_CAR)
            {
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Go back to ~b~your car", 5000, true, false));
                currentStage = MissionStage.GO_CAR_ACTION;
            }
        }

        private void SetupScene()
        {
            var relgroup = Game.Player.Character.RelationshipGroup;
            mafiaBossPed = World.CreatePed(PedHash.Business03AMY, bossPosition[0]);
            mafiaBossPed.Rotation = -1 * bossPosition[1];
            mafiaBossPed.CanBeTargetted = false;
            mafiaBossPed.IsEnemy = false;
            mafiaBossPed.Weapons.Give(WeaponHash.Pistol, 100, false, true);
            mafiaBossPed.Task.StandStill(-1);
            mafiaBossPed.RelationshipGroup = relgroup;
            blip = mafiaBossPed.AddBlip();
            blip.Color = BlipColor.Orange;
            blip.ShowRoute = true;

            for (int i = 0; i < goons.Length; i++)
            {
                PedHash[] hash = new PedHash[3] { PedHash.Goons01GMM, PedHash.MexGoon01GMY, PedHash.MexGoon02GMY };
                Model cultModel = new Model(hash.GetRandomElement());
                Ped goonMember = World.CreatePed(cultModel, goons[i]);
                goonMember.IsEnemy = false;
                goonMember.Rotation = -1 * goons[i];
                goonMember.Weapons.Give(WeaponHash.Pistol, 100, false, true);
                goonMember.RelationshipGroup = relgroup;
                i++;
                goonPeds.Add(goonMember);
            }

            Model carModel = new Model(VehicleHash.Mule3);
            vehicle = World.CreateVehicle(carModel, vehiclePosition[0]);
            vehicle.Rotation = vehiclePosition[1];
        }

        bool isCloseToSubject = false;
        public override bool IsCloseToSubject => isCloseToSubject;

        public void TickMission(object sender, EventArgs e)
        {
            HandleTimerElapsed();

            if (this.missionState == MissionState.STARTED)
            {
                Stage();

                if (!AreTheyHostile)
                {
                    // Make everyone wait
                    foreach (var goon in goonPeds)
                    {
                        if (goon.IsAlive)
                        {
                            goon.Task.StandStill(-1);
                        }
                    }

                    if (mafiaBossPed != null && mafiaBossPed.IsAlive)
                    {
                        mafiaBossPed.Task.StandStill(-1);
                    }
                }
            }

            if (currentStage == MissionStage.GO && mafiaBossPed != null)
            {
                if (Game.Player.Character.Position.DistanceTo(mafiaBossPed.Position) < 10)
                {
                    isCloseToSubject = true;
                    currentStage = MissionStage.TALK;
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
            else
            {
                bool isAnyAlive = false;

                foreach (var goon in goonPeds)
                {
                    if (goon != null && goon.Health > 0)
                    {
                        isAnyAlive = true;
                    }
                    else
                    {
                        if (goonBlips.ContainsKey(goon))
                        {
                            var b = goonBlips[goon];
                            b.Delete();
                            goonBlips.Remove(goon);
                        }
                    }
                }

                if (mafiaBossPed != null && mafiaBossPed.Health > 0)
                {
                    isAnyAlive = true;
                }
                else
                {
                    if (goonBlips.ContainsKey(mafiaBossPed))
                    {
                        var b = goonBlips[mafiaBossPed];
                        b.Delete();
                        goonBlips.Remove(mafiaBossPed);
                    }
                }

                if (!isAnyAlive)
                {
                    if (currentStage != MissionStage.GO_CAR_ACTION || currentStage != MissionStage.GO_CAR)
                    {
                        PartnerMentionEventId = "smuggle_mission";
                        currentStage = MissionStage.GO_CAR;
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
            foreach (var ped in goonPeds)
            {
                ped.IsPersistent = false;
                ped.MarkAsNoLongerNeeded();
            }

            if (mafiaBossPed != null)
            {
                mafiaBossPed.IsPersistent = false;
                mafiaBossPed.MarkAsNoLongerNeeded();
            }

            if (vehicle != null)
            {
                vehicle.IsPersistent = false;
                vehicle.MarkAsNoLongerNeeded();
            }

            if (blip != null)
                blip.Delete();


            foreach (var goonBlip in goonBlips)
            {
                if (goonBlip.Value != null)
                {
                    goonBlip.Value.Delete();
                }
            }
            goonBlips.Clear();

            goonPeds.Clear();

            this.OnMissionDialogueStateChanged(ActorCharacter, DialogueState.DISCONNECT);
        }

    }
}
