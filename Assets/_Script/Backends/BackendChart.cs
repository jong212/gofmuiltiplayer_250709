using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BackEnd;
using LitJson;

public class BackendChart : MonoBehaviour
{
    Dictionary<string, ChartInfo> chartInfoDic = new Dictionary<string, ChartInfo>();   // ��Ʈ �̸����� �����͸� �˻��� ���̱� ������ Dictnary�� �����մϴ�, // �ش� Dictnary�� �ֽ� �������� ������Ʈ�� ��Ʈ ����Ʈ�� ���˴ϴ�.(�ֽ� �����̶�� �ش� ����Ʈ���� ����)
    private List<string> _getCharLocalListname = new List<string>();
    public List<string> GetCharLocalListname    // ĳ�� - Useable Local Charts
    {
        get => _getCharLocalListname;
        set => _getCharLocalListname = value;
    }

    
    public void Step7()
    {
        StartCoroutine(ServerCharLoad());
    }

    
    #region # ��Ʈ �Ŵ��� �����ͼ� �������� ���� �ֽ�ȭ�� �ʿ��ϸ� �ش� ��Ʈ�� ���� �ٿ�ε� �ؼ� ���ÿ� ���� �� ������ �ܰ�� �Ľ�
    IEnumerator ServerCharLoad()
    {
        
        // ��Ʈ �Ŵ��������� ���� ������
        var bro = Backend.Chart.GetChartListByFolder(2549);                                 // ��Ʈ �Ŵ��� ���� ���� 
        if (!bro.IsSuccess())
        {
            Debug.LogError("������ �߻��߽��ϴ� : " + bro.ToString());
            
        }
        string chartManagerFileId = bro.FlattenRows()[0]["selectedChartFileId"].ToString(); // CSV ���� ���ε� �� �� �ο� �� ���� ���� ID ���� ������ ex 145150, // �ش� �������� chartManager ��Ʈ �ϳ��� ������ ���̹Ƿ� 0���� �����մϴ�.
        string chartManagerName = bro.FlattenRows()[0]["chartName"].ToString();
        var serverChartBro = Backend.Chart.GetChartContents(chartManagerFileId);            // �������� ChartManager ��Ʈ�� �ҷ��ɴϴ�. ��⿡ ���������� �ʽ��ϴ�.
        if (serverChartBro.IsSuccess() == false)                                            // �������� �ҷ����� ���� ��쿡�� ������ ���� ������ ���� ������ �����մϴ�.
        {
            Debug.Log("CheckChartStop");
            yield break;
        }

        JsonData newChartManagerJson = serverChartBro.FlattenRows();                        // �������� �ҷ��� ChartManager�� �𸶼��Ͽ� JsonData ���·� ĳ���մϴ�.

        
        foreach (JsonData chartInfoJson in newChartManagerJson)                             // csv �� �� = charinfojson
        {
            ChartInfo chartInfo = new ChartInfo(chartInfoJson);
            chartInfoDic.Add(chartInfo.chartName, chartInfo);
            GetCharLocalListname.Add(chartInfo.chartName);
        }

        string deviceChartManagerString = Backend.Chart.GetLocalChartData(chartManagerName);// ��⿡ ����� chartManager ��Ʈ�� �ҷ��ɴϴ�.
        if (string.IsNullOrEmpty(deviceChartManagerString) == false)                        // ��⿡�� string ���·� ������ �Ǹ�, ����Ǿ����� ���� ��� string.Empty�� ��ȯ�˴ϴ�.
        {

            JsonData deviceChartManagerJson = JsonMapper.ToObject(deviceChartManagerString);// ��⿡ ����� chartManager ��Ʈ�� �����Ѵٸ� // ��⿡ ����� string������ chartManager�� Json ���·� ����
            deviceChartManagerJson = BackendReturnObject.Flatten(deviceChartManagerJson);

            foreach (JsonData deviceChartJson in deviceChartManagerJson["rows"])            // ��⿡ ����� chartManager ��Ʈ �� ��Ʈ���� �������� �ҷ��� �����Ϳ� �����մϴ�.
            {
                ChartInfo deviceChartInfo = new ChartInfo(deviceChartJson);
                if (chartInfoDic.ContainsKey(deviceChartInfo.chartName))                    // �̹� ��⿡ ����Ǿ� �ִ� ��Ʈ�� �ִ��� Ȯ���մϴ�.
                {
                    if (chartInfoDic[deviceChartInfo.chartName].updateDate == deviceChartInfo.updateDate)// ��⿡ ����Ǿ� �ִ� ��Ʈ�� ���� ��¥(updateDate)�� ��ġ�ϴ��� Ȯ���մϴ�.
                    {
                        chartInfoDic.Remove(deviceChartInfo.chartName);                     // ������¥���� ��ġ�� ���, ��ٿ�ε� ����Ʈ(chartInfoDic)���� �����մϴ�.
                    }
                }
            }
        }

        if (chartInfoDic.Count > 0)                                                         // ��ٿ�ε��� ��Ʈ ����Ʈ���� ��Ʈ�� �ϳ��� �����ϴ��� Ȯ���մϴ�.
        {
            foreach (var downloadChartInfo in chartInfoDic)                                 // ��Ʈ�� ��ٿ�ε��Ͽ� ��⿡ �����ϴ�.
            {
                Debug.Log($"LoginScenemanager => �ڷ�ƾ => [3-1] {downloadChartInfo.Value.chartName} ��Ʈ�� ���ο� �������� �ٿ�޽��ϴ�.");
                Backend.Chart.GetOneChartAndSave(downloadChartInfo.Value.chartFileId, downloadChartInfo.Value.chartName);
            }
            Backend.Chart.GetOneChartAndSave(chartManagerFileId, chartManagerName);         // chartManager ��Ʈ�� �ֽ�ȭ�մϴ�.(��������)
        }
        else
        {
            Debug.Log("LoginScenemanager => �ڷ�ƾ => [3-1] ����Ʈ�� ��Ʈ ������ �������� �ʽ��ϴ�.");
        }

        foreach (var chartName in GetCharLocalListname)
        {
            LoadChart(chartName);
        }
        ManagerSystem.Instance.InitStepByCall("8_CashingUserData");

    }
    #endregion

    #region # ��Ʈ ĳ��
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