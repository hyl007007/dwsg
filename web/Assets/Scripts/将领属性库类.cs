using System;
using UnityEngine;

[Serializable]
public class 将领属性库类
{
	public double ID;

	public string 名字;

	public double 头像特效;

	public string 国家 = "";

	public int 职业;

	public double 类型;

	public string 系列;

	public double 成长;

	public double 突围;

	public double 武力;

	public double 智力;

	public double 统帅;

	public void 转换类型职业(string t类型名, string t职业名)
	{
		if (t类型名 == "智")
		{
			类型 = 3.0;
		}
		else if (t类型名 == "攻")
		{
			类型 = 2.0;
		}
		else if (t类型名 == "统")
		{
			类型 = 4.0;
		}
		else if (t类型名 == "平")
		{
			类型 = 1.0;
		}
		if (t职业名 == "骑")
		{
			职业 = 1;
		}
		else if (t职业名 == "步")
		{
			职业 = 2;
		}
		else if (t职业名 == "勇")
		{
			职业 = 3;
		}
		else if (t职业名 == "弓")
		{
			职业 = 4;
		}
	}

	public void 转换特效类型(string 特效颜色)
	{
		if (特效颜色 == "绿色")
		{
			头像特效 = 2.0;
		}
		else if (特效颜色 == "彩色")
		{
			头像特效 = 1.0;
		}
		else if (特效颜色 == "紫色")
		{
			头像特效 = 3.0;
		}
		else if (特效颜色 == "黄色")
		{
			头像特效 = 5.0;
		}
		else if (特效颜色 == "金色")
		{
			头像特效 = 5.0;
		}
		else if (特效颜色 == "红色")
		{
			头像特效 = 6.0;
		}
	}

	public void 快捷生成普通将领(double tID, string t名字)
	{
		ID = tID;
		名字 = t名字;
		头像特效 = 0.0;
		国家 = "";
		系列 = "普通";
		获取随机属性();
	}

	public void 快捷生成良好将领(double tID, string t名字)
	{
		ID = tID;
		名字 = t名字;
		头像特效 = 0.0;
		国家 = "";
		系列 = "良好";
		获取随机属性();
	}

	public void 快捷生成优秀将领(double tID, string t名字)
	{
		ID = tID;
		名字 = t名字;
		头像特效 = 0.0;
		国家 = "";
		系列 = "优秀";
		获取随机属性();
	}

	public void 快捷生成卓越将领(double tID, string t名字)
	{
		ID = tID;
		名字 = t名字;
		头像特效 = 0.0;
		国家 = "";
		系列 = "卓越";
		获取随机属性();
	}

	public void 快捷生成尊将(double tID, string t名字, string 特效颜色, string t类型名, string t职业名)
	{
		ID = tID;
		名字 = t名字;
		头像特效 = 0.0;
		国家 = "";
		突围 = 99.0;
		成长 = 92.0;
		转换类型职业(t类型名, t职业名);
		转换特效类型(特效颜色);
		系列 = "尊将";
		获取随机属性();
	}

	public void 快捷生成战将(double tID, string t名字, string t类型名, string t职业名)
	{
		ID = tID;
		名字 = t名字;
		国家 = "";
		突围 = 99.0;
		成长 = 92.0;
		转换类型职业(t类型名, t职业名);
		转换特效类型("紫色");
		系列 = "战将";
		获取随机属性();
	}

	public void 快捷生成禧将(double tID, string t名字, string t类型名, string t职业名)
	{
		ID = tID;
		名字 = t名字;
		国家 = "";
		成长 = 92.0;
		突围 = 99.0;
		转换类型职业(t类型名, t职业名);
		转换特效类型("红色");
		系列 = "禧将";
		获取随机属性();
	}

	public void 快捷生成君王(double tID, string t名字, string t国家, string t类型名, string t职业名)
	{
		ID = tID;
		名字 = t名字;
		国家 = t国家;
		突围 = 99.0;
		成长 = 92.0;
		转换类型职业(t类型名, t职业名);
		系列 = "君王";
		获取随机属性();
	}

	public void 快捷生成名将(double tID, string t名字, string t国家, string t类型名, string t职业名, double t突围)
	{
		ID = tID;
		名字 = t名字;
		国家 = t国家;
		突围 = t突围;
		转换类型职业(t类型名, t职业名);
		系列 = "名将";
		获取随机属性();
	}

	public void 获取随机属性()
	{
		if (Dwsg.Shared.Generals.GeneralCreationRules.IsOrdinarySeries(系列))
		{
			Newtonsoft.Json.Linq.JObject randomized = Dwsg.Shared.Generals.GeneralCreationRules.RandomizeOrdinary(Newtonsoft.Json.Linq.JObject.FromObject(this), UnityEngine.Random.Range);
			Newtonsoft.Json.JsonConvert.PopulateObject(randomized.ToString(Newtonsoft.Json.Formatting.None), this);
			return;
		}
		if (系列 == "战将")
		{
			if (类型 == 1.0)
			{
				统帅 = (智力 = (武力 = UnityEngine.Random.Range(120, 128)));
			}
			else if (类型 == 2.0)
			{
				武力 = UnityEngine.Random.Range(120, 128);
				智力 = UnityEngine.Random.Range(110, 121);
				统帅 = UnityEngine.Random.Range(110, 121);
			}
			else if (类型 == 3.0)
			{
				武力 = UnityEngine.Random.Range(110, 121);
				智力 = UnityEngine.Random.Range(120, 128);
				统帅 = UnityEngine.Random.Range(110, 121);
			}
			else if (类型 == 4.0)
			{
				武力 = UnityEngine.Random.Range(110, 121);
				智力 = UnityEngine.Random.Range(110, 121);
				统帅 = UnityEngine.Random.Range(120, 128);
			}
		}
		else if (系列 == "禧将")
		{
			if (类型 == 1.0)
			{
				统帅 = (智力 = (武力 = UnityEngine.Random.Range(110, 125)));
			}
			else if (类型 == 2.0)
			{
				武力 = UnityEngine.Random.Range(110, 125);
				智力 = UnityEngine.Random.Range(98, 110);
				统帅 = UnityEngine.Random.Range(90, 110);
			}
			else if (类型 == 3.0)
			{
				武力 = UnityEngine.Random.Range(98, 110);
				智力 = UnityEngine.Random.Range(110, 125);
				统帅 = UnityEngine.Random.Range(98, 110);
			}
			else if (类型 == 4.0)
			{
				武力 = UnityEngine.Random.Range(98, 110);
				智力 = UnityEngine.Random.Range(98, 110);
				统帅 = UnityEngine.Random.Range(110, 125);
			}
		}
		else if (系列 == "尊将")
		{
			if (类型 == 1.0)
			{
				统帅 = (智力 = (武力 = UnityEngine.Random.Range(118, 128)));
			}
			else if (类型 == 2.0)
			{
				武力 = UnityEngine.Random.Range(118, 128);
				智力 = UnityEngine.Random.Range(112, 119);
				统帅 = UnityEngine.Random.Range(112, 119);
			}
			else if (类型 == 3.0)
			{
				武力 = UnityEngine.Random.Range(112, 119);
				智力 = UnityEngine.Random.Range(118, 128);
				统帅 = UnityEngine.Random.Range(112, 119);
			}
			else if (类型 == 4.0)
			{
				武力 = UnityEngine.Random.Range(112, 119);
				智力 = UnityEngine.Random.Range(112, 119);
				统帅 = UnityEngine.Random.Range(118, 128);
			}
		}
		else if (系列 == "君王")
		{
			if (类型 == 1.0)
			{
				统帅 = (智力 = (武力 = UnityEngine.Random.Range(110, 121)));
			}
			else if (类型 == 2.0)
			{
				武力 = UnityEngine.Random.Range(110, 121);
				智力 = UnityEngine.Random.Range(100, 110);
				统帅 = UnityEngine.Random.Range(100, 110);
			}
			else if (类型 == 3.0)
			{
				武力 = UnityEngine.Random.Range(100, 110);
				智力 = UnityEngine.Random.Range(115, 129);
				统帅 = UnityEngine.Random.Range(100, 110);
			}
			else if (类型 == 4.0)
			{
				武力 = UnityEngine.Random.Range(100, 110);
				智力 = UnityEngine.Random.Range(100, 110);
				统帅 = UnityEngine.Random.Range(110, 121);
			}
		}
		else if (系列 == "名将")
		{
			if (突围 == 99.0)
			{
				成长 = 90.0;
				if (类型 == 1.0)
				{
					统帅 = (智力 = (武力 = UnityEngine.Random.Range(100, 111)));
				}
				else if (类型 == 2.0)
				{
					武力 = UnityEngine.Random.Range(101, 111);
					智力 = UnityEngine.Random.Range(85, 101);
					统帅 = UnityEngine.Random.Range(85, 101);
				}
				else if (类型 == 3.0)
				{
					武力 = UnityEngine.Random.Range(85, 101);
					智力 = UnityEngine.Random.Range(100, 111);
					统帅 = UnityEngine.Random.Range(85, 101);
				}
				else if (类型 == 4.0)
				{
					武力 = UnityEngine.Random.Range(85, 101);
					智力 = UnityEngine.Random.Range(85, 101);
					统帅 = UnityEngine.Random.Range(100, 111);
				}
			}
			else if (突围 == 96.0)
			{
				成长 = 86.0;
				if (类型 == 1.0)
				{
					统帅 = (智力 = (武力 = UnityEngine.Random.Range(96, 106)));
				}
				else if (类型 == 2.0)
				{
					武力 = UnityEngine.Random.Range(96, 106);
					智力 = UnityEngine.Random.Range(80, 96);
					统帅 = UnityEngine.Random.Range(80, 96);
				}
				else if (类型 == 3.0)
				{
					武力 = UnityEngine.Random.Range(80, 96);
					智力 = UnityEngine.Random.Range(96, 106);
					统帅 = UnityEngine.Random.Range(80, 96);
				}
				else if (类型 == 4.0)
				{
					武力 = UnityEngine.Random.Range(80, 96);
					智力 = UnityEngine.Random.Range(80, 96);
					统帅 = UnityEngine.Random.Range(96, 106);
				}
			}
			else if (突围 == 94.0)
			{
				成长 = 82.0;
				if (类型 == 1.0)
				{
					统帅 = (智力 = (武力 = UnityEngine.Random.Range(90, 96)));
				}
				else if (类型 == 2.0)
				{
					武力 = UnityEngine.Random.Range(90, 96);
					智力 = UnityEngine.Random.Range(80, 86);
					统帅 = UnityEngine.Random.Range(80, 86);
				}
				else if (类型 == 3.0)
				{
					武力 = UnityEngine.Random.Range(80, 86);
					智力 = UnityEngine.Random.Range(90, 96);
					统帅 = UnityEngine.Random.Range(80, 86);
				}
				else if (类型 == 4.0)
				{
					武力 = UnityEngine.Random.Range(80, 86);
					智力 = UnityEngine.Random.Range(80, 86);
					统帅 = UnityEngine.Random.Range(90, 96);
				}
			}
			else if (突围 == 92.0)
			{
				成长 = 80.0;
				if (类型 == 1.0)
				{
					统帅 = (智力 = (武力 = UnityEngine.Random.Range(85, 93)));
				}
				else if (类型 == 2.0)
				{
					武力 = UnityEngine.Random.Range(85, 94);
					智力 = UnityEngine.Random.Range(80, 83);
					统帅 = UnityEngine.Random.Range(80, 83);
				}
				else if (类型 == 3.0)
				{
					武力 = UnityEngine.Random.Range(90, 101);
					智力 = UnityEngine.Random.Range(80, 83);
					统帅 = UnityEngine.Random.Range(80, 83);
				}
				else if (类型 == 4.0)
				{
					武力 = UnityEngine.Random.Range(80, 83);
					智力 = UnityEngine.Random.Range(80, 83);
					统帅 = UnityEngine.Random.Range(85, 93);
				}
			}
			else if (突围 == 90.0)
			{
				成长 = 80.0;
				if (类型 == 1.0)
				{
					统帅 = (智力 = (武力 = UnityEngine.Random.Range(85, 91)));
				}
				else if (类型 == 2.0)
				{
					武力 = UnityEngine.Random.Range(85, 91);
					智力 = UnityEngine.Random.Range(78, 81);
					统帅 = UnityEngine.Random.Range(78, 81);
				}
				else if (类型 == 3.0)
				{
					武力 = UnityEngine.Random.Range(78, 81);
					智力 = UnityEngine.Random.Range(85, 91);
					统帅 = UnityEngine.Random.Range(78, 81);
				}
				else if (类型 == 4.0)
				{
					武力 = UnityEngine.Random.Range(78, 81);
					智力 = UnityEngine.Random.Range(78, 81);
					统帅 = UnityEngine.Random.Range(85, 91);
				}
			}
		}
		else
		{
			成长 = UnityEngine.Random.Range(60, 81);
			突围 = UnityEngine.Random.Range(70, 91);
			武力 = UnityEngine.Random.Range(70, 100);
			智力 = UnityEngine.Random.Range(70, 100);
			统帅 = UnityEngine.Random.Range(70, 100);
		}
	}
}
