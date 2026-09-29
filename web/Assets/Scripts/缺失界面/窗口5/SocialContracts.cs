using System;
using System.Collections.Generic;

namespace Dwsg.Social
{
    public enum SocialCommandKind
    {
        RegisterContact, RequestFriend, AnswerFriend, CancelFriend, RemoveFriend, Block, Unblock,
        SaveDraft, SendPrivate, CreateGuild, ApplyGuild, AnswerGuild, CancelGuild, LeaveGuild,
        KickGuild, TransferGuild, UpdateGuild, DissolveGuild,
        InviteMentor, InviteApprentice, InviteBrother, AnswerRelation, CancelRelation,
        LeaveMentor, LeaveBrother, KickBrother, TransferBrother,
        RemoveContact, RenameContact
    }
    public enum RequestState { Pending, Accepted, Declined, Cancelled }
    public enum RelationKind { Mentor, Brotherhood }
    public enum MessageDelivery { Sent, Received, Failed }

    [Serializable] public sealed class SocialPlayerDto
    {
        public string Id;
        public string Name;
        public int Level = 1;
        public string Country = "未核验";
        // 手动联系人只用于本地关系记录，绝不能当作服务器身份或在线玩家。
        public bool Verified;
        public bool IsNpc;
        public int Portrait;
    }
    [Serializable] public sealed class FriendDto { public string A; public string B; }
    [Serializable] public sealed class BlockDto { public string Owner; public string Target; }
    [Serializable] public sealed class FriendRequestDto
    {
        public string Id; public string From; public string To; public string Note;
        public RequestState State; public long CreatedUtc;
    }
    [Serializable] public sealed class GuildDto
    {
        public string Id; public string Name; public string Notice; public string Leader;
        public List<string> Members = new List<string>();
    }
    [Serializable] public sealed class GuildApplicationDto
    {
        public string Id; public string Guild; public string Applicant; public string Note;
        public RequestState State; public long CreatedUtc;
    }
    [Serializable] public sealed class RelationInvitationDto
    {
        public string Id; public string From; public string To; public RelationKind Kind;
        public string Mentor; public string Apprentice; public string Group; public string Name;
        public RequestState State; public long CreatedUtc;
    }
    [Serializable] public sealed class MentorDto
    {
        public string Id; public string Mentor; public string Apprentice;
    }
    [Serializable] public sealed class BrotherhoodDto
    {
        public string Id; public string Name; public string Leader;
        public List<string> Members = new List<string>();
    }
    [Serializable] public sealed class PrivateDraftDto
    {
        public string Owner; public string Target; public string Text;
    }
    [Serializable] public sealed class PrivateMessageDto
    {
        public string Id; public string From; public string To; public string Text;
        public MessageDelivery Delivery; public long CreatedUtc;
    }
    // 随世界槽位保存联系人与关系；NPC 资料仅是选中角色的快照，不复制世界数据库。
    [Serializable] public sealed class SocialStateDto
    {
        public int Version = 1;
        public string WorldKey;
        public string OwnerId;
        public List<SocialPlayerDto> Players = new List<SocialPlayerDto>();
        public List<FriendDto> Friends = new List<FriendDto>();
        public List<BlockDto> Blocks = new List<BlockDto>();
        public List<FriendRequestDto> FriendRequests = new List<FriendRequestDto>();
        public List<GuildDto> Guilds = new List<GuildDto>();
        public List<GuildApplicationDto> GuildApplications = new List<GuildApplicationDto>();
        public List<RelationInvitationDto> Invitations = new List<RelationInvitationDto>();
        public List<MentorDto> Mentors = new List<MentorDto>();
        public List<BrotherhoodDto> Brotherhoods = new List<BrotherhoodDto>();
        public List<PrivateDraftDto> Drafts = new List<PrivateDraftDto>();
        public List<PrivateMessageDto> Messages = new List<PrivateMessageDto>();
    }
    // 身份来自适配器会话；命令没有可由 UI 指定的 Actor 字段。
    public sealed class SocialCommand
    {
        public SocialCommandKind Kind;
        public string Target;
        public string Entity;
        public string Name;
        public string Text;
        public bool Accept;
    }
    public sealed class SocialResult
    {
        public bool Succeeded;
        public string Code;
        public string Message;
        public string EntityId;
        public static SocialResult Fail(string code, string message)
        { return new SocialResult { Code = code, Message = message }; }
        public static SocialResult Local(string message, string id = null)
        { return new SocialResult { Succeeded = true, Code = "local", Message = message, EntityId = id }; }
    }
    // M02 实现此接口，以认证角色 ID 提供快照/命令结果，并在主线程触发 Changed。
    // 网络适配器不得信任本地 DTO 的 Verified 或关系结果；必须由服务器鉴权。
    public interface ISocialAdapter
    {
        string CurrentPlayerId { get; }
        bool IsConnected { get; }
        string ConnectionStatus { get; }
        event Action Changed;
        SocialStateDto Snapshot();
        SocialResult Execute(SocialCommand command);
        string ExportJson();
        SocialResult ImportJson(string json);
        void Reset();
    }
}
