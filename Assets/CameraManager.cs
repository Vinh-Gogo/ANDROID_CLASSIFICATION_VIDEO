using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CameraManager : MonoBehaviour
{
    public RawImage rawImage;  // Đối tượng RawImage để hiển thị webcam
    private WebCamTexture webCamTexture;
    
    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        // Khởi tạo webcam đầu tiên nếu có sẵn
        if (devices.Length > 0)
        {
            webCamTexture = new WebCamTexture(devices[0].name);
            rawImage.texture = webCamTexture;
            //rawImage.material.mainTexture = webCamTexture;
            webCamTexture.Play();
        }
        else
        {
            Debug.LogError("Không có thiết bị webcam nào được tìm thấy!");
        }
    }

    private void OnDestroy()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            webCamTexture.Stop();
        }
    }

   
}
