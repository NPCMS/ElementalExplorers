using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerGrappleController : MonoBehaviour
{
    // external parameters
    [Header("Grapple Settings")] 
    [SerializeField] private float maxGrappleLength;
    [SerializeReference] private Transform grappleFirePoint;

    [Header("Grapple Animation Settings")] 
    [SerializeField] private int grappleAnimationQuality;
    [SerializeField] private float waveCount;
    [SerializeField] private float waveHeight;
    [SerializeField] private AnimationCurve affectCurve;
    [SerializeField] private Transform grappleEnd;

    // internal parameters
    private LayerMask _ignoreRaycastLayerMask;
    // grapple animation
    private Vector3 _grappleHitLocation;
    private Vector3 _currentGrapplePosition;
    private float _animationCounter;
    private bool _playParticlesOnce;

    // references
    private Transform _cameraRef;
    private SpringJoint _springJoint;
    private LineRenderer _lineRenderer;

    // control variables
    private bool _isGrappling;

    // Start is called before the first frame update
    void Start()
    {
        // setup refs
        _cameraRef = Camera.main.transform;
        _springJoint = gameObject.GetComponent<SpringJoint>();
        _lineRenderer = gameObject.GetComponent<LineRenderer>();

        // setup layer mask
        _ignoreRaycastLayerMask = LayerMask.GetMask("Ignore Raycast");

        // default not grappling
        _isGrappling = false;

        // setup line renderer
        _lineRenderer.positionCount = 15;
    }

    // Update is called once per frame
    void Update()
    {
        // handle the grapple
        HandleGrapple();
    }

    private void LateUpdate()
    {
        // handles line renderer
        DrawRope();
    }

    private void HandleGrapple()
    {
        // if not grappling allow user to start, else allow user to finish
        if (!_isGrappling)
        {
            // code to execute when not grappling
            // check for grapple start
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                Debug.Log("grappling");
                StartGrapple();
            }
        }
        else
        {
            // code to execute when grappling
            // check for grapple end
            if (Input.GetKeyUp(KeyCode.Mouse0))
            {
                EndGrapple();
            }
        }
    }

    // sets up grapple
    private void StartGrapple()
    {
        // perform raycast
        RaycastHit hit;
        if (Physics.Raycast(_cameraRef.position, _cameraRef.forward, out hit, maxGrappleLength))
        {
            // valid target
            _grappleHitLocation = hit.point;
            _isGrappling = true;
            _playParticlesOnce = true;
        }
        // setup spring joint
        // play particles
    }

    // cleans up grapple
    private void EndGrapple()
    {
        _isGrappling = false;
    }

    // heavily inspired by https://github.com/affaxltd/rope-tutorial/blob/master/GrapplingRope.cs
    void DrawRope()
    {
        //If not grappling, don't draw rope
        if (!_isGrappling)
        {
            _currentGrapplePosition = grappleFirePoint.position;
            if (_animationCounter < 1)
                _animationCounter = 1;
            if (_lineRenderer.positionCount > 0)
                _lineRenderer.positionCount = 0;
            return;
        }

        if (_lineRenderer.positionCount == 0)
        {
            _lineRenderer.positionCount = grappleAnimationQuality + 1;
        }


        var grapplePoint = _grappleHitLocation;
        var gunTipPosition = grappleFirePoint.position;
        var up = Quaternion.LookRotation((grapplePoint - gunTipPosition).normalized) * Vector3.up;

        _currentGrapplePosition = Vector3.Lerp(_currentGrapplePosition, grapplePoint, Time.deltaTime * 100f);
        
        // update grapple head position
        grappleEnd.position = _currentGrapplePosition;
        // check if grapple has hit yet
        if (((_currentGrapplePosition - grapplePoint).magnitude < 0.1f) && _playParticlesOnce )
        {
            grappleEnd.GetComponent<ParticleSystem>().Play();
            _playParticlesOnce = false;
        }

        for (var i = 0; i < grappleAnimationQuality + 1; i++) {
            var delta = i / (float) grappleAnimationQuality;
            var offset = up * waveHeight * Mathf.Sin(delta * waveCount * Mathf.PI) * _animationCounter *
                         affectCurve.Evaluate(delta);
            
            _lineRenderer.SetPosition(i, Vector3.Lerp(gunTipPosition, _currentGrapplePosition, delta) + offset);
        }

        if (_animationCounter > 0)
        {
            _animationCounter -= 0.01f;
        }
        else
        {
            _animationCounter = 0;
        }
    }
}