﻿using System;
using Aiv.Fast2D;
using BehaviourEngine.Interfaces;
using OpenTK;

namespace BehaviourEngine
{
    public sealed class CameraManager : Behaviour, IUpdatable
    {
        private readonly Camera            camera;
        private IOperation                 currentOperation;

        private GameObject                 currentTarget;
        private readonly Vector2           originalCameraPosition;
        private readonly float             originalOrthoSize;

        //Operations
        private readonly OperationNone     operationNone;
        private readonly OperationShake    operationShake;
        private readonly OperationSeek     operationSeek;
        private readonly OperationLerpSeek operationLerpSeek;
        private readonly OperationZoom     operationZoom;
        private readonly OperationLerpZoom operationLerpZoom;

        private CameraManager(GameObject owner) : base(owner)
        {
            camera = new Camera();

            float halfOrthoWidth           = Engine.Window.OrthoWidth * 0.5f;
            float halfOrthoHeight          = Engine.Window.OrthoHeight * 0.5f;
            camera.pivot                   = new Vector2(halfOrthoWidth, halfOrthoHeight);
            camera.position                += camera.pivot;
            originalOrthoSize              = Engine.Window.CurrentOrthoGraphicSize;

            operationNone                  = new OperationNone();
            operationShake                 = new OperationShake(this);
            operationSeek                  = new OperationSeek(this);
            operationLerpSeek              = new OperationLerpSeek(this);
            operationZoom                  = new OperationZoom(this);
            operationLerpZoom              = new OperationLerpZoom(this);

            currentOperation               = operationNone;
            originalCameraPosition         = camera.position;
        }

        public void None()
        {
            operationNone.Init();
            currentOperation = operationNone;
        }
        public IOperation Shake(float amplitude, float duration)
        {
            operationShake.Init(amplitude, duration);
            currentOperation = operationShake;
            return currentOperation;
        }
        public IOperation Seek(GameObject target)
        {
            operationSeek.Init(target);
            currentOperation = operationSeek;
            return currentOperation;
        }
        public IOperation LerpSeek(GameObject target, float speed)
        {
            operationLerpSeek.Init(target, speed);
            currentOperation = operationLerpSeek;
            return currentOperation;
        }
        public IOperation Zoom(float zoomRate)
        {
            operationZoom.Init(zoomRate);
            currentOperation = operationZoom;
            return currentOperation;
        }
        public IOperation LerpZoom(float zoomRate, float duration)
        {
            operationLerpZoom.Init(zoomRate, duration);
            currentOperation = operationLerpZoom;
            return currentOperation;
        }

        #region Singleton
        private static CameraManager instance;
        public static CameraManager Instance => instance ?? (instance = new CameraManager(null));

        #endregion

        #region GameObject
        public bool Active { get; set; }
        public Vector2 Position { get; set; }
        #endregion

        #region IUpdateable
        public void Update()
        {
            if (!currentOperation.Completed)
            {
                currentOperation.Execute();
            }
        }
        #endregion

        #region IOperation
        public interface IOperation
        {
            bool Completed { get; }
            void Execute();
        }
        public class OperationNone : IOperation
        {
            private bool completed;
            public bool Completed => completed;

            public void Init()
            {
                completed = false;
            }

            public void Execute()
            {
            }
        }
        public class OperationShake : IOperation
        {
            private bool completed;
            public bool Completed => completed;

            private readonly CameraManager owner;

            private float originalAmplitude;
            private float currentAmplitude;

            private float originalDuration;
            private float currentDuration;

            private int sign;
            public OperationShake(CameraManager owner)
            {
                this.owner = owner;
            }
            public void Init(float amplitude, float duration)
            {
                originalAmplitude = amplitude;
                currentAmplitude = amplitude;

                originalDuration = duration;
                currentDuration = duration;

                sign = RandomManager.Instance.Random.Next(2) == 0 ? 1 : -1;

                completed = false;
            }

            //Update
            public void Execute()
            {
                owner.camera.position = owner.originalCameraPosition + new Vector2(currentAmplitude * sign, 0f);
                currentDuration -= Time.DeltaTime;

                float rate = (currentDuration / originalDuration);
                currentAmplitude = originalAmplitude * rate;
                sign *= -1;     //invert sign
                Console.WriteLine(currentDuration);
                if (!(currentDuration < 0f)) return;
                completed = true;
                owner.camera.position = owner.originalCameraPosition;
            }


        }
        public class OperationSeek : IOperation
        {
            private bool completed;
            public bool Completed => completed;

            private readonly CameraManager owner;

            public OperationSeek(CameraManager owner)
            {
                this.owner = owner;
            }

            public void Init(GameObject target)
            {
                owner.currentTarget = target;
                completed = false;
            }

            //Update
            public void Execute()
            {
                if (owner.currentTarget.Active)
                {
                    owner.camera.position = owner.currentTarget.Transform.Position;
                }
                else
                {
                    owner.camera.position = owner.originalCameraPosition;
                    completed = true;
                }
            }

        }
        public class OperationLerpSeek : IOperation
        {
            private bool completed;
            public bool Completed => completed;

            private readonly CameraManager owner;
            private float speed;

            public OperationLerpSeek(CameraManager owner)
            {
                this.owner = owner;
            }

            public void Init(GameObject target, float speed)
            {
                owner.currentTarget = target;
                this.speed = speed;
                completed = false;
            }

            //Update
            public void Execute()
            {
                owner.camera.position = Vector2.Lerp(owner.camera.position, owner.currentTarget.Transform.Position, speed * Time.DeltaTime);
            }

        }
        public class OperationZoom : IOperation
        {
            private readonly CameraManager owner;
            private bool completed;
            public bool Completed => completed;

            public OperationZoom(CameraManager owner)
            {
                this.owner = owner;
            }

            public void Execute()
            {
                completed = true;
            }

            public void Init(float zoomRate)
            {
                Engine.Window.SetDefaultOrthographicSize(owner.originalOrthoSize / zoomRate);
                completed = false;
            }
        }
        public class OperationLerpZoom : IOperation
        {
            private readonly CameraManager owner;
            private bool completed;
            public bool Completed => completed;

            private const float defaultZoom = 1f;
            private float additiveZoomRate;
            private float maxTime;
            private float timer;
            private float initialOrthosize;

            public OperationLerpZoom(CameraManager owner)
            {
                this.owner = owner;
            }

            public void Execute()
            {
                float t = timer / maxTime;
                float currentZoomRate = defaultZoom + additiveZoomRate * t;
                Engine.Window.SetDefaultOrthographicSize(initialOrthosize / currentZoomRate);
                Vector2 orthoHalf = new Vector2(Engine.Window.OrthoWidth, Engine.Window.OrthoHeight) * 0.5f;
                owner.camera.pivot = orthoHalf;
                timer += Time.DeltaTime;

                if (timer > maxTime)
                    completed = true;
            }

            public void Init(float zoomRate, float duration)
            {
                if (zoomRate == defaultZoom)
                    additiveZoomRate = 0f;
                else
                    additiveZoomRate = zoomRate < defaultZoom ? zoomRate - defaultZoom : zoomRate;

                initialOrthosize = Engine.Window.CurrentOrthoGraphicSize;
                maxTime = duration;
                timer = 0f;

                completed = false;
            }
        }
        #endregion
    }
}
