using Me.EarzuChan.Ryo.Adaptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Formations
{
    [IAdaptable.AdaptableFormat("sengine.graphics2d.FontSprites")]
    public class FontSprites : IAdaptable
    {
        public int[] IArr;
        public byte[][] BArr;
        public float F;
        public int I;

        [IAdaptable.AdaptableConstructor]
        public FontSprites(int[] iArr, byte[][] bArr, float f, int i)
        {
            IArr = iArr;
            BArr = bArr;
            F = f;
            I = i;
        }

        object[] IAdaptable.GetAdaptedArray() => new object[] { IArr, BArr, F, I };
    }

    [IAdaptable.AdaptableFormat("game31.DialogueTree$DialogueTreeDescriptor")]
    public class Conversations : IAdaptable
    {
        private string DialogueNameSpace;
        private Conversation[] 数组_对话;

        [IAdaptable.AdaptableConstructor]
        public Conversations(string str, Conversation[] r2)
        {
            DialogueNameSpace = str;
            数组_对话 = r2;
        }

        public object[] GetAdaptedArray() => new object[] { DialogueNameSpace, 数组_对话 };
    }

    [IAdaptable.AdaptableFormat("game31.DialogueTree$Conversation")]
    public class Conversation : IAdaptable
    {
        private string[] Tags;
        private string Status;
        private UserMessage[] UserMessages;
        private bool StateOfDiswatch;
        private SenderMessage[] SenderMessagers;
        private string[] TagsToUnlock;
        private string[] TagsToLock;
        private string Trigger;

        [IAdaptable.AdaptableConstructor]
        public Conversation(string[] tags, string status, UserMessage[] userMessages, bool stateOfDiswatch, SenderMessage[] senderMessagers, string[] tagsToUnlock, string[] tagsToLock, string trigger)
        {
            Tags = tags;
            Status = status;
            UserMessages = userMessages;
            StateOfDiswatch = stateOfDiswatch;
            SenderMessagers = senderMessagers;
            TagsToUnlock = tagsToUnlock;
            TagsToLock = tagsToLock;
            Trigger = trigger;
        }

        public object[] GetAdaptedArray() => new object[] { Tags, Status, UserMessages, StateOfDiswatch, SenderMessagers, TagsToUnlock, TagsToLock, Trigger };

    }

    [IAdaptable.AdaptableFormat("game31.DialogueTree$UserMessage")]
    public class UserMessage : IAdaptable
    {
        public bool IsHidden;
        public string Message;

        [IAdaptable.AdaptableConstructor]
        public UserMessage(string str, bool z)
        {
            Message = str;
            IsHidden = z;
        }

        public object[] GetAdaptedArray() => new object[] { Message, IsHidden };
    }

    [IAdaptable.AdaptableFormat("game31.DialogueTree$SenderMessage")]
    public class SenderMessage : IAdaptable
    {
        public string DateText;
        public string Message;
        public string Origin;
        public float IdleTime;
        public float TriggerTime;
        public float TypingTime;
        public string TimeText;
        public string Trigger;

        [IAdaptable.AdaptableConstructor]
        public SenderMessage(string message, string origin, string dataText, string timeText, float idleTime, float typingTime, string trigger, float triggerTime)
        {
            Message = message;
            Origin = origin;
            DateText = dataText;
            TimeText = timeText;
            IdleTime = idleTime;
            TypingTime = typingTime;
            Trigger = trigger;
            TriggerTime = triggerTime;
        }

        public object[] GetAdaptedArray() => new object[] { Message, Origin, DateText, TimeText, IdleTime, TypingTime, Trigger, TriggerTime };
    }
}
