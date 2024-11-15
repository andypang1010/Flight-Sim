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


    [SerializeField] private float flapTerminalX = 5f;
    [SerializeField] private float flapTerminalY = 8f;
    [SerializeField] private float glideTerminalX = 5f;
    [SerializeField] private float gildeTerminalY = -1f;
    [SerializeField] private float gildeTerminalZ = 20f;
    [SerializeField] private float diveTerminalX = 2f;
    [SerializeField] private float diveTerminalY = -10f;


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
        print(rb.velocity);
        switch (state)
        {
            case BirdState.Ground:

                break;
            case BirdState.Flap:
                if (rb.velocity.y < flapTerminalY)
                {
                    rb.AddForce(Vector3.up * flapForce, ForceMode.Acceleration);
                    if (rb.velocity.y > flapTerminalY) rb.velocity = new Vector3(rb.velocity.x, flapTerminalY, rb.velocity.z);
                }

                GetFlapDirection();
                break;
            case BirdState.Glide:
                if (rb.velocity.y > gildeTerminalY)
                {
                    rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
                    if (rb.velocity.y < gildeTerminalY) rb.velocity = new Vector3(rb.velocity.x, gildeTerminalY, rb.velocity.z);
                }
                else if (rb.velocity.y < gildeTerminalY)
                {
                    rb.AddForce(Vector3.up * gravity * 2, ForceMode.Acceleration);
                    if (rb.velocity.y > gildeTerminalY) rb.velocity = new Vector3(rb.velocity.x, gildeTerminalY, rb.velocity.z);
                }

                GetGlideDirection();
                break;
            case BirdState.Dive:
                if (rb.velocity.y > diveTerminalY)
                {
                    rb.AddForce(Vector3.down * gravity * 10, ForceMode.Acceleration);
                    if (rb.velocity.y < diveTerminalY) rb.velocity = new Vector3(rb.velocity.x, diveTerminalY, rb.velocity.z);
                }
                break;
        }
    }

    void GetFlapDirection()
    {
        Vector2 horizontalInput = playerInput.actions["Move"].ReadValue<Vector2>();
        Vector3 horizontalInput3D = new Vector3(horizontalInput.x, 0, horizontalInput.y);

        if (horizontalInput3D != Vector3.zero)
        {
            rb.AddForce(horizontalInput3D * flapForce, ForceMode.Acceleration);
        }
        else
        {
            float damp = .9f;
            rb.velocity = new Vector3(rb.velocity.x * damp, rb.velocity.y, rb.velocity.z * damp);
        }

        //transform.rotation = Quaternion.Euler(0, 0, horizontalInput3D.x * -20);

        Vector2 horizontalVelocity = new(rb.velocity.x, rb.velocity.z);
        if (horizontalVelocity.magnitude > flapTerminalX)
        {
            horizontalVelocity = horizontalVelocity.normalized * flapTerminalX;
            rb.velocity = new Vector3(horizontalVelocity.x, rb.velocity.y, horizontalVelocity.y);
        }
    }

    void GetGlideDirection()
    {
        float xRot = transform.rotation.eulerAngles.x;
        float yRot = transform.rotation.eulerAngles.y;
        float zRot = transform.rotation.eulerAngles.z;

        float glideScale = 1;
        Vector2 horizontalInput = playerInput.actions["Move"].ReadValue<Vector2>();
        Vector3 horizontalInput3D = new Vector3(horizontalInput.x, 0, horizontalInput.y);

        if (Math.Abs(horizontalInput.y) >= .35f)
        {
            glideScale = 1 + Math.Sign(horizontalInput.y) / 2;
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
        rb.AddForce(Vector3.forward * 5 * glideScale, ForceMode.Acceleration);

        if (rb.velocity.z > gildeTerminalZ)
        {
            rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y, gildeTerminalZ);
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
