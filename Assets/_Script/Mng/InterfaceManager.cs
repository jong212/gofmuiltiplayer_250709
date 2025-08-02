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
    public TextMeshProUGUI countText;
    public Joystick _joystick;
    public Joystick JoystickInstance;

    public Image ChargeCircle;
    [field: SerializeField] public Gradient PuttChargeColor { get; private set; }
    
    private void Start()
    {
        if ( JoystickInstance == null)
        {
            JoystickInstance = Instantiate(_joystick, mainCanvas.transform);
        }
    }


}