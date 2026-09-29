using System.Collections;
using UnityEngine;

namespace Dwsg.Network
{
    public sealed class GameNetworkRunner : MonoBehaviour
    {
        private static GameNetworkRunner instance;
        internal static void StartWork(IEnumerator routine)
        {
            if (instance == null)
            {
                var runner = new GameObject("Game network") { hideFlags = HideFlags.HideInHierarchy };
                Object.DontDestroyOnLoad(runner);
                instance = runner.AddComponent<GameNetworkRunner>();
            }
            instance.StartCoroutine(routine);
        }
    }
}
