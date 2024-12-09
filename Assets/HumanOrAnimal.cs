using TMPro;
using Unity.Barracuda;
using UnityEngine;
using UnityEngine.UI;

public class HumanOrAnimal : MonoBehaviour
{

    public Button buttonHuman;
    public Button buttonAnimal;
    
    public NNModel modelAssetHuman;
    public NNModel modelAssetAnimal;

    public RawImage rawImage;
    public TMP_Text text;

    private Model modelHuman;
    private Model modelAnimal;

    private IWorker workerHuman;
    private IWorker workerAnimal;

    // Start is called before the first frame update
    void Start()
    {
        modelHuman = ModelLoader.Load(modelAssetHuman);

        var workerType = WorkerFactory.Type.ComputePrecompiled;
        workerHuman = WorkerFactory.CreateWorker(workerType, modelHuman);

        InvokeRepeating(nameof(Predict), 0f, 1f);
    }

    void Predict()
    {
            var image = ResizeTexture((Texture2D) rawImage.texture, 224, 224);

            var input = new Tensor(image, channels: 3);

            Debug.Log(input.shape);

            workerHuman.Execute(input);
            Tensor output = workerHuman.PeekOutput();

            int pridictLabel = output.ArgMax()[0];

            string[] labels = {"Chó", "Voi", "Heo"};

            text.text = labels[pridictLabel];
            

            buttonHuman.interactable = true;
            Debug.Log(labels[pridictLabel]);

            input.Dispose();
            output.Dispose();
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

    private void OnDestroy()
    {
        workerHuman.Dispose();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
