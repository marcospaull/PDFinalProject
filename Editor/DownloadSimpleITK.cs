using UnityEditor;
using UnityVolumeRendering;

public class DownloadSimpleITK
{
    [MenuItem("CS116A/Download SimpleITK")]
    public static void Download()
    {
        SimpleITKManager.DownloadBinaries();
    }

    [MenuItem("CS116A/Enable SimpleITK")]
    public static void Enable()
    {
        SimpleITKManager.EnableSITK(true);
    }

    [MenuItem("CS116A/Check SimpleITK Status")]
    public static void CheckStatus()
    {
        bool hasDownloaded = SimpleITKManager.HasDownloadedBinaries();
        bool isEnabled = SimpleITKManager.IsSITKEnabled();
        
        UnityEngine.Debug.Log("SimpleITK Downloaded: " + hasDownloaded);
        UnityEngine.Debug.Log("SimpleITK Enabled: " + isEnabled);
    }
}