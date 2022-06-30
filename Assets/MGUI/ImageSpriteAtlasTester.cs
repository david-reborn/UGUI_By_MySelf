
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Myd.UI
{

    /// <summary>
    /// 测试SpriteAtlas
    /// </summary>
    public class ImageSpriteAtlasTester : MonoBehaviour
    {
        public Canvas canvas;
        public GameObject image;
        void OnEnable()
        {
            SpriteAtlasManager.atlasRequested += RequestAtlas;
        }

        void OnDisable()
        {
            SpriteAtlasManager.atlasRequested -= RequestAtlas;
        }

        void RequestAtlas(string tag, System.Action<SpriteAtlas> callback)
        {
            Debug.Log("RequestAtlas--" + tag);
            var sa = Resources.Load<SpriteAtlas>(tag);
            callback(sa);
        }

        public void Update()
        {
            if (Input.GetKeyUp(KeyCode.Alpha1))
            {
                SpriteAtlas obj = Resources.Load<SpriteAtlas>("SpriteAtlas");
                
                //Sprite sprite = obj.GetSprite("common_item_armor");
                Debug.Log($"Load({obj.name})");
            }
            if (Input.GetKeyUp(KeyCode.Alpha2))
            {
                GameObject prefab2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Image.prefab");
                GameObject go2 = Instantiate(prefab2);
                go2.transform.SetParent(canvas.transform, false);
                Debug.Log($"Start3");

                this.image = go2;
            }

            if (Input.GetKeyUp(KeyCode.Alpha3))
            {
                GameObject prefab1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MImage.prefab");
                GameObject go1 = Instantiate(prefab1);
                go1.transform.SetParent(canvas.transform, false);
                this.image = go1;
            }
        }
    }

}
