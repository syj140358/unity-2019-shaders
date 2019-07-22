using System.Collections;
using System;
using UnityEngine;

/// <summary>
/// 工具类
/// </summary>
public static class CoreTool
{
    #region Config配置
    /// <summary>
    /// 验证当前文件是否为配置文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns></returns>
    public static bool IsConfig(string filePath)
    {
        return true;
    }
    #endregion

    #region Camera
    /// <summary>
    /// 将源摄像机状态克隆到目标相机
    /// </summary>
    /// <param name="src">源相机</param>
    /// <param name="dest">目标相机</param>
    public static void CloneCameraModes(Camera src, Camera dest)
    {
        if (dest == null)
            return;
        // set camera to clear the same way as current camera
        dest.clearFlags = src.clearFlags;
        dest.backgroundColor = src.backgroundColor;
        if (src.clearFlags == CameraClearFlags.Skybox)
        {
            Skybox sky = src.GetComponent(typeof(Skybox)) as Skybox;
            Skybox mysky = dest.GetComponent(typeof(Skybox)) as Skybox;
            if (!sky || !sky.material)
            {
                mysky.enabled = false;
            }
            else
            {
                mysky.enabled = true;
                mysky.material = sky.material;
            }
        }
        // update other values to match current camera.
        // even if we are supplying custom camera&projection matrices,
        // some of values are used elsewhere (e.g. skybox uses far plane)
        dest.depth = src.depth;
        dest.farClipPlane = src.farClipPlane;
        dest.nearClipPlane = src.nearClipPlane;
        dest.orthographic = src.orthographic;
        dest.fieldOfView = src.fieldOfView;
        dest.aspect = src.aspect;
        dest.orthographicSize = src.orthographicSize;
    }

    /// <summary>
    /// 计算反射矩阵
    /// </summary>
    /// <param name="reflectionMat">原始矩阵</param>
    /// <param name="plane">反射平面</param>
    /// <returns>反射矩阵</returns>
    public static Matrix4x4 CalculateReflectionMatrix(Matrix4x4 reflectionMat, Vector4 plane)
    {
        reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
        reflectionMat.m01 = (-2F * plane[0] * plane[1]);
        reflectionMat.m02 = (-2F * plane[0] * plane[2]);
        reflectionMat.m03 = (-2F * plane[3] * plane[0]);

        reflectionMat.m10 = (-2F * plane[1] * plane[0]);
        reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
        reflectionMat.m12 = (-2F * plane[1] * plane[2]);
        reflectionMat.m13 = (-2F * plane[3] * plane[1]);

        reflectionMat.m20 = (-2F * plane[2] * plane[0]);
        reflectionMat.m21 = (-2F * plane[2] * plane[1]);
        reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
        reflectionMat.m23 = (-2F * plane[3] * plane[2]);

        reflectionMat.m30 = 0F;
        reflectionMat.m31 = 0F;
        reflectionMat.m32 = 0F;
        reflectionMat.m33 = 1F;
        return reflectionMat;
    }

    /// <summary>
    /// 计算指定平面在摄像机中的空间位置
    /// </summary>
    /// <param name="cam">摄像机</param>
    /// <param name="pos">平面上的点</param>
    /// <param name="normal">平面法线</param>
    /// <param name="sideSign">1：平面正面，-1：平面反面</param>
    /// <param name="clipPlaneOffset">平面法线位置偏移量</param>
    /// <returns></returns>
    public static Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign, float clipPlaneOffset)
    {
        Vector3 offsetPos = pos + normal * clipPlaneOffset;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cpos = m.MultiplyPoint(offsetPos);
        Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
        return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
    }

    /// <summary>
    /// 由剪裁面计算投影倾斜矩阵
    /// </summary>
    /// <param name="projection">投影矩阵</param>
    /// <param name="clipPlane">剪裁面</param>
    /// <param name="sideSign">剪裁平面(-1:平面下面,1:平面上面)</param>
    public static Matrix4x4 CalculateObliqueMatrix(Matrix4x4 projection, Vector4 clipPlane, float sideSign)
    {
        Vector4 q = projection.inverse * new Vector4(
            sgn(clipPlane.x),
            sgn(clipPlane.y),
            1.0f,
            1.0f
        );
        Vector4 c = clipPlane * (2.0F / (Vector4.Dot(clipPlane, q)));
        // third row = clip plane - fourth row
        projection[2] = c.x + Mathf.Sign(sideSign) * projection[3];
        projection[6] = c.y + Mathf.Sign(sideSign) * projection[7];
        projection[10] = c.z + Mathf.Sign(sideSign) * projection[11];
        projection[14] = c.w + Mathf.Sign(sideSign) * projection[15];
        return projection;
    }

    private static float sgn(float a)
    {
        if (a > 0.0f) return 1.0f;
        if (a < 0.0f) return -1.0f;
        return 0.0f;
    }

    /// <summary>
    /// 由水平、垂直距离修改倾斜矩阵
    /// </summary>
    /// <param name="projMatrix">倾斜矩阵</param>
    /// <param name="horizObl">水平方向</param>
    /// <param name="vertObl">垂直方向</param>
    /// <returns>修改后的倾斜矩阵</returns>
    public static Matrix4x4 CalculateObliqueMatrix(Matrix4x4 projMatrix, float horizObl, float vertObl)
    {
        Matrix4x4 mat = projMatrix;
        mat[0, 2] = horizObl;
        mat[1, 2] = vertObl;
        return mat;
    }
    #endregion

    #region Shader Matrix4x4
    /// <summary>
    /// tex2DProj到tex2D的uv纹理转换矩阵
    /// 在shader中,
    /// vert=>o.posProj = mul(_ProjMatrix, v.vertex);
    /// frag=>tex2D(_RefractionTex,float2(i.posProj) / i.posProj.w)
    /// </summary>
    /// <param name="transform">要显示纹理的对象</param>
    /// <param name="cam">当前观察的摄像机</param>
    /// <returns>返回转换矩阵</returns>
    public static Matrix4x4 UV_Tex2DProj2Tex2D(Transform transform, Camera cam)
    {
        Matrix4x4 scaleOffset = Matrix4x4.TRS(
            new Vector3(0.5f, 0.5f, 0.5f), Quaternion.identity, new Vector3(0.5f, 0.5f, 0.5f));
        Vector3 scale = transform.lossyScale;
        Matrix4x4 _ProjMatrix = transform.localToWorldMatrix * Matrix4x4.Scale(new Vector3(1.0f / scale.x, 1.0f / scale.y, 1.0f / scale.z));
        _ProjMatrix = scaleOffset * cam.projectionMatrix * cam.worldToCameraMatrix * _ProjMatrix;
        return _ProjMatrix;
    }
    #endregion
}
