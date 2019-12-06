using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ExtensionMethods
{
    public static Vector3 RandomVectorRange(Vector3 vMin, Vector3 vMax) {
        return new Vector3(
            Random.Range(vMin.x,vMax.x),
            Random.Range(vMin.y,vMax.y),
            Random.Range(vMin.z,vMax.z)
        );
    }

    public static Vector3 RandomLocWithin(this Bounds bnd) {
        return RandomVectorRange(bnd.min, bnd.max);
    }


//    public static string UppercaseFirstLetter(this string value)
//    {
//        // Uppercase the first letter in the string.
//        if (value.Length > 0)
//        {
//            char[] array = value.ToCharArray();
//            array[0] = char.ToUpper(array[0]);
//            return new string(array);
//        }
//        return value;
//    }
}