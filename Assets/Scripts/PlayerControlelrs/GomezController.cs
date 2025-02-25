﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GomezController : MonoBehaviour
{
    [SerializeField] private BoxCollider _collider;
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private Animator _animator;
    [SerializeField] private SpriteAligner _spriteAligner;
    [SerializeField] private FEZCameraController _cameraController;
    [SerializeField] private Passage _startPassage;

    // movement variables
    [Header("Movement Variables")]
    [SerializeField] private float _maxSpeed;
    [SerializeField] private float _maxAirSpeed;
    [SerializeField] private float _groundAccel;
    [SerializeField] private float _airAccel;
    [SerializeField] private float _groundFriction;
    [SerializeField] private float _airFriction;
    [SerializeField] private float _jumpForce;
    [SerializeField] private float _jumpHoldMultiplier;
    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private LayerMask _passageMask;
    [SerializeField] private LayerMask _grabbyMask;

    private bool _grounded = true;

    private float _wishDir = 0;
    private float _wishOrn = 0;
    private int _jumpState = 0;
    private int _climbState = 0;
    private float _prevVelY = 0;
    private Vector3 _lastGroundPos;

    // passage handling
    private Passage _passage = null;
    private float _passageTime = 0.0f;
    private bool _passageEntering = false;

    private bool _blockMovement = false;
    private bool _freezed = false;
	
	private bool boosted = false;

    public FEZCameraController CameraController => _cameraController;
    public bool Grounded => _grounded;
    public bool IsPassing => _passage != null;
    public LayerMask GroundMask => _groundMask;

    private bool JumpOverride = false;
    private bool Vup = false;
    private bool Vdown = false;
    private bool Vleft = false;
    private bool Vright = false;

    private void Start()
    {
        if (_startPassage)
        {
            UsePassage(_startPassage, true);
        }

        _lastGroundPos = transform.position;
        _cameraController?.AddFollowTarget(transform);
    }

    private void FixedUpdate()
    {
        // check if we're standing on ground before doing anything
        CheckGround();

        // deal with passing through passages
        UpdatePassageInteraction();

        // player movement
        if (CanMove() && !_freezed)
        {
            HandleMovement();
        }
        else
        {
            if (_prevVelY == 0 && _rigidbody.velocity.y != 0)
            {
                _prevVelY = _rigidbody.velocity.y;
            }
            _rigidbody.velocity = Vector3.zero;
        }
    }

    private void Update()
    {
        if (LevelManager.IsPaused()) return;


        FetchInputs();

        if (CanMove() || IsPassing)
        {
            _freezed = false;
        } else
        {
            _freezed = true;
        }

        UpdateAnimator();
    }

    private void FetchInputs()
    {
        _wishDir = 0;
        _wishOrn = 0;
        if (_blockMovement) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (_jumpState == 0)
            {
                if (_climbState != 0){
                    JumpOverride = true;
                    _climbState = 0;
                    }
                _jumpState = 1;
            }
        }
        else if (!Input.GetKey(KeyCode.Space))
        {
            _jumpState = 0;
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            if (CanControl())
            {
                AttemptPassage();
                AttemptWallGrab();
            }
        }
        if (Input.GetKey(KeyCode.A)) _wishDir -= 1;

        if (Input.GetKey(KeyCode.D)) _wishDir += 1;

        if (Input.GetKey(KeyCode.S) && _climbState == 2) _wishOrn -= 1;

        if (Input.GetKey(KeyCode.W) && _climbState == 2) _wishOrn += 1;
        //if (Input.GetKey(KeyCode.S) && Input.GetKeyDown(KeyCode.Space)) AttemptLGrab();

    }

    // updates ground flag
    private void CheckGround()
    {
        _grounded = Physics.CheckBox(
            transform.position - new Vector3(0, _collider.size.y * 0.5f, 0),
            new Vector3(_collider.size.x * 0.499f, 0.01f, _collider.size.z * 0.499f),
            Quaternion.identity,
            _groundMask
        );

        // additional check for safe spot for respawning.
        // Why am I doing this differently than normal ground check?
        // Glad you asked. I have absolutely no idea!!!!
        if (CanMove() && !_freezed)
        {
            bool safeGround = Physics.Raycast(
                transform.position - new Vector3(0, _collider.size.y * 0.49f, 0),
                Vector3.down, 0.02f, _groundMask
            );
            if (safeGround)
            {
                _lastGroundPos = transform.position;
            }
        }
    }

    // updates which directions you can go for climbing
    private void CheckVine()
    {
        //LILELY JANKY AS HELL. There probably is a better way to do this, but for now, deal with this hell
        Vector3 OgTM = transform.position;

        Vector3 Pxn = new Vector3(OgTM.x + 0.2f, OgTM.y, OgTM.z);
        Vector3 Pyn = new Vector3(OgTM.x, OgTM.y + 0.2f, OgTM.z);
        Vector3 Pzn = new Vector3(OgTM.x, OgTM.y, OgTM.z + 0.2f);
        Vector3 Nxn = new Vector3(OgTM.x - 0.2f, OgTM.y, OgTM.z);
        Vector3 Nyn = new Vector3(OgTM.x, OgTM.y - 0.2f, OgTM.z);
        Vector3 Nzn = new Vector3(OgTM.x, OgTM.y, OgTM.z - 0.2f);
        Vup = Physics.Raycast(Pyn, Camera.main.transform.forward, out var hit, 128.0f, _grabbyMask);
        Vdown = Physics.Raycast(Nyn, Camera.main.transform.forward, out var hat, 128.0f, _grabbyMask);
        
        if (_cameraController.PhysicsAngle == 0)
        {
            Vleft = Physics.Raycast(Nxn, Camera.main.transform.forward, out var hot, 128.0f, _grabbyMask);
            Vright = Physics.Raycast(Pxn, Camera.main.transform.forward, out var hut, 128.0f, _grabbyMask);
        } else if (_cameraController.PhysicsAngle == 90)
        {
            Vleft = Physics.Raycast(Pzn, Camera.main.transform.forward, out var hot, 128.0f, _grabbyMask);
            Vright = Physics.Raycast(Nzn, Camera.main.transform.forward, out var hut, 128.0f, _grabbyMask);
        } else if (_cameraController.PhysicsAngle == 180)
        {
            Vleft = Physics.Raycast(Pxn, Camera.main.transform.forward, out var hot, 128.0f, _grabbyMask);
            Vright = Physics.Raycast(Nxn, Camera.main.transform.forward, out var hut, 128.0f, _grabbyMask);
        }
        else if (_cameraController.PhysicsAngle == 270)
        {
            Vleft = Physics.Raycast(Nzn, Camera.main.transform.forward, out var hot, 128.0f, _grabbyMask);
            Vright = Physics.Raycast(Pzn, Camera.main.transform.forward, out var hut, 128.0f, _grabbyMask);
        }





    }


    public void PistoneBoost(){
		boosted = true;
	}
    private void HandleMovement()
    {
        Vector3 vel = _rigidbody.velocity;

        // bringing back previous y velocity
        // we reset x velocity, so that doesn't need to be saved
        if (_prevVelY != 0)
        {
            vel.y = _prevVelY;
            _prevVelY = 0;
        }
		
		// boosting
		if (boosted){
			vel.y = 20;
		}
		boosted = false;

        


        // jumping
        if ((_grounded || JumpOverride) && _jumpState != 0)
        {
            JumpOverride = false;
            vel.y = _jumpForce;
            _grounded = false;
            _jumpState = 2;
        }
        if (vel.y < 0)
        {
            _jumpState = 0;
        }

        // gravity
        if (!_grounded && _climbState == 0)
        {
            vel.y += Physics.gravity.y * (_jumpState == 2 ? _jumpHoldMultiplier : 1);
        }

        // horizontal movement
        
        Vector3 moveDir = Quaternion.Euler(0, _cameraController.PhysicsAngle + 90, 0) * Vector3.forward;
        float curSpeed = Vector3.Dot(_rigidbody.velocity, moveDir);
        if (_climbState == 0)
        {
            


            curSpeed += (_grounded ? _groundAccel : _airAccel) * _wishDir;
            float maxCurSpeed = (_grounded ? _maxSpeed : _maxAirSpeed);
            if (Mathf.Abs(curSpeed) > maxCurSpeed)
            {
                curSpeed = curSpeed / Mathf.Abs(curSpeed) * maxCurSpeed;
            }
            if (_wishDir == 0)
            {
                curSpeed *= 1 - (_grounded ? _groundFriction : _airFriction);
            }
            
        } else // climb movement
        {
            CheckVine();
            curSpeed = 2 * _wishDir;
            vel.y = 2 * _wishOrn;
            if (!Vup && vel.y > 0)
            {
                vel.y = 0;
            }
            if (!Vdown && vel.y < 0)
            {
                vel.y = 0;
            }
            if (!Vright && curSpeed > 0)
            {
                curSpeed = 0;
            }
            if (!Vleft && curSpeed < 0)
            {
                curSpeed = 0;
            }

        }
        
        vel = moveDir * curSpeed + Vector3.up * vel.y;

        _rigidbody.velocity = vel;
    }
	
    public void AttemptPassage()
    {
        if (!_grounded) return;

        bool foundPassage = Physics.Raycast(transform.position, Camera.main.transform.forward, out var hit, 128.0f, _passageMask);
        if (foundPassage)
        {
            var obj = hit.collider.gameObject;
            var passage = obj.GetComponent<Passage>();
            if (passage && passage.CanPassThrough() && Vector3.Dot(Camera.main.transform.forward, -hit.normal) > 0.9)
            {
                UsePassage(passage);
            }
        }
    }

    public void AttemptWallGrab()
    {

        bool UpDong = Physics.Raycast(transform.position, Camera.main.transform.forward, out var hit, 128.0f, _grabbyMask);
        if (UpDong)
        {
            var obj = hit.collider.gameObject;
            var direction = obj.transform.forward;
            doClimb(obj.transform.position);
        }
    }

    public void AttemptLGrab()
    {
        bool wallInWay = Physics.Raycast(transform.position, -Camera.main.transform.forward, out var hit, 128.0f, _groundMask);
        if (!_grounded || wallInWay) return;
        
        //This is literally not even a fetus yet. Don't judge, it'll change (I hope)
        DoAnLGrab();
    }

    private void UpdatePassageInteraction()
    {
        if (_passage == null) return;

        var desiredPassageTime = (_passageEntering) ? _passage.WalkInTime : _passage.WalkOutTime;

        var passageFactor = _passageTime / desiredPassageTime;
        if (_passageEntering) passageFactor = 1 - passageFactor;

        var walkInOffset = 0.5f * _collider.size.x * passageFactor;

        // slowly adjust the player position so it gets closer to the passage's align position
        Vector3 pos;
        if (_passageEntering)
        {
            pos = transform.position;
            var offsetForce = Vector3.Dot(_passage.Alignment.forward, pos - _passage.Alignment.position) - walkInOffset;
            pos -= _passage.Alignment.forward * offsetForce;
        } 
        else
        {
            pos = _passage.Alignment.position + _passage.Alignment.forward * walkInOffset;
        }
        
        Vector3 desiredPos = _passage.Alignment.position + _passage.Alignment.forward * walkInOffset;
        transform.position = Vector3.MoveTowards(pos, desiredPos, _maxSpeed * Time.fixedDeltaTime * 0.25f);

        // update some parameters
        _spriteAligner.UsePhysicsAngle = true;
        _cameraController.ControlEnabled = false;
        _collider.enabled = false;
        _rigidbody.velocity = Vector3.zero;

        _passageTime += Time.fixedDeltaTime;
        // passage ends here
        if(_passageTime >= desiredPassageTime)
        {
            if (_passageEntering)
            {
                _passage.OnPassage?.Invoke();
            }

            // move the player to another passage if current one leads to it
            if (_passageEntering && _passage.TargetPassage)
            {
                _passage.TargetPassage.OnPassage?.Invoke();
                UsePassage(_passage.TargetPassage, true);
            } 
            else
            {
                _collider.enabled = true;
                _spriteAligner.UsePhysicsAngle = false;
                _passage.OnPassageExit?.Invoke();

                _cameraController.ControlEnabled = true;
                _passageTime = 0;
                _passage = null;
            }
        }
    }

    private void UsePassage(Passage passage, bool exit=false)
    {
        _passage = passage;
        _passageTime = 0.0f;
        _passageEntering = !exit;

        if (exit)
        {
            _animator.Play("gomez_walkout",0, 0.8f - _passage.WalkOutTime);
        } 
        else
        {
            _animator.Play("gomez_walkin");
        }

        // rotate camera so it enters/exits the passage away from/towards it
        float ang = Vector3.SignedAngle(_cameraController.transform.forward, -passage.transform.forward, Vector3.up);
        int rotations = (int)Mathf.Floor((Mathf.Abs(ang) + 45) / 90);
        for (int i = 0; i < rotations; i++)
        {
            _cameraController.Shift(ang > 0 ? ShiftDirection.LEFT : ShiftDirection.RIGHT);
        }

        if(!exit) passage.OnPassageEntry?.Invoke();
    }

    private void DoAnLGrab()
    {

        //_animator.Play("gomez_ledgegrab", 0, 1.0f);
        _climbState = 1;

    }
    private void doClimb(Vector3 wall)
    {
        if (_cameraController.PhysicsAngle == 0)
        {
            transform.position =  new Vector3(transform.position.x, transform.position.y, wall.z - 0.3f);
        }
        else if (_cameraController.PhysicsAngle == 90)
        {
            transform.position = new Vector3(wall.x - 0.3f, transform.position.y, transform.position.z);
        }
        else if (_cameraController.PhysicsAngle == 180)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, wall.z + 0.3f);
        }
        else if (_cameraController.PhysicsAngle == 270)
        {
            transform.position = new Vector3(wall.x + 0.3f, transform.position.y, transform.position.z);
        }
        //_animator.Play("gomez_wallgrab", 0, 1.0f);
        _climbState = 2;

    }

    private void UpdateAnimator()
    {
        if (CanMove() || IsPassing)
        {
            _animator.SetBool("Grounded", _grounded);

            Vector3 vel = _rigidbody.velocity;

            float jumpState = Mathf.Clamp(-vel.y / (_jumpForce * 2) + 0.5f, 0, 1);
            _animator.SetFloat("FlyState", jumpState);


            vel.y = 0;
            float horizontalVel = vel.magnitude;

            _animator.SetBool("Walking", horizontalVel > 0.1f);
            _animator.SetBool("Running", horizontalVel > _maxSpeed * 0.6f);

            _animator.SetInteger("Timer", (_animator.GetInteger("Timer") + 1) % 1000);
            if (Random.Range(0, 1000) < 2) _animator.SetTrigger("Blink");

            float moveDir = Vector3.Dot(vel, _cameraController.Camera.transform.right);

            int flipDir = 0;
            int edgeFlipDir = 0;

            if (_wishDir > 0)
            {
                flipDir = 1;
            } else if (_wishDir < 0)
            {
                flipDir = -1;
            }

            bool standingOnEdge = false;
            if (_grounded && _rigidbody.velocity.magnitude < 0.01f)
            {
                Vector3 startPos = transform.position + Vector3.down * _collider.size.y * 0.49f;
                Vector3 edgeOffset = Camera.main.transform.right * _collider.size.x * 0.5f;
                if (!Physics.Raycast(startPos, Vector3.down, 0.1f, _groundMask))
                {
                    for (int i = -1; i <= 1; i += 2)
                    {
                        bool hasEdge = Physics.Raycast(startPos + edgeOffset * i, Vector3.down, 0.1f, _groundMask);
                        if (!hasEdge)
                        {
                            standingOnEdge = !standingOnEdge; // prevents activating the animation when standing "midair"
                            edgeFlipDir = i;
                        }
                    }
                }
            }

            if (standingOnEdge) flipDir = edgeFlipDir;
            if (flipDir != 0) FlipSprite(flipDir < 0 ? true : false);

            _animator.SetBool("OnEdge", standingOnEdge);

            // special cases

            if (moveDir * _wishDir < -0.1 && _grounded)
            {
                _animator.Play("gomez_drift");
            }

            _animator.speed = 1;
        } else
        {
            _animator.speed = 0;
        }
    }

    public void FlipSprite(bool flipped)
    {
        _spriteAligner.Mirrored = flipped;
    }

    public void BlockMovement(bool block)
    {
        _blockMovement = block;
        _cameraController.ControlEnabled = !block;
    }

    public bool CanMove()
    {
        return !_cameraController.IsShifting() && !IsPassing;
    }

    public bool CanControl()
    {
        return CanMove() && !_blockMovement;
    }

    public void DieFromFallingIntoDeepAndDarkAbbys()
    {
        StartCoroutine("DieSequence");
		gameObject.GetComponent<ObjectGrabber>().AttemptGrab();

    }

    private IEnumerator DieSequence()
    {
        _animator.SetBool("Dying", true);
        BlockMovement(true);
        yield return new WaitForSeconds(0.5f);
        _cameraController.RemoveFollowTarget(transform);
        yield return new WaitForSeconds(0.5f);

        _animator.SetBool("Dying", false);
        BlockMovement(false);
        _cameraController.AddFollowTarget(transform);
        _rigidbody.velocity = Vector3.zero;
        transform.position = _lastGroundPos;
    }
}
