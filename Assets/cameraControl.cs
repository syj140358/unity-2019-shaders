using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraControl : MonoBehaviour
{
public Camera waterCamera;
float speed = 20;
public float distance_v;
public float distance_h;
public float rotation_H_speed = 1;
public float rotation_V_speed = 1;
public float max_up_angle = 90;              //越大，头抬得越高
    public float max_down_angle = -90;            //越小，头抬得越低


    private float current_rotation_H;      //水平旋转结果
    private float current_rotation_V;  //垂直旋转结果
    void LateUpdate()
{
// 旋转
if (Input.GetMouseButton(1))
{
//控制旋转
current_rotation_H += Input.GetAxis("Mouse X") * rotation_H_speed;
current_rotation_V += Input.GetAxis("Mouse Y") * rotation_V_speed;
//current_rotation_V = Mathf.Clamp(current_rotation_V, max_down_angle, max_up_angle); //限制垂直旋转角度
transform.localEulerAngles = new Vector3(-current_rotation_V, current_rotation_H, 0f);

//改变位置，以跟踪的目标为视野中心，且视野中心总是面向follow_obj
//transform.position = follow_obj.position;
transform.Translate(Vector3.back * distance_h, Space.Self);
transform.Translate(Vector3.up * distance_v, Space.World);          //相对于世界坐标y轴向上
}

// 平移
if (Input.GetMouseButton(2))
{

this.transform.localPosition -= new Vector3(Input.GetAxis("Mouse X") * rotation_H_speed, Input.GetAxis("Mouse Y") * rotation_V_speed, 0f);

}
SetWaterCamera();
}
// Use this for initialization
void Start()
{

}

// Update is called once per frame
void Update()
{
// 移动
if (Input.GetKey(KeyCode.A)) //左移
{
transform.Translate(Vector3.left * speed * Time.deltaTime);
}
if (Input.GetKey(KeyCode.D)) //右移
{
transform.Translate(Vector3.right * speed * Time.deltaTime);

}
if (Input.GetKey(KeyCode.W)) //前移
{
transform.Translate(Vector3.forward * speed * Time.deltaTime);

}
if (Input.GetKey(KeyCode.S)) //后移
{
transform.Translate(Vector3.back * speed * Time.deltaTime);

}

// 缩放
if (Input.GetAxis("Mouse ScrollWheel") != 0)
{
//获取鼠标滚轮的滑动量
float wheel = Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime * 50;

//改变相机的位置
this.transform.Translate(Vector3.forward * wheel);
//float distance = this.transform.position.y + wheel;
//this.transform.SetPositionAndRotation(new Vector3(this.transform.position.x, distance, this.transform.position.z),this.transform.rotation);
}

}

void SetWaterCamera()
{
    Vector3 pos = gameObject.transform.position;
    pos.y = - pos.y;
    waterCamera.transform.position = pos;
    Vector3 rot = gameObject.transform.localEulerAngles;
    rot.x = - rot.x;
    waterCamera.transform.localEulerAngles = rot;
}
}