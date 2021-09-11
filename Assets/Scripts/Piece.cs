﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using SNM;
using UnityEngine;
using Animation = SNM.Animation;
using Animator = UnityEngine.Animator;

public class Piece : Prefab
{
    public PieceAnimator PieceAnimator { get; private set; }

    private Animator animator;
    private Animator Animator => animator ? animator : (animator = GetComponentInChildren<Animator>());

    private ConfigData configData;
    public ConfigData ConfigDataProp => configData;

    private bool isRandomlyRotating = false;
    private Tag[] taggedGameObjects;
    private Transform footTransform;

    public void Setup(ConfigData configData)
    {
        this.configData = configData;
        this.Delay(UnityEngine.Random.Range(0.1f, 2f), () => Animator?.Play("idle"));
        FaceCamera(true, new Vector3(0, UnityEngine.Random.Range(-45f, 45f), 0));

        PieceAnimator = new PieceAnimator();
        taggedGameObjects = GetComponentsInChildren<Tag>();
        footTransform = taggedGameObjects.FirstOrDefault(t => t.ID.Equals("foot"))?.transform;
    }

    private void Update()
    {
        PieceAnimator?.Update(Time.deltaTime);
    }

    public void FaceCamera(bool immediate, Vector3 offset = new Vector3())
    {
        if (Main.Instance.References.Camera != null)
        {
            var dir = Main.Instance.References.Camera.transform.position - transform.position;
            var up = transform.up;
            dir = SNM.Math.Projection(dir, up);
            if (immediate)
            {
                transform.rotation = Quaternion.LookRotation(dir, up);
            }
            else
            {
                var target = Quaternion.LookRotation(dir, up).eulerAngles + offset;
                var duration = (target - transform.eulerAngles).magnitude / PieceAnimator.Config.angularSpeed;
                transform.DORotate(target, duration);
            }
        }
    }

    public void JumpTo(Vector3 pos, int flag, Action<PieceAnimator, int> callback)
    {
        var parallelAnimation = new ParallelAnimation();
        parallelAnimation.Add(new PieceAnimator.JumpAnim(transform,
            new PieceAnimator.JumpAnim.InputData
            {
                target = pos,
                flag = flag,
                callback = callback
            }, BezierEasing.Blueprint1));
        PieceAnimator.Add(new BounceAnim(footTransform, 0.15f));
        PieceAnimator.Add(parallelAnimation);
    }

    public void Land()
    {
        PieceAnimator.Add(new BounceAnim(footTransform, 0.15f));
        PieceAnimator.Add(new PieceAnimator.TurnAway(transform));
    }

    public class BounceAnim : Animation
    {
        private Transform transform;
        private float duration;
        private float time;
        private float offset;
        private bool fullPhase;

        public BounceAnim(Transform transform, float duration, bool fullPhase = false)
        {
            this.transform = transform;
            this.duration = duration;
            this.offset = 0.3f;
            this.fullPhase = fullPhase;
        }

        public override void Update(float deltaTime)
        {
            if (!IsDone)
            {
                time += deltaTime;
                float t = Mathf.Min(time / duration, 1f);

                var scale = transform.localScale;
                if (fullPhase)
                {
                    var s = Mathf.Sin(Mathf.Lerp(0, Mathf.PI * 2f, t));
                    scale.y = 1 + (-s) * offset;
                    scale.x = 1 + (s) * offset * 0.35f;
                    scale.z = 1 + (s) * offset * 0.35f;
                }
                else
                {
                    var c = Mathf.Cos(Mathf.Lerp(0, Mathf.PI * 2f, t));
                    scale.y = 1 + (c) * offset * 0.5f;
                    scale.x = 1 + (-c) * offset * 0.25f;
                    scale.z = 1 + (-c) * offset * 0.25f;
                }

                transform.localScale = scale;

                if (time >= duration)
                {
                    IsDone = true;
                }
            }
        }
    }

#if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        if (ConfigDataProp != null)
            Gizmos.DrawWireCube(transform.position, ConfigDataProp.size);
    }
#endif
    public class PieceToTileSelectorAdaptor : TileSelector.ISelectionAdaptor
    {
        private Piece piece;

        private bool isDeselected;

        public PieceToTileSelectorAdaptor(Piece piece)
        {
            this.piece = piece;
            isDeselected = false;
        }

        public void OnTileSelected()
        {
            piece.FaceCamera(false, new Vector3(0, UnityEngine.Random.Range(-25f, 25f), 0));
            piece.Delay(UnityEngine.Random.Range(0, 0.5f), () =>
                {
                    if (!isDeselected)
                    {
                        piece.Animator?.CrossFade("jump", 0.1f);
                    }
                }
            );
        }

        public void OnTileDeselected()
        {
            piece.Animator?.CrossFade("idle", 0.1f);
            isDeselected = true;
        }
    }

    [Serializable]
    public class ConfigData
    {
        public ConfigData(ConfigData prototype)
        {
            point = prototype.point;
            size = prototype.size;
        }

        public int point;
        public Vector3 size;
    }
}