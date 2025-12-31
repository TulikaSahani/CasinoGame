using UnityEngine;

public class LowSpecBot : MonoBehaviour
{
   

    void Awake()
    {
        int ram = SystemInfo.systemMemorySize; // in MB
        bool isIntelGPU = SystemInfo.graphicsDeviceName.ToLower().Contains("intel");

        if (ram <= 4096 || isIntelGPU)
        {
            QualitySettings.SetQualityLevel(0, true); // Very Low
            Application.targetFrameRate = 30;
            Screen.SetResolution(1024, 576, true);
        }
        else if (ram <= 8192)
        {
            QualitySettings.SetQualityLevel(1, true); // Medium
            Application.targetFrameRate = 60;
            Screen.SetResolution(1280, 720, true);
        }
        else
        {
            QualitySettings.SetQualityLevel(2, true); // High
            Application.targetFrameRate = 60;
            Screen.SetResolution(1920, 1080, true);
        }
    
}
}


