using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class DownMng : MonoBehaviour
{
    [Header("����üũ���Դϴ�.")]
    public GameObject waitMessage;

    [Header("�ٿ�ε� ���� ũ�� �Դϴ�")]
    public GameObject downMessage;
    public TextMeshProUGUI sizeInfoText;

    [Header("������ �ٿ�ε� ���Դϴ�.")]
    public Transform DownlodingObj;
    public Scrollbar downSlider;
    public TextMeshProUGUI downValText;

    public GameObject ParentObj;

    public GameObject FileDownButtonObj;
    public Button GoogleLoginBtn;
    [Header("Label")]
    public AssetLabelReference player;
    public AssetLabelReference ballskin;
    public AssetLabelReference lobbyui;
    public AssetLabelReference obj;

    private long patchSize;
    private Dictionary<string, long> patchMap = new Dictionary<string, long>();

    public static DownMng instance;

    private void Awake()
    {
        if (instance && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        FileDownButtonObj.GetComponent<Button>().onClick.AddListener(() => ManagerSystem.Instance.InitStepByCall("2_FileDownLoad"));
    }
    private void Start()
    {
        ParentObj.gameObject.SetActive(true);
        ManagerSystem.Instance.InitStepByCall("1_FileDownCheck");
    }
    public void Step1()
    {
        waitMessage.SetActive(true);
        downMessage.SetActive(false);
        // ��巹���� �ʱ�ȭ
        StartCoroutine(InitAddressable());

        // �ٿ� �޾ƾ� �ϴ� ������ �ִ��� ������ üũ
        StartCoroutine(CheckUpdateFiles());
    }
    public void Step2()
    {
        StartCoroutine(PatchFiles());
    }

    IEnumerator InitAddressable()
    {

        var init = Addressables.InitializeAsync();
        yield return init;
    }
    IEnumerator CheckUpdateFiles()
    {
        var labels = new List<string>() {player.labelString,ballskin.labelString, lobbyui.labelString, obj.labelString};
        patchSize = default;

        foreach (var label in labels)
        {
            /*
            - ���ÿ� ���ҽ��� ������: GetDownloadSizeAsync�� 0�� ��ȯ.
            - �������� ���ҽ��� ������: GetDownloadSizeAsync�� �ش� ���ҽ��� �ٿ�ε� ũ�⸦ ��ȯ.
            - ���ҽ��� ������Ʈ�� ���: ������ ���ҽ� ũ�⸦ ��ȯ�Ͽ� ������Ʈ�� ���ҽ��� �ٿ�ε��ϰ� ����.
            */
            var handle = Addressables.GetDownloadSizeAsync(label);

            yield return handle;
            patchSize += handle.Result;
        }
        if (patchSize > decimal.Zero)
        {
            waitMessage.SetActive(false);               // ������Ʈ üũ�� �˾� �ݱ�
            downMessage.SetActive(true);                // �ٿ� �޾ƾ� �� ���� �ִٴ� �˾� ����
            Debug.Log("[2 DownManager : �������� �ٿ�ε� �ؾ� �� ���ҽ� ���� Ȯ�� ��]");
            sizeInfoText.text = GetFileSize(patchSize); // �ٿ� �޾ƾ��� ũ�� UI ǥ��
            FileDownButtonObj.gameObject.SetActive(true);

        }
        else // �ٿ� ���� �� ������ �� ����
        {
            downValText.text = "100 %";
            downSlider.size = 1f;
            yield return new WaitForSeconds(2f);
            Debug.Log("[2 LobbyManager : �ٿ�ε� �� ���ҽ� ���� ���� ]");
            ManagerSystem.Instance.InitStepByCall("3_AddressableCashing", null
            );
        }

    }
    private string GetFileSize(long byteCnt)
    {
        string size = "0 Bytes";

        if (byteCnt >= 1073741824) // 1 GB
        {
            size = string.Format("{0:##.##} GB", byteCnt / 1073741824.0);
        }
        else if (byteCnt >= 1048576) // 1 MB
        {
            size = string.Format("{0:##.##} MB", byteCnt / 1048576.0);
        }
        else if (byteCnt >= 1024) // 1 KB
        {
            size = string.Format("{0:##.##} KB", byteCnt / 1024.0);
        }
        else if (byteCnt > 0)
        {
            size = byteCnt + " Bytes";
        }

        return size;
    }


    IEnumerator PatchFiles()
    {
        downMessage.SetActive(false);
        waitMessage.gameObject.SetActive(false);
        FileDownButtonObj.gameObject.SetActive(false);
        DownlodingObj.gameObject.SetActive(true);
        /*
        A �׷쿡 �� ���� ������Ʈ(��: ������ 1, ������ 2)�� �ִٰ� ������.
        �� �߿��� ������ 1���� "default" ���� �����Ǿ� �ְ�, ������ 2�� ���� �������� ���� ���¶�� �սô�.
        �� ���, Addressables.GetDownloadSizeAsync("default")�� ȣ���ϸ� "default" ���� ������ ���ҽ��� �ٿ�ε��� ũ�⸦ Ȯ���ϰ� ��. ��, ������ 1�� �ٿ�ε� ����� �ǰ�, ������ 2�� ����.        
        */
        var labels = new List<string>() { player.labelString, ballskin.labelString, lobbyui.labelString,obj.labelString};

        foreach (var label in labels)
        {
            var handle = Addressables.GetDownloadSizeAsync(label);

            yield return handle;
            if (handle.Result != decimal.Zero)
            {
                StartCoroutine(DownLoadLabel(label));
            }
        }
        yield return CheckDownLoad();
    }
    IEnumerator DownLoadLabel(string label)
    {
        if (!patchMap.ContainsKey(label))
            patchMap.Add(label, 0);

        var handle = Addressables.DownloadDependenciesAsync(label, false);//false�� �ٿ�ε� �� ���ҽ��� �ڵ����� �ε����� �ʰڴٴ� ����
        while (!handle.IsDone)
        {
            patchMap[label] = handle.GetDownloadStatus().DownloadedBytes;
            yield return new WaitForEndOfFrame();

        }
        patchMap[label] = handle.GetDownloadStatus().TotalBytes;
        /*
        �ڵ� ����: Release()�� �ٿ�ε��� ���ҽ��� �����ϴ� ���� �ƴ϶�, �ٿ�ε� �������� ���� handle�� ���� ������ �����ϰ� �޸� ������ �����ϱ� �����Դϴ�.
        handle�� �ٿ�ε� �۾��� �����ϴ� �񵿱� �۾� �ڵ��̸�, �۾��� �Ϸ�� �Ŀ��� �� �̻� �ʿ� �����Ƿ� �̸� �����ϴ� ���Դϴ�.
        �ڵ��� �������� ������, �񵿱� �۾��� ���õ� �޸𸮰� ��� ���� ���� �� �־� �޸� ������ �߻��� �� �ֽ��ϴ�.
        */
        Addressables.Release(handle);

    }
    IEnumerator CheckDownLoad()
    {
        var total = 0f;
        downValText.text = "0 %";

        while (true)
        {
            total = patchMap.Sum(tmp => tmp.Value);

            downSlider.size = total / patchSize;
            downValText.text = (int)(downSlider.size * 100) + " %";

            if (total >= patchSize)
            {
                Debug.Log("[2-1 �ٿ� �Ϸ�]");
                // ��� �񵿱� �۾��� �Ϸ�Ǿ����� Ȯ��
                // �� �񵿱�� �ε�
                ManagerSystem.Instance.InitStepByCall("3_AddressableCashing",null );

                break;
            }

            yield return new WaitForEndOfFrame();
        }
    } 
}
