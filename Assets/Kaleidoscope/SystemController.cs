using UnityEngine;

public class SystemController : MonoBehaviour
{
    int frameCount = 0;
    void Start()
    {
        if(!HDROutputSettings.main.active){
            HDROutputSettings.main.RequestHDRModeChange(true);
        }
        
    }

    void Update()
    {
        if (frameCount < 10)
        {
            Debug.Log("HDROutputSettings.main.active: "+HDROutputSettings.main.active);
        }
        frameCount++;
        
    }
}
