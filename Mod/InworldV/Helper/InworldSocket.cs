using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using WebSocketSharp;

namespace InworldV.Helper
{
    internal enum ConnectionType
    {
        AGENT_AUDIO,
        CHAT,
        USER_TRANSCRIBE,
        START_MIC,
        END_MIC
    }

    internal class InworldSocket
    {
        private readonly string SocketAddress = "ws://127.0.0.1:3000/chat";
        public delegate void GameEvent(string eventid);
        public event GameEvent OnGameEventFromChat;
        public string awaitingTrigger;
        private WebSocket ws;
        private bool isConnectedBackend;
        private bool isCurrentlyConnected;
        private int triggerTimestamp;
        private string currentConnectedId;
        private Action<string> audioReceivedCallback = null;
        private Action<string> messageReceivedCallback = null;
        private Action<string> eventReceivedCallback = null;

        public List<string> eventQueue = new List<string>();
        public List<string> messageQueue = new List<string>();

        public InworldSocket() { }

        public void TickAwaitingQueue(int time)
        {
            if (IsCompanion() && !string.IsNullOrEmpty(awaitingTrigger))
            {
                // check time
                if (time - triggerTimestamp > 1990)
                {
                    // trigger
                    triggerTimestamp = -1;
                    this.TriggerEvent(awaitingTrigger);
                    awaitingTrigger = string.Empty;
                }

            }
        }

        public bool IsConnectedAndToPartner()
        {
            if (string.IsNullOrWhiteSpace(currentConnectedId)) return false;
            return (currentConnectedId == "emily_martinez" || currentConnectedId == "tony_russo" || currentConnectedId == "frank_thompson");
        }

        public void SetAwaitingTrigger(string id, int time)
        {
            if (string.IsNullOrEmpty(id)) return;
            awaitingTrigger = id;
            triggerTimestamp = time;
        }

        private bool IsCompanion()
        {
            if (string.IsNullOrEmpty(awaitingTrigger)) return false;
            return (currentConnectedId == "emily_martinez" || currentConnectedId == "tony_russo" || currentConnectedId == "frank_thompson");
        }

        public void ConnectToCharacter(string id, string playerName = "man")
        {
            if (!isConnectedBackend)
            {
                this.Connect();
            }
            currentConnectedId = id;
            this.Send("connect", playerName, id);
        }

        public void TriggerEvent(string id)
        {
            this.Send("event", string.Empty, id);
        }

        public void DisconnectToCharacter(string id)
        {
            isCurrentlyConnected = false;
            this.Send("disconnect", string.Empty, id);
        }

        public void ReconnectToCharacter()
        {
            this.Send("reconnect", string.Empty, string.Empty);
        }

        public void StartVoiceSending()
        {
            if (!isConnectedBackend) return;
            this.Send("user_voice_start", string.Empty, string.Empty);
            AudioManager.Instance.StartRecording(this.OnVoiceDataReceived);
        }

        public void StopVoiceSending()
        {
            if (!isConnectedBackend) return;
            AudioManager.Instance.StopRecording();
            this.Send("user_voice_pause", string.Empty, string.Empty);
        }

        private void OnVoiceDataReceived(byte[] chunk)
        {
            if (chunk == null || chunk.Length == 0) return;
            IfSocketIsDown();
            ws.Send(chunk);
        }

        public void Connect()
        {
            try
            {
                ws = new WebSocket(SocketAddress);
                ws.OnMessage += OnMessage;
                ws.OnClose += OnClose;
                ws.OnError += OnError;
                ws.Connect();
                isConnectedBackend = true;
            }
            catch (Exception e)
            {
                isConnectedBackend = false;
            }
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            isConnectedBackend = false;
            ws = null;
        }

        private void OnClose(object sender, CloseEventArgs e)
        {
            isConnectedBackend = false;
            ws = null;
        }

        private void OnMessage(object sender, MessageEventArgs e)
        {
            try
            {
                JObject msgObject = JObject.Parse(e.Data);

                if (msgObject.ContainsKey("type") && msgObject["type"].ToString() != null)
                {
                    string msgType = msgObject["type"].ToString();

                    if (msgType == "agent_audio")
                    {
                        AudioManager.Instance.PushChunk(msgObject["data"].ToString());
                    }
                    else if (msgType == "chat")
                    {
                        messageQueue.Add(msgObject["message"].ToString());
                    }
                    else if (msgType == "established")
                    {
                        isCurrentlyConnected = true;
                        messageQueue.Add("Person is now listening you. Press and hold ~g~N ~w~to ~g~talk.");
                    }
                    else if (msgType == "event")
                    {
                        string id = msgObject["event_id"].ToString();
                        eventQueue.Add(id);
                    }
                }
            }
            catch
            {

            }
        }

        public string[] GetEventQueue()
        {
            var queueData = eventQueue.ToArray();
            eventQueue.Clear();
            return queueData;
        }

        public string GetFirstMessage()
        {
            if (messageQueue.Count == 0) return string.Empty;
            string message = messageQueue.ElementAt(0);
            messageQueue.RemoveAt(0);
            return message;
        }

        public void SetEventCallback(Action<string> callback)
        {
            eventReceivedCallback = callback;
        }

        public void Send(string messageType, string messageData, string idData)
        {
            dynamic jsonPayload = new
            {
                type = messageType,
                message = messageData,
                id = idData
            };

            string jsonString = JsonConvert.SerializeObject(jsonPayload);

            if (jsonString == null || jsonString.Length == 0) return;
            IfSocketIsDown();
            ws.Send(jsonString);
        }


        private void IfSocketIsDown()
        {
            if (!isConnectedBackend || ws == null || !ws.IsAlive)
            {
                Connect();
            }
        }
    }

}
