using BackEnd;
using LitJson;
using System.Collections.Generic;
using System.Text;

#region 붕어빵 틀
// 차트 매니저 파일의 row들을 담는용
// 비교용 (로컬, 서버 차트)
public class ChartInfo
{
    public string chartName;
    public string chartFileId;
    public string updateDate;

    public ChartInfo(JsonData json)
    {
        chartName = json["chartName"].ToString();
        chartFileId = json["chartFileId"].ToString();
        updateDate = json["updateDate"].ToString();
    }
}

// 차트 - 캐릭터 모델 정보 (캐릭터별 이름, 속도, addressable을위한 오브젝트 이름, 라벨명)
public class ChartCharacter
{
    public int seq { get; private set; }
    public string name { get; private set; }
    public float posx { get; private set; }
    public float posy { get; private set; }
    public float posz { get; private set; }
    public float scalex { get; private set; }
    public float scaley { get; private set; }
    public float scalez { get; private set; }

    public ChartCharacter(JsonData json)
    {
        seq = int.Parse(json["seq"].ToString());
        name = json["name"].ToString();
        posx = float.Parse(json["posx"].ToString());
        posy = float.Parse(json["posy"].ToString());
        posz = float.Parse(json["posz"].ToString());
        scalex = float.Parse(json["scalex"].ToString());
        scaley = float.Parse(json["scaley"].ToString());
        scalez = float.Parse(json["scalez"].ToString());
    }

}
[System.Serializable]
public class UserData
{ 
    private int _selectedCharId;
    public int SelectedCharId
    {
        get => _selectedCharId;
        set 
        {
            if (_selectedCharId == value) return;
            _selectedCharId = value;
            ManagerSystem.Instance.BackendMng.GameDataUpdate("SelectedCharId", value);
        }
    }

    private int _gold;
    public int Gold
    {
        get => _gold;
        set
        {
            if (_gold == value) return;
            _gold = value;
            ManagerSystem.Instance.BackendMng.GameDataUpdate("Gold", value);
        }
    }

    private int _level;
    public int Level
    {
        get => _level;
        set
        {
            if (_level == value) return;
            _level = value;
            ManagerSystem.Instance.BackendMng.GameDataUpdate("Level", value);
        }
    }

 
    public UserData(JsonData json)
    {
        _selectedCharId = int.Parse(json[0]["SelectedCharId"].ToString());
                  _gold = int.Parse(json[0]["Gold"].ToString());
                  _level = int.Parse(json[0]["Level"].ToString());
    }

    public override string ToString()  // 디버깅 위한 함수 (Debug.Log(UserData);)
    {
        StringBuilder result = new StringBuilder();

        result.AppendLine($"LV : {_selectedCharId}");
        result.AppendLine($"money : {_gold}");
        result.AppendLine($"LastMap : {_level}");
    
        return result.ToString();
    }
}
public class InsertInitUserData
{
    public int SelectedCharId = 1;
    public int Gold = 1000;
    public int Level = 1;

    public Param ToParam()
    {
        var param = new Param();
        param.Add("SelectedCharId", SelectedCharId);
        param.Add("Gold", Gold);
        param.Add("Level", Level);
        return param;
    }
}
#endregion