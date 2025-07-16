using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;

public class AddressableMng : MonoBehaviour
{
    public static AddressableMng instance;

    private Dictionary<string, List<GameObject>> prefabCache = new Dictionary<string, List<GameObject>>();
    private Dictionary<string, SpriteAtlas> spriteCache = new Dictionary<string, SpriteAtlas>();

    private void Awake()
    {
        if (instance && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    /// <summary>
    /// 라벨 리스트에 해당하는 프리팹들을 전부 미리 로드하여 캐싱함 (로딩 씬에서 1회 호출 권장)
    /// </summary>
    public IEnumerator InitializeAllPrefabs(List<string> labelsToPreload, Action onDone)
    {
        //yield return null;
        foreach (var label in labelsToPreload)
        {
            if (!prefabCache.ContainsKey(label))
            {
                yield return LoadPrefabsWithLabelCoroutine(label);
            }
        }

        onDone?.Invoke();
    }

    /// <summary>
    /// 코루틴 기반의 프리팹 로드 및 캐싱
    /// </summary>
    private IEnumerator LoadPrefabsWithLabelCoroutine(string label)
    {
        var handle = Addressables.LoadAssetsAsync<GameObject>(label, null);
        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            if (!prefabCache.ContainsKey(label))
                prefabCache[label] = new List<GameObject>();

            foreach (var prefab in handle.Result)
            {
                if (!prefabCache[label].Contains(prefab))
                {
                    prefabCache[label].Add(prefab);
                    Debug.Log($"[어드레서블 캐싱 완료] {label} - {prefab.name}");
                }
            }
        }
        else
        {
            Debug.LogError($"[어드레서블 로드 실패] {label}: {handle.OperationException}");
        }
    }

    /// <summary>
    /// 라벨에 해당하는 캐싱된 모든 프리팹 리스트 반환
    /// </summary>
    public List<GameObject> GetPrefabsByLabel(string label)
    {
        if (prefabCache.TryGetValue(label, out var prefabs))
        {
            return prefabs;
        }

        Debug.LogError($"[프리팹 없음] 라벨: {label}");
        return new List<GameObject>();
    }

    /// <summary>
    /// 라벨과 이름으로 특정 프리팹 반환
    /// </summary>
    public GameObject GetPrefab(string label, string prefabName)
    {
        if (prefabCache.TryGetValue(label, out var prefabs))
        {
            foreach (var prefab in prefabs)
            {
                if (prefab.name == prefabName)
                    return prefab;
            }

            Debug.LogError($"[프리팹 이름 없음] '{prefabName}' under label '{label}'");
            return null;
        }

        Debug.LogError($"[프리팹 없음] 라벨: {label}");
        return null;
    }

    /// <summary>
    /// 특정 라벨의 프리팹 캐시 해제
    /// </summary>
    public void ReleasePrefabsByLabel(string label)
    {
        if (prefabCache.TryGetValue(label, out var prefabs))
        {
            foreach (var prefab in prefabs)
            {
                Addressables.Release(prefab);
            }
            prefabCache.Remove(label);
            Debug.Log($"[프리팹 해제 완료] 라벨: {label}");
        }
    }

    /// <summary>
    /// 스프라이트 아틀라스 로드 및 캐싱
    /// </summary>
    public void LoadSpritesWithLabel(string label, Action onLoaded)
    {
        Addressables.LoadAssetsAsync<SpriteAtlas>(label, null).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                foreach (var atlas in handle.Result)
                {
                    if (!spriteCache.ContainsKey(atlas.name))
                    {
                        spriteCache[atlas.name] = atlas;
                        Debug.Log($"[스프라이트 캐싱 완료] : {atlas.name}");
                    }
                }
                onLoaded?.Invoke();
            }
            else
            {
                Debug.LogError($"[스프라이트 로드 실패] 라벨 '{label}': {handle.OperationException}");
            }
        };
    }

    /// <summary>
    /// 스프라이트 이름으로 가져오기 (캐싱된 SpriteAtlas 기준)
    /// </summary>
    public Sprite GetSprite(string spriteName)
    {
        foreach (var atlas in spriteCache.Values)
        {
            Sprite sprite = atlas.GetSprite(spriteName);
            if (sprite != null)
                return sprite;
        }

        Debug.LogError($"[스프라이트 없음] 이름: {spriteName}");
        return null;
    }

    /// <summary>
    /// 모든 스프라이트 아틀라스 해제
    /// </summary>
    public void ReleaseAllSprites()
    {
        foreach (var atlas in spriteCache.Values)
        {
            Addressables.Release(atlas);
        }
        spriteCache.Clear();
        Debug.Log("[스프라이트 해제 완료]");
    }
}
