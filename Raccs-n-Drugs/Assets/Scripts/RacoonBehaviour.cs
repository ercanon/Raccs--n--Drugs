﻿using System.Collections.Generic;
using UnityEngine;

public class RacoonBehaviour : MonoBehaviour
{
    enum RacoonState
    {
        onPause,
        idle,
        walking,
        buffed,
        charging,
        dead
    }
    private RacoonState rState;
    public float walkSpeed = 5;
    public float buffSpeed = 8;
    public float rotateSpeed = 3.5f;
    public int charges = 3;
    private float timerCharge = 0f;

    [HideInInspector] public bool owned = false;
    [HideInInspector] public Color[] colors;
    private int colorIndex = 0;

    public GameplayScript gameplayScript;
    private Rigidbody rBody;
    private Animator anim;
    private GameObject buffed;
    private Material mat;

    /// <summary> Functions to override movement speed. Will use the last added override. </summary>
    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();

    void Awake()
    {
        ChangeState((int)RacoonState.onPause);

        rBody = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        buffed = transform.GetChild(0).gameObject;
        mat = transform.GetChild(1).GetComponent<SkinnedMeshRenderer>().material;
    }

    void FixedUpdate()
    {
        if (rState != RacoonState.dead && rState != RacoonState.onPause)
        {
            if (rState != RacoonState.charging)
            {
                if (rState != RacoonState.buffed)
                {
                    if (owned)
                    {
                        float targetMovingSpeed = walkSpeed;

                        if (speedOverrides.Count > 0)
                            targetMovingSpeed = speedOverrides[speedOverrides.Count - 1]();

                        // Get targetVelocity from input.
                        Vector2 targetVelocity = new Vector2(Input.GetAxis("Horizontal") * targetMovingSpeed, Input.GetAxis("Vertical") * targetMovingSpeed);

                        // Apply movement.
                        rBody.velocity = new Vector3(targetVelocity.x, 0, targetVelocity.y);

                        //Apply rotation.
                        if (rBody.velocity.magnitude > 0)
                        {
                            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(rBody.velocity), Time.deltaTime * 10f);
                            ChangeState((int)RacoonState.walking);
                        }
                        else
                            ChangeState((int)RacoonState.idle);
                    }
                }
                else
                {
                    if (owned)
                    {
                        transform.Rotate(0, Input.GetAxis("Horizontal") * rotateSpeed, 0);
                        if (Input.GetKeyDown("space"))
                        {
                            ChangeState((int)RacoonState.charging);
                            gameplayScript.conect.SendClientData(5);
                        }
                    }

                    ChangingColors();
                }
            }
            else
            {
                timerCharge += Time.deltaTime;
                if (timerCharge > 1)
                    ChargedTransitions();
            }
        }
    }

    public void ChangeState(int state)
    {
        switch (state)
        {
            case 0: //On Pause
                if (rState == RacoonState.onPause)
                    return;

                rState = RacoonState.onPause;
                break;
            case 1: //Idle
                if (rState == RacoonState.idle)
                    return;

                rState = RacoonState.idle;
                break;
            
            case 2: // Walking
                if (rState == RacoonState.walking)
                    return;

                rState = RacoonState.walking;
                break;
            
            case 3: //Buffed
                if (rState == RacoonState.buffed)
                    return;

                rBody.velocity = Vector3.zero;
                buffed.SetActive(true);
                timerCharge = 0f;
                if (charges <= 0)
                    charges = 3;

                rState = RacoonState.buffed;
                break;

            case 4: //Charging
                if (rState == RacoonState.charging)
                    return;

                rBody.velocity = transform.forward * buffSpeed;
                charges = charges - 1;

                mat.SetColor("_EmissionColor", colors[0]);

                rState = RacoonState.charging;
                break;

            case 5: //Dead
                if (rState == RacoonState.dead)
                    return;

                rState = RacoonState.dead;
                //animation
                break;

            default:
                break;
        }
        anim.SetInteger("rRacoonAnim", (int)rState);
    }

    private void ChargedTransitions()
    {
        if (charges == 0)
        {
            ChangeState((int)RacoonState.idle);
            buffed.SetActive(false);
            gameplayScript.cocaineCanSpawn = true;
        }
        else
            ChangeState((int)RacoonState.buffed);
    }

    private void ChangingColors()
    {
        Color colEmission = mat.GetColor("_EmissionColor");
        if (CompareColors(colEmission, colors[colorIndex]))
        {
            if (colorIndex == 0) colorIndex = 1;
            else colorIndex = 0;
        }
        mat.SetColor("_EmissionColor", Color.Lerp(colEmission, colors[colorIndex], 2f * Time.deltaTime));
    }

    private bool CompareColors(Color colorOne, Color colorTwo)
    {

        if (Mathf.Abs(colorOne.r - colorTwo.r) < 0.02f &&
            Mathf.Abs(colorOne.g - colorTwo.g) < 0.02f &&
            Mathf.Abs(colorOne.b - colorTwo.b) < 0.02f) 
            return true;

        return false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (rState == RacoonState.charging)
        {
            if (collision.gameObject.CompareTag("Player"))
                collision.gameObject.GetComponent<RacoonBehaviour>().ChangeState((int)RacoonState.dead);

            if (collision.gameObject.CompareTag("Bounds"))
                ChargedTransitions();
        }
    }
}