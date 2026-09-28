using UnityEngine;
using UnityEngine.UI;

public class 显示状态信息 : MonoBehaviour
{
	public GameObject 状态列表对象;

	public GameObject 选择使用道具界面对象;

	private long 刷新计时 = TIME.getTime();

	public void 刷新显示()
	{
		if (TIME.getTime() - 刷新计时 < 1)
		{
			return;
		}
		int 本机身份 = 全局变量.本机身份;
		int count = 全局变量.所有玩家数据表[本机身份].道具状态表.Count;
		for (int i = 0; i < count; i++)
		{
			if (状态列表对象.transform.childCount <= i)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(状态列表对象.transform.GetChild(0).gameObject);
				gameObject.transform.SetParent(状态列表对象.transform);
				gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
			}
			else
			{
				GameObject gameObject2 = 状态列表对象.transform.GetChild(i).gameObject;
			}
			状态列表对象.transform.GetChild(i).GetChild(1).GetComponent<Text>()
				.text = (i + 1).ToString() + "、" + 全局变量.所有玩家数据表[本机身份].道具状态表[i].获取状态显示文本();
				if (全局变量.所有玩家数据表[本机身份].道具状态表[i].获取状态剩余时间() > 0)
				{
					状态列表对象.transform.GetChild(i).GetChild(0).GetComponent<RectTransform>()
						.sizeDelta = new Vector2(455f, 47f);
						状态列表对象.transform.GetChild(i).GetChild(2).gameObject.SetActive(value: false);
						状态列表对象.transform.GetChild(i).GetChild(3).gameObject.SetActive(value: true);
						状态列表对象.transform.GetChild(i).GetChild(4).gameObject.SetActive(value: true);
					}
					else
					{
						状态列表对象.transform.GetChild(i).GetChild(0).GetComponent<RectTransform>()
							.sizeDelta = new Vector2(540f, 47f);
							状态列表对象.transform.GetChild(i).GetChild(2).gameObject.SetActive(value: true);
							状态列表对象.transform.GetChild(i).GetChild(3).gameObject.SetActive(value: false);
							状态列表对象.transform.GetChild(i).GetChild(4).gameObject.SetActive(value: false);
						}
					}
					刷新计时 = TIME.getTime();
				}

				public int 获取选中状态()
				{
					int childCount = 状态列表对象.transform.childCount;
					for (int i = 0; i < childCount; i++)
					{
						if (状态列表对象.transform.GetChild(i).GetChild(0).GetChild(1)
							.gameObject.activeSelf)
							{
								return i;
							}
						}
						return -1;
					}

					public void 开启状态()
					{
						int index = 获取选中状态();
						int 本机身份 = 全局变量.本机身份;
						选择使用道具界面对象.SetActive(value: true);
						使用道具脚本 component = 选择使用道具界面对象.transform.GetComponent<使用道具脚本>();
						component.要显示的列表 = 全局道具库.获取指定类型的道具列表("道具状态_" + 全局变量.所有玩家数据表[本机身份].道具状态表[index].名字);
						component.刷新显示();
					}

					public void 延期状态()
					{
						int index = 获取选中状态();
						int 本机身份 = 全局变量.本机身份;
						选择使用道具界面对象.SetActive(value: true);
						使用道具脚本 component = 选择使用道具界面对象.transform.GetComponent<使用道具脚本>();
						component.要显示的列表 = 全局道具库.获取指定类型的道具列表("道具状态_" + 全局变量.所有玩家数据表[本机身份].道具状态表[index].名字);
						component.刷新显示();
					}

					public void 取消状态()
					{
						int index = 获取选中状态();
						int 本机身份 = 全局变量.本机身份;
						全局变量.所有玩家数据表[本机身份].道具状态表[index].加成 = 全局变量.所有玩家数据表[本机身份].道具状态表[index].初始加成;
						全局变量.所有玩家数据表[本机身份].道具状态表[index].到期时间 = 0L;
						刷新计时 = TIME.getTime() - 2;
					}

					public void 选中高亮()
					{
						int childCount = 状态列表对象.transform.childCount;
						for (int i = 0; i < childCount; i++)
						{
							if (状态列表对象.transform.GetChild(i).GetChild(0).GetChild(1)
								.gameObject.activeSelf)
								{
									状态列表对象.transform.GetChild(i).GetChild(1).GetComponent<Text>()
										.color = 颜色类.GetColor("#78FFC8");
									}
									else
									{
										状态列表对象.transform.GetChild(i).GetChild(1).GetComponent<Text>()
											.color = 颜色类.GetColor("#C8C8C8");
										}
									}
								}

								private void FixedUpdate()
								{
									刷新显示();
								}
							}
