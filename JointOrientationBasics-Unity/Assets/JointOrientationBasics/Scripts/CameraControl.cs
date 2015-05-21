//------------------------------------------------------------------------------
// <copyright file="BodySourceManager.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
namespace JointOrientationBasics
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using UnityEngine;

    public class CameraControl : MonoBehaviour
    {
        public Transform target;

        public Vector2 PanSensitivity = new Vector2(2.0f, 2.0f);
        public Vector2 RotateSensitivity = new Vector2(4.0f, 4.0f);

        public Vector2 KeyPanSensitivity = new Vector2(0.1f, 0.1f);
        public Vector2 KeyRotateSensitivity = new Vector2(0.1f, 0.1f);

        public float ZoomSensititity = 15.0f;

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            float sensitivityScale = Input.GetKey(KeyCode.LeftShift) ? 0.1f : 1.0f;

            Vector3 angle = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 position = new Vector3(0.0f, 0.0f, 0.0f);

            position.z += Input.GetAxis("Mouse ScrollWheel") * ZoomSensititity * sensitivityScale;

            //pan
            if (Input.GetMouseButton(2))
            {
                position.x += Input.GetAxis("Mouse X") * PanSensitivity.x * sensitivityScale;
                position.y += Input.GetAxis("Mouse Y") * PanSensitivity.y * sensitivityScale;
            }
            else if (Input.GetKey(KeyCode.A))
            {
                position.x += KeyPanSensitivity.x * sensitivityScale;
            }
            else if (Input.GetKey(KeyCode.D))
            {
                position.x -= KeyPanSensitivity.x * sensitivityScale;
            }
            else if (Input.GetKey(KeyCode.S))
            {
                position.y += KeyPanSensitivity.y * sensitivityScale;
            }
            else if (Input.GetKey(KeyCode.W))
            {
                position.y -= KeyPanSensitivity.y * sensitivityScale;
            }
            //rotate
            else if (Input.GetMouseButton(1))
            {
                angle.x += Input.GetAxis("Mouse Y") * RotateSensitivity.y * sensitivityScale;
                angle.y += Input.GetAxis("Mouse X") * RotateSensitivity.x * sensitivityScale;
            }
            else if (Input.GetKey(KeyCode.Q))
            {
                angle.y += KeyRotateSensitivity.y * sensitivityScale;
            }
            else if (Input.GetKey(KeyCode.E))
            {
                angle.y -= KeyRotateSensitivity.y * sensitivityScale;
            }
            transform.eulerAngles += angle;
            transform.Translate(position);
        }

    }
}
