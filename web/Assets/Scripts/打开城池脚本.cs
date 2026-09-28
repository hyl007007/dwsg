using UnityEngine;

public class 打开城池脚本 : MonoBehaviour
{
	public void 获取点击的城池序号()
	{
		int num = 0;
		num = base.transform.parent.GetSiblingIndex();
		UnityEngine.Debug.Log("城池" + num.ToString());
		base.transform.parent.parent.GetComponent<所有城池界面脚本>().打开城池信息(num);
	}
}
