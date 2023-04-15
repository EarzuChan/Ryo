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
        public string DialogueNameSpace;
        public List<Conversation> ConversationArray;

        [IAdaptable.AdaptableConstructor]
        public Conversations(string str, Conversation[] r2)
        {
            DialogueNameSpace = str;
            ConversationArray = r2.ToList();
        }

        public object[] GetAdaptedArray() => new object[] { DialogueNameSpace, ConversationArray.ToArray() };
    }

    [IAdaptable.AdaptableFormat("game31.DialogueTree$Conversation")]
    public class Conversation : IAdaptable
    {
        public List<string> Tags;
        public string Status;
        public List<UserMessage> UserMessages;
        public bool StateOfDiswatch;
        public List<SenderMessage> SenderMessagers;
        public List<string> TagsToUnlock;
        public List<string> TagsToLock;
        public string Trigger;

        [IAdaptable.AdaptableConstructor]
        public Conversation(string[] tags, string status, UserMessage[] userMessages, bool stateOfDiswatch, SenderMessage[] senderMessagers, string[] tagsToUnlock, string[] tagsToLock, string trigger)
        {
            Tags = tags.ToList();
            Status = status;
            UserMessages = userMessages.ToList();
            StateOfDiswatch = stateOfDiswatch;
            SenderMessagers = senderMessagers.ToList();
            TagsToUnlock = tagsToUnlock.ToList();
            TagsToLock = tagsToLock.ToList();
            Trigger = trigger;
        }

        public object[] GetAdaptedArray() => new object[] { Tags.ToArray(), Status, UserMessages.ToArray(), StateOfDiswatch, SenderMessagers.ToArray(), TagsToUnlock.ToArray(), TagsToLock.ToArray(), Trigger };

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

    [IAdaptable.AdaptableFormat("ysb.nmsl.Outer")]
    public class YsbNmslOuter : IAdaptable
    {
        public string Origin;
        public YsbNmslInner Inner;

        [IAdaptable.AdaptableConstructor]
        public YsbNmslOuter(string origin, YsbNmslInner inner)
        {
            Origin = origin;
            Inner = inner;
        }

        public object[] GetAdaptedArray() => new object[] { Origin, Inner };
    }

    [IAdaptable.AdaptableFormat("ysb.nmsl.Inner")]
    public class YsbNmslInner : IAdaptable
    {
        public string Origin;

        [IAdaptable.AdaptableConstructor]
        public YsbNmslInner(string origin) => Origin = origin;

        public object[] GetAdaptedArray() => new object[] { Origin };
    }
}
