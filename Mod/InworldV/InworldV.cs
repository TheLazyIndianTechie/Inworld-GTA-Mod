using GTA;
using GTA.Math;
using GTA.Native;
using InworldV.Helper;
using InworldV.Scenario;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Screen = GTA.UI.Screen;

namespace InworldV
{
    public static class SaveSystem
    {
        public static void SaveGame(int day, int mission, int companion, Vector3 position, Vector3 rotation, TimeSpan tod, Vector3 carPosition, Vector3 carRotation, int lastCompletedMissionHour, bool isRiding, string PlayerName)
        {
            using (FileStream fs = new FileStream("InworldSave.blocsave", FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, new GameData(day, mission, companion, position, rotation, tod, carPosition, carRotation, lastCompletedMissionHour, isRiding, PlayerName));
            }
        }

        public static GameData LoadGame()
        {
            if (File.Exists("InworldSave.blocsave"))
            {
                using (FileStream fs = new FileStream("InworldSave.blocsave", FileMode.Open))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    return (GameData)formatter.Deserialize(fs);
                }
            }
            return null;
        }

        [Serializable]
        public class GameData
        {
            public int Day;
            public int Mission;
            public int Companion;
            public int LastCompletedHour;
            public Vector3 Position;
            public Vector3 Rotation;
            public Vector3 CarPosition;
            public Vector3 CarRotation;
            public TimeSpan TimeOfDay;
            public bool IsRidingCar;
            public string PlayerName;

            public GameData(int day, int mission, int companion, Vector3 position, Vector3 rotation, TimeSpan timeofday, Vector3 carPosition, Vector3 carRotation, int lastCompletedMissionHour, bool isRiding, string playerName)
            {
                Day = day;
                Mission = mission;
                Companion = companion;
                Position = position;
                Rotation = rotation;
                CarPosition = carPosition;
                CarRotation = carRotation;
                LastCompletedHour = lastCompletedMissionHour;
                IsRidingCar = isRiding;
                TimeOfDay = timeofday;
                PlayerName = playerName;
            }
        }
    }

    public class InworldV : Script
    {
        private readonly int FILLER_GAP = 44000;
        private int messageTime = 3000;
        private bool consoleEnabled = false;
        private bool isSocketInitialized;
        private bool pushingToTalk = false, connectedToEvent = false;
        private PoliceScenario activeRoleScenario;
        private InworldSocket socket;
        private Keys PushToTalkKey = Keys.N;
        private string currentCommand = "";
        private string currentConnected;
        private float queueMessageTime = -1;
        private float elapsedTime = 0;
        private float LAST_SPOKE_TIME = 60000;
        private float DEFERRED_SPEAK_END = -1;

        public InworldV()
        {
            Tick += OnTick;
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
            StartInworldClient();
        }

        private void StartInworldClient()
        {
            var directory = Directory.GetCurrentDirectory();
            string inworldFolder = System.IO.Path.Combine(directory, "Inworld\\");
            var startInfo = new ProcessStartInfo();
            startInfo.WorkingDirectory = inworldFolder;
            startInfo.WindowStyle = ProcessWindowStyle.Minimized;
            startInfo.FileName = inworldFolder + "GTAInworldClient.exe";
            startInfo.Arguments = inworldFolder + ".env";
            var process = new Process();
            process.StartInfo = startInfo;
            process.Start();
        }

        private void OnTimerTick()
        {
            elapsedTime += Game.LastFrameTime * 1000;
            ConversationFluencyChecker();
            CheckDeferred();
        }

        private void CheckDeferred()
        {
            if (DEFERRED_SPEAK_END != -1)
            {
                if (DEFERRED_SPEAK_END <= elapsedTime)
                {
                    socket.StopVoiceSending();
                    pushingToTalk = false;
                    DEFERRED_SPEAK_END = -1;
                }
            }
        }

        public void ShowText(string txt)
        {
            MessageQueue.Instance.AddSubtitleMessage(new SubtitleMessage(txt, 1500));
        }
        private void ConversationFluencyChecker()
        {
            if (activeRoleScenario != null)
            {
                if (!activeRoleScenario.IsCurrentMissionActive || !activeRoleScenario.IsCloseToSubject)
                {
                    // Filler time
                    if (elapsedTime - LAST_SPOKE_TIME >= FILLER_GAP)
                    {
                        LAST_SPOKE_TIME = elapsedTime;
                        if (socket.IsConnectedAndToPartner())
                        {
                            if (string.IsNullOrEmpty(socket.awaitingTrigger))
                                socket.TriggerEvent("start_small_talk");
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(socket.awaitingTrigger))
                                socket.SetAwaitingTrigger("start_small_talk", (int)elapsedTime);
                            InternalConnect();
                        }
                    }
                }

            }
        }

        private void MakePlayerTalk(bool talk)
        {
            if (talk)
            {
                Game.Player.Character.Task.PlayAnimation("mp_facial", "mic_chatter", 8f, -1, AnimationFlags.Loop | AnimationFlags.Secondary);
            }
            else
            {
                Game.Player.Character.Task.ClearAnimation("mp_facial", "mic_chatter");
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == PushToTalkKey)
            {
                if (pushingToTalk)
                {
                    MakePlayerTalk(false);
                    DEFERRED_SPEAK_END = elapsedTime + 1300;
                    //socket.StopVoiceSending();                   
                    LAST_SPOKE_TIME = elapsedTime;
                }
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Oemtilde)
            {
                consoleEnabled = !consoleEnabled;
            }
            else if (consoleEnabled)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    ExecuteCommand(currentCommand);
                    currentCommand = "";
                    consoleEnabled = false;
                }
                else if (e.KeyCode == Keys.Back)
                {
                    if (currentCommand.Length != 0)
                        currentCommand = currentCommand.Remove(currentCommand.Length - 1);
                }
                else
                {
                    if (e.KeyCode != Keys.Oemtilde)
                    {
                        if (e.KeyCode == Keys.CapsLock || e.KeyCode == Keys.Shift || e.KeyCode == Keys.Alt || e.KeyCode == Keys.Control)
                        { }
                        else if (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9)
                        {
                            currentCommand += e.KeyData.ToString().Replace("D", "");
                        }
                        else if (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9)
                        {
                            currentCommand += e.KeyData.ToString().Replace("NumPad", "");
                        }
                        else if (e.KeyCode == Keys.Space)
                            currentCommand += " ";
                        else
                        {
                            if (e.KeyCode == Keys.OemMinus)
                            {
                                currentCommand += "-";
                            }
                            else if (Regex.IsMatch(e.KeyData.ToString(), "^[A-Za-z0-9 ]+$"))
                            {
                                currentCommand += e.KeyData.ToString();
                            }
                        }
                    }

                }
            }
            if (e.KeyCode == PushToTalkKey)
            {
                if (activeRoleScenario != null && !consoleEnabled)
                {
                    if (!pushingToTalk)
                    {
                        MakePlayerTalk(true);
                        socket.StartVoiceSending();
                        pushingToTalk = true;
                    }
                }
            }
            else if (e.KeyCode == Keys.T)
            {
                if (activeRoleScenario != null && !consoleEnabled)
                {
                    string playerName;
                    string id = activeRoleScenario.GetScenarioCharacter(out playerName);
                    if (id != string.Empty)
                    {
                        if (currentConnected != id)
                        {
                            connectedToEvent = true;
                            queueMessageTime = 0;
                            ShowText("Getting attention of " + GetHumanReadbleVersion(id) + "..");
                            InternalConnect(id);
                        }
                    }
                }
            }
        }

        private string GetHumanReadbleVersion(string id)
        {
            switch (id)
            {
                case "benjamin_steel":
                    return "Cult Leader";// return "Benjamin Steel";
                case "brucie":
                    return "Brucie";
                case "carlos_morales":
                    return "Chihuahua Hotdog Vendor";//return "Carlos Morales";
                case "damien_vex":
                    return "Cult Member";
                case "eddie_thompson":
                    return "Beefy Bill's Vendor";
                case "edna_quirke":
                    return "Old women";
                case "lucas_blackwood":
                    return "Captor";
                case "emily_martinez":
                    return "Emily Martinez";
                case "frank_thompson":
                    return "Frank Thompson";
                case "tony_russo":
                    return "Tony Russo";
                case "oliver_bellamy":
                    return "Mime";
                case "olivia_van_der_woodsen":
                    return "Olivia Van Der Woodsen";
                case "sergeant_alex_mercer":
                    return "Captain Alex Mercer";
                case "vincent_the_viper_moretti":
                    return "Vincent The Viper Moretti";
                default:
                    return "Unknown";
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            try
            {
                OnTimerTick();
                if (!isSocketInitialized)
                {
                    Wait(2000);
                    socket = new InworldSocket();
                    socket.Connect();
                    isSocketInitialized = true;
                    Model policeModel = new Model(PedHash.FreemodeMale01);
                    policeModel.Request(800);
                }

                if (socket != null)
                {
                    socket.TickAwaitingQueue((int)elapsedTime);

                    if (connectedToEvent)
                    {
                        var queue = socket.GetEventQueue();
                        if (queue.Length > 0)
                        {
                            foreach (var item in queue)
                            {
                                if (activeRoleScenario != null)
                                {
                                    activeRoleScenario.ProcessGameEvent(item);
                                }
                            }
                        }
                    }

                    if (elapsedTime - queueMessageTime > messageTime)
                    {
                        queueMessageTime = elapsedTime;
                        var msg = socket.GetFirstMessage();
                        if (msg.Length > 0)
                        {
                            messageTime = msg.Length * 35;
                            MessageQueue.Instance.AddSubtitleMessage(new SubtitleMessage(msg, messageTime));

                            if (activeRoleScenario != null)
                            {
                                activeRoleScenario.TickIncomingMessage();
                            }
                        }
                    }
                }

                AudioManager.Instance.TickSoundManager();


                if (activeRoleScenario != null && activeRoleScenario.IsRunning)
                {
                    bool isTalking = AudioManager.Instance.IsTalking();
                    activeRoleScenario.ProcessTalkingCharacter(isTalking);
                }

                CheckHelpMessages();
                CheckSubtitleMessages();
            }
            catch (Exception ex)
            {

            }
        }


        private void CheckHelpMessages()
        {
            var message = MessageQueue.Instance.GetHelpMessage();
            if (message != null)
            {
                Screen.ShowHelpText(message.HelpText, message.Duration, message.Beep, message.Looped);
            }
        }

        private void CheckSubtitleMessages()
        {
            var message = MessageQueue.Instance.GetSubtitleMessage();
            if (message != null)
            {
                Screen.ShowSubtitle(message.Message, message.Duration, message.DrawImmediately);
            }
        }

        private void ExecuteCommand(string command)
        {
            if (command.ToLower() == "goinworld")
            {
                if (activeRoleScenario != null)
                {
                    Screen.ShowSubtitle("Inworld Mission is already in place. Use leaveinworld to leave Inworld Missions", messageTime, true);
                }
                else
                {

                    activeRoleScenario = InstantiateScript<PoliceScenario>();
                    activeRoleScenario.OnSocketEventId += OnSocketEventReceived;
                    SceneHelper.SetTime(8, "CLOUDY");
                    activeRoleScenario.StartScenario();
                    string name = GetNameCall();
                    ((PoliceScenario)activeRoleScenario).SetNewPlayerName(name);
                }
                LAST_SPOKE_TIME = elapsedTime + FILLER_GAP;
            }
            if (command.ToLower() == "loadinworld")
            {
                activeRoleScenario = InstantiateScript<PoliceScenario>();
                activeRoleScenario.OnSocketEventId += OnSocketEventReceived;
                var loadedGame = SaveSystem.LoadGame();
                if (loadedGame == null)
                {
                    activeRoleScenario.StartScenario();
                }
                else
                {
                    SceneHelper.SetTime(loadedGame.TimeOfDay.Hours);
                    activeRoleScenario.LoadScenario(loadedGame.Companion, loadedGame.Day, loadedGame.Mission, loadedGame.Position, loadedGame.Rotation, loadedGame.CarPosition, loadedGame.CarRotation, loadedGame.LastCompletedHour, loadedGame.IsRidingCar, loadedGame.PlayerName);
                }

                LAST_SPOKE_TIME = elapsedTime + FILLER_GAP;
            }
            else if (command.ToLower() == "leaveinworld")
            {
                if (activeRoleScenario != null)
                {
                    activeRoleScenario.ResetScenario();
                    activeRoleScenario = null;
                }
            }
            else if (command.ToLower() == "fixmycar")
            {
                if (activeRoleScenario != null)
                {
                    ((PoliceScenario)activeRoleScenario).ResetCar();
                }
            }
            if (command.ToLower().Contains("setwtime"))
            {
                var splitted = command.ToLower().Split(' ');
                if (splitted.Length >= 3)
                {
                    int val = int.Parse(splitted[1]);
                    SceneHelper.SetTime(val, splitted[2].ToUpper());
                }
            }
        }

        public string GetNameCall()
        {
            Function.Call(Hash.DISPLAY_ONSCREEN_KEYBOARD, true, "FMMC_MPM_NA", "", "", "", "", "", 30);
            while (Function.Call<int>(Hash.UPDATE_ONSCREEN_KEYBOARD) == 0)
            {
                Script.Yield();
            }
            return Function.Call<string>(Hash.GET_ONSCREEN_KEYBOARD_RESULT);
        }

        private void OnSocketEventReceived(string id, string eventid)
        {
            if (socket == null) return;

            if (id == "TRIGGER_EVENT")
            {
                // Dont upsert anything for comment
                if (!string.IsNullOrEmpty(socket.awaitingTrigger))
                {
                    if (eventid.Contains("comment_"))
                    {
                        return;
                    }
                }
                socket.SetAwaitingTrigger(eventid, (int)elapsedTime);
            }
            else if (id != null && id.StartsWith("START_TRIGGER"))
            {
                if (socket.IsConnectedAndToPartner())
                {
                    socket.TriggerEvent(eventid);
                }
                else
                {
                    socket.SetAwaitingTrigger(eventid, (int)elapsedTime);
                    var splitted = id.Split(new string[] { ";;" }, StringSplitOptions.None);
                    InternalConnect(splitted[1]);
                }
            }
            else if (eventid == "disconnect")
            {
                if (currentConnected == id)
                {
                    socket.DisconnectToCharacter(id);
                    currentConnected = string.Empty;
                    connectedToEvent = false;
                }
            }
            else if (eventid == "connect")
            {
                InternalConnect(id);
            }
            else if (eventid == "reconnect")
            {
                socket.ReconnectToCharacter();
            }
        }

        private void InternalConnect(string id = null)
        {
            string idAlternative = activeRoleScenario.GetScenarioCharacter(out string pname);
            connectedToEvent = true;
            currentConnected = idAlternative;
            socket.ConnectToCharacter(idAlternative, pname);
        }

    }
}
