using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
//using float3x4 = Unity.Mathematics.float3x4;
using quaternion = Unity.Mathematics.quaternion;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Basics
{
    public class Fractal : MonoBehaviour
    {
        [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
        struct UpdateFractalLevelJob : IJobFor
        {
            //public float spinAngleDelta;
            public float m_scale;
            public float m_deltaTime;

            [ReadOnly]
            public NativeArray<FractalPart> m_parents;

            public NativeArray<FractalPart> m_parts;

            [WriteOnly]
            public NativeArray<float3x4> m_matrices;

            public void Execute(int i)
            {
                FractalPart parent = m_parents[i / 5];
                FractalPart part = m_parts[i];
                part.m_spinAngle += part.m_spinVelocity * m_deltaTime;

                float3 upAxis = mul(mul(parent.m_worldRotation, part.m_rotation), up());
                float3 sagAxis = cross(up(), upAxis);
                //sagAxis = normalize(sagAxis);

                float sagMagnitude = length(sagAxis);
                quaternion baseRotation;
                if (sagMagnitude > 0f)
                {
                    sagAxis /= sagMagnitude;
                    quaternion sagRotation = quaternion.AxisAngle(sagAxis, part.m_maxSagAngle * sagMagnitude);
                    baseRotation = mul(sagRotation, parent.m_worldRotation);
                }
                else
                {
                    baseRotation = parent.m_worldRotation;
                }

                part.m_worldRotation = mul(baseRotation,
                    mul(part.m_rotation, quaternion.RotateY(part.m_spinAngle))
                );
                part.m_worldPosition =
                    parent.m_worldPosition +
                    //mul(parent.worldRotation, (1.5f * scale * part.direction));
                    mul(part.m_worldRotation, float3(0f, 1.5f * m_scale, 0f));
                m_parts[i] = part;
                float3x3 r = float3x3(part.m_worldRotation) * m_scale;
                m_matrices[i] = float3x4(r.c0, r.c1, r.c2, part.m_worldPosition);
            }
        }

        struct FractalPart
        {
            //public float3 direction, worldPosition;
            public float3 m_worldPosition;
            public quaternion m_rotation, m_worldRotation;
            //public Transform transform;
            public float m_maxSagAngle, m_spinAngle, m_spinVelocity;
        }

        [SerializeField, Range(3, 8)]
        int m_depth = 4;

        [SerializeField]
        Mesh m_mesh, m_leafMesh;

        [SerializeField]
        Material m_material;

        [SerializeField]
        //Gradient gradient;
        Gradient m_gradientA, m_gradientB;

        [SerializeField]
        Color m_leafColorA, m_leafColorB;

        [SerializeField, Range(0f, 90f)]
        float m_maxSagAngleA = 15f, m_maxSagAngleB = 25f;

        [SerializeField, Range(0f, 90f)]
        float m_spinSpeedA = 20f, m_spinSpeedB = 25f;

        [SerializeField, Range(0f, 1f)]
        float m_reverseSpinChance = 0.25f;

        //static float3[] directions =
        //{
        //    up(),right(),left(),forward(),back()
        //};

        static quaternion[] rotations =
        {
            quaternion.identity,
            //Quaternion.Euler(0f,0f,-90f),Quaternion.Euler(0f,0f,90f),
            //Quaternion.Euler(90f,0f,0f),Quaternion.Euler(-90f,0f,0f)
            quaternion.RotateZ(-0.5f*PI),quaternion.RotateZ(0.5f*PI),
            quaternion.RotateX(0.5f*PI),quaternion.RotateX(-0.5f*PI)
        };

        //FractalPart[][] parts;
        NativeArray<FractalPart>[] m_parts;

        //Matrix4x4[][] matrices;
        NativeArray<float3x4>[] m_matrices;

        ComputeBuffer[] m_matricesBuffers;

        static readonly int
            //baseColorId = Shader.PropertyToID("_BaseColor"),
            s_colorAId = Shader.PropertyToID("_ColorA"),
            s_colorBId = Shader.PropertyToID("_ColorB"),
            s_matricesId = Shader.PropertyToID("_Matrices"),
            s_sequenceNumbersId = Shader.PropertyToID("_SequenceNumbers");

        static MaterialPropertyBlock s_propertyBlock;

        Vector4[] m_sequenceNumbers;

        private void OnEnable()
        {
            //parts = new FractalPart[depth][];
            //matrices = new Matrix4x4[depth][];
            m_parts = new NativeArray<FractalPart>[m_depth];
            m_matrices = new NativeArray<float3x4>[m_depth];
            m_matricesBuffers = new ComputeBuffer[m_depth];
            //int stride = 16 * 4;
            m_sequenceNumbers = new Vector4[m_depth];
            int stride = 12 * 4;
            for (int i = 0, length = 1; i < m_parts.Length; i++, length *= 5)
            {
                //parts[i] = new FractalPart[length];
                //matrices[i] = new Matrix4x4[length];
                m_parts[i] = new NativeArray<FractalPart>(length, Allocator.Persistent);
                m_matrices[i] = new NativeArray<float3x4>(length, Allocator.Persistent);
                m_matricesBuffers[i] = new ComputeBuffer(length, stride);
                m_sequenceNumbers[i] = new Vector4(Random.value, Random.value, Random.value, Random.value);
            }

            //float scale = 1f;
            //parts[0][0] = CreatePart(0, 0, scale);
            m_parts[0][0] = CreatePart(0);
            for (int li = 1; li < m_parts.Length; li++)
            {
                //scale *= 0.5f;
                //FractalPart[] levelParts = parts[li];
                NativeArray<FractalPart> levelParts = m_parts[li];
                for (int fpi = 0; fpi < levelParts.Length; fpi += 5)
                {
                    for (int ci = 0; ci < 5; ci++)
                    {
                        //levelParts[fpi + ci] = CreatePart(li, ci, scale);
                        levelParts[fpi + ci] = CreatePart(ci);
                    }
                }
            }

            s_propertyBlock ??= new MaterialPropertyBlock();
        }

        private void OnDisable()
        {
            for (int i = 0; i < m_matricesBuffers.Length; i++)
            {
                m_matricesBuffers[i].Release();
                m_parts[i].Dispose();
                m_matrices[i].Dispose();
            }
            m_parts = null;
            m_matrices = null;
            m_matricesBuffers = null;
            m_sequenceNumbers = null;
        }

        private void OnValidate()
        {
            if (m_parts != null && enabled)
            {
                OnDisable();
                OnEnable();
            }
        }

        private void Update()
        {
            quaternion deltaRotation = quaternion.Euler(0f, 22.5f * Time.deltaTime, 0f);

            //float spinAngleDelta = 22.5f * Time.deltaTime;
            //float spinAngleDelta = 0.125f * PI * Time.deltaTime;
            float deltaTime = Time.deltaTime;
            FractalPart rootPart = m_parts[0][0];
            //rootPart.rotation *= deltaRotation;
            rootPart.m_spinAngle += rootPart.m_spinVelocity * deltaTime;
            //rootPart.transform.localRotation = rootPart.rotation;
            rootPart.m_worldRotation = mul(transform.rotation,
                mul(rootPart.m_rotation, quaternion.RotateY(rootPart.m_spinAngle))
            );
            rootPart.m_worldPosition = transform.position;
            m_parts[0][0] = rootPart;
            float objectScale = transform.lossyScale.x;
            float3x3 r = float3x3(rootPart.m_worldRotation) * objectScale;
            m_matrices[0][0] = float3x4(r.c0, r.c1, r.c2, rootPart.m_worldPosition);
            float scale = objectScale;
            JobHandle jobHandle = default;
            for (int li = 1; li < m_parts.Length; li++)
            {
                scale *= 0.5f;
                jobHandle = new UpdateFractalLevelJob
                {
                    //spinAngleDelta = spinAngleDelta,
                    m_deltaTime = deltaTime,
                    m_scale = scale,
                    m_parents = m_parts[li - 1],
                    m_parts = m_parts[li],
                    m_matrices = m_matrices[li]
                }.ScheduleParallel(m_parts[li].Length, 5, jobHandle);

                //job.Schedule(parts[li].Length, default).Complete();
                //jobHandle = job.Schedule(parts[li].Length, jobHandle);
                //FractalPart[] parentParts = parts[li - 1];
                //FractalPart[] levelParts = parts[li];
                //Matrix4x4[] levelMatrices = matrices[li];
                //NativeArray<FractalPart> parentParts = parts[li - 1];
                //NativeArray<FractalPart> levelParts = parts[li];
                //NativeArray<Matrix4x4> levelMatrices = matrices[li];
                //for (int fpi = 0; fpi < parts[li].Length; fpi++)
                //{
                //    job.Execute(fpi);
                //    ////Transform parentTransform = parentParts[fpi / 5].transform;
                //    //FractalPart parent = parentParts[fpi / 5];
                //    //FractalPart part = levelParts[fpi];
                //    ////part.rotation *= deltaRotation;
                //    //part.spinAngle += spinAngleDelta;
                //    ////part.transform.localRotation = parentTransform.localRotation * part.rotation;
                //    ////part.transform.localPosition =
                //    ////    parentTransform.localPosition +
                //    ////    parentTransform.localRotation * (1.5f * part.transform.localScale.x * part.direction);
                //    //part.worldRotation =
                //    //    parent.worldRotation *
                //    //    (part.rotation * Quaternion.Euler(0f, part.spinAngle, 0f));
                //    //part.worldPosition =
                //    //    parent.worldPosition +
                //    //    parent.worldRotation * (1.5f * scale * part.direction);
                //    //levelParts[fpi] = part;
                //    //levelMatrices[fpi] = Matrix4x4.TRS(part.worldPosition, part.worldRotation, scale * Vector3.one);
                //}
            }
            jobHandle.Complete();

            var bounds = new Bounds(rootPart.m_worldPosition, 3f * objectScale * Vector3.one);
            int leafIndex = m_matricesBuffers.Length - 1;
            for (int i = 0; i < m_matricesBuffers.Length; i++)
            {
                ComputeBuffer buffer = m_matricesBuffers[i];
                buffer.SetData(m_matrices[i]);
                //propertyBlock.SetColor(
                //    baseColorId, Color.white * (i / (matricesBuffers.Length - 1f))
                //);
                //propertyBlock.SetColor(
                //    baseColorId, Color.Lerp(
                //    Color.yellow, Color.red, i / (matricesBuffers.Length - 1f)
                //    )
                //);
                //propertyBlock.SetColor(
                //    baseColorId, gradient.Evaluate(i / (matricesBuffers.Length - 1f))
                //);
                Color colorA, colorB;
                Mesh instanceMesh;
                if (i == leafIndex)
                {
                    colorA = m_leafColorA;
                    colorB = m_leafColorB;
                    instanceMesh = m_leafMesh;
                }
                else
                {
                    float gradientInterpolator = i / (m_matricesBuffers.Length - 2f);
                    colorA = m_gradientA.Evaluate(gradientInterpolator);
                    colorB = m_gradientB.Evaluate(gradientInterpolator);
                    instanceMesh = m_mesh;
                }
                s_propertyBlock.SetColor(s_colorAId, colorA);
                s_propertyBlock.SetColor(s_colorBId, colorB);
                s_propertyBlock.SetBuffer(s_matricesId, buffer);
                s_propertyBlock.SetVector(s_sequenceNumbersId, m_sequenceNumbers[i]);
                Graphics.DrawMeshInstancedProcedural(instanceMesh, 0, m_material, bounds, buffer.count, s_propertyBlock);
            }
        }

        FractalPart CreatePart(/*int levelIndex,*/ int childIndex/*, float scale*/) => new FractalPart
        {
            //var go = new GameObject("Fractal Part " + levelIndex + " C" + childIndex);
            //go.transform.localScale = scale * Vector3.one;
            //go.transform.SetParent(transform, false);
            //go.AddComponent<MeshFilter>().mesh = mesh;
            //go.AddComponent<MeshRenderer>().material = material;

            //return new FractalPart
            //{
            //    direction = directions[childIndex],
            //    rotation = rotations[childIndex]
            //    transform = go.transform
            //};

            //direction = directions[childIndex],
            m_maxSagAngle = radians(Random.Range(m_maxSagAngleA, m_maxSagAngleB)),
            m_rotation = rotations[childIndex],
            m_spinVelocity =
                (Random.value < m_reverseSpinChance ? -1f : 1f) *
                radians(Random.Range(m_spinSpeedA, m_spinSpeedB))
        };

        //private void Start()
        //{
        //    name = "Fractal " + depth;

        //    if (depth <= 1)
        //    {
        //        return;
        //    }

        //    //Fractal child = Instantiate(this);
        //    //child.depth = depth - 1;
        //    //child.transform.SetParent(transform, false);
        //    //child.transform.localPosition = 0.75f * Vector3.right;
        //    //child.transform.localScale = 0.5f * Vector3.one;

        //    //child = Instantiate(this);
        //    //child.depth = depth - 1;
        //    //child.transform.SetParent(transform, false);
        //    //child.transform.localPosition = 0.75f * Vector3.up;
        //    //child.transform.localScale = 0.5f * Vector3.one;

        //    Fractal childA = CreateChild(Vector3.up, Quaternion.identity);
        //    Fractal childB = CreateChild(Vector3.right, Quaternion.Euler(0f, 0f, -90f));
        //    Fractal childC = CreateChild(Vector3.left, Quaternion.Euler(0f, 0f, 90f));
        //    Fractal childD = CreateChild(Vector3.forward, Quaternion.Euler(90f, 0f, 0f));
        //    Fractal childE = CreateChild(Vector3.back, Quaternion.Euler(-90f, 0f, 0f));

        //    childA.transform.SetParent(transform, false);
        //    childB.transform.SetParent(transform, false);
        //    childC.transform.SetParent(transform, false);
        //    childD.transform.SetParent(transform, false);
        //    childE.transform.SetParent(transform, false);
        //}

        //private void Update()
        //{
        //    transform.Rotate(0f, 22.5f * Time.deltaTime, 0f);
        //}

        //Fractal CreateChild(Vector3 direction, Quaternion rotation)
        //{
        //    Fractal child = Instantiate(this);
        //    child.depth = depth - 1;
        //    child.transform.localPosition = 0.75f * direction;
        //    child.transform.localRotation = rotation;
        //    child.transform.localScale = 0.5f * Vector3.one;
        //    return child;
        //}
    }
}
