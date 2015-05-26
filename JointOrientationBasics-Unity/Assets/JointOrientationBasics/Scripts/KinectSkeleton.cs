//------------------------------------------------------------------------------
// <copyright file="KinectSkeleton.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace JointOrientationBasics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using UnityEngine;
    using Kinect = Windows.Kinect;

    public class KinectSkeleton
        : VisualSkeleton<Kinect.JointType>
    {
        /// <summary>
        /// Joint Smoothing filter
        /// </summary>
        private DoubleExponentialFilter jointSmoother;
        private DoubleExponentialFilter.TRANSFORM_SMOOTH_PARAMETERS smoothingParams;
        private DoubleExponentialFilter.Joint filteredJoint;

        public BodySourceManager BodySourceManager;
        public bool MirrorJoints;

        void Start()
        {
            if (null == this.BodySourceManager)
            {
                BodySourceManager[] objs = FindObjectsOfType(typeof(BodySourceManager)) as BodySourceManager[];
                if (objs.Length > 0)
                {
                    this.BodySourceManager = objs[0];
                }
            }
        }

        void Update()
        {
            // Get the closest body
            Kinect.Body body = this.BodySourceManager.GetClosestBody();
            if (null == body)
            {
                return;
            }
            // update the skeleton with the new body joint/orientation information
            UpdateJoints(body);
        }

        /// <summary>
        /// override base Init function
        /// </summary>
        new internal void Init()
        {
            base.Init();

            if (this.jointSmoother == null)
            {
                this.jointSmoother = new DoubleExponentialFilter();
                this.smoothingParams = jointSmoother.SmoothingParameters;
                this.filteredJoint = new DoubleExponentialFilter.Joint();
            }
        }

        protected override void BuildHeirarchy()
        {
            this.RootName = Kinect.JointType.SpineBase.ToString();

            // spine to head
            GetJoint(Kinect.JointType.SpineBase).AddChild(GetJoint(Kinect.JointType.SpineMid));
            GetJoint(Kinect.JointType.SpineMid).AddChild(GetJoint(Kinect.JointType.SpineShoulder));
            GetJoint(Kinect.JointType.SpineShoulder).AddChild(GetJoint(Kinect.JointType.Neck));
            GetJoint(Kinect.JointType.Neck).AddChild(GetJoint(Kinect.JointType.Head));

            // left leg
            GetJoint(Kinect.JointType.SpineBase).AddChild(GetJoint(Kinect.JointType.HipLeft));
            GetJoint(Kinect.JointType.HipLeft).AddChild(GetJoint(Kinect.JointType.KneeLeft));
            GetJoint(Kinect.JointType.KneeLeft).AddChild(GetJoint(Kinect.JointType.AnkleLeft));
            GetJoint(Kinect.JointType.AnkleLeft).AddChild(GetJoint(Kinect.JointType.FootLeft));

            // right leg
            GetJoint(Kinect.JointType.SpineBase).AddChild(GetJoint(Kinect.JointType.HipRight));
            GetJoint(Kinect.JointType.HipRight).AddChild(GetJoint(Kinect.JointType.KneeRight));
            GetJoint(Kinect.JointType.KneeRight).AddChild(GetJoint(Kinect.JointType.AnkleRight));
            GetJoint(Kinect.JointType.AnkleRight).AddChild(GetJoint(Kinect.JointType.FootRight));

            // left arm
            GetJoint(Kinect.JointType.SpineShoulder).AddChild(GetJoint(Kinect.JointType.ShoulderLeft));
            GetJoint(Kinect.JointType.ShoulderLeft).AddChild(GetJoint(Kinect.JointType.ElbowLeft));
            GetJoint(Kinect.JointType.ElbowLeft).AddChild(GetJoint(Kinect.JointType.WristLeft));
            GetJoint(Kinect.JointType.WristLeft).AddChild(GetJoint(Kinect.JointType.HandLeft));
            GetJoint(Kinect.JointType.HandLeft).AddChild(GetJoint(Kinect.JointType.HandTipLeft));
            GetJoint(Kinect.JointType.WristLeft).AddChild(GetJoint(Kinect.JointType.ThumbLeft));

            // right arm
            GetJoint(Kinect.JointType.SpineShoulder).AddChild(GetJoint(Kinect.JointType.ShoulderRight));
            GetJoint(Kinect.JointType.ShoulderRight).AddChild(GetJoint(Kinect.JointType.ElbowRight));
            GetJoint(Kinect.JointType.ElbowRight).AddChild(GetJoint(Kinect.JointType.WristRight));
            GetJoint(Kinect.JointType.WristRight).AddChild(GetJoint(Kinect.JointType.HandRight));
            GetJoint(Kinect.JointType.HandRight).AddChild(GetJoint(Kinect.JointType.HandTipRight));
            GetJoint(Kinect.JointType.WristRight).AddChild(GetJoint(Kinect.JointType.ThumbRight));
        }

        internal Joint GetJoint(Kinect.JointType? type)
        {
            Joint joint = null;
            if (type != null)
            {
                joint = this.Joints.FirstOrDefault(x => x.Key == type.Value.ToString()).Value;
            }

            return joint;
        }


        internal void UpdateJoints(Kinect.Body body)
        {
            if (body == null)
            {
                return;
            }

            if (this.Joints.Count == 0 || this.jointSmoother == null)
            {
                Init();
            }

            // get the floorClipPlace from the body information
            Vector4 floorClipPlane = Helpers.FloorClipPlane;

            // get rotation of camera
            Quaternion cameraRotation = Helpers.CalculateFloorRotationCorrection(floorClipPlane);

            // generate a vertical offset from floor plane
            Vector3 cameraPosition = Vector3.up * floorClipPlane.w;

            // visualize where the camera is
            // uncomment if you want to see the camera rotation visualized
            //Helpers.DrawDebugQuaternion(cameraPosition, cameraRotation, Helpers.ColorRange.CMYK, .25f);

            // update joints for a body
            UpdateJoint(GetRootJoint(), body, cameraPosition, cameraRotation);
        }

        private void UpdateJoint(Joint joint, Kinect.Body body, Vector3 cameraPosition, Quaternion cameraRotation)
        {
            if(joint == null || body == null)
            {
                return;
            }

            Kinect.JointType? jt = Helpers.ParseEnum<Kinect.JointType>(joint.Name);

            // get Kinects raw value and filter it
            if(jt != null)
            {
                // If inferred, we smooth a bit more by using a bigger jitter radius
                Windows.Kinect.Joint kinectJoint = body.Joints[jt.Value];
                if (kinectJoint.TrackingState == Kinect.TrackingState.Inferred)
                {
                    this.smoothingParams.fJitterRadius *= 2.0f;
                    this.smoothingParams.fMaxDeviationRadius *= 2.0f;
                }

                // get the initial joint value from Kinect, correct for camera
                Vector3 rawPosition = ConvertJointPositionToUnityVector3(body, jt.Value, this.MirrorJoints);
                Quaternion rawRotation = ConvertJointQuaternionToUnityQuaterion(body, jt.Value, this.MirrorJoints);

                // visualize the unadjusted joint information
                // turn on Gizmos if you want to see these in Game mode
                // Helpers.DrawDebugQuaternion(rawPosition, rawRotation, Helpers.ColorRange.BW, .05f);

                // adjust for camera
                rawPosition = cameraRotation * rawPosition;
                rawRotation = cameraRotation * rawRotation;

                // if this is a leaf bone, with no orientation
                if (Helpers.QuaternionZero.Equals(rawRotation) && joint.Parent != null)
                {
                    Vector3 direction = rawPosition - joint.Parent.RawPosition;
                    Vector3 perpendicular = Vector3.Cross(direction, Vector3.up);
                    Vector3 normal = Vector3.Cross(perpendicular, direction);

                    // calculate a rotation, Y forward for Kinect
                    if (normal.magnitude != 0)
                    {
                        rawRotation = Quaternion.LookRotation(normal, direction);
                    }
                    else
                    {
                        rawRotation = Quaternion.identity;
                    }
                }

                // set the filtered joint property with the updates values
                this.filteredJoint.Position = rawPosition;
                this.filteredJoint.Rotation = rawRotation;

                // filter the raw joint
                this.filteredJoint = jointSmoother.UpdateJoint(jt.Value, this.filteredJoint, smoothingParams);
            
                // set the raw joint value for the node
                joint.SetRawData(this.filteredJoint.Position, this.filteredJoint.Rotation);
            }

            // calculate offsets from world position/rotation
            joint.CalculateOffset(cameraPosition, cameraRotation);

            // continue through hierarchy
            if (joint.Children != null)
            {
                foreach (var child in joint.Children)
                {
                    UpdateJoint(child, body, cameraPosition, cameraRotation);
                }
            }
        }

        internal static Vector3 ConvertJointPositionToUnityVector3(Kinect.Body body, Kinect.JointType type, bool mirror = true)
        {
            Vector3 position = new Vector3(body.Joints[type].Position.X,
                body.Joints[type].Position.Y,
                body.Joints[type].Position.Z);

            // translate -x
            if (mirror)
            {
                position.x *= -1;
            }

            return position;
        }

        internal static Quaternion ConvertJointQuaternionToUnityQuaterion(Kinect.Body body, Kinect.JointType jt, bool mirror = true)
        {
            Quaternion rotation = new Quaternion(body.JointOrientations[jt].Orientation.X,
                body.JointOrientations[jt].Orientation.Y,
                body.JointOrientations[jt].Orientation.Z,
                body.JointOrientations[jt].Orientation.W);

            // flip rotation
            if (mirror)
            {
                rotation = new Quaternion(rotation.x, -rotation.y, -rotation.z, rotation.w);
            }

            return rotation;
        }

    }
}
