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

    private float targetFadeFactor=0.0f;
    private float currentFadeFactor=0.0f;

    private float previousTimeForRadius=0.0f;

    private const float RADIUS_FADE_DURATION=1.0f;

    private const float RADIUS_MAIN_MAX=1.0f;

    private float dummyPhaseAngle=0.0f;
    private const float DUMMY_PHASE_ANGULAR_VELOCITY=10f*Mathf.Deg2Rad;

    private const float RADIUS_DUMMY_MAX=0.75f;


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
            this.targetFadeFactor=1.0f;
        }else{
            this.targetFadeFactor=0.0f;
        }
        if(this._kaleidoscopeMaterial){
            float time=Time.realtimeSinceStartup;
            float deltaTime=time-this.previousTimeForRadius;
            float direction=Mathf.Sign(this.targetFadeFactor - this.currentFadeFactor);

            this.currentFadeFactor+=deltaTime/FaceMeshController.RADIUS_FADE_DURATION*direction;
            this.currentFadeFactor=Mathf.Clamp(this.currentFadeFactor,0.0f,1.0f);

            float radiusMin01=0.0f;
            float radiusMax01=this.currentFadeFactor * FaceMeshController.RADIUS_MAIN_MAX;
            float unitLength01=EasingFunction.EaseOutSine(0.0f,1.0f,this.currentFadeFactor) * 0.65f;

            

            Vector2 DUMMY_BASE_POSITION=new Vector2(0.5f,0.5f);
            Vector2 DUMMY_OFFSET_POSITION=new Vector2(0.4f,0.4f);
            this.dummyPhaseAngle=DUMMY_PHASE_ANGULAR_VELOCITY*time;
            float s=Mathf.Sin(this.dummyPhaseAngle);
            float c=Mathf.Cos(this.dummyPhaseAngle);

            Vector2 center02;
            float radiusMin02=0.0f;
            float radiusMax02;
            float unitLength02;
            if(0.0f<s){
                center02=Vector2.Scale(DUMMY_OFFSET_POSITION,new Vector2(-1.0f,-1.0f)) + DUMMY_BASE_POSITION;
                radiusMax02=s*RADIUS_DUMMY_MAX;
                unitLength02=s*0.2f;
            }else{
                center02=Vector2.Scale(DUMMY_OFFSET_POSITION,new Vector2(1.0f,1.0f))+DUMMY_BASE_POSITION;
                radiusMax02=s*-1.0f*RADIUS_DUMMY_MAX;
                unitLength02=s*-1.0f*0.2f;
            }

            Vector2 center03;
            float radiusMin03=0.0f;
            float radiusMax03;
            float unitLength03;
            if(0.0f<c){
                center03=Vector2.Scale(DUMMY_OFFSET_POSITION,new Vector2(1.0f,-1.0f)) + DUMMY_BASE_POSITION;
                radiusMax03=c*RADIUS_DUMMY_MAX;
                unitLength03=c*0.2f;
            }else{
                center03=Vector2.Scale(DUMMY_OFFSET_POSITION,new Vector2(-1.0f,1.0f)) + DUMMY_BASE_POSITION;
                radiusMax03=c*-1.0f*RADIUS_DUMMY_MAX;
                unitLength03=c*-1.0f*0.2f;
            }

            this._kaleidoscopeMaterial.SetFloat("_radiusMin01",radiusMin01);
            this._kaleidoscopeMaterial.SetFloat("_radiusMax01",radiusMax01);
            this._kaleidoscopeMaterial.SetFloat("_unitLength01",unitLength01);

            this._kaleidoscopeMaterial.SetVector("_center02",center02);
            this._kaleidoscopeMaterial.SetFloat("_radiusMin02",radiusMin02);
            this._kaleidoscopeMaterial.SetFloat("_radiusMax02",radiusMax02);
            this._kaleidoscopeMaterial.SetFloat("_unitLength02",unitLength02);

            this._kaleidoscopeMaterial.SetVector("_center03",center03);
            this._kaleidoscopeMaterial.SetFloat("_radiusMin03",radiusMin03);
            this._kaleidoscopeMaterial.SetFloat("_radiusMax03",radiusMax03);
            this._kaleidoscopeMaterial.SetFloat("_unitLength03",unitLength03);

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
