using UnityEngine;
using System.Collections;
using System;
 
/// <summary>
/// 反射效果
/// </summary>
[AddComponentMenu("GameCore/SpecialEffect/Reflection")]
[ExecuteInEditMode]
public class ReflectionEffect : MonoBehaviour
{
    public Camera MainCamera;
    public bool DisablePixelLights = true;
    public int TextureSize = 512;
    public float ClipPlaneOffset = 0;
    public LayerMask ReflectLayers = -1;
 
    private Hashtable m_ReflectionCameras = new Hashtable(); // Camera -> Camera table
    private RenderTexture m_ReflectionTexture = null;
    private int m_OldReflectionTextureSize = 0;
 
    private static bool s_InsideRendering = false;
 
    // This is called when it's known that the object will be rendered by some
    // camera. We render reflections and do other updates here.
    // Because the script executes in edit mode, reflections for the scene view
    // camera will just work!
    public void OnWillRenderObject()
    {
        if (!enabled || !GetComponent<Renderer>() || !GetComponent<Renderer>().sharedMaterial || !GetComponent<Renderer>().enabled)
            return;
 
        Camera cam = MainCamera;
        if (!cam)
            return;
 
        // Safeguard from recursive reflections.        
        if (s_InsideRendering)
            return;
        s_InsideRendering = true;
 
        Camera reflectionCamera;
        CreateMirrorObjects(cam, out reflectionCamera);
 
        // find out the reflection plane: position and normal in world space
        Vector3 pos = transform.position;
        Vector3 normal = transform.up;
 
 
        // Optionally disable pixel lights for reflection
        int oldPixelLightCount = QualitySettings.pixelLightCount;
        if (DisablePixelLights)
            QualitySettings.pixelLightCount = 0;
 
        CoreTool.CloneCameraModes(cam, reflectionCamera);
 
        // Render reflection
        // Reflect camera around reflection plane
        float d = -Vector3.Dot(normal, pos) - ClipPlaneOffset;
        Vector4 reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);
 
 
        Matrix4x4 reflection = CoreTool.CalculateReflectionMatrix(Matrix4x4.zero, reflectionPlane);
 
 
 
 
        Vector3 oldpos = cam.transform.position;
        Vector3 newpos = reflection.MultiplyPoint(oldpos);
        reflectionCamera.worldToCameraMatrix = cam.worldToCameraMatrix * reflection;
 
 
 
        // Setup oblique projection matrix so that near plane is our reflection
        // plane. This way we clip everything below/above it for free.
        Vector4 clipPlane = CoreTool.CameraSpacePlane(reflectionCamera, pos, normal, 1.0f, ClipPlaneOffset);
 
        Matrix4x4 projection = cam.projectionMatrix;
 
        projection = CoreTool.CalculateObliqueMatrix(projection, clipPlane,1);
 
        reflectionCamera.projectionMatrix = projection;
 
        reflectionCamera.cullingMask = ~(1 << 4) & ReflectLayers.value; // never render water layer
        reflectionCamera.targetTexture = m_ReflectionTexture;
 
        GL.SetRevertBackfacing(true);
        reflectionCamera.transform.position = newpos;
        Vector3 euler = cam.transform.eulerAngles;
        reflectionCamera.transform.eulerAngles = new Vector3(0, euler.y, euler.z);
        reflectionCamera.Render();
        reflectionCamera.transform.position = oldpos;
        GL.SetRevertBackfacing(false);
        Material[] materials = GetComponent<Renderer>().sharedMaterials;
        foreach (Material mat in materials)
        {
            if (mat.HasProperty("_ReflectionTex"))
            {
                mat.SetTexture("_ReflectionTex", m_ReflectionTexture);
            }

        }
 
        // Set matrix on the shader that transforms UVs from object space into screen
        // space. We want to just project reflection texture on screen.
        Matrix4x4 scaleOffset = Matrix4x4.TRS(
            new Vector3(0.5f, 0.5f, 0.5f), Quaternion.identity, new Vector3(0.5f, 0.5f, 0.5f));
        Vector3 scale = transform.lossyScale;
        Matrix4x4 mtx = transform.localToWorldMatrix * Matrix4x4.Scale(new Vector3(1.0f / scale.x, 1.0f / scale.y, 1.0f / scale.z));
        mtx = scaleOffset * cam.projectionMatrix * cam.worldToCameraMatrix * mtx;
        foreach (Material mat in materials)
        {
            mat.SetMatrix("_ProjMatrix", mtx);
        }
        // Restore pixel light count
        if (DisablePixelLights)
            QualitySettings.pixelLightCount = oldPixelLightCount;
 
        s_InsideRendering = false;
    }
 
 
    // Cleanup all the objects we possibly have created
    void OnDisable()
    {
        if (m_ReflectionTexture)
        {
            DestroyImmediate(m_ReflectionTexture);
            m_ReflectionTexture = null;
        }
        foreach (DictionaryEntry kvp in m_ReflectionCameras)
            DestroyImmediate(((Camera)kvp.Value).gameObject);
        m_ReflectionCameras.Clear();
    }
 
    // On-demand create any objects we need
    private void CreateMirrorObjects(Camera currentCamera, out Camera reflectionCamera)
    {
        reflectionCamera = null;
 
        // Reflection render texture
        if (!m_ReflectionTexture || m_OldReflectionTextureSize != TextureSize)
        {
            if (m_ReflectionTexture)
                DestroyImmediate(m_ReflectionTexture);
            m_ReflectionTexture = new RenderTexture(TextureSize, TextureSize,0);
            m_ReflectionTexture.name = "__MirrorReflection" + GetInstanceID();
            m_ReflectionTexture.isPowerOfTwo = true;
            m_ReflectionTexture.hideFlags = HideFlags.DontSave;
            m_ReflectionTexture.antiAliasing = 4;
            m_ReflectionTexture.anisoLevel = 0;
            m_OldReflectionTextureSize = TextureSize;
        }
 
        // Camera for reflection
        reflectionCamera = m_ReflectionCameras[currentCamera] as Camera;
        if (!reflectionCamera) // catch both not-in-dictionary and in-dictionary-but-deleted-GO
        {
            GameObject go = new GameObject("Mirror Refl Camera id" + GetInstanceID() + " for " + currentCamera.GetInstanceID(), typeof(Camera), typeof(Skybox));
            reflectionCamera = go.GetComponent<Camera>();
            reflectionCamera.enabled = false;
            reflectionCamera.transform.position = transform.position;
            reflectionCamera.transform.rotation = transform.rotation;
            reflectionCamera.gameObject.AddComponent<FlareLayer>();
            go.hideFlags = HideFlags.HideAndDontSave;
            m_ReflectionCameras[currentCamera] = reflectionCamera;
        }
    }
}
