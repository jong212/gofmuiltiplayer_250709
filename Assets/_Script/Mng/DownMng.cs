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
    [Header("파일체크중입니다.")]
    public GameObject waitMessage;

    [Header("다운로드 파일 크기 입니다")]
    public GameObject downMessage;
    public TextMeshProUGUI sizeInfoText;

    [Header("파일을 다운로드 중입니다.")]
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
        // 어드레서블 초기화
        StartCoroutine(InitAddressable());

        // 다운 받아야 하는 파일이 있는지 없는지 체크
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
            - 로컬에 리소스가 있으면: GetDownloadSizeAsync는 0을 반환.
            - 서버에만 리소스가 있으면: GetDownloadSizeAsync는 해당 리소스의 다운로드 크기를 반환.
            - 리소스가 업데이트된 경우: 서버의 리소스 크기를 반환하여 업데이트된 리소스를 다운로드하게 만듬.
            */
            var handle = Addressables.GetDownloadSizeAsync(label);

            yield return handle;
            patchSize += handle.Result;
        }
        if (patchSize > decimal.Zero)
        {
            waitMessage.SetActive(false);               // 업데이트 체크중 팝업 닫기
            downMessage.SetActive(true);                // 다운 받아야 할 파일 있다는 팝업 오픈
            Debug.Log("[2 DownManager : 서버에서 다운로드 해야 할 리소스 파일 확인 됨]");
            sizeInfoText.text = GetFileSize(patchSize); // 다운 받아야할 크기 UI 표시
            FileDownButtonObj.gameObject.SetActive(true);

        }
        else // 다운 받을 게 없으면 씬 변경
        {
            downValText.text = "100 %";
            downSlider.size = 1f;
            yield return new WaitForSeconds(2f);
            Debug.Log("[2 LobbyManager : 다운로드 할 리소스 파일 없음 ]");
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
        A 그룹에 두 개의 오브젝트(예: 프리팹 1, 프리팹 2)가 있다고 가정합.
        이 중에서 프리팹 1에만 "default" 라벨이 설정되어 있고, 프리팹 2는 라벨이 설정되지 않은 상태라고 합시다.
        이 경우, Addressables.GetDownloadSizeAsync("default")를 호출하면 "default" 라벨이 설정된 리소스만 다운로드할 크기를 확인하게 됨. 즉, 프리팹 1만 다운로드 대상이 되고, 프리팹 2는 무시.        
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

        var handle = Addressables.DownloadDependenciesAsync(label, false);//false는 다운로드 후 리소스를 자동으로 로드하지 않겠다는 설정
        while (!handle.IsDone)
        {
            patchMap[label] = handle.GetDownloadStatus().DownloadedBytes;
            yield return new WaitForEndOfFrame();

        }
        patchMap[label] = handle.GetDownloadStatus().TotalBytes;
        /*
        핸들 해제: Release()는 다운로드한 리소스를 해제하는 것이 아니라, 다운로드 과정에서 사용된 handle에 대한 참조를 정리하고 메모리 누수를 방지하기 위함입니다.
        handle은 다운로드 작업을 관리하는 비동기 작업 핸들이며, 작업이 완료된 후에는 더 이상 필요 없으므로 이를 해제하는 것입니다.
        핸들을 해제하지 않으면, 비동기 작업과 관련된 메모리가 계속 남아 있을 수 있어 메모리 누수가 발생할 수 있습니다.
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
                Debug.Log("[2-1 다운 완료]");
                // 모든 비동기 작업이 완료되었는지 확인
                // 씬 비동기로 로드
                ManagerSystem.Instance.InitStepByCall("3_AddressableCashing",null );

                break;
            }

            yield return new WaitForEndOfFrame();
        }
    } 
}
