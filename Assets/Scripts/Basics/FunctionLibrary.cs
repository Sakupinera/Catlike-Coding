using UnityEngine;
using static UnityEngine.Mathf;

namespace Assets.Scripts.Basics
{
    public static class FunctionLibrary
    {
        public delegate Vector3 FunctionEventHandler(float u, float v, float t);

        public enum FunctionName { Wave, MultiWave, Ripple, Sphere, Torus }

        static FunctionEventHandler[] s_functions = { Wave, MultiWave, Ripple, Sphere, Torus };

        public static FunctionName GetRandomFunctionNameOtherThan(FunctionName name)
        {
            var choice = (FunctionName)Random.Range(1, s_functions.Length);
            return choice == name ? 0 : choice;
        }

        public static Vector3 Morph(float u, float v, float t, FunctionEventHandler from, FunctionEventHandler to, float progress)
        {
            //  The Lerp method clamps its third argument so it falls in the 0–1 range. The Smoothstep method does this as well.
            //  We configured the latter to output a 0–1 value, so the extra clamp of Lerp is not needed.
            //  For cases like this there is an alternative LerpUnclamped method, so let's use that one instead.
            return Vector3.LerpUnclamped(from(u, v, t), to(u, v, t), SmoothStep(0f, 1f, progress));
        }

        public static int FunctionCount => s_functions.Length;

        public static FunctionEventHandler GetFunction(FunctionName name) => s_functions[(int)name];

        public static FunctionName GetNextFunctionName(FunctionName name) =>
            (int)name < s_functions.Length - 1 ? name + 1 : 0;

        public static Vector3 Wave(float u, float v, float t)
        {
            Vector3 p;
            p.x = u;
            p.y = Sin(PI * (u + v + t));
            p.z = v;
            return p;
        }

        //public static Vector3 MultiWave(float u, float v, float t)
        //{
        //    Vector3 p;
        //    p.x = u;
        //    p.y = Sin(PI * (u + t));
        //    p.y += 0.5f * Sin(2f * PI * (u + t));
        //    p.y *= (2f / 3f);    //  To guarantee that we stay in the −1–1 range, we should divide the sum by 1.5.
        //    p.z = v;
        //    return p;
        //}

        //public static Vector3 MultiWave2(float u, float v, float t)
        //{
        //    Vector3 p;
        //    p.x = u;
        //    p.y = Sin(PI * (u + 0.5f * t));
        //    p.y += 0.5f * Sin(2f * PI * (v + t));
        //    p.y *= (2f / 3f);    //  To guarantee that we stay in the −1–1 range, we should divide the sum by 1.5.
        //    p.z = v;
        //    return p;
        //}

        public static Vector3 MultiWave(float u, float v, float t)
        {
            Vector3 p;
            p.x = u;
            p.y = Sin(PI * (u + 0.5f * t));
            p.y += 0.5f * Sin(2f * PI * (v + t));
            p.y += Sin(PI * (u + v + 0.25f * t));
            p.y *= (1f / 2.5f);    //  To guarantee that we stay in the −1–1 range, we should divide the sum by 2.5.
            p.z = v;
            return p;
        }

        public static Vector3 Ripple(float u, float v, float t)
        {
            float d = Sqrt(u * u + v * v);
            Vector3 p;
            p.x = u;
            p.y = Sin(PI * (4f * d - t));
            p.y /= (1f + 10f * d);
            p.z = v;
            return p;
        }

        //public static Vector3 Sphere(float u, float v, float t)
        //{
        //    float r = 0.5f + 0.5f * Sin(PI * t);
        //    float s = r * Cos(0.5f * PI * v);
        //    Vector3 p;
        //    p.x = s * Sin(PI * u);
        //    p.y = r * Sin(PI * 0.5f * v);
        //    p.z = s * Cos(PI * u);
        //    return p;
        //}

        //public static Vector3 Sphere2(float u, float v, float t)
        //{
        //    float r = 0.9f + 0.1f * Sin(8f * PI * u);
        //    //float r = 0.9f + 0.1f * Sin(8f * PI * v);
        //    float s = r * Cos(0.5f * PI * v);
        //    Vector3 p;
        //    p.x = s * Sin(PI * u);
        //    p.y = r * Sin(PI * 0.5f * v);
        //    p.z = s * Cos(PI * u);
        //    return p;
        //}

        public static Vector3 Sphere(float u, float v, float t)
        {
            //float r = 0.9f + 0.1f * Sin(PI * (6f * u + 4f * v + t));
            float r = 0.9f + 0.1f * Sin(PI * (12f * u + 8f * v + t));
            float s = r * Cos(0.5f * PI * v);
            Vector3 p;
            p.x = s * Sin(PI * u);
            p.y = r * Sin(PI * 0.5f * v);
            p.z = s * Cos(PI * u);
            return p;
        }

        //public static Vector3 Torus(float u, float v, float t)
        //{
        //    float r = 1f;
        //    float s = 0.5f + r * Cos(PI * v);
        //    Vector3 p;
        //    p.x = s * Sin(PI * u);
        //    p.y = r * Sin(PI * v);
        //    p.z = s * Cos(PI * u);
        //    return p;
        //}

        //public static Vector3 Torus2(float u, float v, float t)
        //{
        //    float r1 = 0.75f;
        //    float r2 = 0.25f;
        //    float s = r1 + r2 * Cos(PI * v);
        //    Vector3 p;
        //    p.x = s * Sin(PI * u);
        //    p.y = r2 * Sin(PI * v);
        //    p.z = s * Cos(PI * u);
        //    return p;
        //}

        public static Vector3 Torus(float u, float v, float t)
        {
            //float r1 = 0.7f + 0.1f * Sin(PI * (6f * u + 0.5f * t));
            //float r2 = 0.15f + 0.05f * Sin(PI * (8f * u + 4f * v + 2f * t));
            float r1 = 0.7f + 0.1f * Sin(PI * (8f * u + 0.5f * t));
            float r2 = 0.15f + 0.05f * Sin(PI * (16f * u + 8f * v + 3f * t));
            float s = r1 + r2 * Cos(PI * v);
            Vector3 p;
            p.x = s * Sin(PI * u);
            p.y = r2 * Sin(PI * v);
            p.z = s * Cos(PI * u);
            return p;
        }
    }
}
