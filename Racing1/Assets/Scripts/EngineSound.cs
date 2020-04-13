using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineSound : MonoBehaviour
{
    Rigidbody rb;

    public CarController playerCar;
    public List<AudioSource> audioSources;
    public List<AnimationCurve> volumeCurves;

    public List<AnimationCurve> pitchCurves;
    void Awake()
    {
        // audioSources = gameObject.GetComponent<List<AudioSource>>();
        // volumeCurves = gameObject.GetComponent<List<AnimationCurve>>();
        foreach (AudioSource audioSource in audioSources)
        {
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    void OnGUI()
    {

    }
    // Start is called before the first frame update
    void OnEnable()
    {


    }

    // Update is called once per frame
    void Update()
    {
        foreach(AudioSource audioSource in audioSources)
        {
            //Set the levels for the pitch individually based on rpm
            //audioSources[i].pitch = 1.0f + pitchCurves[i].Evaluate(Mathf.Abs(playerCar.GetEngineSpeed()));
            audioSource.volume = volumeCurves[audioSources.IndexOf(audioSource)].Evaluate(Mathf.Abs(playerCar.GetEngineSpeed()));
            audioSource.pitch = pitchCurves[audioSources.IndexOf(audioSource)].Evaluate(Mathf.Abs(playerCar.GetEngineSpeed()));
        }
    }
    private float normalDist(float x, float mean, float stddev)
    {
        return Mathf.Exp(-Mathf.Pow(((x-mean)/stddev), 2));
    }

}
