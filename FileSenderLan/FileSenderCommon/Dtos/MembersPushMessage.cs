namespace FileSenderCommon.Dtos
{
    /// <summary>
    /// 服务端到客户端的推送在线列表
    /// </summary>

    public class MembersPushMessage : Message
    {
        public List<Member> Members { get; set; }
    }
}
