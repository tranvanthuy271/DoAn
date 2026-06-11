#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public class ExportSprites
{
    [MenuItem("Tools/Export Sprites")]
    static void Export()
    {
        Object[] sprites = Selection.GetFiltered(typeof(Sprite), SelectionMode.DeepAssets);

        foreach (Sprite sprite in sprites)
        {
            Texture2D tex = sprite.texture;

            Rect rect = sprite.rect;
            Texture2D newTex = new Texture2D((int)rect.width, (int)rect.height);

            Color[] pixels = tex.GetPixels(
                (int)rect.x,
                (int)rect.y,
                (int)rect.width,
                (int)rect.height
            );

            newTex.SetPixels(pixels);
            newTex.Apply();

            byte[] png = newTex.EncodeToPNG();
            File.WriteAllBytes("Assets/" + sprite.name + ".png", png);
        }

        { /* Export done */ }
    }
}
#endif
