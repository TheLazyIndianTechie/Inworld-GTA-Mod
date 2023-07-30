using GTA.Math;
using GTA.Native;
using System;
using System.Windows.Forms;

namespace InworldV.Scenario
{
    public abstract class RoleScenarioBase
    {
        public RoleScenarioBase()
        {
            Function.Call(Hash.REQUEST_ANIM_DICT, "mp_facial");
        }

        public virtual void OnTick(InworldV sender, EventArgs e)
        {

        }

        public delegate void SocketEventHandler(string id, string eventid);
        public event SocketEventHandler OnSocketEventId;

        public virtual void OnSocketEventChanged(string id, string eventid)
        {
            OnSocketEventId?.Invoke(id, eventid);
        }

        public virtual bool IsCurrentMissionActive
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsCloseToSubject
        {
            get
            {
                return true;
            }
        }

        public virtual void TickIncomingMessage() { }
        public virtual void OnKeyDown(object sender, KeyEventArgs e) { }
        public virtual string GetScenarioCharacter(out string playerName) { playerName = "Officer"; return string.Empty; }
        public virtual void OnTalkTriggered() { }
        public virtual void ProcessGameEvent(string eventId) { }
        public virtual void StartScenario() { }
        public virtual void LoadScenario(int companion, int day, int progress, Vector3 position, Vector3 rotation, Vector3 carPosition, Vector3 carRotation, int lastCompletedMissionHour, bool isRiding, string playerName) { }
        public virtual void ResetScenario() { }
        public virtual void ProcessTalkingCharacter(bool isTalking) { }
    }
}
