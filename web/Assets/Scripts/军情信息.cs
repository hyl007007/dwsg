using System.Collections.Generic;
using 玩家数据结构;
using 缺失界面.窗口4;

public class 军情信息
{
    public 军事任务用途 任务用途;
    // 仅 AI推城的添加Ai攻城生产路径设置；普通城池随机驻军不属于行军存档。
    public bool 临时AI部队;

	public int 战场类型;

	public int 坐标x;

	public int 坐标y;

	public long 到达时间;

	public List<将领信息> 队列将领列表 = new List<将领信息>();

	public bool 已进入战场;

	public int 身份;
}
