<<<<<<< HEAD
﻿using Me.EarzuChan.Ryo.Adaptions;
using Me.EarzuChan.Ryo.Formations;
using Me.EarzuChan.Ryo.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Formations
{
    namespace Universe
    {
        [AdaptableFormat("sengine.graphics2d.FontSprites")]
        public class FontSprites : ICtorAdaptable
        {
            public int[] IArr;
            public byte[][] BArr;
            public float F;
            public int I;

            [ICtorAdaptable.AdaptableConstructor]
            public FontSprites(int[] iArr, byte[][] bArr, float f, int i)
            {
                IArr = iArr;
                BArr = bArr;
                F = f;
                I = i;
            }

            object[] ICtorAdaptable.GetAdaptedArray() => new object[] { IArr, BArr, F, I };
        }
    }

    namespace PipeDream
    {
        [Serializable]
        [AdaptableFormat("game31.DialogueTree$DialogueTreeDescriptor")]
        public class DialogueTreeDescriptor : ICtorAdaptable
        {
            public string DialogueNameSpace;
            public List<Conversation> ConversationList;

            [ICtorAdaptable.AdaptableConstructor]
            public DialogueTreeDescriptor(string nsps, Conversation[] cons)
            {
                DialogueNameSpace = nsps;
                ConversationList = cons.ToList();
            }

            public object[] GetAdaptedArray() => new object[] { DialogueNameSpace, ConversationList.ToArray() };
        }

        [Serializable]
        [AdaptableFormat("game31.DialogueTree$Conversation")]
        public class Conversation : ICtorAdaptable
        {
            public List<string> Tags;
            public string Status;
            public List<UserMessage> UserMessages;
            public bool StateOfDiswatch;
            public List<SenderMessage> SenderMessagers;
            public List<string> TagsToUnlock;
            public List<string> TagsToLock;
            public string Trigger;

            [ICtorAdaptable.AdaptableConstructor]
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

        [Serializable]
        [AdaptableFormat("game31.DialogueTree$UserMessage")]
        public class UserMessage : ICtorAdaptable
        {
            public bool IsHidden;
            public string Message;

            [ICtorAdaptable.AdaptableConstructor]
            public UserMessage(string str, bool z)
            {
                Message = str;
                IsHidden = z;
            }

            public object[] GetAdaptedArray() => new object[] { Message, IsHidden };
        }

        [Serializable]
        [AdaptableFormat("game31.DialogueTree$SenderMessage")]
        public class SenderMessage : ICtorAdaptable
        {
            public string DateText;
            public string Message;
            public string Origin;
            public float IdleTime;
            public float TriggerTime;
            public float TypingTime;
            public string TimeText;
            public string Trigger;

            [ICtorAdaptable.AdaptableConstructor]
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

    namespace WeakPipe
    {
        [AdaptableFormat("me.earzuchan.weakpipe.Outer")]
        public class YsbNmslOuter : ICtorAdaptable
        {
            public string Origin;
            public YsbNmslInner Inner;

            [ICtorAdaptable.AdaptableConstructor]
            public YsbNmslOuter(string origin, YsbNmslInner inner)
            {
                Origin = origin;
                Inner = inner;
            }

            public object[] GetAdaptedArray() => new object[] { Origin, Inner };
        }

        [AdaptableFormat("me.earzuchan.weakpipe.Inner")]
        public class YsbNmslInner : ICtorAdaptable
        {
            public string Origin;

            [ICtorAdaptable.AdaptableConstructor]
            public YsbNmslInner(string origin) => Origin = origin;

            public object[] GetAdaptedArray() => new object[] { Origin };
        }

        [AdaptableFormat("me.earzuchan.weakpipe.HugeOuter")]
        public class YsbNmslHugeOuter : ICtorAdaptable
        {
            public string Origin;
            public YsbNmslInner Inner;
            public YsbNmslInner[] Inners;

            [ICtorAdaptable.AdaptableConstructor]
            public YsbNmslHugeOuter(string origin, YsbNmslInner inner, YsbNmslInner[] inners)
            {
                Origin = origin;
                Inner = inner;
                Inners = inners;
            }

            public object[] GetAdaptedArray() => new object[] { Origin, Inner, Inners };
        }
    }

    namespace SIM
    {
        [AdaptableFormat("game23.model.DialogueTreeModel")]
        public class SaraDialogueTree
        {

            public ConditionMacroModel[] ConditionMacros;
            public ConversationModel[] Conversations;
            public string Initialization;
            public string Namespace;
        }

        [AdaptableFormat("game23.model.DialogueTreeModel$ConditionMacroModel")]
        public class ConditionMacroModel
        {
            public string Condition;
            public string Name;
        }

        [AdaptableFormat("game23.model.DialogueTreeModel$ConversationModel")]
        public class ConversationModel
        {
            public string Condition;
            public bool IsUserIgnored;
            public SenderMessageModel[] SenderMessages;
            public string Tags;
            public string TagsToLock;
            public string TagsToUnlock;
            public string Trigger;
            public UserMessageModel[] UserMessages;
        }

        [AdaptableFormat("game23.model.DialogueTreeModel$SenderMessageModel")]
        public class SenderMessageModel
        {
            public string DateText;
            public float IdleTime;
            public string Message;
            public string Origin;
            public string TimeText;
            public string Trigger;
            public float TriggerTime;
            public float TypingTime;
        }

        [AdaptableFormat("game23.model.DialogueTreeModel$UserMessageModel")]
        public class UserMessageModel
        {
            public bool IsHidden;
            public string Message;
        }
    }
}
=======
﻿using Me.EarzuChan.Ryo.Adaptions;
using Me.EarzuChan.Ryo.Utils;
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
        public Conversations(string nsps, Conversation[] cons)
        {
            DialogueNameSpace = nsps;
            ConversationArray = cons.ToList();
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

    [IAdaptable.AdaptableFormat("me.earzuchan.weakpipe.Outer")]
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

    [IAdaptable.AdaptableFormat("me.earzuchan.weakpipe.Inner")]
    public class YsbNmslInner : IAdaptable
    {
        public string Origin;

        [IAdaptable.AdaptableConstructor]
        public YsbNmslInner(string origin) => Origin = origin;

        public object[] GetAdaptedArray() => new object[] { Origin };
    }

    [IAdaptable.AdaptableFormat("me.earzuchan.weakpipe.HugeOuter")]
    public class YsbNmslHugeOuter : IAdaptable
    {
        public string Origin;
        public YsbNmslInner Inner;
        public YsbNmslInner[] Inners;

        [IAdaptable.AdaptableConstructor]
        public YsbNmslHugeOuter(string origin, YsbNmslInner inner, YsbNmslInner[] inners)
        {
            Origin = origin;
            Inner = inner;
            Inners = inners;
        }

        public object[] GetAdaptedArray() => new object[] { Origin, Inner, Inners };
    }
}
>>>>>>> 62013cbf2e8e2181ffdf41d357ae518f5ad00c74
