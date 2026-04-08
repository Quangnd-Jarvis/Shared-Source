using System;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using NaughtyAttributes;
using UnityEngine;

namespace Unity_Animation_Package.Runtime
{
    public class MoveToTargetUI : MonoBehaviour
    {
        [SerializeField] private RectTransform        _startPoint;
        [SerializeField] private RectTransform        _endPoint;
        [SerializeField] private RectTransform        _movingObject;
        [SerializeField] private float                _duration = 0.5f;
        [SerializeField] private MoveToTargetUIConfig _config   = new();
    
        [Button]
        private void Play()
        {
            ExecuteMovevement().Forget();
        }
    
        public async UniTask ExecuteMovevement()
        {
            if (!HasValidTargets())
            {
                return;
            }

            ResetMovingObject();
            await PlayPressPhase();
            await PlayLiftPhase();
            await PlayDropPhase();
            await PlayLandingPhase();
        }

        private bool HasValidTargets()
        {
            return _startPoint != null && _endPoint != null && _movingObject != null;
        }

        private void ResetMovingObject()
        {
            _movingObject.anchoredPosition = _startPoint.anchoredPosition;
            _movingObject.localScale       = Vector3.one;
            _movingObject.localEulerAngles = Vector3.zero;
        }

        private async UniTask PlayPressPhase()
        {
            var phase = _config.PressPhase;
            var handle = LMotion.Create(Vector3.one, phase.TargetScale, PhaseDuration(phase.DurationRatio))
               .WithEase(phase.Ease)
               .BindToLocalScale(_movingObject)
               .AddTo(this);
            await handle.ToUniTask(this.GetCancellationTokenOnDestroy());
        }

        private async UniTask PlayLiftPhase()
        {
            var phase    = _config.LiftPhase;
            var midpoint = GetMidpoint(_config.JumpHeight);
            await UniTask.WhenAll(
                PlayAnchoredPosition(midpoint, PhaseDuration(phase.DurationRatio), phase.Ease),
                PlayRotation(phase.TargetRotation, PhaseDuration(phase.DurationRatio), phase.Ease),
                PlayScale(phase.TargetScale, PhaseDuration(phase.DurationRatio), phase.Ease));
        }

        private async UniTask PlayDropPhase()
        {
            var phase  = _config.DropPhase;
            var target = _endPoint.anchoredPosition;
            await UniTask.WhenAll(
                PlayAnchoredPosition(target, PhaseDuration(phase.DurationRatio), phase.Ease),
                PlayRotation(phase.TargetRotation, PhaseDuration(phase.DurationRatio), phase.Ease),
                PlayScale(phase.TargetScale, PhaseDuration(phase.DurationRatio), phase.Ease));
        }

        private async UniTask PlayLandingPhase()
        {
            var phase         = _config.LandingPhase;
            await UniTask.WhenAll(
                    PlayLandingRotation(phase),
                    PlayScale(phase.TargetScale, PhaseDuration(phase.DurationRatio), phase.Ease)
                );
        }

        private Vector2 GetMidpoint(float jumpHeight)
        {
            var midpoint = (_startPoint.anchoredPosition + _endPoint.anchoredPosition) * 0.5f;
            var direction = (_endPoint.transform.position - _startPoint.transform.position).normalized;
            var perpendicular = new Vector2(-direction.y, direction.x);
            midpoint += perpendicular * jumpHeight;
            return midpoint;
        }

        private UniTask PlayAnchoredPosition(Vector2 target, float duration, Ease ease)
        {
            var handle = LMotion.Create(_movingObject.anchoredPosition, target, duration)
               .WithEase(ease)
               .BindToAnchoredPosition(_movingObject)
               .AddTo(this);
            return handle.ToUniTask(this.GetCancellationTokenOnDestroy());
        }

        private UniTask PlayRotation(float zRotation, float duration, Ease ease)
        {
            var start = GetSignedZRotation();
            var handle = LMotion.Create(start, zRotation, duration)
               .WithEase(ease)
               .BindToLocalEulerAnglesZ(_movingObject)
               .AddTo(this);
            return handle.ToUniTask(this.GetCancellationTokenOnDestroy());
        }

        private async UniTask PlayLandingRotation(LandingPhaseConfig phase)
        {
            await PlayRotation(phase.TargetRotation, PhaseDuration(phase.FirstStepRatio), phase.Ease);
            await PlayRotation(phase.SecondaryRotation, PhaseDuration(phase.SecondStepRatio), phase.Ease);
            await PlayRotation(phase.FinalRotation,  PhaseDuration(phase.FinalStepRatio), phase.Ease);
        }

        private float GetSignedZRotation()
        {
            var zRotation = _movingObject.localEulerAngles.z;
            return zRotation > 180f ? zRotation - 360f : zRotation;
        }

        private UniTask PlayScale(Vector3 target, float duration, Ease ease)
        {
            var handle = LMotion.Create(_movingObject.localScale, target, duration)
               .WithEase(ease)
               .BindToLocalScale(_movingObject)
               .AddTo(this);
            return handle.ToUniTask(this.GetCancellationTokenOnDestroy());
        }

        private float PhaseDuration(float ratio)
        {
            return _duration * ratio;
        }
    }

    [Serializable]
    public class MoveToTargetUIConfig
    {
        public float              JumpHeight   = 90f;
        public BasePhaseConfig    PressPhase   = new(0.2f, Ease.InQuad, new Vector3(1.12f, 0.82f, 1f), 0f);
        public BasePhaseConfig    LiftPhase    = new(0.35f, Ease.OutBack, Vector3.one, -12f);
        public BasePhaseConfig    DropPhase    = new(0.3f, Ease.InQuad, new Vector3(0.95f, 1.08f, 1f), 10f);
        public LandingPhaseConfig LandingPhase = new(0.15f, Ease.OutBack, Vector3.one, -8f, 6f, 0f, 0.35f, 0.3f, 0.35f);
    }

    [Serializable]
    public class BasePhaseConfig
    {
        public float   DurationRatio = 0.5f;
        public Ease    Ease          = Ease.OutQuad;
        public Vector3 TargetScale   = Vector3.one;
        public float   TargetRotation;

        public BasePhaseConfig()
        {
        }

        public BasePhaseConfig(float durationRatio, Ease ease, Vector3 targetScale, float targetRotation)
        {
            DurationRatio  = durationRatio;
            Ease           = ease;
            TargetScale    = targetScale;
            TargetRotation = targetRotation;
        }
    }

    [Serializable]
    public class LandingPhaseConfig : BasePhaseConfig
    {
        public float   SecondaryRotation;
        public float   FinalRotation;
        public float   FirstStepRatio = 1f;
        public float   SecondStepRatio;
        public float   FinalStepRatio;
        public Vector2 RightBottomPivot = new(1f, 0f);
        public Vector2 LeftBottomPivot  = new(0f, 0f);
        
        public LandingPhaseConfig(
            float durationRatio,
            Ease ease,
            Vector3 targetScale,
            float targetRotation,
            float secondaryRotation,
            float finalRotation,
            float firstStepRatio,
            float secondStepRatio,
            float finalStepRatio)
            : base(durationRatio, ease, targetScale, targetRotation)
        {
            SecondaryRotation = secondaryRotation;
            FinalRotation     = finalRotation;
            FirstStepRatio    = firstStepRatio;
            SecondStepRatio   = secondStepRatio;
            FinalStepRatio    = finalStepRatio;
        }
    }
}