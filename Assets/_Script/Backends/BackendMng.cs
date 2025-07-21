using BackEnd;
using BackEnd.Functions;
using System;
using UnityEngine;

public class BackendMng : MonoBehaviour
{
    public void Step4()
    {
        var bro = Backend.Initialize(); // 뒤끝 초기화

        if (bro.IsSuccess())
        {
            Debug.Log("초기화 성공 : " + bro); // 성공일 경우 statusCode 204 Success
            ManagerSystem.Instance.StepByCall("5_GoogleLoginBtnActive");

        }
        else
        {
            Debug.LogError("초기화 실패 : " + bro); // 실패일 경우 statusCode 400대 에러 발생
        }
    }


    public void CheckNickName(string nickname, Action<bool> onResult)
    {
        Backend.BMember.CheckNicknameDuplication(nickname, (callback) =>
        {
            if (callback.StatusCode == 204)
            {
                onResult?.Invoke(false); // 사용 가능
            }
            else if (callback.StatusCode == 409)
            {
                onResult?.Invoke(true); // 중복
            }
        });
    }
    public void Step6(string nickname)
    {
        CreateNickName(nickname);
        SetDefaultUserData();
    }
    void CreateNickName(string nickname)
    {
        Backend.BMember.CreateNickname(nickname, (callback) =>
        {
            Debug.Log("닉네임 설정 완료");
         
        });
    }

    void SetDefaultUserData()
    {
        InsertInitUserData initData = new InsertInitUserData();
        InsertGameData("Character", initData.ToParam(), (bro) => {
            ManagerSystem.Instance.StepByCall("7_BackendChartLoad");
        });
    }

    public void Step8()
    {
        CashingUserData();
    }
    void CashingUserData()
    {
        var bro = Backend.GameData.GetMyData("Character", new Where());
        if (bro.IsSuccess())
        {
            LitJson.JsonData gameDataJson = bro.FlattenRows(); // Json으로 리턴된 데이터를 받아옵니다.  
            if (gameDataJson.Count <= 0) // 받아온 데이터의 갯수가 0이라면 데이터가 존재하지 않는 것입니다.  
            {
                Debug.LogWarning("데이터가 존재하지 않습니다.");
            } else
            {
               UserData tempdata = new UserData(gameDataJson);
               ManagerSystem.Instance.BackendCash.UserData = tempdata;
               DownMng.instance.ParentObj.gameObject.SetActive(false);


                //Debug.Log(ManagerSystem.Instance.BackendCash.UserData.ToString());
                /*  var chartList = ManagerSystem.Instance.BackendCash.ChartCharacter;

                  if (chartList == null || chartList.Count == 0)
                  {
                      Debug.Log("ChartCharacter 리스트가 비어있음.");
                  }
                  else
                  {
                      foreach (var c in chartList)
                      {
                          Debug.Log($"seq: {c.seq}, title: {c.title}, speed: {c.speed}, name: {c.name}, label: {c.label}");
                      }
                  }*/
            }
        }
    }

    #region UPDATE, INERT 
    public void InsertGameData(string tableName, Param param, Action<BackendReturnObject> onComplete = null)
    {
        var bro = Backend.GameData.Insert(tableName, param);

        onComplete?.Invoke(bro);
    }

    public void GameDataUpdate<T>(string columName, T Parameter)
    {
        if (ManagerSystem.Instance.BackendCash.UserData == null)
        {
            Debug.LogError("서버에서 다운받거나 새로 삽입한 데이터가 존재하지 않습니다. Insert 혹은 Get을 통해 데이터를 생성해주세요.");
            return;
        }

        Param param = new Param();
        param.Add(columName, Parameter);


        Backend.GameData.UpdateV2("Character", ManagerSystem.Instance.BackendCash.gameDataRowInDate, Backend.UserInDate, param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Debug.Log("뒤끝 : 게임 정보 데이터 수정에 성공했습니다. : " + callback);
            }
            else
            {
                Debug.LogError("뒤끝 : 게임 정보 데이터 수정에 실패했습니다. : " + callback);
            }
        });
    }
    #endregion

}
