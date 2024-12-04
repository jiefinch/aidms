using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorChanger : MonoBehaviour
{
    // ===============
    private MaterialPropertyBlock propertyBlock;
    public float brightness = 0.5f;
    public float R; 

    // Start is called before the first frame update
    void Start()
    {
        propertyBlock = new MaterialPropertyBlock();
        UpdateColor();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateColor();
    }

    void UpdateColor()
    {
        propertyBlock.SetFloat("_ratio", R);
        propertyBlock.SetFloat("_brightness", brightness);
        // Apply the property block to the renderer
        // Apply the MaterialPropertyBlock to the GameObject
        GetComponent<SpriteRenderer>().SetPropertyBlock(propertyBlock);
    }

}
