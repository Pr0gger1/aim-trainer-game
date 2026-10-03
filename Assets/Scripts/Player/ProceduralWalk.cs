using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(Animator))]
    public class ProceduralWalk : MonoBehaviour
    {
        [Header("Locomotion")]
        [Tooltip("Legacy cadence scale. 5.5 keeps the original inspector value while the new gait uses a human-readable frequency.")]
        public float walkCycle = 5.5f;
        [Tooltip("Maximum forward/backward swing of the thighs in degrees.")]
        public float legSwing = 26f;
        [Tooltip("Maximum bend of a knee while its foot is in the air.")]
        public float kneeBend = 34f;
        [Tooltip("Upper-arm swing in degrees.")]
        public float armSwing = 18f;
        [Tooltip("Planar speed at which the gait starts.")]
        public float minSpeed = 0.35f;
        [Tooltip("Total visual bob applied to the model root, not the CharacterController.")]
        public float bobHeight = 0.04f;

        [SerializeField, Min(0.1f)] private float walkFrequency = 1.55f;
        [SerializeField, Min(0.1f)] private float sprintFrequency = 2.25f;
        [SerializeField, Min(0.1f)] private float fullStrideSpeed = 4.5f;
        [SerializeField, Min(0.1f)] private float sprintSpeed = 8f;
        [SerializeField, Min(0f)] private float sprintAmplitude = 1.08f;
        [SerializeField, Min(0.01f)] private float accelerationTime = 0.08f;
        [SerializeField, Min(0.01f)] private float decelerationTime = 0.16f;
        [SerializeField, Min(0.01f)] private float gaitBlendTime = 0.14f;
        [SerializeField, Min(0.01f)] private float directionSmoothing = 0.1f;
        [SerializeField, Min(0.01f)] private float modelBobSmoothing = 0.06f;

        [Header("Footwork")]
        [SerializeField, Min(0f)] private float footRoll = 12f;
        [SerializeField, Min(0f)] private float toeLift = 10f;
        [SerializeField, Min(0f)] private float hipSway = 2.2f;
        [SerializeField, Min(0f)] private float hipRoll = 2.4f;
        [SerializeField, Min(0f)] private float hipTwist = 2.1f;
        [SerializeField, Min(0f)] private float lateralBob = 0.012f;

        [Header("Upper Body")]
        [SerializeField, Min(0f)] private float accelerationLean = 3.5f;
        [SerializeField, Min(0f)] private float sprintLean = 2.5f;
        [SerializeField, Min(0f)] private float spineCounterRoll = 1.8f;
        [SerializeField, Min(0f)] private float spineTwist = 1.4f;
        [SerializeField, Min(0f)] private float shoulderSway = 3.5f;
        [SerializeField, Min(0f)] private float forearmBend = 8f;
        [SerializeField, Min(0f)] private float headStabilization = 1.5f;

        private const float TwoPi = Mathf.PI * 2f;
        private const float LegacyCycleReference = 5.5f;

        private CharacterController _controller;
        private Animator _animator;
        private Transform _model;

        private Transform _hips;
        private Transform _spine;
        private Transform _spine1;
        private Transform _spine2;
        private Transform _neck;
        private Transform _head;
        private Transform _leftUpLeg;
        private Transform _rightUpLeg;
        private Transform _leftLeg;
        private Transform _rightLeg;
        private Transform _leftFoot;
        private Transform _rightFoot;
        private Transform _leftToe;
        private Transform _rightToe;
        private Transform _leftShoulder;
        private Transform _rightShoulder;
        private Transform _leftArm;
        private Transform _rightArm;
        private Transform _leftForeArm;
        private Transform _rightForeArm;

        private Quaternion _baseHips;
        private Quaternion _baseSpine;
        private Quaternion _baseSpine1;
        private Quaternion _baseSpine2;
        private Quaternion _baseNeck;
        private Quaternion _baseHead;
        private Quaternion _baseLeftUpLeg;
        private Quaternion _baseRightUpLeg;
        private Quaternion _baseLeftLeg;
        private Quaternion _baseRightLeg;
        private Quaternion _baseLeftFoot;
        private Quaternion _baseRightFoot;
        private Quaternion _baseLeftToe;
        private Quaternion _baseRightToe;
        private Quaternion _baseLeftShoulder;
        private Quaternion _baseRightShoulder;
        private Quaternion _baseLeftArm;
        private Quaternion _baseRightArm;
        private Quaternion _baseLeftForeArm;
        private Quaternion _baseRightForeArm;

        private Vector3 _baseModelPosition;
        private Vector3 _smoothedDirection = Vector3.forward;
        private float _smoothedSpeed;
        private float _speedVelocity;
        private float _gaitWeight;
        private float _gaitWeightVelocity;
        private float _smoothedAcceleration;
        private float _previousSpeed;
        private float _phase;
        private bool _initialized;

        private void Awake()
        {
            _model = transform;
            _controller = GetComponentInParent<CharacterController>();
            _animator = GetComponent<Animator>();

            if (_animator != null)
                _animator.enabled = false;

            CacheBones();
            CacheBasePose();
            _initialized = true;
        }

        private void LateUpdate()
        {
            if (!_initialized)
                return;

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
                return;

            Vector3 planarVelocity = _controller != null
                ? new Vector3(_controller.velocity.x, 0f, _controller.velocity.z)
                : Vector3.zero;
            Vector3 localVelocity = _model.InverseTransformDirection(planarVelocity);
            float targetSpeed = localVelocity.magnitude;
            bool grounded = _controller == null || _controller.isGrounded;

            UpdateLocomotion(localVelocity, targetSpeed, grounded, deltaTime);
            ApplyPose(deltaTime);
        }

        private void UpdateLocomotion(Vector3 localVelocity, float targetSpeed, bool grounded, float deltaTime)
        {
            float smoothingTime = targetSpeed > _smoothedSpeed ? accelerationTime : decelerationTime;
            _smoothedSpeed = Mathf.SmoothDamp(
                _smoothedSpeed,
                targetSpeed,
                ref _speedVelocity,
                Mathf.Max(0.01f, smoothingTime),
                Mathf.Infinity,
                deltaTime);

            float rawAcceleration = (_smoothedSpeed - _previousSpeed) / deltaTime;
            _smoothedAcceleration = Mathf.Lerp(
                _smoothedAcceleration,
                Mathf.Clamp(rawAcceleration, -20f, 20f),
                DampFactor(0.08f, deltaTime));
            _previousSpeed = _smoothedSpeed;

            if (localVelocity.sqrMagnitude > 0.0001f)
            {
                Vector3 targetDirection = localVelocity.normalized;
                _smoothedDirection = Vector3.Lerp(
                    _smoothedDirection,
                    targetDirection,
                    DampFactor(directionSmoothing, deltaTime));
                _smoothedDirection.Normalize();
            }

            float targetWeight = grounded
                ? Mathf.InverseLerp(minSpeed, fullStrideSpeed, _smoothedSpeed)
                : 0f;
            _gaitWeight = Mathf.SmoothDamp(
                _gaitWeight,
                Mathf.Clamp01(targetWeight),
                ref _gaitWeightVelocity,
                Mathf.Max(0.01f, gaitBlendTime),
                Mathf.Infinity,
                deltaTime);

            if (grounded && _smoothedSpeed > minSpeed * 0.8f)
            {
                float strideSpeed = Mathf.InverseLerp(minSpeed, fullStrideSpeed, _smoothedSpeed);
                float sprintWeight = Mathf.InverseLerp(fullStrideSpeed, sprintSpeed, _smoothedSpeed);
                float legacyCadence = Mathf.Clamp(walkCycle / LegacyCycleReference, 0.35f, 2f);
                float cadence = Mathf.Lerp(walkFrequency, sprintFrequency, sprintWeight) * legacyCadence;
                float speedFactor = Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(strideSpeed));
                float directionSign = _smoothedDirection.z < -0.2f ? -1f : 1f;

                _phase = Mathf.Repeat(
                    _phase + cadence * TwoPi * speedFactor * directionSign * deltaTime,
                    TwoPi);
            }
        }

        private void ApplyPose(float deltaTime)
        {
            float sprintWeight = Mathf.InverseLerp(fullStrideSpeed, sprintSpeed, _smoothedSpeed);
            float strideAmount = _gaitWeight * Mathf.Lerp(1f, sprintAmplitude, sprintWeight);
            float leftStride = Mathf.Sin(_phase);
            float rightStride = Mathf.Sin(_phase + Mathf.PI);
            float leftKnee = KneeCurve(_phase);
            float rightKnee = KneeCurve(_phase + Mathf.PI);
            float leftFootRoll = FootRollCurve(_phase);
            float rightFootRoll = FootRollCurve(_phase + Mathf.PI);
            float leftToeLift = ToeCurve(_phase);
            float rightToeLift = ToeCurve(_phase + Mathf.PI);

            float forwardAmount = _smoothedDirection.z;
            float strafeAmount = _smoothedDirection.x;
            float strideScale = Mathf.Lerp(0.72f, 1f, Mathf.Abs(forwardAmount));
            float effectiveLegSwing = legSwing * strideScale;
            float effectiveArmSwing = armSwing * Mathf.Lerp(0.8f, 1f, Mathf.Abs(forwardAmount));
            float bodyWeight = strideAmount;

            float accelerationLeanAngle = Mathf.Clamp(_smoothedAcceleration / 8f, -1f, 1f) * accelerationLean;
            float leanAngle = accelerationLeanAngle + sprintWeight * sprintLean * Mathf.Sign(forwardAmount);
            float pelvisRoll = leftStride * hipRoll * bodyWeight;
            float pelvisYaw = leftStride * hipTwist * bodyWeight + strafeAmount * hipTwist * 0.35f * bodyWeight;
            float pelvisPitch = -leanAngle * bodyWeight;

            ApplyRotation(_hips, _baseHips, pelvisPitch, pelvisYaw, pelvisRoll);
            ApplyRotation(_spine, _baseSpine, -pelvisPitch * 0.18f, -pelvisYaw * 0.2f, -pelvisRoll * spineCounterRoll / 2f);
            ApplyRotation(_spine1, _baseSpine1, -pelvisPitch * 0.22f, -pelvisYaw * 0.25f, -pelvisRoll * spineCounterRoll / 2f);
            ApplyRotation(_spine2, _baseSpine2, -pelvisPitch * 0.28f, -pelvisYaw * 0.3f, -pelvisRoll * spineCounterRoll);
            ApplyRotation(_neck, _baseNeck, -pelvisPitch * 0.2f, -pelvisYaw * 0.35f, -pelvisRoll * headStabilization);
            ApplyRotation(_head, _baseHead, -pelvisPitch * 0.25f, -pelvisYaw * 0.45f, -pelvisRoll * headStabilization);

            ApplyRotation(_leftUpLeg, _baseLeftUpLeg, leftStride * effectiveLegSwing * bodyWeight, 0f, 0f);
            ApplyRotation(_rightUpLeg, _baseRightUpLeg, rightStride * effectiveLegSwing * bodyWeight, 0f, 0f);
            ApplyRotation(_leftLeg, _baseLeftLeg, leftKnee * kneeBend * bodyWeight, 0f, 0f);
            ApplyRotation(_rightLeg, _baseRightLeg, rightKnee * kneeBend * bodyWeight, 0f, 0f);
            ApplyRotation(_leftFoot, _baseLeftFoot, leftFootRoll * bodyWeight, 0f, 0f);
            ApplyRotation(_rightFoot, _baseRightFoot, rightFootRoll * bodyWeight, 0f, 0f);
            ApplyRotation(_leftToe, _baseLeftToe, leftToeLift * bodyWeight, 0f, 0f);
            ApplyRotation(_rightToe, _baseRightToe, rightToeLift * bodyWeight, 0f, 0f);

            float leftArmSwing = -rightStride * effectiveArmSwing * bodyWeight;
            float rightArmSwing = -leftStride * effectiveArmSwing * bodyWeight;
            float leftShoulderRoll = -rightStride * shoulderSway * bodyWeight;
            float rightShoulderRoll = -leftStride * shoulderSway * bodyWeight;
            float leftForearm = leftKnee * forearmBend * bodyWeight;
            float rightForearm = rightKnee * forearmBend * bodyWeight;

            ApplyRotation(_leftShoulder, _baseLeftShoulder, 0f, 0f, leftShoulderRoll);
            ApplyRotation(_rightShoulder, _baseRightShoulder, 0f, 0f, rightShoulderRoll);
            ApplyRotation(_leftArm, _baseLeftArm, leftArmSwing, 0f, leftShoulderRoll * 0.35f);
            ApplyRotation(_rightArm, _baseRightArm, rightArmSwing, 0f, rightShoulderRoll * 0.35f);
            ApplyRotation(_leftForeArm, _baseLeftForeArm, leftForearm, 0f, 0f);
            ApplyRotation(_rightForeArm, _baseRightForeArm, rightForearm, 0f, 0f);

            float bob = Mathf.Sin(_phase * 2f + Mathf.PI * 0.5f) * bobHeight * bodyWeight;
            float sway = Mathf.Sin(_phase + Mathf.PI * 0.5f) * lateralBob * bodyWeight;
            Vector3 targetPosition = _baseModelPosition + new Vector3(sway, bob, 0f);
            _model.localPosition = Vector3.Lerp(
                _model.localPosition,
                targetPosition,
                DampFactor(modelBobSmoothing, deltaTime));
        }

        private float KneeCurve(float phase)
        {
            float swing = Mathf.Max(0f, Mathf.Sin(phase));
            return Mathf.Pow(swing, 1.45f);
        }

        private float FootRollCurve(float phase)
        {
            float normalizedPhase = Mathf.Repeat(phase / TwoPi, 1f);
            if (normalizedPhase < 0.18f)
                return Mathf.Lerp(footRoll, 0f, normalizedPhase / 0.18f);
            if (normalizedPhase < 0.54f)
                return 0f;
            if (normalizedPhase < 0.75f)
                return Mathf.Lerp(0f, -footRoll * 0.65f, (normalizedPhase - 0.54f) / 0.21f);

            return Mathf.Lerp(-footRoll * 0.65f, footRoll * 0.35f, (normalizedPhase - 0.75f) / 0.25f);
        }

        private float ToeCurve(float phase)
        {
            float normalizedPhase = Mathf.Repeat(phase / TwoPi, 1f);
            if (normalizedPhase < 0.54f)
                return 0f;
            if (normalizedPhase < 0.75f)
                return Mathf.Lerp(0f, -toeLift * 0.7f, (normalizedPhase - 0.54f) / 0.21f);

            return Mathf.Lerp(-toeLift * 0.7f, toeLift, (normalizedPhase - 0.75f) / 0.25f);
        }

        private void CacheBones()
        {
            Transform[] bones = GetComponentsInChildren<Transform>(true);
            _hips = FindBone(bones, "Hips");
            _spine = FindBone(bones, "Spine");
            _spine1 = FindBone(bones, "Spine1");
            _spine2 = FindBone(bones, "Spine2");
            _neck = FindBone(bones, "Neck");
            _head = FindBone(bones, "Head");
            _leftUpLeg = FindBone(bones, "LeftUpLeg");
            _rightUpLeg = FindBone(bones, "RightUpLeg");
            _leftLeg = FindBone(bones, "LeftLeg");
            _rightLeg = FindBone(bones, "RightLeg");
            _leftFoot = FindBone(bones, "LeftFoot");
            _rightFoot = FindBone(bones, "RightFoot");
            _leftToe = FindBone(bones, "LeftToeBase");
            _rightToe = FindBone(bones, "RightToeBase");
            _leftShoulder = FindBone(bones, "LeftShoulder");
            _rightShoulder = FindBone(bones, "RightShoulder");
            _leftArm = FindBone(bones, "LeftArm");
            _rightArm = FindBone(bones, "RightArm");
            _leftForeArm = FindBone(bones, "LeftForeArm");
            _rightForeArm = FindBone(bones, "RightForeArm");

            if (_hips == null || _leftUpLeg == null || _rightUpLeg == null)
            {
                Debug.LogWarning(
                    $"{nameof(ProceduralWalk)} on {name} could not find the complete lower-body rig. " +
                    "Animation will use only the bones that are present.",
                    this);
            }
        }

        private void CacheBasePose()
        {
            _baseHips = GetLocalRotation(_hips);
            _baseSpine = GetLocalRotation(_spine);
            _baseSpine1 = GetLocalRotation(_spine1);
            _baseSpine2 = GetLocalRotation(_spine2);
            _baseNeck = GetLocalRotation(_neck);
            _baseHead = GetLocalRotation(_head);
            _baseLeftUpLeg = GetLocalRotation(_leftUpLeg);
            _baseRightUpLeg = GetLocalRotation(_rightUpLeg);
            _baseLeftLeg = GetLocalRotation(_leftLeg);
            _baseRightLeg = GetLocalRotation(_rightLeg);
            _baseLeftFoot = GetLocalRotation(_leftFoot);
            _baseRightFoot = GetLocalRotation(_rightFoot);
            _baseLeftToe = GetLocalRotation(_leftToe);
            _baseRightToe = GetLocalRotation(_rightToe);
            _baseLeftShoulder = GetLocalRotation(_leftShoulder);
            _baseRightShoulder = GetLocalRotation(_rightShoulder);
            _baseLeftArm = GetLocalRotation(_leftArm);
            _baseRightArm = GetLocalRotation(_rightArm);
            _baseLeftForeArm = GetLocalRotation(_leftForeArm);
            _baseRightForeArm = GetLocalRotation(_rightForeArm);
            _baseModelPosition = _model.localPosition;
        }

        private static Quaternion GetLocalRotation(Transform bone)
        {
            return bone != null ? bone.localRotation : Quaternion.identity;
        }

        private static Transform FindBone(Transform[] bones, string boneName)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i].name == boneName)
                    return bones[i];
            }

            return null;
        }

        private void ApplyRotation(Transform bone, Quaternion restRotation, float pitch, float yaw, float roll)
        {
            if (bone == null)
                return;

            Quaternion offset = Quaternion.identity;
            offset = Quaternion.AngleAxis(pitch, ParentLocalAxis(_model.right, bone)) * offset;
            offset = Quaternion.AngleAxis(yaw, ParentLocalAxis(_model.up, bone)) * offset;
            offset = Quaternion.AngleAxis(roll, ParentLocalAxis(_model.forward, bone)) * offset;
            bone.localRotation = offset * restRotation;
        }

        private static Vector3 ParentLocalAxis(Vector3 worldAxis, Transform bone)
        {
            Transform parent = bone.parent;
            Vector3 localAxis = parent != null
                ? parent.InverseTransformDirection(worldAxis)
                : worldAxis;

            return localAxis.sqrMagnitude > 0.0001f ? localAxis.normalized : Vector3.right;
        }

        private void OnDisable()
        {
            if (!_initialized)
                return;

            RestorePose();
            _gaitWeight = 0f;
            _gaitWeightVelocity = 0f;
            _smoothedSpeed = 0f;
            _speedVelocity = 0f;
        }

        private void RestorePose()
        {
            RestoreRotation(_hips, _baseHips);
            RestoreRotation(_spine, _baseSpine);
            RestoreRotation(_spine1, _baseSpine1);
            RestoreRotation(_spine2, _baseSpine2);
            RestoreRotation(_neck, _baseNeck);
            RestoreRotation(_head, _baseHead);
            RestoreRotation(_leftUpLeg, _baseLeftUpLeg);
            RestoreRotation(_rightUpLeg, _baseRightUpLeg);
            RestoreRotation(_leftLeg, _baseLeftLeg);
            RestoreRotation(_rightLeg, _baseRightLeg);
            RestoreRotation(_leftFoot, _baseLeftFoot);
            RestoreRotation(_rightFoot, _baseRightFoot);
            RestoreRotation(_leftToe, _baseLeftToe);
            RestoreRotation(_rightToe, _baseRightToe);
            RestoreRotation(_leftShoulder, _baseLeftShoulder);
            RestoreRotation(_rightShoulder, _baseRightShoulder);
            RestoreRotation(_leftArm, _baseLeftArm);
            RestoreRotation(_rightArm, _baseRightArm);
            RestoreRotation(_leftForeArm, _baseLeftForeArm);
            RestoreRotation(_rightForeArm, _baseRightForeArm);
            _model.localPosition = _baseModelPosition;
        }

        private static void RestoreRotation(Transform bone, Quaternion rotation)
        {
            if (bone != null)
                bone.localRotation = rotation;
        }

        private static float DampFactor(float smoothTime, float deltaTime)
        {
            return 1f - Mathf.Exp(-deltaTime / Mathf.Max(0.0001f, smoothTime));
        }
    }
}
