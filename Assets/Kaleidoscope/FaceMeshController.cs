// based on FaceMesh/FaceMeshSample.cs
using System.Linq;
using TensorFlowLite;
using TextureSource;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(VirtualTextureSource))]
public sealed class FaceMeshController : MonoBehaviour
{
    [Header("Model Settings")]
    [SerializeField, FilePopup("*.tflite")]
    private string faceModelFile = "coco_ssd_mobilenet_quant.tflite";

    [SerializeField, FilePopup("*.tflite")]
    private string faceMeshModelFile = "coco_ssd_mobilenet_quant.tflite";

    [SerializeField]
    private bool useLandmarkToDetection = true;

    [Header("UI")]
    [SerializeField]
    private RawImage inputPreview = null;

    [SerializeField]
    private RawImage fullInputPreview = null;

    [SerializeField]
    private Material faceMaterial = null;

    [SerializeField] private Material _kaleidoscopeMaterial;

    [SerializeField]
    private bool canDrawDebug = false;

    private FaceDetect faceDetect;
    private FaceMesh faceMesh;
    private PrimitiveDraw draw;
    private MeshFilter faceMeshFilter;
    private Vector3[] faceVertices;
    private FaceDetect.Result detectionResult;
    private FaceMesh.Result meshResult;
    private readonly Vector3[] rtCorners = new Vector3[4];
    private Material previewMaterial;
    private Material fullPreviewMaterial;

    private float targetRadius=0.0f;
    private float currentRadius=0.0f;

    private float previousTimeForRadius=0.0f;

    private const float RADIUS_FADE_VELOCITY=1.0f;
    private const float RADIUS_MAX=1.0f;

    private void Start()
    {
        faceDetect = new FaceDetect(faceModelFile);
        faceMesh = new FaceMesh(faceMeshModelFile);
        draw = new PrimitiveDraw(Camera.main, gameObject.layer);

        previewMaterial = new Material(Shader.Find("Hidden/TFLite/InputMatrixPreview"));
        inputPreview.material = previewMaterial;

        fullPreviewMaterial = new Material(Shader.Find("Hidden/TFLite/InputMatrixPreview"));
        fullInputPreview.material = fullPreviewMaterial;

        // Create Face Mesh Renderer
        {
            var go = new GameObject("Face");
            go.transform.SetParent(transform);
            var faceRenderer = go.AddComponent<MeshRenderer>();
            faceRenderer.material = faceMaterial;

            faceMeshFilter = go.AddComponent<MeshFilter>();
            faceMeshFilter.sharedMesh = FaceMeshBuilder.CreateMesh();

            faceVertices = new Vector3[FaceMesh.KEYPOINT_COUNT];
        }

        if (TryGetComponent(out VirtualTextureSource source))
        {
            source.OnTexture.AddListener(OnTextureUpdate);
        }
    }

    private void OnDestroy()
    {
        if (TryGetComponent(out VirtualTextureSource source))
        {
            source.OnTexture.RemoveListener(OnTextureUpdate);
        }

        faceDetect?.Dispose();
        faceMesh?.Dispose();
        draw?.Dispose();
        Destroy(previewMaterial);
        Destroy(fullPreviewMaterial);
    }

    private void Update()
    {
        DrawResults(detectionResult, meshResult);
    }

    private void OnTextureUpdate(Texture texture)
    {
        if (detectionResult == null || !useLandmarkToDetection)
        {
            faceDetect.Run(texture);

            inputPreview.texture = texture;
            previewMaterial.SetMatrix("_TransformMatrix", faceDetect.InputTransformMatrix);
            fullInputPreview.texture = texture;

            // Debug.Log(faceDetect.InputTransformMatrix.ToString());
            if (fullInputPreview.TryGetComponent(out UnityEngine.UI.AspectRatioFitter aspectRatioFilter))
            {
                float aspect=faceDetect.InputTransformMatrix.m11/faceDetect.InputTransformMatrix.m00;
                aspectRatioFilter.aspectRatio=aspect;
            }

            detectionResult = faceDetect.GetResults().FirstOrDefault();

            if (detectionResult == null)
            {
                return;
            }
        }

        faceMesh.Face = detectionResult;
        faceMesh.Run(texture);
        meshResult = faceMesh.GetResult();

        if (meshResult.score < 0.5f)
        {
            detectionResult = null;
            return;
        }

        if (useLandmarkToDetection)
        {
            detectionResult = meshResult.ToDetection();
        }
    }

    private Rect RemapRect(Vector3 min,Vector3 max,Rect originalRect)
    {
        Rect rectFlipedY=originalRect.FlipY();
        Rect rect = MathTF.Lerp((Vector3)min, (Vector3)max, rectFlipedY);
        
        // クランプされてしまうので上書きする
        rect.x=Mathf.LerpUnclamped(min.x,max.x,rectFlipedY.x);
        rect.y=Mathf.LerpUnclamped(min.y,max.y,rectFlipedY.y);
        return rect;
    }
    private float3 RemapPoint(float3 min,float3 max,Vector2 originalPoint)
    {
        float3 point=math.lerp(min, max, new float3(originalPoint.x, 1f - originalPoint.y, 0));
        return point;
    }
    

    private void DrawResults(FaceDetect.Result detection, FaceMesh.Result face)
    {
        inputPreview.rectTransform.GetWorldCorners(rtCorners);
        float3 min = rtCorners[0];
        float3 max = rtCorners[2];

        if(!this._kaleidoscopeMaterial)
        {
            Debug.LogWarning("_kaleidoscopeMaterial not found");
        }

        // Draw Face Detection
        if (detection != null)
        {
            // draw.color = Color.blue;
            draw.color = new Color(3,0,0,1);
            // draw.color = new Color(0,3,0,1);
            Rect rect = this.RemapRect(min, max, detection.rect);

            if(this.canDrawDebug)
            {
                draw.Rect(rect, 0.05f);
                foreach (Vector2 p in detection.keypoints)
                {
                    float3 point=this.RemapPoint(min,max,p);
                    draw.Point(point, 0.1f);
                }
                draw.Apply();
            }
            if(this._kaleidoscopeMaterial)
            {
                Vector2 center01=detection.rect.FlipY().center;
                float aspect=faceDetect.InputTransformMatrix.m11/faceDetect.InputTransformMatrix.m00;
                center01.x= (center01.x - 0.5f) / aspect + 0.5f;


                this._kaleidoscopeMaterial.SetVector("_center01",center01);
            // Debug.Log("OK");
            }
            this.targetRadius=FaceMeshController.RADIUS_MAX;
        }else{
            this.targetRadius=0.0f;
        }
        if(this._kaleidoscopeMaterial){
            float time=Time.realtimeSinceStartup;
            float deltaTime=time-this.previousTimeForRadius;
            float direction=Mathf.Sign(this.targetRadius - this.currentRadius);

            this.currentRadius+=deltaTime*FaceMeshController.RADIUS_FADE_VELOCITY*direction;
            this.currentRadius=Mathf.Clamp(this.currentRadius,0.0f,FaceMeshController.RADIUS_MAX);

            float radiusMin01=0.0f;
            float radiusMax01=this.currentRadius;
            this._kaleidoscopeMaterial.SetFloat("_radiusMin01",radiusMin01);
            this._kaleidoscopeMaterial.SetFloat("_radiusMax01",radiusMax01);

            this.previousTimeForRadius=time;
        }

        if (face != null)
        {
            if(this.canDrawDebug)
            {
                // Draw face
                draw.color = Color.green;
                float zScale = (max.x - min.x) / 2;
                for (int i = 0; i < face.keypoints.Length; i++)
                {
                    float3 kp = face.keypoints[i];
                    float3 p = math.lerp(min, max, kp);
                    // TODO: projection is not correct
                    p.z = kp.z * zScale;

                    faceVertices[i] = p;
                    if(this.canDrawDebug)
                    {
                        draw.Point(p, 0.05f);
                    }
                }
                draw.Apply();
            }

            // Update Mesh
            FaceMeshBuilder.UpdateMesh(faceMeshFilter.sharedMesh, faceVertices);
        }


    }
}
