using System.Collections.Generic;
using UnityEngine;

public class BackendCashAll : MonoBehaviour
{
    #region ## 수정가능 (데이터 서버 반영됨) 
    private UserData _userData;
    public UserData UserData
    {
        get => _userData;
        set => _userData = value;
    }
    #endregion

    #region ## 수정미권장 (차트 값 불러와서 캐싱해놓고 쓰는 용도)
    private List<ChartCharacter> _chartCharacter = new List<ChartCharacter>();
    public List<ChartCharacter> ChartCharacter // 캐싱 -기본 캐릭터 어드레서블 차트
    {
        get => _chartCharacter;
        set => _chartCharacter = value;
    }

    public string gameDataRowInDate = string.Empty;
    #endregion

    #region ## 비즈니스 로직

    #endregion
}
