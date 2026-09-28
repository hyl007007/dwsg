using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class 创建武将脚本 : MonoBehaviour
{
    public Image 将领头像;
    private List<Texture2D> texure = new List<Texture2D>();
    private List<Sprite> sprites = new List<Sprite>();


    void Start()
    {
        LoadTexture();
        print(Application.temporaryCachePath);
    }

    void Update()
    {
        
    }

    public void 切换头像()
    {
        if (sprites.Count > 0)
        {
            将领头像.sprite = sprites[0];
            print(sprites.Count);
        }
    }

    private static byte[] GetImageByte(string imagePath)
    {
        FileStream files = new FileStream(imagePath, FileMode.Open);
        byte[] bytes = new byte[files.Length];
        files.Read(bytes,0,bytes.Length);
        files.Close();
        return bytes;
    }

    private List<string> getImagePath()
    {
        List<string> filePath = new List<string>();
        string imgType = "*.JPG|*.PNG|*.jpg|*.png";
        string[] imageType = imgType.Split('|');
        for (int i = 0; i < imageType.Length; i++)
        {
            string[] dirs = Directory.GetFiles(Application.temporaryCachePath+"/头像资源", imageType[i]);
            for (int j = 0; j < dirs.Length; j++)
            {
                filePath.Add(dirs[j]);
            }
        }

        return filePath;
    }

    private void LoadTexture()
    {
        texure.Clear();
        sprites.Clear();
        List<string> filePaths = getImagePath();
        for (int i = 0; i < filePaths.Count; i++)
        {
            Texture2D texture2 = new Texture2D(48, 48);
            texture2.LoadImage(GetImageByte(filePaths[i]));
            texure.Add(texture2);
            Sprite sprite = Sprite.Create(texture2, new Rect(0,0,48,48), Vector2.zero);
            sprites.Add(sprite);
        }
    }
}
