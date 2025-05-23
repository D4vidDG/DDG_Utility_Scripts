﻿using System;
using UnityEditor;
using UnityEngine;

namespace RPG.Saving
{
    [System.Serializable]
    public class SerializableVector3
    {
        public readonly float x, y, z;

        public SerializableVector3()
        {

        }

        public SerializableVector3(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }

        internal Vector3 ToVector()
        {
            return new Vector3(x,y,z);
        }

    }
}