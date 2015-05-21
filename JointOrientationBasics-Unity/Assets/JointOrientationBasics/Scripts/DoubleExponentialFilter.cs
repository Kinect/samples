//------------------------------------------------------------------------------
// <copyright file="DoubleExponentialFilter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
namespace JointOrientationBasics
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using UnityEngine;
    using Kinect = Windows.Kinect;

    public class DoubleExponentialFilter
    {
        public class Joint
        {
            public Vector3 Position;
            public Quaternion Rotation;

            public Joint()
            {
                Position = Vector3.zero;
                Rotation = Quaternion.identity;
            }

            public Joint(Kinect.CameraSpacePoint position, Windows.Kinect.Vector4 rotation)
            {
                this.Position = new Vector3(position.X, position.Y, position.Z);
                this.Rotation = new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
            }
            public Joint(Vector3 position, Quaternion rotation)
            {
                this.Position = position;
                this.Rotation = rotation;
            }

            public void Reset()
            {
                Position = Vector3.zero;
                Rotation = Quaternion.identity;
            }

            internal static Joint Add(Joint joint, Joint prevJoint)
            {
                Joint op = new Joint();

                op.Position = joint.Position + prevJoint.Position;
                op.Rotation = joint.Rotation * prevJoint.Rotation;

                return op;
            }

            internal static Joint Subtract(Joint joint, Joint prevJoint)
            {
                Joint op = new Joint();

                op.Position = joint.Position - prevJoint.Position;
                op.Rotation = joint.Rotation * Quaternion.Inverse(prevJoint.Rotation);

                return op;
            }

            internal static Joint Scale(Joint joint, float scale)
            {
                Joint op = new Joint();

                op.Position = joint.Position * scale;
                op.Rotation = Quaternion.Slerp(Quaternion.identity, joint.Rotation, scale);

                return op;
            }
        }

        public struct TRANSFORM_SMOOTH_PARAMETERS
        {
            public float fSmoothing;             // [0..1], lower values closer to raw data
            public float fCorrection;            // [0..1], lower values slower to correct towards the raw data
            public float fPrediction;            // [0..n], the number of frames to predict into the future
            public float fJitterRadius;          // The radius in meters for jitter reduction
            public float fMaxDeviationRadius;    // The maximum radius in meters that filtered positions are allowed to deviate from raw data
        }

        public class DoubleExponentialFilterData
        {
            public Joint RawJoint;
            public Joint FilteredJoint;
            public Joint Trend;

            public int FrameCount;
        }

        // Holt Double Exponential Smoothing filter
        private Dictionary<Kinect.JointType, Joint> filteredJoints;
        private Dictionary<Kinect.JointType, DoubleExponentialFilterData> history;

        private TRANSFORM_SMOOTH_PARAMETERS smoothParams;
        public TRANSFORM_SMOOTH_PARAMETERS SmoothingParameters
        {
            get { return this.smoothParams; }
            set
            {
                if (!value.Equals(this.smoothParams))
                {
                    this.smoothParams = value;
                }
            }
        }

        public DoubleExponentialFilter()
        {
            filteredJoints = new Dictionary<Kinect.JointType, Joint>();

            history = new Dictionary<Kinect.JointType, DoubleExponentialFilterData>();
            foreach (Kinect.JointType jt in Kinect.JointType.GetValues(typeof(Kinect.JointType)))
            {
                history.Add(jt, new DoubleExponentialFilterData());
            }

            Init();
        }

        public void Init(float fSmoothing = 0.25f, float fCorrection = 0.25f, float fPrediction = 0.25f, float fJitterRadius = 0.03f, float fMaxDeviationRadius = 0.05f)
        {
            Reset(fSmoothing, fCorrection, fPrediction, fJitterRadius, fMaxDeviationRadius);
        }

        public void Reset(float fSmoothing = 0.25f, float fCorrection = 0.25f, float fPrediction = 0.25f, float fJitterRadius = 0.03f, float fMaxDeviationRadius = 0.05f)
        {
            if (filteredJoints == null || history == null)
            {
                return;
            }

            // Check for divide by zero. Use an epsilon of a 10th of a millimeter
            fJitterRadius = Mathf.Max(0.0001f, fJitterRadius);

            smoothParams.fCorrection = fCorrection;                     // How much to correct back from prediction.  Can make things springy
            smoothParams.fJitterRadius = fJitterRadius;                 // Size of the radius where jitter is removed. Can do too much smoothing when too high
            smoothParams.fMaxDeviationRadius = fMaxDeviationRadius;     // Size of the max prediction radius Can snap back to noisy data when too high
            smoothParams.fPrediction = fPrediction;                     // Amount of prediction into the future to use. Can over shoot when too high
            smoothParams.fSmoothing = fSmoothing;                       // How much smothing will occur.  Will lag when too high

            foreach (Kinect.JointType jt in Kinect.JointType.GetValues(typeof(Kinect.JointType)))
            {
                if (!filteredJoints.ContainsKey(jt))
                {
                    filteredJoints.Add(jt, new Joint());
                }
                filteredJoints[jt].Reset();

                if (!history.ContainsKey(jt))
                {
                    history.Add(jt, new DoubleExponentialFilterData());
                }

                if (history[jt].RawJoint == null)
                {
                    history[jt].RawJoint = new Joint();
                }
                history[jt].RawJoint.Reset();

                if (history[jt].FilteredJoint == null)
                {
                    history[jt].FilteredJoint = new Joint();
                }
                history[jt].FilteredJoint.Reset();

                if (history[jt].Trend == null)
                {
                    history[jt].Trend = new Joint();
                }
                history[jt].Trend.Reset();

                history[jt].FrameCount = 0;
            }
        }

        //--------------------------------------------------------------------------------------
        // Implementation of a Holt Double Exponential Smoothing filter. The double exponential
        // smooths the curve and predicts.  There is also noise jitter removal. And maximum
        // prediction bounds.  The paramaters are commented in the init function.
        //--------------------------------------------------------------------------------------
        public void Update(Kinect.Body body)
        {
            if (body == null)
            {
                return;
            }

            // Check for divide by zero. Use an epsilon of a 10th of a millimeter
            smoothParams.fJitterRadius = Mathf.Max(0.0001f, smoothParams.fJitterRadius);

            TRANSFORM_SMOOTH_PARAMETERS smoothingParams;

            foreach (Kinect.JointType jt in Kinect.JointType.GetValues(typeof(Kinect.JointType)))
            {
                smoothingParams.fSmoothing = smoothParams.fSmoothing;
                smoothingParams.fCorrection = smoothParams.fCorrection;
                smoothingParams.fPrediction = smoothParams.fPrediction;
                smoothingParams.fJitterRadius = smoothParams.fJitterRadius;
                smoothingParams.fMaxDeviationRadius = smoothParams.fMaxDeviationRadius;

                // If inferred, we smooth a bit more by using a bigger jitter radius
                Windows.Kinect.Joint joint = body.Joints[jt];
                if (joint.TrackingState == Kinect.TrackingState.Inferred)
                {
                    smoothingParams.fJitterRadius *= 2.0f;
                    smoothingParams.fMaxDeviationRadius *= 2.0f;
                }

                // set initial joint value from Kinect
                Joint fj = new Joint(body.Joints[jt].Position, body.JointOrientations[jt].Orientation);

                UpdateJoint(jt, fj, smoothingParams);
            }
        }

        public Joint UpdateJoint(Kinect.JointType jt, Joint rawJoint, TRANSFORM_SMOOTH_PARAMETERS smoothingParams)
        {
            Joint prevFilteredJoint;
            Joint prevRawJoint;
            Joint prevTrend;

            Joint filteredJoint;
            Joint predictedJoint;
            Joint diff;
            Joint trend;
            float fDiff;

            prevFilteredJoint = history[jt].FilteredJoint;
            prevTrend = history[jt].Trend;
            prevRawJoint = history[jt].RawJoint;

            // if joint is invalid, reset the filter
            bool jointIsValid = JointPositionIsValid(rawJoint);
            if (!jointIsValid)
            {
                history[jt].FrameCount = 0;
            }

            // initial start values
            if (history[jt].FrameCount == 0)
            {
                filteredJoint = rawJoint;
                trend = new Joint();
                history[jt].FrameCount++;
            }
            else if (history[jt].FrameCount == 1)
            {
                filteredJoint = Joint.Scale(Joint.Add(rawJoint, prevRawJoint), 0.5f);
                diff = Joint.Subtract(filteredJoint, prevFilteredJoint);
                trend = Joint.Add(Joint.Scale(diff, smoothingParams.fCorrection), Joint.Scale(prevTrend, 1.0f - smoothingParams.fCorrection));
                history[jt].FrameCount++;
            }
            else
            {
                // First apply jitter filter
                diff = Joint.Subtract(rawJoint, prevFilteredJoint);
                fDiff = diff.Position.magnitude;

                if (fDiff <= smoothingParams.fJitterRadius)
                {
                    filteredJoint = Joint.Add(Joint.Scale(rawJoint, fDiff / smoothingParams.fJitterRadius),
                        Joint.Scale(prevFilteredJoint, 1.0f - fDiff / smoothingParams.fJitterRadius));
                }
                else
                {
                    filteredJoint = rawJoint;
                }

                // Now the double exponential smoothing filter
                filteredJoint = Joint.Add(Joint.Scale(filteredJoint, 1.0f - smoothingParams.fSmoothing),
                    Joint.Scale(Joint.Add(prevFilteredJoint, prevTrend), smoothingParams.fSmoothing));


                diff = Joint.Subtract(filteredJoint, prevFilteredJoint);
                trend = Joint.Add(Joint.Scale(diff, smoothingParams.fCorrection), Joint.Scale(prevTrend, 1.0f - smoothingParams.fCorrection));
            }

            // Predict into the future to reduce latency
            predictedJoint = Joint.Add(filteredJoint, Joint.Scale(trend, smoothingParams.fPrediction));

            // Check that we are not too far away from raw data
            diff = Joint.Subtract(predictedJoint, rawJoint);
            fDiff = diff.Position.magnitude;

            if (fDiff > smoothingParams.fMaxDeviationRadius)
            {
                predictedJoint = Joint.Add(Joint.Scale(predictedJoint, smoothingParams.fMaxDeviationRadius / fDiff),
                    Joint.Scale(rawJoint, 1.0f - smoothingParams.fMaxDeviationRadius / fDiff));
            }

            // Save the data from this frame
            history[jt].RawJoint = rawJoint;
            history[jt].FilteredJoint = filteredJoint;
            history[jt].Trend = trend;

            // Output the data
            filteredJoints[jt] = predictedJoint;

            return predictedJoint;
        }

        public Joint GetFilteredJoint(Kinect.JointType jt)
        {
            if(!filteredJoints.ContainsKey(jt))
            {
                return null;
            }

            return filteredJoints[jt];
        }

        private bool JointPositionIsValid(Joint joint)
        {
            // if joint is 0 it is not valid.
            bool posIsValid = joint.Position.Equals(Vector3.zero);
            //bool orientationIsValid = joint.Rotation.Equals(Helpers.QuaternionZero);
            bool orientationIsValid = true;

            return posIsValid && orientationIsValid;
        }
    }
}