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
    [SerializeField] private float flapTerminalVelocity = 5f;
    [SerializeField] private float gildeTerminalVelocity = -1f;
    [SerializeField] private float diveTerminalVelocity = -10f;


    private Animator animator;
    private Rigidbody rb;
    private PlayerInput playerInput;

    private BirdState state;
    private bool grounded;


    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        state = BirdState.Ground;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        print(rb.velocity.y);
        switch (state)
        {
            case BirdState.Ground:

                break;
            case BirdState.Flap:
                if (rb.velocity.y < flapTerminalVelocity)
                {
                    rb.AddForce(Vector3.up * flapForce, ForceMode.Acceleration);
                }
                break;
            case BirdState.Glide:
                if (rb.velocity.y > gildeTerminalVelocity)
                {
                    rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
                }
                break;
            case BirdState.Dive:
                if (rb.velocity.y > diveTerminalVelocity)
                {
                    rb.AddForce(Vector3.down * gravity * 10, ForceMode.Acceleration);
                }
                break;
        }
    }

    void OnFlap()
    {
        state = BirdState.Flap;
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
