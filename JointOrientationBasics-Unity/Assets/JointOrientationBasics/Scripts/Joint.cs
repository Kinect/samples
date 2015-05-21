//------------------------------------------------------------------------------
// <copyright file="Joint.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
namespace JointOrientationBasics
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// stores joint information for the body data
    /// </summary>
    public class Joint
    {
        public string Name;

        public Joint Parent;

        public Vector3 LocalPosition;

        public Quaternion LocalRotation;

        public Vector3 Position
        {
            get { return GetPosition(); }
        }

        public Quaternion Rotation
        {
            get { return GetRotation(); }
        }

        public Vector3 RawPosition;

        public Quaternion RawRotation;

        private List<Joint> children;
        public List<Joint> Children
        {
            get
            {
                if (this.children == null)
                {
                    this.children = new List<Joint>();
                }

                return this.children;
            }
        }

        /// <summary>
        /// Initialize the joint
        /// </summary>
        /// <param name="name">joint name</param>
        public void Init(string name)
        {
            this.Name = name;

            this.Parent = null;

            this.Children.Clear();

            this.LocalPosition = Vector3.zero;

            this.LocalRotation = Quaternion.identity;

            this.RawPosition = Vector3.zero;

            this.RawRotation = Quaternion.identity;
        }

        public void AddChild(Joint joint)
        {
            joint.Parent = this;

            this.Children.Add(joint);
        }

        public void SetRawData(Vector3 position, Quaternion rotation)
        {
            this.RawPosition = position;
            this.RawRotation = rotation;
        }

        public void CalculateOffset(Vector3 worldOffset, Quaternion worldRotation)
        {
            // local position from parent
            this.LocalPosition = this.RawPosition + worldOffset;
            if (this.Parent != null)
            {
                this.LocalPosition -= this.Parent.GetPosition();
            }

            // to calculate local rotation from parent
            this.LocalRotation = this.RawRotation;
            if (this.Parent != null)
            {
                this.LocalRotation = Quaternion.Inverse(this.Parent.Rotation) * this.LocalRotation;
            }
        }

        public void CalculateAllOffsets(Vector3 parentPosition, Quaternion parentRotation)
        {
            CalculateOffset(parentPosition, parentRotation);

            // update children
            if (this.Children != null)
            {
                foreach (var bone in this.Children)
                {
                    bone.CalculateAllOffsets(parentPosition, parentRotation);
                }
            }
        }

        /// <summary>
        /// returns the postion of the joint from the parent
        /// </summary>
        public Vector3 GetPosition()
        {
            Vector3 position = this.LocalPosition;

            // traverse tree to get all positions to this joint
            if (this.Parent != null)
            {
                position = this.Parent.GetPosition() + position;
            }

            return position;
        }

        /// <summary>
        /// returns the total rotation from all the parents and itself
        /// </summary>
        public Quaternion GetRotation()
        {
            Quaternion rotation = this.LocalRotation;

            // traverse tree to get all rotations to this joint
            if (this.Parent != null)
            {
                rotation = this.Parent.GetRotation() * this.LocalRotation;
            }

            return rotation;
        }
    }
}