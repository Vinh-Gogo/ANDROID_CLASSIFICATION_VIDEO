using TMPro; // Yêu cầu gói TextMeshPro cho TextMesh
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using Microsoft.ML.OnnxRuntime;
using System.IO;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Linq;
using System;
using System.Threading;
using uPLibrary.Networking.M2Mqtt;
using System.Text;
using uPLibrary.Networking.M2Mqtt.Messages;
using Unity.Barracuda;
using System.Drawing;

public class DuDoan : MonoBehaviour
{
    public Button btnDanger;
    public Button btnHuman;
    public Button btnAnimal;
    public Button btnNormal;
    public Button btnSilent;
    public Button btnTraffic;

    public RawImage inputRawImage;
    public TMP_Text textPredict;
    public TMP_Text textLabelOld;
    public TMP_Text textStatus;
    public RawImage imageSave;
    public float getImgSeconds;
    public string pathModelDanger;
    public string pathModelAnimal;
    public int bufferSize = 20; // Chỉ cần lưu trữ 20 frames

    public NNModel modelAssetHuman;
    public NNModel modelAssetAnimal;

    private Model modelHuman;
    private Model modelAnimal;

    private IWorker workerHuman;
    private IWorker workerAnimal;


    private MqttClient m_client;
    public string broken_address = "io.adafruit.com";
    public string user_name = "hinody";
    public string user_key = "aio_MWQa1007PgC9MYqi6ZpsYghLKdj2";


    private InferenceSession sessionDanger;
    private InferenceSession sessionAnimal;
    private Queue<Texture2D> frameBuffer = new Queue<Texture2D>();



    void Start()
    {
        connect_server();

        btnDanger.interactable = false;
        btnHuman.interactable = false;
        btnAnimal.interactable = false;
        btnNormal.interactable = false;
        btnSilent.interactable = false;
        btnTraffic.interactable = false;

        textPredict.text = "Đang tải mô hình";
        Thread.Sleep(2000);
        string modelPathDanger = Path.Combine(Application.streamingAssetsPath, pathModelDanger);
        string modelPathAnimal = Path.Combine(Application.streamingAssetsPath, pathModelAnimal);

        sessionDanger = new InferenceSession(modelPathDanger);
        sessionAnimal = new InferenceSession(modelPathAnimal);

        Debug.Log(modelPathAnimal);
        Debug.Log(modelPathDanger);


        textPredict.text = "Chờ kết quả dự đoán !";
        InvokeRepeating(nameof(CaptureFrame), 0f, getImgSeconds);

        InvokeRepeating(nameof(DuDoanAnimals), 0f, 3);
    }

    private void Update()
    {
        
        // Kiểm tra nếu đã đủ 20 frames thì dừng thu thập và bắt đầu dự đoán
        if (frameBuffer.Count >= bufferSize)
        {
            var tensor = ProcessFrames();
            // Bắt đầu dự đoán sau khi thu thập đủ frames
            string res = predict_Rawimage(tensor);

            if (res != null)
            {
                textPredict.text = res;
                string check_s = "";
                if (res != check_s)
                {
                    check_s = res;
                    string format = $"{user_name}/feeds/cnm";
                    m_client.Publish(format, Encoding.UTF8.GetBytes(res), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, false);
                }
                SaveImage();
            }
            // Xóa frame buffer sau khi dự đoán
            frameBuffer.Clear();
        }


    }

    private void PredictHuman()
    {
        throw new NotImplementedException();
    }

    private void DuDoanAnimals()
    {
        // Lấy ảnh hiện tại
        Texture2D currentFrame = GetTextureFromRawImage(inputRawImage);
        if (currentFrame != null) {
            if (! btnSilent.IsInteractable())
            {
                currentFrame = ResizeTexture(currentFrame, 224, 224);
                var input = ConvertTextureToTensor(currentFrame);
                
                // Tạo biến đầu vào cho mô hình
                var inputs = new NamedOnnxValue[]
                {
                NamedOnnxValue.CreateFromTensor(sessionAnimal.InputNames[0], input)
                };
                // Chạy dự đoán
                using var results = sessionAnimal.Run(inputs);
                // Xử lý kết quả
                var resultTensor = results.First().AsTensor<float>();
                var outputArray = resultTensor.ToArray();
                var maxValue = outputArray.Max();
                var maxIndex = Array.IndexOf(outputArray, maxValue);

                string[] labels = { "Chó", "Voi", "Heo" };

                btnAnimal.interactable = true;
                SaveImage();
                textLabelOld.text =$" {labels[maxIndex]}";
            }
            else
            {
                btnAnimal.interactable = false;
            }
        }
    }

        private void CaptureFrame()
        {
            Texture2D currentFrame = GetTextureFromRawImage(inputRawImage);
            if (currentFrame != null)
            {
                currentFrame = ResizeTexture(currentFrame, 224, 224);
                AddFrameToBuffer(currentFrame);
            }
            textStatus.text = $"Đang thu thập dữ liệu: {frameBuffer.Count / 20.0 * 100}%";
        }

        private DenseTensor<float> ProcessFrames()
        {
            List<Texture2D> selectedFrames = frameBuffer.ToList();
            DenseTensor<float> tensor = ConvertFramesToTensor(selectedFrames);

            return tensor;
        }

        private void connect_server()
        {
            m_client = new MqttClient(broken_address);
            m_client.Connect(user_name, user_name, user_key);

            if (m_client.IsConnected)
            {
                textLabelOld.text = "Connected server adafruit IO !";
            }
            else
            {
                textLabelOld.text = "Fail when connect with server adafruit IO !";
            }
            Thread.Sleep(2000);

        }



        private DenseTensor<float> ConvertFramesToTensor(List<Texture2D> frames)
        {
            // Đảm bảo các frames đã được resize về 224224
            int numFrames = frames.Count;
            int frameHeight = 224; int frameWidth = 224; int channels = 3;

            // Khởi tạo DenseTensor với hình dạng {1, 20, 224, 224, 3}
            DenseTensor<float> tensor = new DenseTensor<float>(new[] { 1, numFrames, frameHeight, frameWidth, channels });

            for (int n = 0; n < numFrames; n++)
            {
                Texture2D frame = frames[n];
                Color32[] pixels = frame.GetPixels32();

                for (int y = 0; y < frameHeight; y++)
                {
                    for (int x = 0; x < frameWidth; x++)
                    {
                        int pixelIndex = y * frameWidth + x;
                        Color32 color = pixels[pixelIndex];

                        tensor[0, n, y, x, 0] = color.r / 255.0f;
                        tensor[0, n, y, x, 1] = color.g / 255.0f;
                        tensor[0, n, y, x, 2] = color.b / 255.0f;
                    }
                }
            }

            return tensor;

        }

    private DenseTensor<float> ConvertTextureToTensor(Texture2D texture)
    {
        int width = texture.width;
        int height = texture.height;

        Color32[] pixels = texture.GetPixels32();
        DenseTensor<float> tensor = new DenseTensor<float>(new[] { 1, height, width, 3});

        for (int y = 0;  y < height; y++)
        {
            for (int x = 0; x < width ; x++)
            {
                int pixelIndex = y * width + x;
                UnityEngine.Color pixel = pixels[pixelIndex];

                tensor[0, y, x, 0] = pixel.r / 255.0f;
                tensor[0, y, x, 1] = pixel.g / 255.0f;
                tensor[0, y, x, 2] = pixel.b / 255.0f;

            }
        }

        return tensor;
    }




        Texture2D GetTextureFromRawImage(RawImage rawImage)
        {
            // Kiểm tra xem rawImage có texture hay không
            if (rawImage.texture != null)
            {
                // Lấy Texture từ RawImage
                Texture originalTexture = rawImage.texture;
                // Tạo một Texture2D mới với kích thước của originalTexture
                Texture2D newTexture = new Texture2D(originalTexture.width, originalTexture.height, TextureFormat.RGBA32, false);
                // Sao chép các pixel từ originalTexture sang newTexture
                RenderTexture renderTexture = RenderTexture.GetTemporary(
                                                            originalTexture.width, originalTexture.height, 0,
                                                            RenderTextureFormat.Default,
                                                            RenderTextureReadWrite.Linear);
                Graphics.Blit(originalTexture, renderTexture);
                RenderTexture previousRenderTexture = RenderTexture.active;
                RenderTexture.active = renderTexture;
                newTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                newTexture.Apply();
                RenderTexture.active = previousRenderTexture;
                RenderTexture.ReleaseTemporary(renderTexture);
                return newTexture;
            }
            return null;
        }

        Texture2D ResizeTexture(Texture2D texture, int width, int height)
        {
            RenderTexture rt = RenderTexture.GetTemporary(width, height);
            Graphics.Blit(texture, rt);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D resizedTexture = new Texture2D(width, height);
            resizedTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            resizedTexture.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);

            return resizedTexture;
        }

        private void AddFrameToBuffer(Texture2D frame)
        {
            if (frameBuffer.Count >= bufferSize)
            {
                Texture2D oldFrame = frameBuffer.Dequeue();
                Destroy(oldFrame);
            }
            frameBuffer.Enqueue(frame);
        }


        private string predict_Rawimage(DenseTensor<float> inputTensor)
        {
            // Tạo biến đầu vào cho mô hình
            var inputs = new NamedOnnxValue[]
            {
            NamedOnnxValue.CreateFromTensor(sessionDanger.InputNames[0], inputTensor)
            };

            // Chạy dự đoán
            using var results = sessionDanger.Run(inputs);

            // Xử lý kết quả
            var resultTensor = results.First().AsTensor<float>();
            var outputArray = resultTensor.ToArray();
            var maxValue = outputArray.Max();
            var maxIndex = Array.IndexOf(outputArray, maxValue);

            string[] labels = { "Hoạt động tiêu chuẩn", "Hoạt động nguy hiểm", "Không có hoạt động", "Hoạt động giao thông" };

            updateStatusButton(maxIndex);

            textLabelOld.text = labels[maxIndex];

            return $"{labels[maxIndex]} Độ chính xác: {Math.Round(maxValue * 100, 2)}%";
        }

        private void updateStatusButton(int maxIndex)
        {
            btnNormal.interactable = false;
            btnDanger.interactable = false;
            btnSilent.interactable = false;
            btnTraffic.interactable = false;

            if (maxIndex == 0)
            {
                btnNormal.interactable = true;
            }
            else if (maxIndex == 1)
            {
                btnDanger.interactable = true;
            }
            else if (maxIndex == 2)
            {
                btnSilent.interactable = true;
            }
            else if (maxIndex == 3) {
                btnTraffic.interactable = true;
            }

        }

        private void SaveImage()
        {
            imageSave.texture = GetTextureFromRawImage(inputRawImage);

        }


        void OnDestroy()
        {
            sessionDanger.Dispose();
            sessionAnimal.Dispose();
        }
}

