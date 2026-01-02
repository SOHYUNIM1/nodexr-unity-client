using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class UIInputModeSwitcher : MonoBehaviour
{
    [Header("Assign on the same EventSystem object")]
#if ENABLE_INPUT_SYSTEM
    public InputSystemUIInputModule pcModule;
#endif
    public BaseInputModule xrModule; // OVRInputModule or XRUIInputModule

    [Header("Optional: if you want to auto-detect HMD")]
    public bool autoDetectHMD = true;

    void Reset()
    {
#if ENABLE_INPUT_SYSTEM
        pcModule = GetComponent<InputSystemUIInputModule>();
#endif
        // xrModule는 Inspector에서 직접 드래그하는 걸 권장
    }

    void OnEnable()
    {
        Apply();
    }

    void Update()
    {
        // 에디터에서 Play 중 HMD 연결/해제 같은 변화를 반영하고 싶으면 Update에서 Apply 호출
        if (autoDetectHMD)
            Apply();
    }

    void Apply()
    {
        bool hmdActive = XRSettings.isDeviceActive;

#if ENABLE_INPUT_SYSTEM
        if (pcModule != null) pcModule.enabled = !hmdActive;
#endif
        if (xrModule != null) xrModule.enabled = hmdActive;
    }
}
