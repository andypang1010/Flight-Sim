using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BirdController : MonoBehaviour
{
    enum BirdState { Ground, Flap, Glide, Dive }

    [SerializeField] private float gravity = 3f;
    [SerializeField] private float flapForce = 5f;


    [SerializeField] private float flapTerminalX = 5f;
    [SerializeField] private float flapTerminalY = 8f;
    [SerializeField] private float glideTerminalX = 8f;
    [SerializeField] private float gildeTerminalY = -1f;
    [SerializeField] private float diveTerminalX = 2f;
    [SerializeField] private float diveTerminalY = -10f;


    private Animator animator;
    private Rigidbody rb;
    private PlayerInput playerInput;

    private BirdState state;


    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
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
        Vector3 facing = new Vector3(horizontalInput.x, 0, horizontalInput.y);

        if (facing != Vector3.zero)
        {
            //transform.forward = facing;
            rb.AddForce(facing * flapForce, ForceMode.Acceleration);
        }
        else
        {
            float damp = .9f;
            rb.velocity = new Vector3(rb.velocity.x * damp, rb.velocity.y, rb.velocity.z * damp);
        }


        Vector2 horizontalVelocity = new(rb.velocity.x, rb.velocity.z);
        if (horizontalVelocity.magnitude > flapTerminalX)
        {
            horizontalVelocity = horizontalVelocity.normalized * flapTerminalX;
            rb.velocity = new Vector3(horizontalVelocity.x, rb.velocity.y, horizontalVelocity.y);
        }

        // if (playerInput.actions["Forward"].IsPressed())
        // {
        //     if (rb.velocity.z < flapTerminalX)
        //     {
        //         rb.AddForce(Vector3.forward * flapForce, ForceMode.Acceleration);
        //         if (rb.velocity.z > flapTerminalX) rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y, flapTerminalX);
        //     }
        // }
        // else if (playerInput.actions["Back"].IsPressed())
        // {
        //     if (rb.velocity.z > -flapTerminalX)
        //     {
        //         rb.AddForce(Vector3.back * flapForce, ForceMode.Acceleration);
        //         if (rb.velocity.z < -flapTerminalX) rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y, -flapTerminalX);
        //     }
        // }

        // if (playerInput.actions["Left"].IsPressed())
        // {
        //     if (rb.velocity.x > -flapTerminalX)
        //     {
        //         rb.AddForce(Vector3.left * flapForce, ForceMode.Acceleration);
        //         if (rb.velocity.x < -flapTerminalY) rb.velocity = new Vector3(-flapTerminalY, rb.velocity.y, rb.velocity.z);
        //     }
        //     //add angle left
        // }
        // else if (playerInput.actions["Right"].IsPressed())
        // {
        //     if (rb.velocity.x < flapTerminalX)
        //     {
        //         rb.AddForce(Vector3.right * flapForce, ForceMode.Acceleration);
        //         if (rb.velocity.x > flapTerminalY) rb.velocity = new Vector3(flapTerminalY, rb.velocity.y, rb.velocity.z);
        //     }
        //     //add angle right
        // }
    }

    void GetGlideDirection()
    {
        // if (playerInput.actions["Forward"].IsPressed())
        // {

        // }
        // else if (playerInput.actions["Back"].IsPressed())
        // {

        // }
        // else
        // {
        //     if (rb.velocity.z < glideTerminalX)
        //     {
        //         rb.AddForce(Vector3.forward * 3, ForceMode.Acceleration);
        //         if (rb.velocity.z > glideTerminalX) rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y, glideTerminalX);
        //     }
        // }


        // if (playerInput.actions["Left"].IsPressed())
        // {
        //     if (rb.velocity.x > -flapTerminalX)
        //     {
        //         rb.AddForce(Vector3.left * flapForce, ForceMode.Acceleration);
        //         if (rb.velocity.x < -flapTerminalY) rb.velocity = new Vector3(-flapTerminalY, rb.velocity.y, rb.velocity.z);
        //     }
        //     //add angle left
        // }
        // else if (playerInput.actions["Right"].IsPressed())
        // {
        //     if (rb.velocity.x < flapTerminalX)
        //     {
        //         rb.AddForce(Vector3.right * flapForce, ForceMode.Acceleration);
        //         if (rb.velocity.x > flapTerminalY) rb.velocity = new Vector3(flapTerminalY, rb.velocity.y, rb.velocity.z);
        //     }
        //     //add angle right
        // }
    }


    // Input System Methods

    void OnFlap()
    {
        state = BirdState.Flap;
        rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y * .1f, rb.velocity.z);
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
