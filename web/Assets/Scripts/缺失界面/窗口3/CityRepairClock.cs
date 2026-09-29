using System.Collections;
using UnityEngine;

namespace Dwsg.Window3
{
    // A pending local world task may outlive its UI. This never scans or refreshes any panel.
    public sealed class CityRepairClock : MonoBehaviour
    {
        private static CityRepairClock clock;
        private CityLocalAdapter adapter;
        private Coroutine work;

        internal static void Watch(CityLocalAdapter source)
        {
            if (!Application.isPlaying || !source.HasPendingWork) return;
            if (clock == null)
            {
                var root = new GameObject("窗口3_本地修筑计时");
                DontDestroyOnLoad(root); clock = root.AddComponent<CityRepairClock>();
            }
            clock.adapter = source;
            if (clock.work == null) clock.work = clock.StartCoroutine(clock.Run());
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RestoreClock() { Watch(CityLocalAdapter.Local); }

        private IEnumerator Run()
        {
            while (adapter != null && adapter.HasPendingWork)
            {
                yield return new WaitForSecondsRealtime(1);
                adapter.Settle();
            }
            work = null; if (clock == this) clock = null; Destroy(gameObject);
        }
        private void OnDestroy() { if (clock == this) clock = null; }
    }
}
