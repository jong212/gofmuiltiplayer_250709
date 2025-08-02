using DG.Tweening;
using TMPro;
using UnityEngine;

public class LobbyPlayerinfo : MonoBehaviour
{
    public TextMeshProUGUI NickNameText;
    public int CharId;
    public string PlayerRefId;
    public Transform PlayerSpawnPoint;

    public void SetPlayer(string playerRefId, string nick, int charId)
    {
        PlayerRefId = playerRefId;
        NickNameText.text = nick;
        CharId = charId;

        var tempMtData = ManagerSystem.Instance.BackendCash.ChartCharacter[charId - 1];

        GameObject bodyPrefab = AddressableMng.instance.GetPrefab("ballskin", tempMtData.name);
        GameObject instance = Instantiate(bodyPrefab, PlayerSpawnPoint);
        
        instance.transform.localPosition = new Vector3(tempMtData.posx, tempMtData.posy, tempMtData.posz);
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = new Vector3(tempMtData.scalex, tempMtData.scaley, tempMtData.scalez);

        Rigidbody rb = instance.AddComponent<Rigidbody>();
        SphereCollider sq = instance.AddComponent<SphereCollider>();
        rb.useGravity = true;
        rb.isKinematic = false;

        instance.transform.DORotate(new Vector3(0, 360, 0), 20f, RotateMode.LocalAxisAdd)
        .SetLoops(-1, LoopType.Incremental)
        .SetEase(Ease.Linear);

    }

    public void ClearSlot()
    {
        PlayerRefId = null;
        NickNameText.text = "";
        CharId = -1;
        if (PlayerSpawnPoint.childCount > 0)
        {
            for (int i = 0; i < PlayerSpawnPoint.childCount; i++)
            {
                Destroy(PlayerSpawnPoint.GetChild(i).gameObject);
            }
        }
    }
    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
