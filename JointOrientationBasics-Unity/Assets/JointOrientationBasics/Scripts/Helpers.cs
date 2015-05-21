//------------------------------------------------------------------------------
// <copyright file="Helpers.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
namespace JointOrientationBasics
{
    using System;

    public static class Helpers
    {
        private static readonly UnityEngine.Object floorLock = new UnityEngine.Object();
        private static UnityEngine.Vector4 floorClipPlane = UnityEngine.Vector4.zero;
        public static UnityEngine.Vector4 FloorClipPlane
        {
            get
            {
                lock (floorLock)
                {
                    return floorClipPlane;
                }
            }

            set
            {
                lock (floorLock)
                {
                    if (!value.Equals(floorClipPlane))
                    {
                        floorClipPlane = value;
                    }
                }
            }
        }

        public static UnityEngine.Quaternion CalculateFloorRotationCorrection(UnityEngine.Vector4 floorNormal)
        {
            UnityEngine.Vector3 up = floorNormal;
            UnityEngine.Vector3 forward = UnityEngine.Vector3.forward;
            UnityEngine.Vector3 right = UnityEngine.Vector3.Cross(up, forward);

            // correct forward direction
            forward = UnityEngine.Vector3.Cross(right, up);

            return UnityEngine.Quaternion.LookRotation(new UnityEngine.Vector3(forward.x, -forward.y, forward.z), new UnityEngine.Vector3(up.x, up.y, -up.z));
        }

        public static UnityEngine.Quaternion QuaternionZero = new UnityEngine.Quaternion(0, 0, 0, 0);

        public enum ColorRange { BW = 0, RGB, RGBTint, CMYK, CMYKTint };
        public static UnityEngine.Color[] Colors = { 
                                                   UnityEngine.Color.black, UnityEngine.Color.grey, UnityEngine.Color.white,
                                                   UnityEngine.Color.red, UnityEngine.Color.green, UnityEngine.Color.blue, 
                                                   new UnityEngine.Color(.5f, 0, 0), new UnityEngine.Color(0, .5f, 0), new UnityEngine.Color(0, 0, .5f), 
                                                   UnityEngine.Color.magenta, UnityEngine.Color.yellow, UnityEngine.Color.cyan, 
                                                   new UnityEngine.Color(.5f, 0, .5f), new UnityEngine.Color(.5f,.5f,0), new UnityEngine.Color(0,.5f,.5f), 
                                               };


        public static void DrawDebugQuaternion(UnityEngine.Vector3 startPosition, UnityEngine.Quaternion rotation, ColorRange colorRange = ColorRange.RGB, float scale = .05f)
        {
            UnityEngine.Vector3 right = rotation * UnityEngine.Vector3.right;
            UnityEngine.Vector3 up = rotation * UnityEngine.Vector3.up;
            UnityEngine.Vector3 forward = rotation * UnityEngine.Vector3.forward;

            Helpers.DrawDebugLine(startPosition, right * scale, 3 * (short)colorRange + 0);
            Helpers.DrawDebugLine(startPosition, up * scale, 3 * (short)colorRange + 1);
            Helpers.DrawDebugLine(startPosition, forward * scale, 3 * (short)colorRange + 2);
        }

        public static void DrawDebugBoneYDirection(UnityEngine.Vector3 startPosition, float length, UnityEngine.Quaternion rotation, ColorRange colorRange = ColorRange.RGB)
        {
            UnityEngine.Vector3 right = rotation * UnityEngine.Vector3.right * length * .25f;
            UnityEngine.Vector3 up = rotation * UnityEngine.Vector3.up * length;
            UnityEngine.Vector3 forward = rotation * UnityEngine.Vector3.forward * length * .25f;

            Helpers.DrawDebugLine(startPosition, right, 3 * (short)colorRange + 0);
            Helpers.DrawDebugLine(startPosition, up, 3 * (short)colorRange + 1);
            Helpers.DrawDebugLine(startPosition, forward, 3 * (short)colorRange + 2);
        }

        public static void DrawDebugBone(UnityEngine.Vector3 position, UnityEngine.Vector3 right, UnityEngine.Vector3 up, UnityEngine.Vector3 forward, ColorRange colorRange = ColorRange.RGB)
        {
            right *= .2f;
            up *= .2f;
            forward *= .2f;

            Helpers.DrawDebugLine(position, right, 3 * (short)colorRange + 0);
            Helpers.DrawDebugLine(position, up, 3 * (short)colorRange + 1);
            Helpers.DrawDebugLine(position, forward, 3 * (short)colorRange + 2);

            Helpers.DrawDebugLine(position + (forward * .8f), forward, 2);
        }

        public static void DrawDebugLine(UnityEngine.Vector3 start, UnityEngine.Vector3 end, int colorIndex = 0)
        {
            UnityEngine.Debug.DrawRay(start, end, Helpers.Colors[colorIndex], 0.0f, false);
            UnityEngine.Debug.DrawRay(start + end, -(end * .05f), UnityEngine.Color.white, 0.0f, false);
        }

        public static bool SetVisible(UnityEngine.GameObject gameObject, bool isVisible)
        {
            var renderers = gameObject.GetComponentsInChildren<UnityEngine.Renderer>();
            for (int i = 0; i < renderers.Length; ++i)
            {
                renderers[i].enabled = isVisible;
            }

            return isVisible;
        }

        public static T? ParseEnum<T>(string value) where T : struct, IConvertible
        {
            T? item = null;

            foreach (T type in Enum.GetValues(typeof(T)))
            {
                if (type.ToString().ToLower().Equals(value.Trim().ToLower()))
                {
                    item = type;
                    break;
                }
            }

            return item;
        }
    }
}