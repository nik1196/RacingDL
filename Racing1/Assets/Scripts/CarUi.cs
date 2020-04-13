using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CarUi : MonoBehaviour
{
    // Start is called before the first frame update

    public CarController playerCar;
    public Slider torqueSlider, brakeSlider;
    public Text gearText, speedText, RPMText;

    public Image redlineImage, RPMImage;

    public int maxRPM = 10000;
    void Awake() 
    {
        brakeSlider.maxValue = playerCar.brakingForce;
        torqueSlider.maxValue = 1f;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void OnGUI()
    {
        double speed = System.Math.Round(playerCar.transform.InverseTransformDirection(playerCar.GetRigidbody().velocity).z,2);
        speed *= 3.6;
        torqueSlider.value = playerCar.GetWheelTorque();
        brakeSlider.value = playerCar.GetBrakeForce();
        gearText.text = playerCar.GetGear().ToString();
        speedText.text = speed.ToString();
        RPMText.text = playerCar.GetEngineSpeed().ToString();
        RPMImage.fillAmount = playerCar.GetEngineSpeed()/maxRPM;
        redlineImage.fillAmount = (maxRPM -playerCar.maxEngineSpeed)/maxRPM;
    }
}
