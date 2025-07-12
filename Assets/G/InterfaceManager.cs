using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

// ����� ���� �� ���� ������ ������ �ִ°� ���� ������ UI��
public class InterfaceManager : MonoBehaviour
{
    #region Singleton

    public static InterfaceManager instance;

    private void Awake()
    {
        if (instance && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    #endregion

    [Header("------ Canvases ------")]
    public Canvas mainCanvas;
    public Canvas worldCanvas;
    public TextMeshProUGUI countText;
    public TextMeshProUGUI coomPlayerCount;
    public Joystick _joystick;
    public Image ChargeCircle;

    public void Init()
    {

    }
}