
using System;
using UnityEngine;
using MVR.FileManagementSecure;

public static class ImageUtilities
{

    public static Texture2D LoadTGA(string fileName)
    {
        TargaImage tt = new TargaImage(fileName);
        return tt.unityTexture;
    }

    public static Texture2D loadPNGOrJPG(string filename)
    {
        Texture2D ret = loadPNGOrJPG(FileManagerSecure.ReadAllBytes(filename));
        return ret;
    }

    public static Texture2D loadBMP(string filename)
    {
        BMPLoader loader = new BMPLoader();
        BMPImage bmp =   loader.LoadBMP(FileManagerSecure.ReadAllBytes(filename));
        return bmp.ToTexture2D();
    }

    public static Texture2D LoadDXT(string fileName)
    {
        DDSImage dds = new DDSImage(FileManagerSecure.ReadAllBytes(fileName));
        return dds.UnityImage;      
    }

    public static Texture2D loadPNGOrJPG(byte[] fileData)
    {
        Texture2D te = null;
        if (fileData != null)
        {
            te = new Texture2D(2, 2);
            if (ImageConversion.LoadImage(te, fileData)) //..this will auto-resize the texture dimensions.
            {                              
                te.Apply();
            }
        }
        
        return te;
    }

    public static Texture2D getTex(string path, string originalFilename)
    {
        string[] pathElems = path.Split('/');

        string texPath = "";

        for (int k = 0; k < pathElems.Length - 1; k++)
        {
            if (texPath.Length == 0)
                texPath = pathElems[k];
            else
                texPath = texPath + "/" + pathElems[k];
        }

        string[] imgPath = originalFilename.Split('\\');

        texPath = texPath + "/" + imgPath[imgPath.Length - 1].Trim();
        Texture2D te = new Texture2D(2, 2);
        bool imageLoaded = false;
        try
        {
            if (texPath.ToLower().Contains(".tga"))
            {
                te = LoadTGA(texPath);                
            }
            else if (texPath.ToLower().Contains(".dds"))
            {
                te = LoadDXT(texPath);
            }
            else if (texPath.ToLower().Contains(".png") || texPath.ToLower().Contains(".jpg"))
            {
                te = loadPNGOrJPG(texPath);
            }
            else if (texPath.ToLower().Contains(".bmp"))
            {
                te = loadBMP(texPath);
                
            }

            imageLoaded = true;
        }
        catch (Exception e) {
            imageLoaded = false;
            //hmm maybe this isnt the format that was specified ? fall back and try to defaul to png/jpg.
        }

        if(!imageLoaded)
        {
            try
            {
                 te = loadPNGOrJPG(texPath);
                imageLoaded = true;
            }
            catch (Exception e)
            {
                imageLoaded = false;
                //nope this is fucked.
                SuperController.LogError(e.Message);
                SuperController.LogError(e.StackTrace);
            }
        }

        return te;
    }

    public static Texture2D getNormalTexture(Texture2D source)
    {
        Texture2D normalTexture = new Texture2D(source.width, source.height, TextureFormat.RGB24, true, true);
        Color theColour = new Color();
        for (int x = 0; x < source.width; x++)
        {
            for (int y = 0; y < source.height; y++)
            {
                theColour.r = 1;
                theColour.g = source.GetPixel(x, y).g;
                theColour.b = 1;
                theColour.a = source.GetPixel(x, y).r;
                normalTexture.SetPixel(x, y, theColour);
            }
        }
        normalTexture.Apply();
        return normalTexture;
    }

    public static bool HasAlpha(Color[] aColors)
    {
        for (int i = 0; i < aColors.Length; i++)
            if (aColors[i].a < 1f)
                return true;
        return false;
    }

    public static Texture2D getAlphaTexture(Texture2D source)
    {
        
        Texture2D normalTexture = new Texture2D(source.width, source.height, TextureFormat.Alpha8, false, true);
        bool hasAlpha = HasAlpha(source.GetPixels());

        Color theColour = new Color();
        for (int x = 0; x < source.width; x++)
        {
            for (int y = 0; y < source.height; y++)
            {
                if(hasAlpha)
                    theColour.a = source.GetPixel(x, y).a;// - source.GetPixel(x, y).a;
                else
                    theColour.a = source.GetPixel(x, y).r;// - source.GetPixel(x, y).a;

                theColour.r = 0;
                theColour.g = 0;
                theColour.b = 0;

                normalTexture.SetPixel(x, y, theColour);
            }
        }
        normalTexture.Apply();
        return normalTexture;
    }

    public static Texture2D convertToNormal(Texture2D loadedTexture)
    {

        Texture2D normalTexture = new Texture2D(loadedTexture.width, loadedTexture.height, TextureFormat.RGBA32, true, true);
        if (loadedTexture.format.Equals(TextureFormat.RGB24))
        {
            
            Color32[] colours = loadedTexture.GetPixels32();
            for (int i = 0; i < colours.Length; i++)
            {
                Color32 c = colours[i];


                byte b = c.r;
                byte r = c.b;

                c.b = b;
                c.r = r;
                c.a = r;

                //c.b = b;
                //c.r = r;
                //c.a = b;
                colours[i] = c;
            }
            normalTexture.SetPixels32(colours);
            normalTexture.Apply();

        }
        else
        {

            
            Color32[] colours = loadedTexture.GetPixels32();
            for (int i = 0; i < colours.Length; i++)
            {
                Color32 c = colours[i];
                c.a = c.r;
                colours[i] = c;
            }
            normalTexture.SetPixels32(colours);
            normalTexture.Apply();
        }
        return normalTexture;
    }
}
