using Me.EarzuChan.Ryo.Core.Codecations;
using System;

namespace Me.EarzuChan.Ryo.Core.Formations
{
    namespace DataFormations
    {
        namespace Universe
        {
            [CodecableFormation("sengine.graphics2d.FontSprites")]
            public class FontSprites : ICtorCodecable
            {
                public int[] IArr;
                public byte[][] BArr;
                public float F;
                public int I;

                [ICtorCodecable.CodecableConstructor]
                public FontSprites(int[] iArr, byte[][] bArr, float f, int i)
                {
                    IArr = iArr;
                    BArr = bArr;
                    F = f;
                    I = i;
                }

                object[] ICtorCodecable.GetCodecatedArray() => [IArr, BArr, F, I];
            }

            /*[CodecableFormation("sengine.materials.SimpleMaterial")]
            public class SimpleMaterial : ICtorCodecable
            {
                public int I;
                public bool Z;
                public int I2;
                public int I3;
                public int I4;
                public string TextureFilename;
                public float Length;
                public TextureZero 零;
                public Shader NormalShader;
                public float TextureLodBias;
                public bool IsStreamed;
                // 我干宁娘 public Texture.TextureFilter MinFilter;
                // 我干宁娘 public Texture.TextureFilter MagFilter;
                // 我干宁娘 public Texture.TextureWrap UWrap;
                // 我干宁娘 public Texture.TextureWrap VWrap;
                public float TGarbageTime;

                [ICtorCodecable.CodecableConstructor]
                public SimpleMaterial(int i, bool z, int i2, int i3, int i4, string str, float f, TextureZero r10, Shader r11, float f2, bool z2, Texture.TextureFilter textureFilter, Texture.TextureFilter textureFilter2, Texture.TextureWrap textureWrap, Texture.TextureWrap textureWrap2, float f3)
                {
                    I = i;
                    Z = z;
                    I2 = i2;
                    I3 = i3;
                    I4 = i4;
                    TextureFilename = str;
                    Length = f;
                    零 = r10;
                    NormalShader = r11;
                    TextureLodBias = f2;
                    IsStreamed = z2;
                    // MinFilter = textureFilter;
                    // MagFilter = textureFilter2;
                    // UWrap = textureWrap;
                    // VWrap = textureWrap2;
                    TGarbageTime = f3;
                }

                public object[] GetCodecatedArray()
                {
                    throw new NotImplementedException();
                }
            }*/

            [CodecableFormation("sengine.graphics2d.TextureZERO")]
            public class TextureZero
            {
            } // TODO：Mass

            [CodecableFormation("sengine.graphics2d.Shader")]
            public class Shader
            {
            } // TODO：ShaderCodec
        }

        namespace PipeDream
        {
            [CodecableFormation("game31.model.PhoneAppModel")]
            public class PhoneAppModel
            {
                public ContactModel[] contacts;
                public string default_ringtone;
                public string[] favourites;
                public PhoneRecentModel[] recents = [];
                public string[] emergency_numbers = [];
            }

            [CodecableFormation("game31.model.PhoneAppModel$PhoneRecentModel")]
            public class PhoneRecentModel
            {
                public string name;
                public string time;
                public string type;
            }

            [CodecableFormation("game31.model.ContactModel")]
            public class ContactModel
            {
                public string device;
                public string dialogue_tree_path;
                public string google_sheet_url;
                public bool is_hidden;
                public string name;
                public string number;
                public string trigger;
                public string profile_pic_path = "system/profile.png";
                public string big_profile_pic_path = "system/profile-big.png";
                public ContactAttributeModel[] attributes = [];
            }

            [CodecableFormation("game31.model.ContactModel$ContactAttributeModel")]
            public class ContactAttributeModel
            {
                public string action;
                public string attribute;
                public string value;
            }


            [CodecableFormation("game31.DialogueTree$DialogueTreeDescriptor")]
            public class DialogueTreeDescriptor : ICtorCodecable
            {
                public string DialogueNameSpace;
                public List<Conversation> ConversationList;

                [ICtorCodecable.CodecableConstructor]
                public DialogueTreeDescriptor(string nsps, Conversation[] cons)
                {
                    DialogueNameSpace = nsps;
                    ConversationList = cons.ToList();
                }

                // For Json
                public DialogueTreeDescriptor()
                {
                }

                public object[] GetCodecatedArray() => [DialogueNameSpace, ConversationList.ToArray()];
            }

            [CodecableFormation("game31.DialogueTree$Conversation")]
            public class Conversation : ICtorCodecable
            {
                public List<string> Tags;
                public string Status;
                public List<UserMessage> UserMessages;
                public bool StateOfDiswatch;
                public List<SenderMessage> SenderMessagers;
                public List<string> TagsToUnlock;
                public List<string> TagsToLock;
                public string Trigger;

                [ICtorCodecable.CodecableConstructor]
                public Conversation(string[] tags, string status, UserMessage[] userMessages, bool stateOfDiswatch,
                    SenderMessage[] senderMessagers, string[] tagsToUnlock, string[] tagsToLock, string trigger)
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

                // For Json
                public Conversation()
                {
                }

                public object[] GetCodecatedArray() =>
                [
                    Tags.ToArray(), Status, UserMessages.ToArray(), StateOfDiswatch, SenderMessagers.ToArray(),
                    TagsToUnlock.ToArray(), TagsToLock.ToArray(), Trigger
                ];
            }

            [CodecableFormation("game31.DialogueTree$UserMessage")]
            public class UserMessage : ICtorCodecable
            {
                public bool IsHidden;
                public string Message;

                [ICtorCodecable.CodecableConstructor]
                public UserMessage(string str, bool z)
                {
                    Message = str;
                    IsHidden = z;
                }

                // For Json
                public UserMessage()
                {
                }

                public object[] GetCodecatedArray() => [Message, IsHidden];
            }

            [CodecableFormation("game31.DialogueTree$SenderMessage")]
            public class SenderMessage : ICtorCodecable
            {
                public string DateText;
                public string Message;
                public string Origin;
                public float IdleTime;
                public float TriggerTime;
                public float TypingTime;
                public string TimeText;
                public string Trigger;

                [ICtorCodecable.CodecableConstructor]
                public SenderMessage(string message, string origin, string dataText, string timeText, float idleTime,
                    float typingTime, string trigger, float triggerTime)
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

                // For Json
                public SenderMessage()
                {
                }

                public object[] GetCodecatedArray() =>
                [
                    Message, Origin, DateText, TimeText, IdleTime, TypingTime, Trigger, TriggerTime
                ];
            }
        }

        // TIPS：WeakPipe is for internal research only, not presenting in real games
        namespace WeakPipe
        {
            [CodecableFormation("me.earzuchan.weakpipe.Outer")]
            public class Outer : ICtorCodecable
            {
                public string Origin;
                public Inner Inner;

                [ICtorCodecable.CodecableConstructor]
                public Outer(string origin, Inner inner)
                {
                    Origin = origin;
                    Inner = inner;
                }

                public object[] GetCodecatedArray() => [Origin, Inner];
            }

            [CodecableFormation("me.earzuchan.weakpipe.Inner")]
            public class Inner : ICtorCodecable
            {
                public string Origin;

                [ICtorCodecable.CodecableConstructor]
                public Inner(string origin) => Origin = origin;

                public object[] GetCodecatedArray() => [Origin];
            }

            [CodecableFormation("me.earzuchan.weakpipe.HugeOuter")]
            public class HugeOuter : ICtorCodecable
            {
                public string Origin;
                public Inner Inner;
                public Inner[] Inners;

                [ICtorCodecable.CodecableConstructor]
                public HugeOuter(string origin, Inner inner, Inner[] inners)
                {
                    Origin = origin;
                    Inner = inner;
                    Inners = inners;
                }

                public object[] GetCodecatedArray() => [Origin, Inner, Inners];
            }
        }

        namespace SIM
        {
            [CodecableFormation("game23.model.DialogueTreeModel")]
            public class SaraDialogueTree
            {
                public ConditionMacroModel[] ConditionMacros;
                public ConversationModel[] Conversations;
                public string Initialization;
                public string Namespace;
            }

            [CodecableFormation("game23.model.DialogueTreeModel$ConditionMacroModel")]
            public class ConditionMacroModel
            {
                public string Condition;
                public string Name;
            }

            [CodecableFormation("game23.model.DialogueTreeModel$ConversationModel")]
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

            [CodecableFormation("game23.model.DialogueTreeModel$SenderMessageModel")]
            // ReSharper disable once ClassNeverInstantiated.Global
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

            [CodecableFormation("game23.model.DialogueTreeModel$UserMessageModel")]
            public class UserMessageModel
            {
                public bool IsHidden;
                public string Message;
            }
        }

        namespace SimuOne {
            [CodecableFormation("game27.model.DialogueTreeModel")]
            public class DialogueTreeModel {
                public string Filename;
                public string Namespace;
                public ConversationModel[] conversations;
            }
            
            [CodecableFormation("game27.model.DialogueTreeModel$ConversationModel")]
            public class ConversationModel
            {
                public string Tags;
                public string Condition;
                public UserMessageModel[] UserMessages;
                public bool IsUserIgnored;
                public SenderMessageModel[] SenderMessages;
                public string TagsToUnlock;
                public string TagsToLock;
                public string Trigger;
            }
            
            [CodecableFormation("game27.model.DialogueTreeModel$UserMessageModel")]
            public class UserMessageModel
            {
                public string Message;
                public bool IsHidden;
            }

            [CodecableFormation("game27.model.DialogueTreeModel$SenderMessageModel")]
            public class SenderMessageModel
            {
                public string Message;
                public string Origin;
                public string DateText;
                public string TimeText;
                public float IdleTime;
                public float TypingTime;
                public string Trigger;
                public float TriggerTime;
            }
            
            [CodecableFormation("game27.DialogueTree$DialogueTreeDescriptor")]
            public class DialogueTreeDescriptor : ICtorCodecable
            {
                public string DialogueNameSpace;
                public List<Conversation> ConversationList;

                [ICtorCodecable.CodecableConstructor]
                public DialogueTreeDescriptor(string nsps, Conversation[] cons)
                {
                    DialogueNameSpace = nsps;
                    ConversationList = cons?.ToList() ?? new List<Conversation>();
                }

                public object[] GetCodecatedArray() => [DialogueNameSpace, ConversationList.ToArray()];
            }
            
            [CodecableFormation("game27.DialogueTree$Conversation")]
            public class Conversation : ICtorCodecable
            {
                public string[] Tags;
                public string Condition;
                public UserMessage[] UserMessages;
                public bool IsUserIgnored;
                public SenderMessage[] SenderMessages;
                public string[] TagsToUnlock;
                public string[] TagsToLock;
                public string Trigger;

                [ICtorCodecable.CodecableConstructor]
                public Conversation(string[] tags, string condition, UserMessage[] userMessages, bool isUserIgnored, SenderMessage[] senderMessages, string[] tagsToUnlock, string[] tagsToLock, string trigger)
                {
                    Tags = tags;
                    Condition = condition;
                    UserMessages = userMessages;
                    IsUserIgnored = isUserIgnored;
                    SenderMessages = senderMessages;
                    TagsToUnlock = tagsToUnlock;
                    TagsToLock = tagsToLock;
                    Trigger = trigger;
                }

                public object[] GetCodecatedArray() => 
                [
                    Tags, 
                    Condition, 
                    UserMessages, 
                    IsUserIgnored, 
                    SenderMessages, 
                    TagsToUnlock, 
                    TagsToLock, 
                    Trigger
                ];
            }
            
            [CodecableFormation("game27.DialogueTree$SenderMessage")]
            public class SenderMessage : ICtorCodecable
            {
                public string Message;
                public string Origin;
                public string DateText;
                public string TimeText;
                public float TIdleTime;
                public float TTypingTime;
                public string Trigger;
                public float TTriggerTime;

                [ICtorCodecable.CodecableConstructor]
                public SenderMessage(string message, string origin, string dateText, string timeText, float tIdleTime, float tTypingTime, string trigger, float tTriggerTime)
                {
                    Message = message;
                    Origin = origin;
                    DateText = dateText;
                    TimeText = timeText;
                    TIdleTime = tIdleTime;
                    TTypingTime = tTypingTime;
                    Trigger = trigger;
                    TTriggerTime = tTriggerTime;
                }

                public object[] GetCodecatedArray() => 
                [
                    Message, 
                    Origin, 
                    DateText, 
                    TimeText, 
                    TIdleTime, 
                    TTypingTime, 
                    Trigger, 
                    TTriggerTime
                ];
            }
            
            [CodecableFormation("game27.DialogueTree$UserMessage")]
            public class UserMessage : ICtorCodecable
            {
                public string Message;
                public bool IsHidden;

                [ICtorCodecable.CodecableConstructor]
                public UserMessage(string message, bool isHidden)
                {
                    Message = message;
                    IsHidden = isHidden;
                }

                public object[] GetCodecatedArray() => [Message, IsHidden];
            }
        }
    }
}