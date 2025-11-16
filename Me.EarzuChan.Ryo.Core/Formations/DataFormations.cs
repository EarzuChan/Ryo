using Me.EarzuChan.Ryo.Core.Adaptations;
using System;

namespace Me.EarzuChan.Ryo.Core.Formations
{
    namespace DataFormations
    {
        namespace Universe
        {
            [AdaptableFormation("sengine.graphics2d.FontSprites")]
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

                object[] ICtorAdaptable.GetAdaptedArray() => [IArr, BArr, F, I];
            }

            /*[AdaptableFormat("sengine.materials.SimpleMaterial")]
            public class SimpleMaterial : ICtorAdaptable
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

                [ICtorAdaptable.AdaptableConstructor]
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

                public object[] GetAdaptedArray()
                {
                    throw new NotImplementedException();
                }
            }*/

            [AdaptableFormation("sengine.graphics2d.TextureZERO")]
            public class TextureZero
            {
            } // TODO：Mass

            [AdaptableFormation("sengine.graphics2d.Shader")]
            public class Shader
            {
            } // TODO：ShaderAdapter
        }

        namespace PipeDream
        {
            [AdaptableFormation("game31.model.PhoneAppModel")]
            public class PhoneAppModel
            {
                public ContactModel[] contacts;
                public string default_ringtone;
                public string[] favourites;
                public PhoneRecentModel[] recents = [];
                public string[] emergency_numbers = [];
            }

            [AdaptableFormation("game31.model.PhoneAppModel$PhoneRecentModel")]
            public class PhoneRecentModel
            {
                public string name;
                public string time;
                public string type;
            }

            [AdaptableFormation("game31.model.ContactModel")]
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

            [AdaptableFormation("game31.model.ContactModel$ContactAttributeModel")]
            public class ContactAttributeModel
            {
                public string action;
                public string attribute;
                public string value;
            }


            [AdaptableFormation("game31.DialogueTree$DialogueTreeDescriptor")]
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

                // For Json
                public DialogueTreeDescriptor()
                {
                }

                public object[] GetAdaptedArray() => [DialogueNameSpace, ConversationList.ToArray()];
            }

            [AdaptableFormation("game31.DialogueTree$Conversation")]
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

                public object[] GetAdaptedArray() =>
                [
                    Tags.ToArray(), Status, UserMessages.ToArray(), StateOfDiswatch, SenderMessagers.ToArray(),
                    TagsToUnlock.ToArray(), TagsToLock.ToArray(), Trigger
                ];
            }

            [AdaptableFormation("game31.DialogueTree$UserMessage")]
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

                // For Json
                public UserMessage()
                {
                }

                public object[] GetAdaptedArray() => [Message, IsHidden];
            }

            [AdaptableFormation("game31.DialogueTree$SenderMessage")]
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

                public object[] GetAdaptedArray() =>
                [
                    Message, Origin, DateText, TimeText, IdleTime, TypingTime, Trigger, TriggerTime
                ];
            }
        }

        namespace WeakPipe
        {
            [AdaptableFormation("me.earzuchan.weakpipe.Outer")]
            public class Outer : ICtorAdaptable
            {
                public string Origin;
                public Inner Inner;

                [ICtorAdaptable.AdaptableConstructor]
                public Outer(string origin, Inner inner)
                {
                    Origin = origin;
                    Inner = inner;
                }

                public object[] GetAdaptedArray() => [Origin, Inner];
            }

            [AdaptableFormation("me.earzuchan.weakpipe.Inner")]
            public class Inner : ICtorAdaptable
            {
                public string Origin;

                [ICtorAdaptable.AdaptableConstructor]
                public Inner(string origin) => Origin = origin;

                public object[] GetAdaptedArray() => [Origin];
            }

            [AdaptableFormation("me.earzuchan.weakpipe.HugeOuter")]
            public class HugeOuter : ICtorAdaptable
            {
                public string Origin;
                public Inner Inner;
                public Inner[] Inners;

                [ICtorAdaptable.AdaptableConstructor]
                public HugeOuter(string origin, Inner inner, Inner[] inners)
                {
                    Origin = origin;
                    Inner = inner;
                    Inners = inners;
                }

                public object[] GetAdaptedArray() => [Origin, Inner, Inners];
            }
        }

        namespace SIM
        {
            [AdaptableFormation("game23.model.DialogueTreeModel")]
            public class SaraDialogueTree
            {
                public ConditionMacroModel[] ConditionMacros;
                public ConversationModel[] Conversations;
                public string Initialization;
                public string Namespace;
            }

            [AdaptableFormation("game23.model.DialogueTreeModel$ConditionMacroModel")]
            public class ConditionMacroModel
            {
                public string Condition;
                public string Name;
            }

            [AdaptableFormation("game23.model.DialogueTreeModel$ConversationModel")]
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

            [AdaptableFormation("game23.model.DialogueTreeModel$SenderMessageModel")]
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

            [AdaptableFormation("game23.model.DialogueTreeModel$UserMessageModel")]
            public class UserMessageModel
            {
                public bool IsHidden;
                public string Message;
            }
        }
    }
}