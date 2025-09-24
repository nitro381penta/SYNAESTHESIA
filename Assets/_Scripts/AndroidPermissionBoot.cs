#if UNITY_ANDROID
using UnityEngine.Android;
#endif
using UnityEngine;

public class AndroidMicPermissionBoot : MonoBehaviour
{
    static bool _checked;

    void Awake()
    {
        #if UNITY_ANDROID
        if (_checked) return; // avoid re-asking if scene reloads
        _checked = true;

        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
        #endif
    }
}
