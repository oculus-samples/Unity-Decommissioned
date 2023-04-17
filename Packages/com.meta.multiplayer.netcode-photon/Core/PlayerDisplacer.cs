// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Meta.Multiplayer.Avatar;
using Meta.Multiplayer.Core;
using Meta.Utilities;
using UnityEngine;

namespace Meta.Multiplayer.Core
{
    public class PlayerDisplacer : Multiton<PlayerDisplacer>
    {
        [SerializeField]
        private float m_displacementRadius = 0.1f;
        [SerializeField, AutoSet] private AvatarEntity m_playerEntity;

        public static PlayerDisplacer GetByAvatarEntity(AvatarEntity avatar) => Instances.FirstOrDefault(displacer => displacer.m_playerEntity == avatar);

        private void Update()
        {
            var selfPoint = Vector3.zero;
            if (m_playerEntity.SkeletonJointCount == 0)
            {
                return;
            }

            if (m_playerEntity.IsLocal)
            {
                (selfPoint, _) = GetEntityPointRadius();
            }

            Debug.DrawLine(selfPoint, selfPoint + new Vector3(m_displacementRadius, 0, 0));
            foreach (var entity in Instances)
            {
                if (entity != this)
                {
                    var (point, radius) = entity.GetEntityPointRadius();
                    var (colliding, projection) = CollideAndProject(selfPoint, m_displacementRadius, point, radius);
                    if (colliding)
                    {
                        //if the entities are overlapping, push them backwards
                        var rootTransform = entity.transform;
                        rootTransform.position += projection;
                    }
                }
            }

        }

        private (bool, Vector3) CollideAndProject(Vector3 aPoint, float aRadius, Vector3 bPoint, float bRadius)
        {
            var dist = Vector3.Distance(aPoint, bPoint);
            var collide = dist < aRadius + bRadius;
            var project = Vector3.zero;
            if (collide)
            {
                project = (bPoint - aPoint).normalized * (aRadius + bRadius - dist);
            }

            return (collide, project);
        }

        private (Vector3, float) GetEntityPointRadius()
        {
            if (m_playerEntity.SkeletonJointCount == 0)
            {
                return (Vector3.zero, 0f);
            }

            var entityPoint = Vector3.zero;
            entityPoint.y = 0;
            var entityRadius = 0f;
            var rootTransform = m_playerEntity.GetJointTransform(Oculus.Avatar2.CAPI.ovrAvatar2JointType.Root);
            if (rootTransform != null)
            {
                var trackedTransforms = new Transform[]
                {
                m_playerEntity.GetJointTransform(Oculus.Avatar2.CAPI.ovrAvatar2JointType.Head),
                m_playerEntity.GetJointTransform(Oculus.Avatar2.CAPI.ovrAvatar2JointType.LeftHandWrist),
                m_playerEntity.GetJointTransform(Oculus.Avatar2.CAPI.ovrAvatar2JointType.RightHandWrist)
                };
                entityPoint = rootTransform.position;
                entityPoint.y = 0;

                foreach (var trackedTransform in trackedTransforms)
                {
                    var squishedPos = trackedTransform.position;
                    squishedPos.y = 0;
                    var dist = Vector3.Distance(squishedPos, entityPoint);
                    if (dist > entityRadius)
                    {
                        entityRadius = dist;
                    }
                }
            }
            return (entityPoint, entityRadius);


        }
    }
}
