using System;
using System.Diagnostics;
using UnityEngine;

public class ManagerSystem : MonoBehaviour
{
    public static ManagerSystem Instance { get; private set; }

    public AddressableMng AddrMng { get; private set; }
    public BackendMng BackendMng { get; private set; }
    public BackendChart BackendChart { get; private set; }
    public BackendCashAll BackendCash{ get; private set; }
    public InterfaceManager UIManager { get; private set; }
    public MatchManager MatchManager { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject); // 이게 더 안전합니다
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject); // ✅ 씬이 바뀌어도 살아있게 만듦

        AddrMng = GetComponentInChildren<AddressableMng>();
        BackendMng = GetComponentInChildren<BackendMng>();
        BackendChart = GetComponentInChildren<BackendChart>();
        BackendCash = GetComponentInChildren<BackendCashAll>();


        UIManager = GetComponentInChildren<InterfaceManager>();
        MatchManager = GetComponentInChildren<MatchManager>();
    }
    private void Start()
    {
        UIManager.Init();
        MatchManager.Init();
    }

    public void StepByCall(string type, string type2 = null)
    {
        switch (type)
        {
            case "1_FileDownCheck":
                DownMng.instance.Step1();                
                break;
            case "2_FileDownLoad":
                DownMng.instance.Step2();
                break;
            case "3_AddressableCashing":
                AddressableMng.instance.Step3();
                break;
            case "4_BackendInit":
                BackendMng.Step4();
                break;
            case "5_GoogleLoginBtnActive":
                DownMng.instance.ParentObj.gameObject.SetActive(false);
                LoginWithGoogle.instance.Step5();
                break;
            case "6_CreateNickAndSetDefaultCharacterInfo":
                BackendMng.Step6(type2);
                break;
            case "7_BackendChartLoad":
                BackendChart.Step7();
                break;
            case "8_CashingUserData":
                BackendMng.Step8();
                break;
        }
    }
}
