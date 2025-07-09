using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

// 여기는 게임 씬 안쪽 데이터 가지고 있는거 비추 가급적 UI만
public class InterfaceManager : MonoBehaviour
{
    #region Singleton

    public static InterfaceManager instance;

    private void Awake()
    {
        if (instance)
        {
            Debug.LogWarning("Instance already exists!");
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    #endregion

    [Header("------ Canvases ------")]
    public Canvas mainCanvas;
    public Canvas worldCanvas;
    public VariableJoystick joystick;
    public TextMeshProUGUI countText;
    public TextMeshProUGUI coomPlayerCount;
    public Image ChargeCircle;

}