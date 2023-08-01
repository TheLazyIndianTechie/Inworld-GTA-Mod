using GTA;
using GTA.Math;
using GTA.Native;
using InworldV.Agent;
using InworldV.Ambiance;
using InworldV.Helper;
using InworldV.Missions;
using System;
using System.Windows.Forms;

namespace InworldV.Scenario
{
    internal enum MissionType
    {
        POOL_CRASH,
        CULT_SHOOT,
        HOSTAGE,
        QUEEN,
        REPORT,
        VENDOR_HOTDOG,
        VENDOR_BEEFY,
        SMUGGLING,
        FINAL_MISSION,
        BRUCIE,
        PARTNER_SELECTION
    }

    [ScriptAttributes(NoDefaultInstance = true)]
    internal class PoliceScenario : Script
    {
        private readonly string TITLE = "Sentient Streets: AI Mod";
        private readonly int FADE_TIME = 2000;
        private readonly int COMMENT_COOLOFF = 35000;
        private readonly int MISSION_WAIT_TIME = 30000;

        private Ped connectedPed;
        private Blip CarBlip;
        private PoliceChatter radio;
        private bool isCharacterTalking = false;
        private Model previousModel;
        private Vehicle shop;
        private Companion companion;
        private PoliceMissionBase currentMission;
        private int PLAYER_PARTNER_TYPE = 1;
        private int MISSION_DAY = 1;
        private int MISSION_STAGE = 1;
        private int lastCompletedMissionHour = 0;
        private GTA.UI.TextElement textElement;
        private float WaitTimeText = 0;
        private float lastBadSkillComment = 0;
        private bool fadeInRequired = false;
        private bool isEndOfMissions = false;
        private float TextShownTime = 0;
        private float fadeInTime = -1;
        private float LAST_MS_CHECKED_FOR_MISSION = 0;
        private float CURRENT_ELAPSED_MS = 0;
        private float showFinishScreen = -1;

        public bool DidFinishedMod = false;
        public string PLAYER_NAME;
        public bool IsCurrentMissionActive
        {
            get
            {
                return (currentMission != null);
            }
        }


        public PoliceScenario()
        {

            Function.Call(Hash.REQUEST_ANIM_DICT, "random@arrests");
        }

        private void TickTimerActivities()
        {
            CURRENT_ELAPSED_MS += (Game.LastFrameTime * 1000);

            if (textElement != null)
            {
                if (CURRENT_ELAPSED_MS - WaitTimeText >= TextShownTime)
                {
                    textElement = null;
                    GTA.UI.Screen.StopEffect(!isEndOfMissions ? GTA.UI.ScreenEffect.SwitchOpenMichaelIn : GTA.UI.ScreenEffect.HeistCelebEnd);
                }
            }

            if (fadeInRequired && fadeInTime <= CURRENT_ELAPSED_MS)
            {
                fadeInTime = -1;
                GTA.UI.Screen.FadeIn(FADE_TIME);
                fadeInRequired = false;
            }

            if (!fadeInRequired)
            {
                if (GTA.UI.Screen.IsFadedOut)
                {
                    fadeInTime = -1;
                    GTA.UI.Screen.FadeIn(FADE_TIME);
                    fadeInRequired = false;
                }
            }

            if (showFinishScreen != -1)
            {
                if (showFinishScreen <= CURRENT_ELAPSED_MS)
                {
                    if (companion == null || !companion.IsAlive)
                    {
                        showFinishScreen = -1;
                        companion = null;
                        WriteText("The NihilAIsts have been defeated...for now..", 10000, true);
                    }
                    else
                    {
                        showFinishScreen = CURRENT_ELAPSED_MS + 500;
                    }
                }
            }
        }

        private int GetGameHour()
        {
            int hours = Function.Call<int>(Hash.GET_CLOCK_HOURS);
            return hours;
        }

        private void ProgressInStory()
        {
            if (CURRENT_ELAPSED_MS - LAST_MS_CHECKED_FOR_MISSION < MISSION_WAIT_TIME) return;

            if (currentMission != null) return;

            try
            {
                // Proceed to next stage 
                if (MISSION_DAY == 1)
                {
                    if (MISSION_STAGE == 1)
                    {
                        // Pool Mission
                        StartMission(MissionType.POOL_CRASH);
                    }
                    else if (MISSION_STAGE == 2)
                    {
                        // Shooutout
                        StartMission(MissionType.CULT_SHOOT);
                    }
                    else if (MISSION_STAGE == 3)
                    {
                        // Report
                        StartMission(MissionType.REPORT);
                    }
                    else if (MISSION_STAGE == 4)
                    {
                        if (MISSION_DAY == 1) MISSION_DAY++;
                    }
                }
                else if (MISSION_DAY == 2)
                {
                    if (MISSION_STAGE == 4)
                    {
                        // Coffee
                        StartMission(MissionType.VENDOR_HOTDOG);
                    }
                    else if (MISSION_STAGE == 5)
                    {
                        // Queen
                        StartMission(MissionType.QUEEN);
                    }
                    else if (MISSION_STAGE == 6)
                    {
                        // Hostage
                        StartMission(MissionType.HOSTAGE);
                    }
                    else if (MISSION_STAGE == 7)
                    {
                        // Report
                        StartMission(MissionType.REPORT);
                    }
                    else if (MISSION_STAGE == 8)
                    {
                        if (MISSION_DAY == 2) MISSION_DAY++;
                    }
                }
                else if (MISSION_DAY == 3)
                {
                    if (MISSION_STAGE == 8)
                    {
                        // Burger
                        StartMission(MissionType.VENDOR_BEEFY);
                    }
                    else if (MISSION_STAGE == 9)
                    {
                        // Smuggle
                        StartMission(MissionType.SMUGGLING);
                    }
                    else if (MISSION_STAGE == 10)
                    {
                        // Podcast
                        StartMission(MissionType.BRUCIE);
                    }
                    else if (MISSION_STAGE == 11)
                    {
                        // Final mission
                        StartMission(MissionType.FINAL_MISSION);
                    }
                }
                MISSION_STAGE++;
                LAST_MS_CHECKED_FOR_MISSION = CURRENT_ELAPSED_MS;
            }
            catch
            {

            }
        }

        public void ProcessGameEvent(string eventId)
        {
            if (companion != null)
            {
                companion.ProcessDirective(eventId);
            }
            if (currentMission != null)
                currentMission.ProcessGameEvent(eventId);
        }

        public void ProcessTalkingCharacter(bool isTalking)
        {
            if (isTalking == isCharacterTalking) return;
            if (connectedPed == null) return;

            isCharacterTalking = isTalking;

            if (isCharacterTalking)
            {
                connectedPed.Task.PlayAnimation("mp_facial", "mic_chatter", 8f, -1, AnimationFlags.Loop | AnimationFlags.Secondary);
            }
            else
            {
                connectedPed.Task.ClearAnimation("mp_facial", "mic_chatter");
            }
        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
        }

        public static string GetCalloutTrigger(MissionType missionType)
        {
            switch (missionType)
            {
                case MissionType.CULT_SHOOT:
                    return "convoy_callout_trig";
                case MissionType.POOL_CRASH:
                    return "crash_callout_trig";
                case MissionType.HOSTAGE:
                    return "hostage_callout_trig";
                case MissionType.QUEEN:
                    return "queen_callout_trig";
                case MissionType.VENDOR_HOTDOG:
                    return "start_coffee_get_mission";
                case MissionType.VENDOR_BEEFY:
                    return "start_burger_get_mission";
                case MissionType.SMUGGLING:
                    return "smuggle_callout_trig";
                case MissionType.FINAL_MISSION:
                    return "attack_callout_trig";
                case MissionType.BRUCIE:
                    return "podcaster_callout_trig";
                default:
                    return string.Empty;
            }
        }

        private void StartMission(MissionType type)
        {
            if (currentMission != null)
            {
                currentMission.Cleanup();
                currentMission.Abort();
            }

            switch (type)
            {
                case MissionType.CULT_SHOOT:
                    currentMission = InstantiateScript<CultShootMission>();
                    break;
                case MissionType.POOL_CRASH:
                    currentMission = InstantiateScript<PoolCrashMission>();
                    break;
                case MissionType.HOSTAGE:
                    currentMission = InstantiateScript<HostageMission>();
                    break;
                case MissionType.QUEEN:
                    currentMission = InstantiateScript<QueenMission>();
                    break;
                case MissionType.REPORT:
                    currentMission = InstantiateScript<ReportToSupervisor>();
                    break;
                case MissionType.SMUGGLING:
                    currentMission = InstantiateScript<LiquorSmugglingMission>();
                    break;
                case MissionType.BRUCIE:
                    currentMission = InstantiateScript<BruciePodcastMission>();
                    break;
                case MissionType.FINAL_MISSION:
                    currentMission = InstantiateScript<CultHitMission>();
                    ((CultHitMission)currentMission).TurnBackTrigger = this.TurnBack;
                    ((CultHitMission)currentMission).TeleportTrigger = this.TeleportTrigger;
                    break;
                case MissionType.VENDOR_HOTDOG:
                    currentMission = InstantiateScript<VendorMission>();
                    ((VendorMission)currentMission).VENDOR_TYPE = 0;
                    break;
                case MissionType.VENDOR_BEEFY:
                    currentMission = InstantiateScript<VendorMission>();
                    ((VendorMission)currentMission).VENDOR_TYPE = 1;
                    break;
                case MissionType.PARTNER_SELECTION:
                    currentMission = InstantiateScript<PartnerSelectionMission>();
                    ((PartnerSelectionMission)currentMission).PartnerSelected = this.OnPartnerSelected;
                    break;
            }

            string getStartComment = GetCalloutTrigger(type);
            if (getStartComment != string.Empty)
                this.OnSocketEventChanged("START_TRIGGER;;" + this.GetCompanionId(), getStartComment);

            currentMission.policeVehicle = this.shop;
            currentMission.MissionStateChanged += OnCurrentMissionChanged;
            currentMission.MissionDialogueStateChanged += OnCurrentMissionDialogueChanged;
            currentMission.StartMission();
        }

        private void TeleportTrigger()
        {
            // teleport to player's back
            if (companion != null && companion.IsAlive)
            {
                if (companion.Character != null)
                {
                    Vector3 teleportPosition = Game.Player.Character.Position - Game.Player.Character.ForwardVector * 2f;
                    companion.Character.Position = teleportPosition;
                }
            }
        }

        public void TurnBack(bool turnBack)
        {
            if (this.companion == null) return;

            if (turnBack)
            {
                var ped = this.companion.Character;
                ped.LeaveGroup();
                Game.Player.Character.LeaveGroup();
                Game.Player.Character.RelationshipGroup = World.AddRelationshipGroup("playernew");
                try
                {
                    ped.IsInvincible = false;
                    ped.CanBeTargetted = true;
                }
                catch
                {
                }
                Game.Player.Character.RelationshipGroup.SetRelationshipBetweenGroups(ped.RelationshipGroup, Relationship.Hate);
                ped.RelationshipGroup.SetRelationshipBetweenGroups(Game.Player.Character.RelationshipGroup, Relationship.Hate);
                ped.CanBeTargetted = true;
                ped.IsEnemy = true;
                Game.Player.Character.CanBeTargetted = true;
                ped.Task.FightAgainstHatedTargets(10);
                ped.Task.ShootAt(Game.Player.Character.Position, -1);
                this.companion.IsEnemy = true;
            }
        }

        public void UpdatePlayerLook()
        {
            Ped CurrentPed = Game.Player.Character;
            Function.Call(Hash.SET_PED_COMPONENT_VARIATION, CurrentPed, 0, 12, 0, 2); //'FACE
            Function.Call(Hash.SET_PED_COMPONENT_VARIATION, CurrentPed, 2, 18, 0, 2); //'HAIR
            Function.Call(Hash.SET_PED_HEAD_BLEND_DATA, CurrentPed, 0, 0, 0, 0, 0, 0, 0.5f, 0.5f, 0, false);
            Function.Call(Hash.SET_PED_HAIR_TINT, CurrentPed, 57, 1);
            Function.Call(Hash.SET_PED_COMPONENT_VARIATION, CurrentPed, 6, 24, 0, 2);
            Function.Call(Hash.SET_PED_COMPONENT_VARIATION, CurrentPed, 4, 25, 2, 2); //'LEGS/PANTS
            Function.Call(Hash.SET_PED_COMPONENT_VARIATION, CurrentPed, 8, 58, 0, 2); //'ACCESSORIES
            Function.Call(Hash.SET_PED_COMPONENT_VARIATION, CurrentPed, 10, 6, 0, 0); //'TEXTURES
            Function.Call(Hash.SET_PED_COMPONENT_VARIATION, CurrentPed, 11, 55, 0, 2); //'TORSO2
        }

        private void OnCurrentMissionDialogueChanged(string id, PoliceMissionBase.DialogueState e)
        {
            if (e == PoliceMissionBase.DialogueState.DISCONNECT)
            {
                AudioManager.Instance.StopEverythingAbruptly();
                MessageQueue.Instance.AddSubtitleMessage(new SubtitleMessage("-", 1000, true));
                this.OnSocketEventChanged(id, "disconnect");
            }
            else if (e == PoliceMissionBase.DialogueState.CONNECT)
            {
                this.OnSocketEventChanged(id, "connect");
            }
            else if (e == PoliceMissionBase.DialogueState.RECONNECT)
            {
                this.OnSocketEventChanged(null, "reconnect");
            }
        }

        public string GetScenarioCharacter(out string playerName)
        {

            if (DidFinishedMod)
            {
                playerName = string.Empty;
                return string.Empty;
            }

            if (currentMission != null)
            {
                string connectId;
                bool canConnect = currentMission.TryToConnectToActor(out connectedPed, out connectId, out playerName);
                if (canConnect && connectId != string.Empty)
                {
                    if (playerName == "Mike")
                    {
                        if (!string.IsNullOrEmpty(PLAYER_NAME))
                            playerName = PLAYER_NAME;
                        else
                            playerName = "Mike";
                    }
                    return connectId;
                }
            }

            // Check if player is close enough to companion OR running mission character
            if (companion != null && companion.IsAlive)
            {
                playerName = "Mike";
                if (playerName == "Mike")
                {
                    if (!string.IsNullOrEmpty(PLAYER_NAME))
                        playerName = PLAYER_NAME;
                    else
                        playerName = "Mike";
                }
                connectedPed = companion.Character;
                return GetCompanionId();
            }

            playerName = "Officer";
            return string.Empty;
        }

        public string GetCompanionId()
        {
            if (PLAYER_PARTNER_TYPE == 1)
            {
                return "emily_martinez";
            }
            else if (PLAYER_PARTNER_TYPE == 2)
            {
                return "tony_russo";
            }
            else if (PLAYER_PARTNER_TYPE == 3)
            {
                return "frank_thompson";
            }

            return "frank_thompson";
        }

        public void FadeOut()
        {
            fadeInTime = CURRENT_ELAPSED_MS + FADE_TIME;
            GTA.UI.Screen.FadeOut(FADE_TIME);
            fadeInRequired = true;
        }

        public void ResetCar()
        {
            if (this.shop != null)
            {
                this.shop.Repair();
                Vector3 teleportPosition = Game.Player.Character.Position - Game.Player.Character.ForwardVector * 2f;
                companion.Character.Position = teleportPosition;
                this.shop.Position = teleportPosition;
                this.shop.PlaceOnNextStreet();
            }
            else
            {
                if (this.CarBlip != null)
                    this.CarBlip.Delete();

                SpawnShopCar(false);
            }
        }

        public bool IsCloseToSubject
        {
            get
            {
                if (currentMission != null)
                    return currentMission.IsCloseToSubject;
                else
                    return false;
            }
        }

        public void TickIncomingMessage()
        {
            if (currentMission != null)
                currentMission.TickIncomingMessage();
        }


        private bool IsFinalMissionFinished()
        {
            if (currentMission is CultHitMission)
            {
                currentMission.Cleanup();
                currentMission.Abort();
                currentMission = null;
                showFinishScreen = CURRENT_ELAPSED_MS + 6000;
                AudioManager.Instance.StartSoundtrack(1);
                CarBlip.Delete();
                CarBlip = null;
                MISSION_DAY++;
                DidFinishedMod = true;
                return true;
            }

            return false;
        }

        private void OnCurrentMissionChanged(object sender, PoliceMissionBase.MissionState e)
        {
            // Not used anymore
            return;
        }

        public void OnTick(object sender, EventArgs e)
        {
            try
            {
                if (Game.IsPaused) return;
                TickTimerActivities();
                FinishMissionAndIterThrough();
                ProgressInStory();
                DontLetHimDieOnMe();
                NeverGetChased();
                ProcessMission();
                ProcessBlips();
                if (this.companion != null)
                    companion.Think();

                DistanceBasedSpeechAudioVolume();
                CheckIfPlayerIsHorrible();
                textElement?.Draw();
            }
            catch (Exception ex)
            {
            }
        }

        private void DistanceBasedSpeechAudioVolume()
        {
            if (connectedPed != null)
            {
                float distance = Game.Player.Character.Position.DistanceTo(connectedPed.Position);
                float volume = 0.95f - (distance / 25f);
                volume = Math.Max(0, Math.Min(1, volume));
                Helper.AudioManager.Instance.SetVolume(volume);
            }
        }

        private void DontLetHimDieOnMe()
        {
            var playerPed = Game.Player.Character;
            if (playerPed.Health < (playerPed.MaxHealth * 0.7f))
                playerPed.IsInvincible = true;
            else
                playerPed.IsInvincible = false;
        }

        private void ProcessMission()
        {
            if (currentMission != null)
            {
                bool isFinished = currentMission.IsFinished();
                if (isFinished)
                {
                    var isFinishMission = IsFinalMissionFinished();
                    if (isFinishMission) return;
                    if (currentMission.IsRunning)
                    {
                        currentMission.Abort();
                    }

                    currentMission = null;
                    SaveCurrentState();
                    LAST_MS_CHECKED_FOR_MISSION = CURRENT_ELAPSED_MS;
                }
            }
        }

        private void CheckIfPlayerIsHorrible()
        {
            if (currentMission == null || !currentMission.IsCloseToSubject)
            {
                if (CURRENT_ELAPSED_MS - lastBadSkillComment < COMMENT_COOLOFF) return;

                if (Game.Player.Character.IsInVehicle())
                {
                    Vehicle playerVehicle = Game.Player.Character.CurrentVehicle;

                    bool didHitPedestrian = false;
                    Ped[] nearbyPeds = World.GetNearbyPeds(Game.Player.Character, 10f);
                    foreach (Ped ped in nearbyPeds)
                    {
                        if (ped.HasBeenDamagedBy(Game.Player.Character))
                        {
                            didHitPedestrian = true;
                            break;
                        }
                    }

                    if (didHitPedestrian)
                    {
                        lastBadSkillComment = CURRENT_ELAPSED_MS;
                        this.OnSocketEventChanged("TRIGGER_EVENT", "comment_hitting_pedestrian");
                        return;
                    }

                    Vehicle[] closestVehicles = World.GetNearbyVehicles(playerVehicle.Position, 10f);
                    foreach (var closestVehicle in closestVehicles)
                    {
                        if (closestVehicle != null && closestVehicle != this.shop && closestVehicle.HasBeenDamagedBy(playerVehicle))
                        {
                            lastBadSkillComment = CURRENT_ELAPSED_MS;
                            this.OnSocketEventChanged("TRIGGER_EVENT", "comment_bad_driving");
                            break;
                        }
                    }
                }
            }
        }

        private void FinishMissionAndIterThrough()
        {
            if (currentMission != null)
            {
                bool isFinished = currentMission.IsFinished();
                if (isFinished)
                {
                    bool isDone = IsFinalMissionFinished();
                    if (isDone)
                        return;

                    if (currentMission is ReportToSupervisor)
                    {
                        // Proceed to next day
                        FadeOut();
                        Function.Call(Hash.SET_FADE_OUT_AFTER_DEATH, false);
                        Game.Player.Character.Position = new Vector3(457.8029f, -1008.894f, 28.29712f);
                        Game.Player.Character.ClearBloodDamage();
                        Game.Player.Character.ClearVisibleDamage();
                        Game.Player.Character.Health = Game.Player.Character.MaxHealth;
                        if (this.companion != null)
                        {
                            this.companion.Character.Position = new Vector3(408.0949f, -1025.009f, 29.36744f);
                            this.companion.CurrentDirective = Companion.Directive.FOLLOW;
                        }

                        this.shop.Repair();
                        SceneHelper.SetTime(8, "CLEAR");
                        // Set time, move player
                        lastCompletedMissionHour = 7;
                        MISSION_DAY++;
                        WriteText("Day " + MISSION_DAY, 7000);
                    }
                    else
                    {

                        if (currentMission is PartnerSelectionMission)
                        {
                            MessageQueue.Instance.AddSubtitleMessage(new SubtitleMessage("~b~Wait for your partner and start your patrolling"));
                            companion.CurrentDirective = Companion.Directive.FOLLOW;
                        }
                        else
                        {
                            MessageQueue.Instance.AddSubtitleMessage(new SubtitleMessage("~g~Great, continue your patrolling"));
                            this.OnSocketEventChanged("TRIGGER_EVENT", currentMission.PartnerMentionEventId);
                        }

                        lastCompletedMissionHour = GetGameHour();
                    }

                    Game.Player.Character.Weapons.Give(WeaponHash.CombatShotgun, 250, false, true);
                    Game.Player.Character.Weapons.Give(WeaponHash.Pistol, 150, false, true);

                    currentMission.Cleanup();
                    currentMission.Abort();
                    currentMission = null;
                    Game.Player.Character.Health = Game.Player.Character.MaxHealth;
                    Game.Player.Character.Armor = Game.Player.Character.MaxHealth;
                    this.shop.Health = this.shop.MaxHealth;
                    SaveCurrentState();

                    LAST_MS_CHECKED_FOR_MISSION = CURRENT_ELAPSED_MS;
                }
            }
        }


        private void ProcessBlips()
        {
            if (Game.Player.Character.CurrentVehicle == shop)
            {
                if (CarBlip != null)
                {
                    CarBlip.Delete();
                    CarBlip = null;
                }
            }
            else
            {
                if (CarBlip == null)
                    CreateCarBlip();
            }
        }

        private void IsDeadAndFinished()
        {
            if (Game.Player.Character.IsDead)// || Game.Player.Character.Health < 5)
            {
                Function.Call(Hash.SET_FADE_OUT_AFTER_DEATH, false);
                Function.Call(Hash.NETWORK_REQUEST_CONTROL_OF_ENTITY, Game.Player.Character);
                Game.Player.Character.IsInvincible = true;
                Vector3 respawnLocation = new Vector3(457.8029f, -1008.894f, 28.29712f);
                Function.Call(Hash.SET_ENTITY_COORDS_NO_OFFSET, Game.Player.Character, respawnLocation.X, respawnLocation.Y, respawnLocation.Z, true, true, true);
                Game.Player.Character.ClearBloodDamage();
                Game.Player.Character.ClearVisibleDamage();
                // Reset the player state
                Game.Player.Character.Health = Game.Player.Character.MaxHealth;
                Game.Player.Character.IsInvincible = false;
                Function.Call(Hash.SET_FADE_OUT_AFTER_DEATH, true);
            }
        }

        private void NeverGetChased()
        {
            Game.MaxWantedLevel = 0;
            Game.Player.WantedLevel = 0;
            Game.Player.IgnoredByPolice = true;
        }

        public void StartScenario()
        {
            SpawnShopCar(true);
            StartMission(MissionType.PARTNER_SELECTION);
            ChangePlayerModel();
            KeyDown += OnKeyDown;
            Tick += OnTick;
            radio = new PoliceChatter();
            radio.StartChatter();
            Game.MaxWantedLevel = 0;
            Game.Player.WantedLevel = 0;
            Game.Player.IgnoredByPolice = true;
        }

        public void SetNewPlayerName(string name)
        {
            PLAYER_NAME = name;
            DeferredStart();
        }

        private void DeferredStart()
        {
            WriteText(TITLE, 7000);
        }

        private void OnPartnerSelected(Ped selectedPartner, int partnertype)
        {
            PLAYER_PARTNER_TYPE = partnertype;
            this.companion = new Companion();
            companion.SetShop(this.shop);
            this.companion.CreateCompanion(selectedPartner);
            companion.CurrentDirective = Companion.Directive.HOLD;
            MessageQueue.Instance.AddSubtitleMessage(new SubtitleMessage("Partner is now selected"));
        }

        private void SaveCurrentState()
        {
            var playerPed = Game.Player.Character;
            SaveSystem.SaveGame(MISSION_DAY, MISSION_STAGE, PLAYER_PARTNER_TYPE, playerPed.Position, playerPed.Rotation, World.CurrentTimeOfDay, this.shop.Position, this.shop.Rotation, lastCompletedMissionHour, playerPed.CurrentVehicle != null, PLAYER_NAME);
        }
        public void LoadScenario(int companionId, int day, int progress, Vector3 position, Vector3 rotation, Vector3 carPosition, Vector3 carRotation, int lcmh, bool isRiding, string PlayerName)
        {
            Game.Player.Character.Position = position;
            Game.Player.Character.Rotation = rotation;
            PLAYER_PARTNER_TYPE = companionId;
            PLAYER_NAME = PlayerName;
            ChangePlayerModel();
            SpawnShopCar();
            companion = new Companion();
            companion.SetShop(this.shop);
            companion.CreateCompanion(companionId);
            this.shop.Position = carPosition;
            this.shop.Rotation = carRotation;
            lastCompletedMissionHour = lcmh;
            MISSION_STAGE = progress;
            MISSION_DAY = day;
            if (isRiding)
            {
                Game.Player.Character.SetIntoVehicle(this.shop, VehicleSeat.Driver);
                if (companion != null)
                    companion.Character.SetIntoVehicle(this.shop, VehicleSeat.LeftFront);
            }
            KeyDown += OnKeyDown;
            Tick += OnTick;
            radio = new PoliceChatter();
            radio.StartChatter();
            Game.MaxWantedLevel = 0;
            Game.Player.WantedLevel = 0;
            Game.Player.IgnoredByPolice = true;
        }

        public delegate void SocketEventHandler(string id, string eventid);
        public event SocketEventHandler OnSocketEventId;
        public void OnSocketEventChanged(string id, string eventid)
        {
            OnSocketEventId?.Invoke(id, eventid);
        }

        public void ResetScenario()
        {
            if (currentMission != null)
            {
                currentMission.Cleanup();
                currentMission.StopMission();
                currentMission.Abort();
                currentMission = null;
            }

            if (companion != null)
            {
                companion.RemoveCompanion();
                companion = null;
            }

            if (CarBlip != null)
                CarBlip.Delete();

            if (shop != null)
            {
                shop.IsPersistent = false;
                shop.MarkAsNoLongerNeeded();
                shop.Delete();
                shop = null;
            }

            if (previousModel != null)
                Game.Player.ChangeModel(previousModel);
        }

        private void ChangePlayerModel()
        {
            Model policeModel = new Model(PedHash.FreemodeMale01);
            previousModel = Game.Player.Character.Model;
            bool isChanged = Game.Player.ChangeModel(policeModel);
            if (isChanged)
            {
                UpdatePlayerLook();
                Game.Player.Character.Weapons.Give(WeaponHash.CombatShotgun, 250, false, true);
                Game.Player.Character.Weapons.Give(WeaponHash.Pistol, 150, false, true);
                Game.Player.IgnoredByPolice = true;
            }
        }

        private void SpawnShopCar(bool isFirst = false)
        {
            Model shopModel = new Model(VehicleHash.Police2);
            if (isFirst)
            {
                Vector3 location = new Vector3(408.0949f, -1025.009f, 29.36744f);
                shop = World.CreateVehicle(shopModel, location);
            }
            else
            {
                shop = World.CreateVehicle(shopModel, Game.Player.Character.Position + new Vector3(0, 5, 0));
                shop.PlaceOnNextStreet();
            }

            shop.CanEngineDegrade = false;

            CreateCarBlip();
        }

        private void CreateCarBlip()
        {
            CarBlip = shop.AddBlip();
            CarBlip.Name = "Your shop";
            CarBlip.ShowRoute = true;
            CarBlip.Color = BlipColor.Blue;
            Function.Call(Hash.SET_BLIP_ROUTE_COLOUR, CarBlip.Handle, BlipColor.Blue);
        }

        public void WriteText(string text, float time, bool isEnd = false)
        {
            WaitTimeText = time - 1;
            TextShownTime = CURRENT_ELAPSED_MS - 1;
            int screenWidth = GTA.UI.Screen.Resolution.Width;
            int screenHeight = GTA.UI.Screen.Resolution.Height;
            float size = 1.5f;
            float textScaledWidth = GTA.UI.TextElement.GetScaledStringWidth(text, GTA.UI.Font.ChaletLondon, size);
            float posX = (screenWidth / 3);
            textElement = new GTA.UI.TextElement(text, new System.Drawing.PointF(posX, screenHeight / 3), size, System.Drawing.Color.White, GTA.UI.Font.ChaletLondon, GTA.UI.Alignment.Center);
            textElement.Outline = true;
            textElement.Draw();
            isEndOfMissions = isEnd;
            GTA.UI.Screen.StartEffect(!isEnd ? GTA.UI.ScreenEffect.SwitchOpenMichaelIn : GTA.UI.ScreenEffect.HeistCelebPass, (int)time - 10, false);
        }
    }
}
