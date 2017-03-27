using UnityEngine;
using VRTK;

public sealed class AvatarCameraRigSync : MonoBehaviour {
    public GameObject AvatarHead;
    public GameObject LeftHand;
    public GameObject RightHand;

    private void OnEnable() {
        SetUpTransformFollow(AvatarHead, VRTK_DeviceFinder.Devices.Headset);
        SetUpTransformFollow(LeftHand, VRTK_DeviceFinder.Devices.LeftController);
        SetUpTransformFollow(RightHand, VRTK_DeviceFinder.Devices.RightController);
    }

    private static void SetUpTransformFollow(GameObject avatarComponent, VRTK_DeviceFinder.Devices device) {

        var transformFollow = avatarComponent.AddComponent<VRTK_TransformFollow>();
        transformFollow.gameObjectToFollow = VRTK_DeviceFinder.DeviceTransform(device).gameObject;
        transformFollow.followsScale = false;

        /*
         * Work around stupid bug in VRTK_TransformFollow:
         * VRTK_TransformFollow only caches the transforms in OnEnable but we can't set
         * gameObjectToFollow before OnEnable is run, so we trigger it again here.
         * 
         * This will be fixed in the future by @bddckr so the following isn't needed anymore.
         */
        transformFollow.enabled = false;
        transformFollow.enabled = true;
        // Bug fix end.
    }
}
