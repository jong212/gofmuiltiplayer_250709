using System.Collections.Generic;
using UnityEngine;

public class BackendCashAll : MonoBehaviour
{
    #region ## �������� (������ ���� �ݿ���) 
    private UserData _userData;
    public UserData UserData
    {
        get => _userData;
        set => _userData = value;
    }
    #endregion

    #region ## �����̱��� (��Ʈ �� �ҷ��ͼ� ĳ���س��� ���� �뵵)
    private List<ChartCharacter> _chartCharacter = new List<ChartCharacter>();
    public List<ChartCharacter> ChartCharacter // ĳ�� -�⺻ ĳ���� ��巹���� ��Ʈ
    {
        get => _chartCharacter;
        set => _chartCharacter = value;
    }

    public string gameDataRowInDate = string.Empty;
    #endregion

    #region ## ����Ͻ� ����

    #endregion
}
