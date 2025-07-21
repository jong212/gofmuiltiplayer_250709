using BackEnd;
using BackEnd.Functions;
using System;
using UnityEngine;

public class BackendMng : MonoBehaviour
{
    public void Step4()
    {
        var bro = Backend.Initialize(); // �ڳ� �ʱ�ȭ

        if (bro.IsSuccess())
        {
            Debug.Log("�ʱ�ȭ ���� : " + bro); // ������ ��� statusCode 204 Success
            ManagerSystem.Instance.StepByCall("5_GoogleLoginBtnActive");

        }
        else
        {
            Debug.LogError("�ʱ�ȭ ���� : " + bro); // ������ ��� statusCode 400�� ���� �߻�
        }
    }


    public void CheckNickName(string nickname, Action<bool> onResult)
    {
        Backend.BMember.CheckNicknameDuplication(nickname, (callback) =>
        {
            if (callback.StatusCode == 204)
            {
                onResult?.Invoke(false); // ��� ����
            }
            else if (callback.StatusCode == 409)
            {
                onResult?.Invoke(true); // �ߺ�
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
            Debug.Log("�г��� ���� �Ϸ�");
         
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
            LitJson.JsonData gameDataJson = bro.FlattenRows(); // Json���� ���ϵ� �����͸� �޾ƿɴϴ�.  
            if (gameDataJson.Count <= 0) // �޾ƿ� �������� ������ 0�̶�� �����Ͱ� �������� �ʴ� ���Դϴ�.  
            {
                Debug.LogWarning("�����Ͱ� �������� �ʽ��ϴ�.");
            } else
            {
               UserData tempdata = new UserData(gameDataJson);
               ManagerSystem.Instance.BackendCash.UserData = tempdata;
               DownMng.instance.ParentObj.gameObject.SetActive(false);


                //Debug.Log(ManagerSystem.Instance.BackendCash.UserData.ToString());
                /*  var chartList = ManagerSystem.Instance.BackendCash.ChartCharacter;

                  if (chartList == null || chartList.Count == 0)
                  {
                      Debug.Log("ChartCharacter ����Ʈ�� �������.");
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
            Debug.LogError("�������� �ٿ�ްų� ���� ������ �����Ͱ� �������� �ʽ��ϴ�. Insert Ȥ�� Get�� ���� �����͸� �������ּ���.");
            return;
        }

        Param param = new Param();
        param.Add(columName, Parameter);


        Backend.GameData.UpdateV2("Character", ManagerSystem.Instance.BackendCash.gameDataRowInDate, Backend.UserInDate, param, (callback) =>
        {
            if (callback.IsSuccess())
            {
                Debug.Log("�ڳ� : ���� ���� ������ ������ �����߽��ϴ�. : " + callback);
            }
            else
            {
                Debug.LogError("�ڳ� : ���� ���� ������ ������ �����߽��ϴ�. : " + callback);
            }
        });
    }
    #endregion

}
