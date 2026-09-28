using UnityEngine;

public class 主界面UI脚本 : MonoBehaviour
{
	public GameObject camera1;

	public GameObject camera2;

	public GameObject camera3;

	public GameObject 战斗界面UI对象;

	public GameObject 大地图布局对象;

	public GameObject 封地布局对象;

	public AudioSource 背景音乐对象;

	public AudioClip 封地背景音乐;

	public AudioClip 大地图背景音乐;

	private void Start()
	{
		全局变量.主相机 = camera1;
		全局变量.战斗地图相机 = camera2;
		全局变量.大地图相机 = camera3;
		全局变量.主界面UI对象 = base.gameObject;
		全局变量.战斗界面UI对象 = 战斗界面UI对象;
		全局变量.大地图布局对象 = 大地图布局对象;
		全局变量.封地布局对象 = 封地布局对象;
	}

	public void 加载大地图场景()
	{
		全局变量.大地图布局对象.transform.GetChild(0).GetChild(0).GetChild(0)
			.GetChild(0)
			.GetComponent<所有城池界面脚本>()
			.定位当前封地位置();
		全局变量.主相机.SetActive(value: false);
		全局变量.战斗地图相机.SetActive(value: false);
		全局变量.大地图相机.SetActive(value: true);
		全局变量.大地图布局对象.SetActive(value: true);
		全局变量.封地布局对象.SetActive(value: false);
		背景音乐对象.clip = 大地图背景音乐;
		背景音乐对象.Play();
	}

	public void 加载封地场景()
	{
		全局变量.主相机.SetActive(value: true);
		全局变量.战斗地图相机.SetActive(value: false);
		全局变量.大地图相机.SetActive(value: false);
		全局变量.大地图布局对象.SetActive(value: false);
		全局变量.封地布局对象.SetActive(value: true);
		背景音乐对象.clip = 封地背景音乐;
		背景音乐对象.Play();
	}

	public void 加载测试场景()
	{
	}

	public void 加载城池战斗场景()
	{
	}

	public void 加载山贼战斗场景()
	{
		全局变量.主相机.SetActive(value: false);
		全局变量.战斗地图相机.SetActive(value: true);
	}

	public void 测试初始化()
	{
	}

	

}
