using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PostEffectController : MonoBehaviour
{
  [Header("Custom")]
 
  [SerializeField] private ScriptableRendererFeature _fullscreenCustom;
  [SerializeField] private Material _material;

  Vector2 center=new Vector2(0.25f,0.25f);
  float radiusMin=0.1f;
  float radiusMax=0.5f;

  // Start is called before the first frame update
  void Start()
  {

  }

  // Update is called once per frame
  void Update()
  {
    if(this._material)
    {
      this._material.SetVector("_center",center);
      this._material.SetFloat("_radiusMin",radiusMin);
      this._material.SetFloat("_radiusMax",radiusMax);
      // Debug.Log("OK");
    }else{
      Debug.LogWarning("material not found");
    }
    

  }
}
