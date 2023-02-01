using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingSphere : MonoBehaviour
{
    /// <summary>
    /// 最大移动速度
    /// </summary>
    [SerializeField, Range(0f, 100f)]
    float maxSpeed = 10f;

    /// <summary>
    /// 地面加速度和空中加速度
    /// </summary>
    [SerializeField, Range(0f, 100f)]
    float maxAcceleration = 10f, maxAirAcceleration = 1f;

    /// <summary>
    /// 跳跃高度设定
    /// </summary>
    [SerializeField, Range(0f, 10f)]
    float jumpHeight = 2f;

    /// <summary>
    /// 最大的空中跳跃次数
    /// </summary>
    [SerializeField, Range(0, 5)]
    int maxAirJumps = 0;

    /// <summary>
    /// 最大能攀爬的斜坡角度
    /// </summary>
    [SerializeField, Range(0f, 90f)]
    float maxGroundAngle = 25f;

    /// <summary>
    /// 当前处于第几段跳跃
    /// </summary>
    int jumpPhase;

    #region 1.Sliding a Sphere
    //[SerializeField]
    //Rect allowedArea = new Rect(-5f, -5f, 10f, 10f);

    //[SerializeField, Range(0f, 1f)]
    //float bounciness = 0.5f;
    #endregion

    /// <summary>
    /// 小球当前速度和预期速度
    /// </summary>
    Vector3 velocity, desiredVelocity;

    /// <summary>
    /// 用于判断跳跃键是否按下
    /// </summary>
    bool desiredJump;

    //bool onGround;

    /// <summary>
    /// 接触地面的碰撞器数量
    /// </summary>
    int groundContactCount;

    /// <summary>
    /// 距离上次处于地面经历的时间步长
    /// </summary>
    int stepsSinceLastGrounded;

    /// <summary>
    /// 调用时通过groundContactCount来判断是否处在地面
    /// </summary>
    bool OnGround => groundContactCount > 0;

    /// <summary>
    /// 小球的碰撞体
    /// </summary>
    Rigidbody body;

    /// <summary>
    /// 由法线的Y轴分量来判断是否与地面接触
    /// </summary>
    float minGroundDotProduct;

    /// <summary>
    /// 与碰撞体接触的法向量
    /// </summary>
    Vector3 contactNormal;

    #region 1.Sliding a Sphere
    /*
    void Update()
    {
        Vector2 playerInput;
        playerInput.x = Input.GetAxis("Horizontal");
        playerInput.y = Input.GetAxis("Vertical");

        //playerInput = Vector2.ClampMagnitude(playerInput, 1);
        //transform.localPosition = new Vector3(playerInput.x, 0.5f, playerInput.y);

        Vector3 desiredSpeed = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;
        float maxSpeedChange = maxAcceleration * Time.deltaTime;
        //if(velocity.x < desiredSpeed.x)
        //{
        //    velocity.x = Mathf.Min(velocity.x + maxSpeedChange, desiredSpeed.x);
        //}
        //else if(velocity.x > desiredSpeed.x)
        //{
        //    velocity.x = Mathf.Max(velocity.x - maxSpeedChange, desiredSpeed.x);
        //}
        velocity.x = Mathf.MoveTowards(velocity.x, desiredSpeed.x, maxSpeedChange);
        velocity.z = Mathf.MoveTowards(velocity.z, desiredSpeed.z, maxSpeedChange);
        Vector3 displacement = velocity * Time.deltaTime;
        //transform.localPosition += displacement;
        Vector3 newPosition = transform.localPosition + displacement;
        //if (!allowedArea.Contains(new Vector2(newPosition.x,newPosition.z)))
        //{
        //    //newPosition = transform.localPosition;
        //    newPosition.x =
        //        Mathf.Clamp(newPosition.x, allowedArea.xMin, allowedArea.xMax);
        //    newPosition.z =
        //        Mathf.Clamp(newPosition.z, allowedArea.yMin, allowedArea.yMax);
        //}
        if (newPosition.x < allowedArea.xMin)
        {
            newPosition.x = allowedArea.xMin;
            //velocity.x = 0f;
            velocity.x = -velocity.x * bounciness;
        }
        else if(newPosition.x > allowedArea.xMax)
        {
            newPosition.x = allowedArea.xMax;
            //velocity.x = 0f;
            velocity.x = -velocity.x * bounciness;
        }
        if(newPosition.z < allowedArea.yMin)
        {
            newPosition.z = allowedArea.yMin;
            //velocity.z = 0f;
            velocity.z = -velocity.z * bounciness;
        }
        else if(newPosition.z > allowedArea.yMax)
        {
            newPosition.z = allowedArea.yMax;
            //velocity.z = 0f;
            velocity.z = -velocity.z * bounciness;
        }
        transform.localPosition = newPosition;
    }
    */
    #endregion

    #region 2.Physics

    private void OnValidate()
    {
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        OnValidate();
    }

    void Update()
    {
        //GetComponent<Renderer>().material.SetColor(
        //    "_Color", Color.white * (groundContactCount * 0.25f)
        //);

        GetComponent<Renderer>().material.SetColor(
            "_Color", OnGround ? Color.black : Color.white
        );

        Vector2 playerInput;
        playerInput.x = Input.GetAxis("Horizontal");
        playerInput.y = Input.GetAxis("Vertical");
        playerInput = Vector2.ClampMagnitude(playerInput, 1f);

        desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;

        desiredJump |= Input.GetButtonDown("Jump");
    }

    private void FixedUpdate()
    {
        //velocity = body.velocity;
        UpdateState();
        AdjustVelocity();
        //float acceleration = onGround ? maxAcceleration : maxAirAcceleration;
        //float maxSpeedChange = acceleration * Time.deltaTime;
        //velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);
        //velocity.z = Mathf.MoveTowards(velocity.z, desiredVelocity.z, maxSpeedChange);

        if (desiredJump)
        {
            desiredJump = false;
            Jump();
        }

        body.velocity = velocity;

        ClearState();
    }

    void ClearState()
    {
        //onGround = false;
        groundContactCount = 0;
        contactNormal = Vector3.zero;
    }

    void UpdateState()
    {
        stepsSinceLastGrounded += 1;
        velocity = body.velocity;
        if (OnGround || SnapToGround())
        {
            stepsSinceLastGrounded = 0;
            jumpPhase = 0;
            if (groundContactCount > 1)
            {
                contactNormal.Normalize();
            }
        }
        else
        {
            contactNormal = Vector3.up;
        }
    }

    bool SnapToGround()
    {
        if (stepsSinceLastGrounded > 1)
        {
            return false;
        }
        if (!Physics.Raycast(body.position, Vector3.down, out RaycastHit hit))
        {
            return false;
        }
        if (hit.normal.y < minGroundDotProduct)
        {
            return false;
        }

        groundContactCount = 1;
        contactNormal = hit.normal;
        float speed = velocity.magnitude;
        float dot = Vector3.Dot(velocity, hit.normal);

        if (dot > 0f)
        {
            velocity = (velocity - hit.normal * dot).normalized * speed;
        }

        return true;
    }

    void Jump()
    {
        if (OnGround || jumpPhase < maxAirJumps)
        {
            jumpPhase += 1;
            float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
            float alignedSpeed = Vector3.Dot(velocity, contactNormal);
            if (alignedSpeed > 0f)
            {
                jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
            }
            //velocity.y += jumpSpeed;
            velocity += contactNormal * jumpSpeed;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        EvaluateCollision(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        //onGround = true;
        EvaluateCollision(collision);
    }

    //private void OnCollisionExit()
    //{
    //    onGround = false;
    //}

    void EvaluateCollision(Collision collision)
    {
        for(int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            //onGround |= (normal.y >= minGroundDotProduct);
            if(normal.y >= minGroundDotProduct)
            {
                //onGround = true;
                groundContactCount += 1;
                contactNormal += normal;
            }
        }
    }

    void AdjustVelocity()
    {
        Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
        Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

        float currentX = Vector3.Dot(velocity, xAxis);
        float currentZ = Vector3.Dot(velocity, zAxis);

        float acceleration = OnGround ? maxAcceleration : maxAirAcceleration;
        float maxSpeedChange = acceleration * Time.deltaTime;

        float newX = Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange);
        float newZ = Mathf.MoveTowards(currentZ, desiredVelocity.z, maxSpeedChange);

        velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
    }

    Vector3 ProjectOnContactPlane(Vector3 vector)
    {
        return vector - contactNormal * Vector3.Dot(vector, contactNormal);
    }
    #endregion
}
