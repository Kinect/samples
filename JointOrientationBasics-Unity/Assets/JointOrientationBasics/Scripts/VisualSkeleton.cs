//------------------------------------------------------------------------------
// <copyright file="VisualSkeleton.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace JointOrientationBasics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using UnityEngine;

    public abstract class VisualSkeleton<T> 
        : MonoBehaviour
    {
        private Dictionary<string, Joint> joints;
        protected Dictionary<string, Joint> Joints
        {
            get
            {
                if (this.joints == null)
                {
                    this.joints = new Dictionary<string, Joint>();
                }

                return this.joints;
            }
        }

        private static string[] jointNames;
        public static string[] JointNames
        {
            get
            {
                if (jointNames == null || jointNames.Length == 0)
                {
                    jointNames = Enum.GetNames(typeof(T));
                }

                return jointNames;
            }
        }

        void Awake()
        {
            Init();
        }

        protected void Init()
        {
            CreateJoints();

            BuildHeirarchy();
        }

        protected abstract void BuildHeirarchy();

        protected string RootName { get; set; }

        protected void CreateJoints()
        {
            // ensure the collection exists and clear it
            this.Joints.Clear();

            // create joints
            foreach (T type in Enum.GetValues(typeof(T)))
            {
                Joint joint = GetJoint(type);
                if (joint == null)
                {
                    joint = new Joint();
                    joint.Init(type.ToString());
                }

                this.Joints.Add(joint.Name, joint);
            }
        }

        internal Joint GetJoint(T type)
        {
            Joint joint = null;
            if (type != null)
            {
                joint = this.Joints.FirstOrDefault(x => x.Key == type.ToString()).Value;
            }

            return joint;
        }

        internal Joint GetRootJoint()
        {
            return this.Joints[RootName];
        }
    }

}