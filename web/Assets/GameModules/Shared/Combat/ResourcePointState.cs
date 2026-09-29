using System.Collections.Generic;
using Newtonsoft.Json;

namespace Dwsg.Window3
{
    public enum 资源点类型 { 铜矿 = 0, 牧场 = 1 }

    public sealed class 资源点配置
    {
        public string 标识;
        public 资源点类型 类型;
        public int 坐标x, 坐标y;
    }

    // 独立快照，不复制玩家、城池或军情。十秒节奏余秒与 /3600 产出余数都保存。
    public sealed class 资源点状态
    {
        [JsonProperty(Required = Required.Always)] public string 标识;
        [JsonProperty(Required = Required.Always)] public 资源点类型 类型;
        [JsonProperty(Required = Required.Always)] public int 坐标x;
        [JsonProperty(Required = Required.Always)] public int 坐标y;
        [JsonProperty(Required = Required.Always)] public int 时产;
        [JsonProperty(Required = Required.Always)] public int 剩余库存;
        [JsonProperty(Required = Required.Always)] public int 占领玩家ID = -1;
        [JsonProperty(Required = Required.Always)] public long 结算时间;
        [JsonProperty(Required = Required.Always)] public int 节奏余秒;
        [JsonProperty(Required = Required.Always)] public int 产出余数;
        [JsonProperty(Required = Required.Always)] public long 恢复时间;
        [JsonProperty(Required = Required.Always)] public int 批次;
        public 资源点状态 副本() { return (资源点状态)MemberwiseClone(); }
    }

    public sealed class 资源点存档
    {
        [JsonProperty(Required = Required.Always)] public int 版本 = 1;
        [JsonProperty(Required = Required.Always)] public int 配置版本 = 1;
        [JsonProperty(Required = Required.Always)] public string 世界标识;
        [JsonProperty(Required = Required.Always)] public bool 待生成;
        [JsonProperty(Required = Required.Always)] public List<资源点状态> 点位 = new List<资源点状态>();
    }

}
