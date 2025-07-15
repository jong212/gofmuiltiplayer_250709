using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;
//https://chatgpt.com/share/671545ed-6cd0-800b-ae52-d92b932c3177
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
    // ���� �󺧰��� �ش��ϴ� ��巹�����µ��� "�ε�" �� "ĳ��"
    // ��ųʸ��� Ű = ���̸� , �� = ������ �̸� ���� ���� �ؼ� ������Ʈ �߰���
    public void LoadPrefabsWithLabel(string label, System.Action onLoaded)
    {
        Addressables.LoadAssetsAsync<GameObject>(label, null).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                if (!prefabCache.ContainsKey(label))
                {
                    prefabCache[label] = new List<GameObject>();
                }

                foreach (var prefab in handle.Result) // �� handle ������ �� ����� �ҵ� ����� �غ��� ���� �޸� �ε� �� �ν��Ͻ� �� ������Ʈ�� �迭�ε� ������ ���� Ȯ���� swoard1...2...3 �׷��� �� ���� �Ʒ����� ĳ�� �ϴµ�
                {
                    prefabCache[label].Add(prefab);
                    Debug.Log(" [��巹���� �ε� �� ĳ�� �Ϸ�] :" + prefab.name);
                }
                onLoaded?.Invoke();
            }
            else
            {
                Debug.LogError($"Failed to load prefabs with label '{label}' from Addressables: {handle.OperationException}");
            }
        };
    }
    public IEnumerator LoadPrefabsWithLabels(string label)
    {
        Debug.Log("AddressableManager => �ڷ�ƾ => [���� ���� ���� ���� 3]");

        var handle = Addressables.LoadAssetsAsync<GameObject>(label, null);
        yield return handle;
        Debug.Log("AddressableManager => �ڷ�ƾ => [���� ���� ���� ���� 5]");

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            if (!prefabCache.ContainsKey(label))
            {
                prefabCache[label] = new List<GameObject>();
            }

            foreach (var prefab in handle.Result)
            {
                prefabCache[label].Add(prefab);
                Debug.Log(" AddressableManager => �ڷ�ƾ => [��巹���� �ε� �� ĳ�� �Ϸ�] :" + prefab.name);
            }
        }
        else
        {
            Debug.LogError($"Failed to load prefabs with label '{label}' from Addressables: {handle.OperationException}");
        }
    }

    // (�� �� �Ű� ������ �ް�) ������Ʈ�� ����Ʈ ���·� ��ȯ ��
    public List<GameObject> GetPrefabsByLabel(string label)
    {
        if (prefabCache.TryGetValue(label, out List<GameObject> prefabs))
        {
            return prefabs;
        }
        Debug.LogError($"No prefabs found with label: {label}");
        return new List<GameObject>();
    }

    //(�󺧰�,�������̸� �Ű� ������ �ް�) ĳ�� �� ���� ������Ʈ�� ������ �̸����� ã�Ƽ� ��ȯ 

    public GameObject GetPrefab(string label, string prefabName)
    {
        if (prefabCache.TryGetValue(label, out List<GameObject> prefabs))
        {
            foreach (var prefab in prefabs)
            {
                if (prefab.name == prefabName)
                {
                    return prefab;
                }
            }
            Debug.LogError($"Prefab '{prefabName}' not found under label '{label}'.");
            return null;
        }
        Debug.LogError($"No prefabs found with label: {label}");
        return null;
    }
    // Release a Prefab (optional for memory management)
    public void ReleasePrefabsByLabel(string label)
    {
        if (prefabCache.TryGetValue(label, out List<GameObject> prefabs))
        {
            foreach (var prefab in prefabs)
            {
                Addressables.Release(prefab);
            }
            prefabCache.Remove(label);
            Debug.Log($"All prefabs with label '{label}' released.");
        }
        else
        {
            Debug.LogWarning($"No prefabs found with label: {label} to release.");
        }
    }
    public void LoadSpritesWithLabel(string label, Action onLoaded)
    {
        Addressables.LoadAssetsAsync<SpriteAtlas>(label, null).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                foreach (var sprite in handle.Result)
                {
                    if (!spriteCache.ContainsKey(sprite.name))
                    {
                        spriteCache[sprite.name] = sprite;
                        Debug.Log($"[��������Ʈ �ε� �� ĳ�� �Ϸ�] : {sprite.name}");
                    }
                }
                onLoaded?.Invoke();
            }
            else
            {
                Debug.LogError($"Failed to load sprites with label '{label}': {handle.OperationException}");
            }
        };
    }
    public Sprite GetSprite(string spriteName)
    {
        if (spriteCache.TryGetValue("New Sprite Atlas", out SpriteAtlas sprite))
        {
            return sprite.GetSprite(spriteName);
        }
        Debug.LogError($"Sprite '{spriteName}' not found in cache.");
        return null;
    }
    public void ReleaseSprites()
    {
        foreach (var sprite in spriteCache.Values)
        {
            Addressables.Release(sprite);
        }
        spriteCache.Clear();
        Debug.Log("All cached sprites released.");
    }
    /*
        SpriteManager.instance.LoadSpritesWithLabel("MyLabel", () =>
    {
        Debug.Log("��� ��������Ʈ �ε� �� ĳ�� �Ϸ�");
    });
    Sprite mySprite = SpriteManager.instance.GetSprite("SpriteName");
    if (mySprite != null)
    {
        // ��������Ʈ�� ����� �� �ֽ��ϴ�.
    }
    */
}