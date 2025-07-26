using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BackEnd;
using LitJson;

public class BackendChart : MonoBehaviour
{
    Dictionary<string, ChartInfo> chartInfoDic = new Dictionary<string, ChartInfo>();   // 차트 이름으로 데이터를 검색할 것이기 때문에 Dictnary로 생성합니다, // 해당 Dictnary는 최신 버전으로 업데이트할 차트 리스트로 사용됩니다.(최신 버전이라면 해당 리스트에서 제외)
    private List<string> _getCharLocalListname = new List<string>();
    public List<string> GetCharLocalListname    // 캐싱 - Useable Local Charts
    {
        get => _getCharLocalListname;
        set => _getCharLocalListname = value;
    }

    
    public void Step7()
    {
        StartCoroutine(ServerCharLoad());
    }

    
    #region # 차트 매니저 가져와서 수정일자 보고 최신화가 필요하면 해당 차트만 따로 다운로드 해서 로컬에 저장 후 마지막 단계로 파싱
    IEnumerator ServerCharLoad()
    {
        
        // 차트 매니저폴더의 파일 가져옴
        var bro = Backend.Chart.GetChartListByFolder(2549);                                 // 차트 매니저 폴더 접근 
        if (!bro.IsSuccess())
        {
            Debug.LogError("에러가 발생했습니다 : " + bro.ToString());
            
        }
        string chartManagerFileId = bro.FlattenRows()[0]["selectedChartFileId"].ToString(); // CSV 파일 업로드 할 때 부여 된 고유 파일 ID 값을 가져옴 ex 145150, // 해당 폴더에는 chartManager 차트 하나만 존재할 것이므로 0으로 접근합니다.
        string chartManagerName = bro.FlattenRows()[0]["chartName"].ToString();
        var serverChartBro = Backend.Chart.GetChartContents(chartManagerFileId);            // 서버에서 ChartManager 차트를 불러옵니다. 기기에 저장하지는 않습니다.
        if (serverChartBro.IsSuccess() == false)                                            // 서버에서 불러오지 못할 경우에는 데이터 꼬임 방지를 위해 진행을 중지합니다.
        {
            Debug.Log("CheckChartStop");
            yield break;
        }

        JsonData newChartManagerJson = serverChartBro.FlattenRows();                        // 서버에서 불러온 ChartManager을 언마샬하여 JsonData 형태로 캐싱합니다.

        
        foreach (JsonData chartInfoJson in newChartManagerJson)                             // csv 한 줄 = charinfojson
        {
            ChartInfo chartInfo = new ChartInfo(chartInfoJson);
            chartInfoDic.Add(chartInfo.chartName, chartInfo);
            GetCharLocalListname.Add(chartInfo.chartName);
        }

        string deviceChartManagerString = Backend.Chart.GetLocalChartData(chartManagerName);// 기기에 저장된 chartManager 차트를 불러옵니다.
        if (string.IsNullOrEmpty(deviceChartManagerString) == false)                        // 기기에는 string 형태로 저장이 되며, 저장되어있지 않을 경우 string.Empty가 반환됩니다.
        {

            JsonData deviceChartManagerJson = JsonMapper.ToObject(deviceChartManagerString);// 기기에 저장된 chartManager 차트가 존재한다면 // 기기에 저장된 string형태의 chartManager를 Json 형태로 변경
            deviceChartManagerJson = BackendReturnObject.Flatten(deviceChartManagerJson);

            foreach (JsonData deviceChartJson in deviceChartManagerJson["rows"])            // 기기에 저장된 chartManager 차트 속 차트들을 서버에서 불러온 데이터와 대조합니다.
            {
                ChartInfo deviceChartInfo = new ChartInfo(deviceChartJson);
                if (chartInfoDic.ContainsKey(deviceChartInfo.chartName))                    // 이미 기기에 저장되어 있는 차트가 있는지 확인합니다.
                {
                    if (chartInfoDic[deviceChartInfo.chartName].updateDate == deviceChartInfo.updateDate)// 기기에 저장되어 있는 차트의 수정 날짜(updateDate)가 일치하는지 확인합니다.
                    {
                        chartInfoDic.Remove(deviceChartInfo.chartName);                     // 수정날짜까지 일치할 경우, 재다운로드 리스트(chartInfoDic)에서 제외합니다.
                    }
                }
            }
        }

        if (chartInfoDic.Count > 0)                                                         // 재다운로드할 차트 리스트에서 차트가 하나라도 존재하는지 확인합니다.
        {
            foreach (var downloadChartInfo in chartInfoDic)                                 // 차트를 재다운로드하여 기기에 덮어씌웁니다.
            {
                Debug.Log($"LoginScenemanager => 코루틴 => [3-1] {downloadChartInfo.Value.chartName} 차트를 새로운 버전으로 다운받습니다.");
                Backend.Chart.GetOneChartAndSave(downloadChartInfo.Value.chartFileId, downloadChartInfo.Value.chartName);
            }
            Backend.Chart.GetOneChartAndSave(chartManagerFileId, chartManagerName);         // chartManager 차트를 최신화합니다.(로컬저장)
        }
        else
        {
            Debug.Log("LoginScenemanager => 코루틴 => [3-1] 업이트할 차트 내역이 존재하지 않습니다.");
        }

        foreach (var chartName in GetCharLocalListname)
        {
            LoadChart(chartName);
        }
        ManagerSystem.Instance.InitStepByCall("8_CashingUserData");

    }
    #endregion

    #region # 차트 캐싱
    private void LoadChart(string chartName)
    {
        string chartDataString = Backend.Chart.GetLocalChartData(chartName);
        JsonData chartJson = JsonMapper.ToObject(chartDataString);
        chartJson = BackendReturnObject.Flatten(chartJson);

        switch (chartName)
        {
            case nameof(ChartCharacter):
                foreach (JsonData row in chartJson["rows"])
                {
                    ChartCharacter classRef = new ChartCharacter(row);
                    ManagerSystem.Instance.BackendCash.ChartCharacter.Add(classRef);
                }
                break;
            
        }
    }
    #endregion
}