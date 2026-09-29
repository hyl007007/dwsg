using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dwsg.Social
{
    public static class 社交界面入口
    {
        public static ISocialAdapter Adapter { get; private set; }
        internal static SocialPanel Panel;
        private static bool externallyConfigured;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
            SceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }
        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive) return;
            bool main = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponent<SocialPanel>() != null) return;
                if (root.GetComponent<主界面UI脚本>() != null) main = true;
            }
            if (!main) { Panel = null; return; }
            var host = new GameObject("窗口5社交控制器");
            SceneManager.MoveGameObjectToScene(host, scene);
            Panel = host.AddComponent<SocialPanel>();
            Panel.StartCoroutine(Initialize(Panel));
        }
        private static IEnumerator Initialize(SocialPanel panel)
        {
            // 主场景全局任务和导航管理器先完成初始化，不修改它们的脚本。
            yield return null;
            for (int frame = 0; frame < 120; frame++)
            {
                int index = 全局变量.本机身份;
                if (全局变量.主界面UI对象 != null && 全局变量.所有玩家数据表 != null && index >= 0 && index < 全局变量.所有玩家数据表.Count)
                {
                    if (!externallyConfigured)
                    {
                        var info = 全局变量.所有玩家数据表[index].基础信息;
                        // 仅读取本机身份，绝不遍历世界玩家表把 NPC 添加为联系人。
                        Adapter = new LocalSocialAdapter(new SocialPlayerDto
                        {
                            Id = "local-" + info.ID, Name = info.名字,
                            Level = Mathf.Clamp(Mathf.FloorToInt(info.等级), 1, 999),
                            Country = string.IsNullOrEmpty(info.国家) ? "无" : info.国家
                        }, Guid.NewGuid().ToString("N"));
                    }
                    externallyConfigured = false;
                    panel.Initialize(Adapter);
                    yield break;
                }
                yield return null;
            }
            Debug.LogWarning("社交界面未初始化：主场景本机身份尚未准备好");
        }
        public static bool 打开(string page)
        {
            if (Panel == null || !Panel.Ready) return false;
            var local = Adapter as LocalSocialAdapter;
            int index = 全局变量.本机身份;
            if (local != null && 全局变量.所有玩家数据表 != null && index >= 0 && index < 全局变量.所有玩家数据表.Count)
            {
                var info = 全局变量.所有玩家数据表[index].基础信息;
                if (local.CurrentPlayerId == "local-" + info.ID)
                    local.UpdateLocalPlayer(new SocialPlayerDto
                    {
                        Id = local.CurrentPlayerId, Name = info.名字, Level = Mathf.Clamp(Mathf.FloorToInt(info.等级), 1, 999),
                        Country = string.IsNullOrEmpty(info.国家) ? "无" : info.国家
                    });
            }
            Panel.Open(page); return true;
        }
        // 主窗口存档接线：将 ExportJson 放入世界存档的可选 social 字段，随同槽位原子写入。
        // 读档后使用世界自己的稳定 worldKey 和当前角色资料调用 RestoreForWorld；旧档 json 为空。
        // 新游戏/换槽位同样调用此方法，避免不同世界/角色之间泄漏关系。此模块不写独立文件。
        public static string ExportJson() { return Adapter == null ? null : Adapter.ExportJson(); }
        public static SocialResult ImportJson(string json)
        { return Adapter == null ? SocialResult.Fail("identity", "请先绑定当前世界和角色") : Adapter.ImportJson(json); }
        public static SocialResult RestoreForWorld(string worldKey, SocialPlayerDto self, string json)
        {
            if (string.IsNullOrEmpty(worldKey) || worldKey.Length > 128 || self == null || !LocalSocialAdapter.ValidId(self.Id) || !LocalSocialAdapter.ValidName(self.Name, 20))
                return SocialResult.Fail("invalid", "社交世界或角色身份无效");
            var adapter = new LocalSocialAdapter(self, worldKey);
            var result = string.IsNullOrEmpty(json) ? SocialResult.Local("已初始化空社交记录") : adapter.ImportJson(json);
            if (!result.Succeeded) return result;
            var profileResult = adapter.UpdateLocalPlayer(self);
            if (!profileResult.Succeeded) return profileResult;
            SetAdapter(adapter); return result;
        }
        // M02 使用认证会话实现 ISocialAdapter 后在主线程调用，界面随适配器重绑。
        public static void SetAdapter(ISocialAdapter adapter)
        {
            if (adapter == null) throw new ArgumentNullException("adapter");
            Adapter = adapter;
            // 允许存档加载器在切场景之前配置；配置明确携带到下一次主场景初始化。
            externallyConfigured = true;
            if (Panel != null && Panel.Ready) Panel.Initialize(adapter);
        }
        public static void Reset()
        { if (Adapter != null) Adapter.Reset(); }
        internal static void Detached(SocialPanel panel)
        {
            if (Panel != panel) return;
            Panel = null;
            // 场景切换后的本地关系由世界存档 RestoreForWorld 接回，不能误复用上一局。
            if (!externallyConfigured) Adapter = null;
        }
    }
}
