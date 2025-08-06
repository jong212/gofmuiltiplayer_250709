using BackEnd;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Google : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartGoogleLogin();
    }
    public void StartGoogleLogin()
    {
        TheBackend.ToolKit.GoogleLogin.Android.GoogleLogin(GoogleLoginCallback);
    }
    void Update()
    {
        if (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began)
        {
            PointerEventData ped = new PointerEventData(EventSystem.current);
            ped.position = Input.touches[0].position;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(ped, results);

            foreach (RaycastResult r in results)
            {
                Debug.Log("Hit UI: " + r.gameObject.name);
            }

            if (results.Count == 0)
                Debug.Log("UI에 아무것도 안 걸림");
        }
    }
    private void GoogleLoginCallback(bool isSuccess, string errorMessage, string token)
    {
        if (isSuccess == false)
        {
            Debug.LogError(errorMessage);
            return;
        }

        Debug.Log("구글 토큰 : " + token);
        var bro = Backend.BMember.AuthorizeFederation(token, FederationType.Google);
        Debug.Log("페데레이션 로그인 결과 : " + bro);
    }
}
