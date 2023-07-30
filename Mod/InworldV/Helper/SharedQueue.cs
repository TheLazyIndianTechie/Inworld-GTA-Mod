using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InworldV.Helper
{
    internal class HelpMessage
    {
        public string HelpText;
        public int Duration = -1;
        public bool Beep = true;
        public bool Looped = false;

        public HelpMessage(string helpText, int duration = -1, bool beep = true, bool looped = false)
        {
            this.HelpText = helpText;
            this.Duration = duration;
            this.Beep = beep;
            this.Looped = looped;
        }
    }

    internal class SubtitleMessage
    {
        public string Message;
        public int Duration = -1;
        public bool DrawImmediately = true;

        public SubtitleMessage(string message, int duration = 3000, bool drawImmediately = true)
        {
            this.Message = message;
            this.Duration = duration;
            this.DrawImmediately = drawImmediately;
        }
    }

    internal class MessageQueue
    {
        private static MessageQueue instance;

        private MessageQueue() {}

        public static MessageQueue Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MessageQueue();
                }

                return instance;
            }
        }

        private List<HelpMessage> helpMessages = new List<HelpMessage>();
        private List<SubtitleMessage> subtitleMessages = new List<SubtitleMessage>();


        public void AddHelpMessage(HelpMessage message)
        {
            if (helpMessages == null)
                helpMessages = new List<HelpMessage>();

            if (!helpMessages.Contains(message))
                helpMessages.Add(message);
        }

        public void AddSubtitleMessage(SubtitleMessage message)
        {
            if (subtitleMessages == null)
                subtitleMessages = new List<SubtitleMessage>();

            if (!subtitleMessages.Contains(message))
                subtitleMessages.Add(message);
        }

        public HelpMessage GetHelpMessage()
        {
            if (helpMessages.Count == 0)
                return null;
            var returnThis = helpMessages[0];
            helpMessages.RemoveAt(0);
            return returnThis;
        }

        public SubtitleMessage GetSubtitleMessage()
        {
            if (subtitleMessages.Count == 0)
                return null;
            var returnThis = subtitleMessages[0];
            subtitleMessages.RemoveAt(0);
            return returnThis;
        }

    }
}
