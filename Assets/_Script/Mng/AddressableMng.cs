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

    /*private Dictionary<string, SpriteAtlas> spriteCache = new();
    private Dictionary<string, AsyncOperationHandle<IList<SpriteAtlas>>> spriteHandles = new();*/

    private void Awake()
    {
        if (instance && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    public void Step3()
    {
        StartCoroutine(InitializeAllAssets(
           new List<string> { "player" },
           new List<string> { "ballskin" }
          
       ));
    }
    public IEnumerator InitializeAllAssets(List<string> prefabLabels, List<string> materialLabels)
    {
        if (prefabLabels != null) yield return InitializeAllPrefabs(prefabLabels);
        if (materialLabels != null) yield return InitializeAllMaterials(materialLabels);

        ManagerSystem.Instance.StepByCall("4_BackendInit");
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
/*
    public IEnumerator InitializeAllSpriteAtlases(List<string> labels)
    {
        foreach (var label in labels)
        {
            if (!spriteHandles.ContainsKey(label))
            {
                var handle = Addressables.LoadAssetsAsync<SpriteAtlas>(label, null);
                yield return handle;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    spriteHandles[label] = handle;
                    foreach (var atlas in handle.Result)
                        spriteCache[atlas.name] = atlas;
                    Debug.Log($"[스프라이트 아틀라스 로드 완료] {label}");
                }
                else
                {
                    Debug.LogError($"[스프라이트 아틀라스 로드 실패] {label}: {handle.OperationException}");
                }
            }
        }
    }
*/
    public GameObject GetPrefab(string label, string prefabName)
    {
        if (prefabCache.TryGetValue(label, out var list))
        {
            return list.Find(p => p.name == prefabName);
        }
        return null;
    }

    public Material GetMaterial(string name) => materialCache.TryGetValue(name, out var mat) ? mat : null;
    /*
        public Sprite GetSprite(string spriteName)
        {
            foreach (var atlas in spriteCache.Values)
            {
                var sprite = atlas.GetSprite(spriteName);
                if (sprite != null) return sprite;
            }
            return null;
        }*/

    public void ApplyMaterialTo(GameObject go, string materialName, bool instantiate = false)
    {
        if (!go)
        {
            Debug.LogError("[ApplyMaterialTo] GameObject가 null임");
            return;
        }

        if (!materialCache.TryGetValue(materialName, out var mat))
        {
            Debug.LogError($"[ApplyMaterialTo] '{materialName}' 머티리얼을 찾지 못함");
            return;
        }

        Debug.Log($"[ApplyMaterialTo] 머티리얼 '{materialName}' 적용 시도 중");

        var renderers = go.GetComponentsInChildren<MeshRenderer>(true);
        Debug.Log($"[ApplyMaterialTo] Renderer 개수: {renderers.Length}");

        foreach (var renderer in renderers)
        {
            Debug.Log($"[ApplyMaterialTo] 적용 대상: {renderer.name}");

            if (instantiate)
                renderer.material = mat;
            else
                renderer.sharedMaterial = mat;
        }
    }


    public void ReleaseEverything()
    {
        foreach (var h in prefabHandles.Values) Addressables.Release(h);
        prefabHandles.Clear(); prefabCache.Clear();

        foreach (var h in materialHandles.Values) Addressables.Release(h);
        materialHandles.Clear(); materialCache.Clear();
/*
        foreach (var h in spriteHandles.Values) Addressables.Release(h);
        spriteHandles.Clear(); spriteCache.Clear();
*/
        Debug.Log("[어드레서블 모든 캐시 해제 완료]");
    }
}