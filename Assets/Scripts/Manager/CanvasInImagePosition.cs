using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class CanvasInImagePosition : MonoBehaviour
{
    public MainManager mainManager;

    public string filename;

    [SerializeField]
    private Canvas Canvs = default;
    [SerializeField]
    private CanvasScaler scaler = default;

    [SerializeField]
    public RectTransform Image = default;

    public Camera camera;

    public string path;

    //private Camera Camera
    //{
    //    get
    //    {
    //        return Camera.main;
    //    }
    //}


    public void Start()
    {
        path = Application.dataPath + "/ScreenShot/";

        DirectoryInfo dir = new DirectoryInfo(path);

        if (!dir.Exists)
        {
            Directory.CreateDirectory(path);
        }
    }


    [ContextMenu("계산")]
    public void GetCalcPosition()
    {
        //Debug.Log($" Camera.WorldToViewportPoin: {Camera.WorldToViewportPoint(Image.transform.position)}");
        StartCoroutine(TakeSnapShotAndSave());

    }



    //Using a Coroutine instead of normal method
    public IEnumerator TakeSnapShotAndSave()
    {
        //Code will throw error at runtime if this is removed
        yield return new WaitForEndOfFrame();

        //Get the corners of RectTransform rect and store it in a array vector
        Vector3[] corners = new Vector3[4];
        //RectTransform _objToScreenshot = Image.GetComponent<RectTransform>();
        RectTransform _objToScreenshot = Image;
        _objToScreenshot.GetWorldCorners(corners);


        Vector3[] worldToScreenPointCorners = new Vector3[4];
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 screenPoint = camera.WorldToScreenPoint(corners[i]);

            /*Vector2 result;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(transform.parent.GetComponent<RectTransform>(), screenPoint, Camera, out result);*/

            //worldToScreenPointCorners[i]= new Vector3(result.x, result.y, 0f);
            worldToScreenPointCorners[i] = screenPoint;
        }

        /*
        RectTransform mCanvas = Canvs.GetComponent<RectTransform>();
        float scalerX = mCanvas.rect.width / (float)Camera.pixelWidth;//(float)Screen.width / 1080f;
        float scalerY = mCanvas.rect.height / (float)Camera.pixelHeight;//(float)Screen.height / 1920f;
        */
        float scalerX = GetScale(Screen.width, Screen.height, scaler.referenceResolution, scaler.matchWidthOrHeight);
        float scalerY = GetScale(Screen.width, Screen.height, scaler.referenceResolution, scaler.matchWidthOrHeight);

        //Remove 100 and you will get error
        int width = (int)(_objToScreenshot.rect.width * scalerX);//((int)corners[3].x - (int)corners[0].x) - 100;
        int height = (int)(_objToScreenshot.rect.height * scalerY);// (int)corners[1].y - (int)corners[0].y;
        /* int width = ((int)worldToScreenPointCorners[3].x - (int)worldToScreenPointCorners[0].x);
         int height =  (int)worldToScreenPointCorners[1].y - (int)worldToScreenPointCorners[0].y;*/

        var startX = worldToScreenPointCorners[0].x;
        var startY = worldToScreenPointCorners[0].y;

        //Make a temporary texture and read pixels from it
        Rect pixelsRect = new Rect(startX, startY, width, height);
        //Rect pixelsRect = new Rect(startX , startY , width , height);
        camera.Render();
        RenderTexture.active = camera.targetTexture;
        Texture2D ss = new Texture2D(width, height, TextureFormat.RGB24, false);
        ss.ReadPixels(pixelsRect, 0, 0);
        ss.Apply();

        Debug.Log("Start X : " + startX + " Start Y : " + startY);
        Debug.Log("Screen Width : " + Screen.width + " Screen Height : " +
        Screen.height);
        Debug.Log("Texture Width : " + width + " Texture Height : " + height);


        Debug.Log($"Draw Rect : {pixelsRect}");

        //Save the screenshot to disk
        byte[] byteArray = ss.EncodeToJPG(100);
        string savePath = path + filename + ".jpg";
        System.IO.File.WriteAllBytes(savePath, byteArray);
        Debug.Log("Screenshot Path : " + savePath);

        mainManager.viewLoading.Open();
        mainManager.loadMsg.text = "";

        yield return new WaitForSeconds(1);

        mainManager.viewLoading.Close();

        // 저장 후 토스트 팝업

        CommonToastPopup popup = Instantiate(mainManager.commonToastPopup, mainManager.contents);

        popup.msg = "저장되었습니다.";

        popup.Init();        

        // Destroy texture to avoid memory leaks
        if (Application.isPlayer)
            Destroy(ss);
    }
    private float GetScale(int width, int height, Vector2 scalerReferenceResolution, float scalerMatchWidthOrHeight)
    {
        return Mathf.Pow(width / scalerReferenceResolution.x, 1f - scalerMatchWidthOrHeight) *
               Mathf.Pow(height / scalerReferenceResolution.y, scalerMatchWidthOrHeight);
    }
}
