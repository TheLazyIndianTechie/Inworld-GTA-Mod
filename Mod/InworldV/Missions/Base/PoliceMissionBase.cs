using GTA;
using System;

namespace InworldV.Missions
{
    internal abstract class PoliceMissionBase : Script
    {
        public enum MissionState
        {
            STARTED,
            SUCCESS,
            FAILED,
            REMOVED,
            WAITING
        };

        public enum DialogueState
        {
            CONNECT,
            DISCONNECT,
            RECONNECT
        };

        public PoliceMissionBase() { }

        public Vehicle policeVehicle;
        public MissionState missionState = MissionState.WAITING;
        public delegate void MissionStateChangedEventHandler(object sender, MissionState e);
        public delegate void MissionDialogueChangedEventHandler(string id, DialogueState e);
        public event MissionStateChangedEventHandler MissionStateChanged;
        public event MissionDialogueChangedEventHandler MissionDialogueStateChanged;
        protected virtual void OnMissionStateChanged(MissionState e)
        {
            MissionStateChanged?.Invoke(this, e);
        }

        protected virtual void OnMissionDialogueStateChanged(string id, DialogueState e)
        {
            MissionDialogueStateChanged?.Invoke(id, e);
        }

        public virtual void StartMission() { OnMissionStateChanged(MissionState.STARTED); }
        public virtual void StopMission() { OnMissionStateChanged(MissionState.REMOVED); }
        public virtual bool IsFinished() { return false; }
        public virtual bool IsActive() { return false; }
        public virtual void Cleanup() { }
        public virtual void ProcessGameEvent(string id) { }
        public virtual void ShowScreen() { }
        public virtual Ped GetArrestPed() { return null; }

        public virtual bool TryToConnectToActor(out Ped connected, out string characterId, out string playerName) { connected = null; playerName = "Officer"; characterId = string.Empty; return false; }

        public string ActorCharacter;
        public string PartnerMentionEventId = string.Empty;

        private int _displayDuration = 5000;
        private DateTime _startTime;
        private Scaleform _shownForm;
        private string _passTitle, _passDsc, _failTitle, _failDsc;

        protected void CreateScreen(string passTitle, string passDescription, string failTitle, string failDescription)
        {
            _passTitle = passTitle;
            _passDsc = passDescription;
            _failTitle = failTitle;
            _failDsc = failDescription;
        }

        protected void ShowMissionScreen(bool isSuccess)
        {
            if (isSuccess)
            {
                GTA.UI.Screen.StartEffect(GTA.UI.ScreenEffect.SuccessNeutral, _displayDuration);
                _shownForm = new Scaleform("MP_BIG_MESSAGE_FREEMODE");
                _shownForm.CallFunction("SHOW_SHARD_WASTED_MP_MESSAGE", _passTitle, _passDsc);
            }
            else
            {
                GTA.UI.Screen.StartEffect(GTA.UI.ScreenEffect.DeathFailNeutralIn, _displayDuration);
                _shownForm = new Scaleform("MP_BIG_MESSAGE_FREEMODE");
                _shownForm.CallFunction("SHOW_SHARD_WASTED_MP_MESSAGE", _failTitle, _failDsc);
            }

            _startTime = DateTime.Now;
            _shownForm.Render2D();
        }

        protected void CheckMissionScreen()
        {
            if (_shownForm != null && (DateTime.Now - _startTime).TotalMilliseconds > _displayDuration)
            {
                _shownForm.Dispose();
                _shownForm = null;
            }
        }

        public virtual void TickIncomingMessage()
        {

        }

        public virtual bool IsCloseToSubject
        {
            get
            {
                return true;
            }
        }
    }
}
