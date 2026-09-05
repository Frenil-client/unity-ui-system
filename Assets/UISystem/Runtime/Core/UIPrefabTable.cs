using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace UISystem
{
    /// <summary>
    /// 타입과 프리팹을 직접 참조로 묶는 조회 표. 경로 문자열도, 리플렉션 조회도 쓰지 않는다.
    /// </summary>
    [CreateAssetMenu(fileName = "UIPrefabTable", menuName = "UI System/UI Prefab Table")]
    public sealed class UIPrefabTable : ScriptableObject, IUIPrefabProvider
    {
        [Serializable]
        private struct Entry
        {
            public string TypeName;
            public GameObject Prefab;
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        private Dictionary<string, GameObject> _lookup;

        public Awaitable<GameObject> LoadPrefabAsync(Type viewType, CancellationToken cancellationToken = default)
        {
            var source = new AwaitableCompletionSource<GameObject>();

            if (TryGetPrefab(viewType, out var prefab))
                source.SetResult(prefab);
            else
                source.SetException(new KeyNotFoundException($"[UISystem] '{viewType.Name}' 프리팹이 표에 등록되어 있지 않다."));

            return source.Awaitable;
        }

        // 직접 참조라 놓을 핸들이 없다.
        public void ReleaseAll()
        {
        }

        public bool TryGetPrefab(Type viewType, out GameObject prefab)
        {
            BuildLookup();

            var key = viewType.FullName ?? viewType.Name;
            return _lookup.TryGetValue(key, out prefab) && prefab != null;
        }

        private void BuildLookup()
        {
            if (_lookup != null)
                return;

            _lookup = new Dictionary<string, GameObject>(_entries.Length);
            foreach (var entry in _entries)
            {
                if (string.IsNullOrEmpty(entry.TypeName) || entry.Prefab == null)
                    continue;

                _lookup[entry.TypeName] = entry.Prefab;
            }
        }

        private void OnDisable() => _lookup = null;
    }
}
