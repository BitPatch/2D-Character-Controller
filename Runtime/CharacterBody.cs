using System;
using UnityEngine;

namespace Bitpatch.CharacterController
{
    public class CharacterBody : MonoBehaviour
    {
        [Tooltip("Specify the character's Rigidbody2D.")] [SerializeField]
        private Rigidbody2D _rigidbody;

        [Tooltip("Determines the degree of influence of gravity on a character.")]
        [Min(0)]
        [field: SerializeField]
        public float GravityFactor { get; private set; } = 1f;

        [Tooltip("Specify the layers that will be considered a solid surface.")] [SerializeField]
        private LayerMask _solidLayers;

        [Tooltip("Maximum movement speed of the body.")] [Min(0)] [SerializeField]
        private float _maxSpeed = 30;

        [Tooltip("Specify the maximum distance from the ground at which a player will"
                 + " automatically be attracted to it and become grounded.")]
        [Min(0)]
        [SerializeField]
        private float _surfaceAnchor = 0.1f;

        [Tooltip("When the velocity movement causes a contact with a Collider2D, a slide maybe occur if the surface " +
                 "angle is less than this angle.")]
        [Range(0, 90)]
        [SerializeField]
        private float _surfaceSlideAngle = 45f;

        [Tooltip("The current speed of the character.")] [SerializeField]
        private Vector2 _velocity;

        [Tooltip("The current status of the character.")] [field: SerializeField]
        private CharacterState _state;

        public Vector2 Velocity
        {
            get => _velocity;

            set => _velocity = value.sqrMagnitude > _sqrMaxSpeed
                ? value.normalized * _maxSpeed
                : value;
        }

        public CharacterState State
        {
            get => _state;

            private set
            {
                if (_state != value)
                {
                    var previousState = _state;
                    _state = value;
                    StateChanged?.Invoke(previousState, value);
                }
            }
        }

        private float _sqrMaxSpeed;

        private float _minGroundVertical;

        public event Action<CharacterState, CharacterState> StateChanged;

        public void Jump(float jumpSpeed)
        {
            Velocity = new Vector2(_velocity.x, jumpSpeed);
            State = CharacterState.Airborne;
        }

        public void SetLocomotionVelocity(float locomotionVelocity)
        {
            Velocity = new Vector2(locomotionVelocity, _velocity.y);
        }

        private void Awake()
        {
            _minGroundVertical = Mathf.Cos(_surfaceSlideAngle * Mathf.PI / 180f);
            _sqrMaxSpeed = _maxSpeed * _maxSpeed;
        }

        private void FixedUpdate()
        {
            Velocity += Time.fixedDeltaTime * GravityFactor * Physics2D.gravity;
            var slideResults = _rigidbody.Slide(_velocity, Time.fixedDeltaTime, CreateSlideMovement());

            if (slideResults.slideHit)
            {
                Velocity = ClipVector(_velocity, slideResults.slideHit.normal);
            }

            if (_velocity.y <= 0 && slideResults.surfaceHit)
            {
                var surfaceHit = slideResults.surfaceHit;
                Velocity = ClipVector(_velocity, surfaceHit.normal);

                if (surfaceHit.normal.y >= _minGroundVertical)
                {
                    State = CharacterState.Grounded;
                    return;
                }
            }

            State = CharacterState.Airborne;
        }

        private Rigidbody2D.SlideMovement CreateSlideMovement()
        {
            return new Rigidbody2D.SlideMovement
            {
                maxIterations = 3,
                surfaceSlideAngle = 90,
                gravitySlipAngle = _surfaceSlideAngle,
                surfaceUp = Vector2.up,
                surfaceAnchor = Vector2.down * _surfaceAnchor,
                gravity = Vector2.zero,
                layerMask = _solidLayers,
                useLayerMask = true,
            };
        }

        private static Vector2 ClipVector(Vector2 vector, Vector2 hitNormal)
        {
            return vector - Vector2.Dot(vector, hitNormal) * hitNormal;
        }
    }
}