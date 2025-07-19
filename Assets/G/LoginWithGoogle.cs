using BackEnd;
using UnityEngine;
using UnityEngine.UI;
using static TheBackend.ToolKit.GoogleLogin.Android;

public class LoginWithGoogle : MonoBehaviour {
    
    public static LoginWithGoogle instance;
    public GameObject LoginButton;
    public GameObject NickNameSetPanel;
    public InputField Name_Input;

    private void Awake()
    {
        if (instance && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        LoginButton.GetComponent<Button>().onClick.AddListener(() => CustomLogin("test123", "123"));
        NickNameSetPanel.GetComponentInChildren<Button>().onClick.AddListener(() => DuplicateCheckNick());

    }
    public void Step5()
    {
        LoginButton.gameObject.SetActive(true);
    }

    public void CustomLogin(string id, string pw)
    {
        LoginButton.gameObject.SetActive(false);
        var bro = Backend.BMember.CustomLogin(id, pw);        
        var nickData = Backend.BMember.GetUserInfo();

        LitJson.JsonData userInfoJson = nickData.GetReturnValuetoJSON()["row"];
        string nick = userInfoJson["nickname"]?.ToString();

        // 200: 기존 회원, 201: 신규 사용자 회원가입 및 로그인 성공
        // 기존 회원 && 닉네임이 비어있지 않은 경우 에만 if문탐 (기존 회원이 아니거나 닉설정 안되면 else 탐)
        if (bro.StatusCode == 200 && !string.IsNullOrEmpty(nick))
        {
            ManagerSystem.Instance.StepByCall("7_BackendChartLoad");
        } else
        {
            NickNameSetPanel.gameObject.SetActive(true);
        }
    }
    void DuplicateCheckNick()
    {
        ManagerSystem.Instance.BackendMng.CheckNickName(Name_Input.text.ToString(), (isDuplicate) =>
        {
            if (isDuplicate)
            {
                Debug.Log("닉네임 중복!");
            }
            else
            {
                Debug.Log("사용 가능!");
                ManagerSystem.Instance.StepByCall("6_CreateNickAndSetDefaultCharacterInfo", Name_Input.text.ToString());

            }
        });
    }

    /*public void TryGoogleLogin()
    {
        TheBackend.ToolKit.GoogleLogin.Android.GoogleLogin(true, GoogleLoginCallback);
    }
    private void GoogleLoginCallback(bool isSuccess, string errorMessage, string token)
    {
        if (isSuccess == false)
        {
            Debug.LogError(errorMessage);
            return;
        }

        var bro = Backend.BMember.AuthorizeFederation(token, FederationType.Google);
        Debug.Log("구글 페데레이션 로그인 결과 : " + bro);


        if (bro.StatusCode == 200 || bro.StatusCode == 201)                              // 200: 기존 회원, 201: 신규 사용자 회원가입 및 로그인 성공
        {
            var nickData = Backend.BMember.GetUserInfo();
            LitJson.JsonData userInfoJson = nickData.GetReturnValuetoJSON()["row"];
            string nick = userInfoJson["nickname"]?.ToString();
            BackendGameData.Instance.SetNickname(nick);

          *//*  if (string.IsNullOrEmpty(nick))                 // 닉네임이 비어있음 > 닉네임 설정 UI 오픈
            {
                StaticManager.UI.CommonOpen(UIType.BackEndName, LoginUICanvas.transform, true, () => Debug.Log("test"));
                Selecter.gameObject.SetActive(true);
            }
            else // TO DO 닉네임 설정 되어있음 > 이후 처리 로직 작성 필요
            {
                SetWaitRoom();
            }*//*
        }

    }*/
}
