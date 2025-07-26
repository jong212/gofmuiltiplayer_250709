// AddressableMng.cs
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

    public Dictionary<string, List<GameObject>> prefabCache = new();
    private Dictionary<string, AsyncOperationHandle<IList<GameObject>>> prefabHandles = new();

    public Dictionary<string, Material> materialCache = new();
    private Dictionary<string, AsyncOperationHandle<IList<Material>>> materialHandles = new();

    public Dictionary<string, List<Sprite>> spriteCache = new();
    private Dictionary<string, AsyncOperationHandle<IList<Sprite>>> spriteHandles = new();

    private void Awake()
    {
        if (instance && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    #region # 리소스 캐싱
    public void Step3()
    {
        StartCoroutine(InitializeAllAssets(
           new List<string> { "player" },
           new List<string> { "ballskin" },
           new List<string> { "lobbyui" }
       ));
    }
    public IEnumerator InitializeAllAssets(List<string> prefabLabels, List<string> materialLabels, List<string> spriteLabels)
    {
        if (prefabLabels != null) yield return InitializeAllPrefabs(prefabLabels);
        if (materialLabels != null) yield return InitializeAllMaterials(materialLabels);
        if (spriteLabels != null) yield return InitializeAllSprites(spriteLabels);
        ManagerSystem.Instance.InitStepByCall("4_BackendInit");
    }

    public IEnumerator InitializeAllPrefabs(List<string> labels)
    {
        foreach (var label in labels)
        {
            if (!prefabCache.ContainsKey(label))
            {
                var handle = Addressables.LoadAssetsAsync<GameObject>(label, null);
                yield return handle;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    prefabHandles[label] = handle;
                    prefabCache[label] = new List<GameObject>(handle.Result);
                    Debug.Log($"[프리팹 로드 완료] {label}");
                }
                else
                {
                    Debug.LogError($"[프리팹 로드 실패] {label}: {handle.OperationException}");
                }
            }
        }
    }

    public IEnumerator InitializeAllMaterials(List<string> labels)
    {
        foreach (var label in labels)
        {
            if (!materialHandles.ContainsKey(label))
            {
                var handle = Addressables.LoadAssetsAsync<Material>(label, null);
                yield return handle;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    materialHandles[label] = handle;
                    foreach (var mat in handle.Result)
                        materialCache[mat.name] = mat;
                    Debug.Log($"[머티리얼 로드 완료] {label}");
                }
                else
                {
                    Debug.LogError($"[머티리얼 로드 실패] {label}: {handle.OperationException}");
                }
            }
        }
    }
    public IEnumerator InitializeAllSprites(List<string> labels)
    {
        foreach (var label in labels)
        {
            if (!spriteCache.ContainsKey(label))
            {
                var handle = Addressables.LoadAssetsAsync<Sprite>(label, null);
                yield return handle;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    spriteHandles[label] = handle;
                    spriteCache[label] = new List<Sprite>(handle.Result);
                    Debug.Log($"[스프라이트 로드 완료] {label}");
                }
                else
                {
                    Debug.LogError($"[스프라이트 로드 실패] {label}: {handle.OperationException}");
                }
            }
        }
    }
    #endregion

    #region # 리소스 반환
    public GameObject GetPrefab(string label, string prefabName)
    {
        if (prefabCache.TryGetValue(label, out var list))
        {
            return list.Find(p => p.name == prefabName);
        }
        return null;
    }

    public Sprite GetSprite(string label, string spriteName)
    {
        if (spriteCache.TryGetValue(label, out var list))
        {
            return list.Find(s => s.name == spriteName);
        }
        return null;
    }

    #endregion

}