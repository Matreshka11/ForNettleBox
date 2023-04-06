using System;
using UnityEngine;

namespace Nettle {

public class VectorsByteConverter{

    public static Vector2 BytesToVector2(byte[] bytes) {
        float[] parsed = ParseVector(bytes, 2);
        return new Vector2(parsed[0], parsed[1]);
    }

    public static Vector3 BytesToVector3(byte[] bytes) {
        float[] parsed = ParseVector(bytes, 3);
        return new Vector3(parsed[0], parsed[1], parsed[2]);
    }

    public static float[] ParseVector(byte[] bytes, int dimensions) {
        float[] result = new float[dimensions];
        for (int i = 0; i < dimensions; i++) {
            int id = i * sizeof(float);
            if (id <= bytes.Length - sizeof(float)) {
                result[i] = BitConverter.ToSingle(bytes, id);
            }
            else {
                result[i] = 0;
            }
        }
        return result;
    }

    public static byte[] VectorToBytes(Vector2 vector) {
        byte[] result = new byte[2 * sizeof(float)];
        for (int i = 0; i < 2; i++) {
            Array.Copy(BitConverter.GetBytes(vector[i]), 0, result, i * sizeof(float), sizeof(float));
        }
        return result;
    }

    public static byte[] VectorToBytes(Vector3 vector) {
        byte[] result = new byte[3 * sizeof(float)];
        for (int i = 0; i < 3; i++) {
            Array.Copy(BitConverter.GetBytes(vector[i]), 0, result, i * sizeof(float), sizeof(float));
        }
        return result;
    }
}
}
