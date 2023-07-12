using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class ScreenShot : MonoBehaviour
{
    public Camera camera;       //보여지는 카메라.

    private int resWidth = 3840;
    private int resHeight = 2160;
    public string path;

    public string shotID;

    public string filename;
    // Use this for initialization
    void Start()
    {
        //Debug.Log("ScreenShot Start");

        // resWidth = 3840;
        // resHeight = 2160;
        //path = Application.dataPath + "/ScreenShot/";
        //Debug.Log(path);
    }

    public void init()
    {
        resWidth = 3840;
        resHeight = 2160;
    }

    // Update is called once per frame
    
    //void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.Z))
    //    {
    //        ClickScreenShot();
    //    }
    //}

    public void ClickScreenShot()
    {
        DirectoryInfo dir = new DirectoryInfo(path);
        if (!dir.Exists)
        {
            Directory.CreateDirectory(path);
        }
        string name;
        //name = $"{path}{filename}_{shotID}.jpg";
        name = Path.Combine(path, $"{filename}{shotID}.jpg");

        //Debug.Log("NAME : " + name);

        //RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
        //camera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
        Rect rec = new Rect(0, 0, screenShot.width, screenShot.height);
        camera.Render();
        RenderTexture.active = camera.targetTexture;
        screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
        screenShot.Apply();

        byte[] bytes = screenShot.EncodeToJPG(100);
        File.WriteAllBytes(name, bytes);
    }
}