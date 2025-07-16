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
    /// �� ����Ʈ�� �ش��ϴ� �����յ��� ���� �̸� �ε��Ͽ� ĳ���� (�ε� ������ 1ȸ ȣ�� ����)
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
    /// �ڷ�ƾ ����� ������ �ε� �� ĳ��
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
                    Debug.Log($"[��巹���� ĳ�� �Ϸ�] {label} - {prefab.name}");
                }
            }
        }
        else
        {
            Debug.LogError($"[��巹���� �ε� ����] {label}: {handle.OperationException}");
        }
    }

    /// <summary>
    /// �󺧿� �ش��ϴ� ĳ�̵� ��� ������ ����Ʈ ��ȯ
    /// </summary>
    public List<GameObject> GetPrefabsByLabel(string label)
    {
        if (prefabCache.TryGetValue(label, out var prefabs))
        {
            return prefabs;
        }

        Debug.LogError($"[������ ����] ��: {label}");
        return new List<GameObject>();
    }

    /// <summary>
    /// �󺧰� �̸����� Ư�� ������ ��ȯ
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

            Debug.LogError($"[������ �̸� ����] '{prefabName}' under label '{label}'");
            return null;
        }

        Debug.LogError($"[������ ����] ��: {label}");
        return null;
    }

    /// <summary>
    /// Ư�� ���� ������ ĳ�� ����
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
            Debug.Log($"[������ ���� �Ϸ�] ��: {label}");
        }
    }

    /// <summary>
    /// ��������Ʈ ��Ʋ�� �ε� �� ĳ��
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
                        Debug.Log($"[��������Ʈ ĳ�� �Ϸ�] : {atlas.name}");
                    }
                }
                onLoaded?.Invoke();
            }
            else
            {
                Debug.LogError($"[��������Ʈ �ε� ����] �� '{label}': {handle.OperationException}");
            }
        };
    }

    /// <summary>
    /// ��������Ʈ �̸����� �������� (ĳ�̵� SpriteAtlas ����)
    /// </summary>
    public Sprite GetSprite(string spriteName)
    {
        foreach (var atlas in spriteCache.Values)
        {
            Sprite sprite = atlas.GetSprite(spriteName);
            if (sprite != null)
                return sprite;
        }

        Debug.LogError($"[��������Ʈ ����] �̸�: {spriteName}");
        return null;
    }

    /// <summary>
    /// ��� ��������Ʈ ��Ʋ�� ����
    /// </summary>
    public void ReleaseAllSprites()
    {
        foreach (var atlas in spriteCache.Values)
        {
            Addressables.Release(atlas);
        }
        spriteCache.Clear();
        Debug.Log("[��������Ʈ ���� �Ϸ�]");
    }
}
