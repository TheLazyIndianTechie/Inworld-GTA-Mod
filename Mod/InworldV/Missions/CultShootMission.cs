using ExtensionMethods;
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
    internal class CultShootMission : PoliceMissionBase
    {
        private enum MissionStage
        {
            UNKNOWN,
            ELIMINATE,
            ELIMINATE_ACTION,
            TALK,
            DECIDE,
            DECIDE_ACTION,
            GO_CAR,
            CHECK_CAR
        };

        Vector3[] cars = new Vector3[]
        {
            new Vector3(1817.325f, 330.5203f, 171.8705f), new Vector3(6.683361E-06f,-1.527684E-09f,140.3506f),
            new Vector3(1805.334f, 344.7235f, 171.9205f), new Vector3(6.683314E-06f,0f,-18.65784f),
            new Vector3(1814.475f, 348.2505f, 171.697f), new Vector3(6.683604E-06f,0f,99.82596f),
            new Vector3(1840.716f, 313.7353f, 161.3612f), new Vector3(-7.048566E-05f,0f,-10.48265f),
            new Vector3(1803.259f, 372.6614f, 171.6088f), new Vector3(-8.47266E-05f,3.256888E-12f,-48.20987f)
        };

        Vector3[] bodyLocations = new Vector3[]
        {
            new Vector3(1813.694f, 329.9965f, 171.4579f),
            new Vector3(1805.453f, 339.7641f, 172.0123f),
            new Vector3(1813.614f, 344.512f, 171.7452f),
        };

        // Reduce amount so that we will have a chance
        Vector3[] locations = new Vector3[] {
            //new Vector3(1800.799f, 347.6296f, 172.6684f),
            //new Vector3(1819.76f, 347.481f, 171.4927f),
            //new Vector3(1820.229f, 325.373f, 171.9107f),
            //new Vector3(1818.237f, 320.5696f, 171.7791f),
            //new Vector3(1805.308f, 309.4589f, 171.5831f),
            //new Vector3(1842.484f, 324.9873f, 161.4843f),
            new Vector3(1797.709f, 358.423f, 171.9092f),
            new Vector3(1799.672f, 353.8398f, 171.851f),
            new Vector3(1814.06f, 303.763f, 172.6151f),
            new Vector3(1798.276f, 309.0478f, 171.6584f),
            new Vector3(1819.51f, 354.7814f, 172.0002f),
            new Vector3(1843.399f, 335.0944f, 161.4841f),
            new Vector3(1838.413f, 312.7782f, 161.3006f)
        };

        Vector3 finalFanatic = new Vector3(1819.51f, 354.7814f, 172.0002f);

        private MissionStage currentStage = MissionStage.UNKNOWN;
        Dictionary<Ped, Blip> blipList = new Dictionary<Ped, Blip>();
        List<Ped> enemyList = new List<Ped>();
        List<Ped> bodies = new List<Ped>();
        List<Vehicle> vehicleList = new List<Vehicle>();
        bool haveLeader = false;
        bool shootoutStarted = false;

        PedGroup cultGroup;
        RelationshipGroup relGroup;
        private Ped lastFanaticPed;
        private Blip decisionBlip;

        public CultShootMission()
        {

        }

        public override void StartMission()
        {
            base.StartMission();
            MessageQueue.Instance.AddHelpMessage(new HelpMessage("~r~10-33 ~w~| Caller reported gunshots and multiple assailants attacking the convoy. Suspects are armed and dangerous!", 5000, true, false));
            this.missionState = MissionState.STARTED;
            Function.Call(Hash.REQUEST_ANIM_DICT, "amb@world_human_picnic@male@base");
            CreateMission();
            Tick += TickMission;
            KeyDown += OnKeyDown;
        }

        public override void ProcessGameEvent(string id)
        {
            if (id.Contains("complete.order_explain_situation"))
            {
                this.currentStage = MissionStage.DECIDE;
            }
        }

        private int incomingMessageTime = 0;
        private bool isCharacterConnectionRequested = false;
        public override void TickIncomingMessage()
        {
            if (!isCharacterConnectionRequested) return;
            incomingMessageTime++;
            if (this.currentStage == MissionStage.UNKNOWN)
            {
                if (incomingMessageTime >= 3)
                {
                    this.currentStage = MissionStage.DECIDE;
                }
            }
        }

        private void TickStageMessages()
        {
            if (currentStage == MissionStage.ELIMINATE)
            {
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~w~Eliminate ~r~threats", 5000, true, false));
                currentStage = MissionStage.ELIMINATE_ACTION;
            }
            if (currentStage == MissionStage.TALK)
            {
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("Talk to ~r~perpetrator ~w~ by pressing T and ask him to explain what happened here", 5000, true, false));
                currentStage = MissionStage.UNKNOWN;
            }
            else if (currentStage == MissionStage.DECIDE)
            {
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("~r~Eliminate ~w~the threat or ~b~press ~INPUT_CONTEXT~ to handcuff ~w~and leave for tactical units", 5000, true, false));
                currentStage = MissionStage.DECIDE_ACTION;
                ChangeFanaticStats();
            }
            else if (currentStage == MissionStage.GO_CAR)
            {
                MessageQueue.Instance.AddHelpMessage(new HelpMessage("Go back to ~b~your car", 5000, true, false));
                currentStage = MissionStage.CHECK_CAR;
            }
        }

        private void ChangeFanaticStats()
        {
            lastFanaticPed.LeaveGroup();
            cultGroup.Add(lastFanaticPed, false);
            var relg = World.AddRelationshipGroup("somegroup");
            Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(lastFanaticPed.RelationshipGroup, Relationship.Hate, false);
            lastFanaticPed.RelationshipGroup = relg;
            lastFanaticPed.IsEnemy = true;
            lastFanaticPed.CanBeTargetted = true;
            lastFanaticPed.BlockPermanentEvents = true;
            lastFanaticPed.Task.StandStill(-1);
            lastFanaticPed.Task.Wait(-1);
            Function.Call(Hash.SET_ENTITY_CAN_BE_DAMAGED, lastFanaticPed.Handle, true);
        }

        private void TickStageMissions()
        {
            TickStageMessages();
        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (this.currentStage == MissionStage.DECIDE_ACTION)
            {
                if (e.KeyCode == Keys.E)
                {
                    this.CallUnitsIn();
                }
            }
        }

        bool isCloseToSubject = false;
        public override bool IsCloseToSubject => isCloseToSubject;

        public void TickMission(object sender, EventArgs e)
        {
            TickStageMissions();
            if (this.missionState == MissionState.STARTED)
            {

                if (!shootoutStarted)
                {
                    var loc = locations[0];
                    float dist = loc.DistanceTo(Game.Player.Character.Position);
                    if (dist < 150)
                    {
                        foreach (Ped p in enemyList)
                        {
                            if (p.Health > 0)
                            {
                                p.Task.FightAgainstHatedTargets(150);
                            }
                        }
                        shootoutStarted = true;
                        currentStage = MissionStage.ELIMINATE;
                        isCloseToSubject = true;
                    }
                }
                else
                {
                    IsFinished();
                }
            }
        }

        public override bool TryToConnectToActor(out Ped connected, out string characterId, out string playerName)
        {
            characterId = string.Empty;
            connected = null;
            playerName = "cop";
            if (lastFanaticPed == null) return false;
            if (lastFanaticPed.Position.DistanceTo(Game.Player.Character.Position) < 5)
            {
                characterId = "damien_vex";
                connected = lastFanaticPed;
                isCharacterConnectionRequested = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool IsFinished()
        {
            if (this.missionState == MissionState.SUCCESS || this.missionState == MissionState.FAILED) return true;
            if (this.missionState != MissionState.STARTED) return false;
            bool isFinished = true;
            foreach (Ped p in enemyList)
            {
                if (p.Health > 0)
                {
                    isFinished = false;
                }
                else
                {
                    if (blipList.ContainsKey(p))
                    {
                        blipList[p].Delete();
                        blipList.Remove(p);
                    }
                }
            }
            if (!isFinished)
                return false;

            // if we are on eliminate stage, we are done with this 
            if (this.currentStage == MissionStage.ELIMINATE_ACTION)
            {
                SetupFinalStage();
                return false;
            }

            if (this.currentStage == MissionStage.DECIDE_ACTION)
            {
                if (lastFanaticPed != null && lastFanaticPed.Health > 0)
                {
                    return false;
                }
                else
                {
                    PartnerMentionEventId = "convoy_mission_execute";
                    this.currentStage = MissionStage.GO_CAR;
                    decisionBlip.Delete();
                    decisionBlip = null;
                    this.OnMissionDialogueStateChanged(ActorCharacter, DialogueState.DISCONNECT);
                    return false;
                }
            }

            if (this.currentStage == MissionStage.CHECK_CAR)
            {
                if (Game.Player.Character.IsInVehicle(policeVehicle))
                {
                    this.missionState = MissionState.SUCCESS;
                    this.OnMissionStateChanged(this.missionState);
                    return true;
                }
            }

            return false;
        }

        private void SetupFinalStage()
        {
            Vector3 finalFanaticPos = finalFanatic;
            Ped playerPed = Game.Player.Character;
            PedGroup randomGroup = new PedGroup();
            var finalRelGroup = World.AddRelationshipGroup("Cult");
            finalRelGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Like, true);
            Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(finalRelGroup, Relationship.Like, true);
            lastFanaticPed = CreateCultMember(finalFanaticPos, randomGroup, playerPed.RelationshipGroup, false);
            lastFanaticPed.CanBeTargetted = false;
            decisionBlip = lastFanaticPed.AddBlip();
            decisionBlip.Color = BlipColor.Red3;
            this.currentStage = MissionStage.TALK;
            lastFanaticPed.Task.ClearAllImmediately();
            lastFanaticPed.Task.PlayAnimation("amb@world_human_picnic@male@base", "base", 8f, -4, -1, AnimationFlags.NotInterruptable | AnimationFlags.Loop, 0);
            lastFanaticPed.Task.StandStill(-1);
            lastFanaticPed.BlockPermanentEvents = true;
        }

        private void CallUnitsIn()
        {
            Ped target = Game.Player.Character;
            target.Task.PlayAnimation("random@arrests", "generic_radio_chatter", 8f, 2, 3000, AnimationFlags.NotInterruptable | AnimationFlags.Loop, 0);
            Function.Call(Hash.SET_ENABLE_HANDCUFFS, lastFanaticPed, true);
            lastFanaticPed.Task.PlayAnimation("random@arrests", "kneeling_arrest_idle", 8f, -4, -1, AnimationFlags.NotInterruptable | AnimationFlags.Loop, 0);
            PartnerMentionEventId = "convoy_mission_turnedin";
            this.currentStage = MissionStage.GO_CAR;
            decisionBlip.Delete();
            decisionBlip = null;
        }

        private void CreateMission()
        {
            cultGroup = new PedGroup();
            relGroup = World.AddRelationshipGroup("Cult");
            relGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Hate, true);
            Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(relGroup, Relationship.Hate, true);
            foreach (Vector3 pos in locations)
            {
                Ped member = CreateCultMember(pos, cultGroup, relGroup);
                bodies.Add(CreateBody(pos));
                Blip bl = member.AddBlip();
                bl.Color = BlipColor.Red2;
                bl.ScaleX = 0.5f;
                bl.ScaleY = 0.5f;
                bl.ShowRoute = true;
                blipList.Add(member, bl);
                enemyList.Add(member);
            }

            foreach (Vector3 pos in bodyLocations)
            {
                bodies.Add(CreateBody(pos));
            }

            for (int i = 0; i < cars.Length; i = i + 2)
            {
                Vector3 location = cars[i];
                Vector3 rotation = cars[i + 1];

                Model carModel = new Model(GetHash(i));
                Vehicle car = World.CreateVehicle(carModel, location);
                car.Rotation = rotation;
                vehicleList.Add(car);
            }
        }

        private Ped CreateBody(Vector3 pos)
        {
            Random r = new Random();
            pos.X += r.Next(-2, 2);
            pos.Y += r.Next(1, 2);
            pos.Z += r.Next(-2, 2);
            PedHash[] randomHash = new PedHash[3] { PedHash.Armymech01SMY, PedHash.Marine02SMM, PedHash.Marine01SMM };
            Ped deadBody = World.CreatePed(randomHash.GetRandomElement(), pos);
            Function.Call(Hash.APPLY_PED_BLOOD, deadBody, 3, 1.0, 2.0, -1.0, "wound_sheet");
            deadBody.Kill();
            return deadBody;
        }

        private VehicleHash GetHash(int i)
        {
            switch (i)
            {
                case 0:
                    return VehicleHash.Crusader;
                case 2:
                    return VehicleHash.Crusader;
                case 4:
                    return VehicleHash.Barracks3;
                case 6:
                    return VehicleHash.Barracks2;
                case 8:
                    return VehicleHash.RatLoader;
                default:
                    return VehicleHash.Crusader;
            }
        }

        private Ped CreateCultMember(Vector3 spawnPosition, PedGroup cultGroup, RelationshipGroup relgroup, bool weapons = true)
        {
            PedHash[] hash = new PedHash[3] { PedHash.Acult01AMO, PedHash.Acult02AMO, PedHash.Acult02AMY };
            Model cultModel = new Model(hash.GetRandomElement());
            Ped cultMember = World.CreatePed(cultModel, spawnPosition);
            cultMember.RelationshipGroup = relgroup;
            if (weapons)
            {
                cultMember.Weapons.Give(WeaponHash.Machete, 1, true, true);
                cultMember.Weapons.Give(WeaponHash.VintagePistol, 100, true, true);

                if (cultGroup.MemberCount % 3 == 0)
                    cultMember.Weapons.Give(WeaponHash.DoubleBarrelShotgun, 100, true, true);
            }

            cultGroup.Add(cultMember, !haveLeader);
            if (!haveLeader)
            {
                haveLeader = true;
                Function.Call(Hash.SET_PED_AS_GROUP_LEADER, cultMember, cultGroup);
            }
            else
            {
                Function.Call(Hash.SET_PED_AS_GROUP_MEMBER, cultMember, cultGroup);
            }
            return cultMember;
        }


        public override void Cleanup()
        {
            foreach (var ped in enemyList)
            {
                ped.IsPersistent = false;
                ped.MarkAsNoLongerNeeded();
            }

            foreach (var ped in bodies)
            {
                ped.IsPersistent = false;
                ped.MarkAsNoLongerNeeded();
            }

            foreach (var vehicle in vehicleList)
            {
                vehicle.IsPersistent = false;
                vehicle.MarkAsNoLongerNeeded();
            }

            foreach (var blippo in blipList)
            {
                blippo.Value.Delete();
            }

            if (lastFanaticPed != null)
            {
                lastFanaticPed.MarkAsNoLongerNeeded();
                if (decisionBlip != null)
                {
                    decisionBlip.Delete();
                    decisionBlip = null;
                }
            }
            this.OnMissionDialogueStateChanged(ActorCharacter, DialogueState.DISCONNECT);
            bodies.Clear();
            enemyList.Clear();
            vehicleList.Clear();
        }
    }

}
