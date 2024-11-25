using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using Quaternion = UnityEngine.Quaternion;

public class BirdController : MonoBehaviour
{
    enum BirdState { Ground, Flap, Glide, Dive }

    [SerializeField] private float gravity = 3f;
    [SerializeField] private float flapForce = 5f;


    [SerializeField] private float flapTerminalXZ = 5f;
    [SerializeField] private float flapTerminalY = 8f;


    [SerializeField] private float glideTerminalXZ = 20f;
    [SerializeField] private float glideTerminalY = -1f;


    [SerializeField] private float diveTerminalXZ = 5f;
    [SerializeField] private float diveTerminalY = -10f;
    [SerializeField] private float diveForceXZ = 5f;
    [SerializeField] private float diveForceY = 30f;



    private Animator animator;
    private Rigidbody rb;
    private PlayerInput playerInput;

    private BirdState state;


    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();

        state = BirdState.Ground;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        print(rb.velocity + " , mag: " + rb.velocity.magnitude);

        switch (state)
        {
            case BirdState.Ground:

                break;
            case BirdState.Flap:
                FlapMovement();
                break;
            case BirdState.Glide:
                GlideMovement();
                break;
            case BirdState.Dive:
                DiveMovement();
                break;
        }
    }

    void FlapMovement()
    {
        //Vertical force
        if (rb.velocity.y < flapTerminalY)
        {
            rb.AddForce(Vector3.up * flapForce, ForceMode.Acceleration);
            if (rb.velocity.y > flapTerminalY) rb.velocity = new Vector3(rb.velocity.x, flapTerminalY, rb.velocity.z);
        }

        //Horizontal force
        Vector2 horizontalInput = playerInput.actions["Move"].ReadValue<Vector2>();
        Vector3 horizontalInput3D = transform.forward * horizontalInput.y + transform.right * horizontalInput.x;

        Vector2 horizontalVelocity = new(rb.velocity.x, rb.velocity.z);
        Vector3 horizontalVelocity3D = new(rb.velocity.x, 0, rb.velocity.z);

        if (horizontalInput == Vector2.zero || horizontalVelocity.magnitude > flapTerminalXZ)
        {
            rb.AddForce(-horizontalVelocity3D * flapForce * .03f, ForceMode.Acceleration);
        }
        else
        {
            rb.AddForce(horizontalInput3D * flapForce, ForceMode.Acceleration);
        }

        //Z Rotation
        float zRot = transform.rotation.eulerAngles.z;
        if (Math.Abs(horizontalInput.x) >= .35f)
        {
            //Z rotation - Wing tilt
            if (Math.Abs(zRot - 180) >= 170f)
            {
                zRot -= Math.Sign(horizontalInput.x);
                if (Math.Abs(zRot - 180) < 170f)
                {
                    zRot = Math.Sign(zRot - 180) * 170 + 180;
                }
            }
        }
        else
        {
            if (zRot != 0)
            {
                int prev = Math.Sign(zRot - 180);
                zRot += Math.Sign(zRot - 180);
                if (Math.Sign(zRot - 180) != prev)
                {
                    zRot = 0;
                }
            }
        }
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, zRot);

    }

    void GlideMovement()
    {
        float xRot = transform.rotation.eulerAngles.x;
        float yRot = transform.rotation.eulerAngles.y;
        float zRot = transform.rotation.eulerAngles.z;

        float glideScale = 1;
        Vector2 horizontalInput = playerInput.actions["Move"].ReadValue<Vector2>();

        if (Math.Abs(horizontalInput.y) >= .35f)
        {
            glideScale += Math.Sign(horizontalInput.y) > 0 ? 1 / 10f : -1 / 30f;
            //X Rotation - Nose up/down
            //160 being weird
            if (Math.Abs(xRot - 180) >= 159.999f) //between -20 and 20
            {
                xRot += Math.Sign(horizontalInput.y) * .5f;
                if (Math.Abs(xRot - 180) < 160f) //outside -20 and 20
                {
                    xRot = Math.Sign(xRot - 180) * 160 + 180; //clamp
                }
            }
        }
        else
        {
            if (xRot != 0)
            {
                int prev = Math.Sign(xRot - 180);
                xRot += Math.Sign(xRot - 180);
                if (Math.Sign(xRot - 180) != prev)
                {
                    xRot = 0;
                }
            }
        }

        if (Math.Abs(horizontalInput.x) >= .35f)
        {
            //Y rotation - direction facing
            yRot += horizontalInput.x * .5f;

            //Z rotation - Wing tilt
            if (Math.Abs(zRot - 180) >= 160f)
            {
                zRot -= Math.Sign(horizontalInput.x);
                if (Math.Abs(zRot - 180) < 160f)
                {
                    zRot = Math.Sign(zRot - 180) * 160 + 180;
                }
            }
        }
        else
        {
            if (zRot != 0)
            {
                int prev = Math.Sign(zRot - 180);
                zRot += Math.Sign(zRot - 180);
                if (Math.Sign(zRot - 180) != prev)
                {
                    zRot = 0;
                }
            }
        }
        transform.rotation = Quaternion.Euler(xRot, yRot, zRot);

        //Need velocty to be facing forward
        rb.velocity = transform.forward * rb.velocity.magnitude;
        rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y * glideScale - .3f, rb.velocity.z);

        Vector2 horizontalVelocity = new(rb.velocity.x, rb.velocity.z);
        if (horizontalVelocity.magnitude > glideTerminalXZ)
        {
            horizontalVelocity = horizontalVelocity.normalized * glideTerminalXZ;
            rb.velocity = new Vector3(horizontalVelocity.x, rb.velocity.y, horizontalVelocity.y);
        }
    }

    void DiveMovement()
    {
        //damp previous XZ velocity
        //rb.velocity = new Vector3(rb.velocity.x * .8f, rb.velocity.y, rb.velocity.z * .8f);

        //Add downward force
        if (rb.velocity.y > diveTerminalY)
        {
            rb.AddForce(Vector3.down * diveForceY, ForceMode.Acceleration);
            if (rb.velocity.y < diveTerminalY) rb.velocity = new Vector3(rb.velocity.x, diveTerminalY, rb.velocity.z);
        }

        //Add XZ force
        Vector2 horizontalInput = playerInput.actions["Move"].ReadValue<Vector2>();
        Vector3 horizontalInput3D = transform.forward * horizontalInput.y + transform.right * horizontalInput.x;

        Vector2 horizontalVelocity = new(rb.velocity.x, rb.velocity.z);
        Vector3 horizontalVelocity3D = new(rb.velocity.x, 0, rb.velocity.z);

        if (horizontalInput == Vector2.zero || horizontalVelocity.magnitude > diveTerminalXZ)
        {
            rb.AddForce(-horizontalVelocity3D * diveForceXZ * .1f, ForceMode.Acceleration);

        }
        else
        {
            rb.AddForce(horizontalInput3D * diveForceXZ, ForceMode.Acceleration);

        }
    }


    // Input System Methods

    void OnFlap()
    {
        state = BirdState.Flap;
        rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y * .1f, rb.velocity.z);
        rb.rotation = Quaternion.Euler(rb.rotation.eulerAngles.x, rb.rotation.eulerAngles.y, 0);
        animator.SetTrigger("Flap");
    }

    void OnFlapRelease()
    {
        if (state == BirdState.Flap)
        {
            state = BirdState.Glide;
            animator.SetTrigger("Glide");
        }
    }

    void OnDive()
    {
        if (state != BirdState.Ground)
        {
            state = BirdState.Dive;
            animator.SetTrigger("Dive");
        }
    }

    void OnDiveRelease()
    {
        if (state == BirdState.Dive)
        {
            state = BirdState.Glide;
            animator.SetTrigger("Glide");
        }
    }
}
