using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tester : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var list = ListPool<Image>.Get();
        this.gameObject.GetComponentsInParent(false, list);
        list.ForEach(a=>Debug.Log(a.name));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
