using UnityEngine;
using UnityEngine.Android;

public class CheckPermissions : MonoBehaviour
{
    void Start()
    {
        // Kiểm tra và yêu cầu quyền WRITE_EXTERNAL_STORAGE
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageWrite);
        }

        // Kiểm tra và yêu cầu quyền READ_EXTERNAL_STORAGE
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
        }
    }
}
