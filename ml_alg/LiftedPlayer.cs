﻿using System;
using UnityEngine;

namespace ml_alg
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    class LiftedPlayer : MonoBehaviour
    {
        static readonly Vector4 ms_pointVector = new Vector4(0f, 0f, 0f, 1f);
        static readonly HumanBodyBones[] ms_updateBones = { HumanBodyBones.Neck, HumanBodyBones.Head, HumanBodyBones.Hips, HumanBodyBones.LeftHand, HumanBodyBones.RightHand, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot };
        static readonly HumanBodyBones[] ms_lateUpdateBones = { HumanBodyBones.Head, HumanBodyBones.Hips, HumanBodyBones.LeftHand, HumanBodyBones.RightHand, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot };

        enum BodyTrackingMode
        {
            Generic = 0,
            FBT
        }

        enum RotationReapplyState
        {
            None = 0,
            Await,
            Final
        }

        struct LiftBodyPart
        {
            public LifterPlayer m_source;
            public Transform m_sourceTarget;
            public Matrix4x4 m_targetOffset;
            public Vector3 m_targetPos;
            public Quaternion m_targetRot;
            public RotationReapplyState m_reapplyState;
            public bool m_saved;
        };

        readonly LiftBodyPart[] m_liftBodyParts = null;
        float m_grabDistance = 0.25f;
        Vector3 m_lastPoint;
        Vector3 m_frameVelocity;
        float m_velocityMultiplier = 5f;
        bool m_allowPull = true;
        bool m_allowHeadPull = true;
        bool m_allowHandsPull = true;
        bool m_allowHipsPull = true;
        bool m_allowLegsPull = true;
        bool m_savePose = false;
        bool m_useVelocity = false;
        bool m_averageVelocity = true;
        bool m_distanceScale = true;

        VRCPlayer m_player;
        readonly VRCPlayer.OnAvatarIsReady m_avatarReadyEvent = null;
        Animator m_animator = null;
        RootMotion.FinalIK.IKSolverVR m_solver = null;
        RootMotion.FinalIK.FullBodyBipedIK m_fbtIK = null;
        VRCFbbIkController m_fbbIK = null;
        BodyTrackingMode m_trackingMode = BodyTrackingMode.Generic;

        public float GrabDistance
        {
            set => m_grabDistance = value;
        }

        public bool AllowPull
        {
            set => m_allowPull = value;
        }

        public bool AllowHeadPull
        {
            set => m_allowHeadPull = value;
        }

        public bool AllowHandsPull
        {
            set => m_allowHandsPull = value;
        }

        public bool AllowHipsPull
        {
            set => m_allowHipsPull = value;
        }

        public bool AllowLegsPull
        {
            set => m_allowLegsPull = value;
        }

        public bool SavePose
        {
            set
            {
                m_savePose = value;
                if(!m_savePose)
                    ClearSavedPose();
            }
        }

        public bool UseVelocity
        {
            set => m_useVelocity = value;
        }

        public float VelocityMultiplier
        {
            set => m_velocityMultiplier = value;
        }

        public bool AverageVelocity
        {
            set => m_averageVelocity = value;
        }

        public bool DistanceScale
        {
            set => m_distanceScale = value;
        }

        public LiftedPlayer(IntPtr ptr) : base(ptr)
        {
            // Redundant array: 6 used, 49 dummies
            m_liftBodyParts = new LiftBodyPart[(int)HumanBodyBones.LastBone];
            for(int i = 0; i < (int)HumanBodyBones.LastBone; i++)
            {
                m_liftBodyParts[i] = new LiftBodyPart
                {
                    m_source = null,
                    m_sourceTarget = null,
                    m_targetOffset = Matrix4x4.identity,
                    m_targetPos = Vector3.zero,
                    m_targetRot = Quaternion.identity,
                    m_reapplyState = RotationReapplyState.None,
                    m_saved = false
                };
            }

            m_avatarReadyEvent = new Action(this.RecacheComponents);
        }

        void Awake()
        {
            m_player = this.GetComponent<VRCPlayer>();

            m_player.field_Private_OnAvatarIsReady_0 += m_avatarReadyEvent;
        }

        void OnDestroy()
        {
            if(m_player != null)
                m_player.field_Private_OnAvatarIsReady_0 -= m_avatarReadyEvent;
        }

        void Update()
        {
            // Recheck tracking mode
            m_trackingMode = BodyTrackingMode.Generic;
            if((m_fbtIK != null) && m_fbtIK.enabled)
                m_trackingMode = BodyTrackingMode.FBT;

            foreach(int i in ms_updateBones)
            {
                if(m_liftBodyParts[i].m_sourceTarget != null)
                {
                    switch(i)
                    {
                        case (int)HumanBodyBones.Neck:
                        {
                            Transform l_transform = m_player.transform;
                            if(l_transform != null)
                            {
                                l_transform.position = (m_liftBodyParts[i].m_sourceTarget.GetMatrix(true, false) * m_liftBodyParts[i].m_targetOffset) * ms_pointVector;
                                m_player.field_Private_VRCPlayerApi_0.SetVelocity(Vector3.zero);
                            }

                            if(m_useVelocity)
                            {
                                Vector3 l_position = m_player.field_Private_VRCPlayerApi_0.GetPosition();
                                Vector3 l_frameVelocity = ((l_position - m_lastPoint) / Time.deltaTime) * m_velocityMultiplier;
                                m_frameVelocity = (m_averageVelocity ? ((m_frameVelocity + l_frameVelocity) * 0.5f) : l_frameVelocity);
                                m_lastPoint = l_position;
                            }
                        }
                        break;

                        case (int)HumanBodyBones.Head:
                        case (int)HumanBodyBones.Hips:
                        case (int)HumanBodyBones.LeftFoot:
                        case (int)HumanBodyBones.RightFoot:
                        {
                            Matrix4x4 l_resultMatrix = m_liftBodyParts[i].m_sourceTarget.GetMatrix() * m_liftBodyParts[i].m_targetOffset;
                            m_liftBodyParts[i].m_targetPos = l_resultMatrix * ms_pointVector;
                            m_liftBodyParts[i].m_targetRot = l_resultMatrix.rotation;
                        }
                        break;

                        case (int)HumanBodyBones.LeftHand:
                        case (int)HumanBodyBones.RightHand:
                        {
                            // Ha-ha, funny hands, very cool
                            ReapplyRotation(i);

                            Matrix4x4 l_resultMatrix = m_liftBodyParts[i].m_sourceTarget.GetMatrix() * m_liftBodyParts[i].m_targetOffset;
                            m_liftBodyParts[i].m_targetPos = l_resultMatrix * ms_pointVector;
                            m_liftBodyParts[i].m_targetRot = l_resultMatrix.rotation;
                        }
                        break;
                    }

                    continue;
                }

                if(m_savePose && m_liftBodyParts[i].m_saved)
                {
                    switch(i)
                    {
                        case (int)HumanBodyBones.Head:
                        case (int)HumanBodyBones.Hips:
                        case (int)HumanBodyBones.LeftHand:
                        case (int)HumanBodyBones.RightHand:
                        case (int)HumanBodyBones.LeftFoot:
                        case (int)HumanBodyBones.RightFoot:
                        {
                            Matrix4x4 l_resultMatrix = m_player.transform.GetMatrix() * m_liftBodyParts[i].m_targetOffset;
                            m_liftBodyParts[i].m_targetPos = l_resultMatrix * ms_pointVector;
                            m_liftBodyParts[i].m_targetRot = l_resultMatrix.rotation;
                        }
                        break;
                    }
                }
            }
        }

        void LateUpdate()
        {
            foreach(int i in ms_lateUpdateBones)
            {
                if((m_liftBodyParts[i].m_sourceTarget != null) || m_liftBodyParts[i].m_saved)
                {
                    switch(m_trackingMode)
                    {
                        case BodyTrackingMode.Generic:
                        {
                            switch(i)
                            {
                                case (int)HumanBodyBones.Head:
                                {
                                    var l_spine = m_solver?.spine;
                                    if(l_spine != null)
                                    {
                                        l_spine.headPosition = m_liftBodyParts[i].m_targetPos;
                                        l_spine.IKPositionHead = m_liftBodyParts[i].m_targetPos;
                                        l_spine.headRotation = m_liftBodyParts[i].m_targetRot;
                                        l_spine.IKRotationHead = m_liftBodyParts[i].m_targetRot;

                                        if((l_spine.headTarget != null) && (l_spine.headTarget.transform.parent != null))
                                        {
                                            l_spine.headTarget.transform.parent.position = m_liftBodyParts[i].m_targetPos;
                                            l_spine.headTarget.transform.parent.rotation = m_liftBodyParts[i].m_targetRot;
                                        }
                                    }
                                }
                                break;

                                case (int)HumanBodyBones.Hips:
                                {
                                    var l_spine = m_solver?.spine;
                                    if(l_spine?.pelvisTarget != null)
                                    {
                                        l_spine.pelvisPositionWeight = 1f;
                                        l_spine.IKPositionPelvis = m_liftBodyParts[i].m_targetPos;
                                        l_spine.pelvisTarget.position = m_liftBodyParts[i].m_targetPos;

                                        l_spine.pelvisRotationWeight = 1f;
                                        l_spine.IKRotationPelvis = m_liftBodyParts[i].m_targetRot;
                                        l_spine.pelvisTarget.rotation = m_liftBodyParts[i].m_targetRot;
                                    }
                                }
                                break;

                                case (int)HumanBodyBones.LeftHand:
                                case (int)HumanBodyBones.RightHand:
                                {
                                    var l_ikArm = (i == (int)HumanBodyBones.LeftHand) ? m_solver?.leftArm : m_solver?.rightArm;
                                    if(l_ikArm?.target != null)
                                    {
                                        l_ikArm.positionWeight = 1f;
                                        l_ikArm.target.transform.position = m_liftBodyParts[i].m_targetPos;

                                        l_ikArm.rotationWeight = 1f;
                                        l_ikArm.target.transform.rotation = m_liftBodyParts[i].m_targetRot;
                                    }
                                }
                                break;

                                case (int)HumanBodyBones.LeftFoot:
                                case (int)HumanBodyBones.RightFoot:
                                {
                                    var l_ikLeg = (i == (int)HumanBodyBones.LeftFoot) ? m_solver?.leftLeg : m_solver?.rightLeg;
                                    if(l_ikLeg != null)
                                    {
                                        l_ikLeg.IKPosition = m_liftBodyParts[i].m_targetPos;
                                        l_ikLeg.IKRotation = m_liftBodyParts[i].m_targetRot;
                                    }
                                }
                                break;
                            }
                        }
                        break;

                        case BodyTrackingMode.FBT:
                        {
                            if(m_fbtIK != null)
                            {
                                RootMotion.FinalIK.IKEffector l_effector = null;
                                switch(i)
                                {
                                    case (int)HumanBodyBones.Hips:
                                        l_effector = m_fbtIK.solver?.bodyEffector;
                                        break;
                                    case (int)HumanBodyBones.LeftHand:
                                        l_effector = m_fbtIK.solver?.leftHandEffector;
                                        break;
                                    case (int)HumanBodyBones.RightHand:
                                        l_effector = m_fbtIK.solver?.rightHandEffector;
                                        break;
                                    case (int)HumanBodyBones.LeftFoot:
                                        l_effector = m_fbtIK.solver?.leftFootEffector;
                                        break;
                                    case (int)HumanBodyBones.RightFoot:
                                        l_effector = m_fbtIK.solver?.rightFootEffector;
                                        break;
                                }
                                if(l_effector?.target != null)
                                {
                                    l_effector.position = m_liftBodyParts[i].m_targetPos;
                                    l_effector.target.position = m_liftBodyParts[i].m_targetPos;

                                    l_effector.rotation = m_liftBodyParts[i].m_targetRot;
                                    l_effector.target.rotation = m_liftBodyParts[i].m_targetRot;
                                }
                            }

                            if((i == (int)HumanBodyBones.Head) && (m_fbbIK != null))
                            {
                                var l_headEffector = m_fbbIK.field_Private_FBBIKHeadEffector_0;
                                if(l_headEffector != null)
                                {
                                    l_headEffector.transform.position = m_liftBodyParts[i].m_targetPos;
                                    l_headEffector.transform.rotation = m_liftBodyParts[i].m_targetRot;
                                }
                            }
                        }
                        break;
                    }
                }
            }
        }

        void RecacheComponents()
        {
            m_animator = m_player.field_Internal_Animator_0;
            if(m_player.field_Private_VRC_AnimationController_0.field_Private_VRIK_0 != null)
                m_solver = m_player.field_Private_VRC_AnimationController_0.field_Private_VRIK_0.solver;
            else
                m_solver = null; // Generic avatar
            m_fbtIK = m_player.field_Private_VRC_AnimationController_0.field_Private_FullBodyBipedIK_0;
            m_fbbIK = m_player.field_Private_VRC_AnimationController_0.field_Private_VRCFbbIkController_0;
        }

        public void OnLifterGesture(LifterPlayer p_lifter, Transform p_target, bool p_state)
        {
            if(p_state)
            {
                if(m_animator != null)
                {
                    HumanBodyBones l_nearestBone = GetNearestBone(p_target, this.gameObject == p_lifter.gameObject);
                    if(l_nearestBone != HumanBodyBones.LastBone)
                    {
                        switch(l_nearestBone)
                        {
                            case HumanBodyBones.Neck:
                            {
                                if(m_allowPull)
                                    AssignLiftedBone(p_lifter, l_nearestBone, p_target);
                            }
                            break;

                            case HumanBodyBones.Head:
                            {
                                if(m_allowHeadPull)
                                    AssignLiftedBone(p_lifter, l_nearestBone, p_target);
                            }
                            break;

                            case HumanBodyBones.LeftHand:
                            case HumanBodyBones.RightHand:
                            {
                                if(m_allowHandsPull)
                                    AssignLiftedBone(p_lifter, l_nearestBone, p_target);
                            }
                            break;

                            case HumanBodyBones.Hips:
                            {
                                if(m_allowHipsPull)
                                    AssignLiftedBone(p_lifter, l_nearestBone, p_target);
                            }
                            break;

                            case HumanBodyBones.LeftFoot:
                            case HumanBodyBones.RightFoot:
                            {
                                if(m_allowLegsPull)
                                    AssignLiftedBone(p_lifter, l_nearestBone, p_target);
                            }
                            break;
                        }
                    }
                }
            }
            else
                UnassignRemoteLifter(p_lifter, p_target);
        }

        void AssignLiftedBone(LifterPlayer p_lifter, HumanBodyBones p_localBone, Transform p_remoteTarget)
        {
            m_liftBodyParts[(int)p_localBone].m_source = p_lifter;
            m_liftBodyParts[(int)p_localBone].m_sourceTarget = p_remoteTarget;
            switch(p_localBone)
            {
                case HumanBodyBones.Neck:
                {
                    m_liftBodyParts[(int)p_localBone].m_targetOffset = m_liftBodyParts[(int)p_localBone].m_sourceTarget.GetMatrix(true, false).inverse * m_player.transform.GetMatrix(true, false);
                    m_player.prop_VRCPlayerApi_0.Immobilize(true);

                    if(m_useVelocity)
                    {
                        m_frameVelocity = Vector3.zero;
                        m_lastPoint = m_player.field_Private_VRCPlayerApi_0.GetPosition();
                    }
                }
                break;

                case HumanBodyBones.Head:
                {
                    if((m_solver?.spine?.headTarget != null) && (m_solver.spine.headTarget.transform.parent != null))
                    {
                        m_liftBodyParts[(int)p_localBone].m_targetOffset = m_liftBodyParts[(int)p_localBone].m_sourceTarget.GetMatrix().inverse * m_solver.spine.headTarget.transform.parent.GetMatrix();
                    }
                }
                break;

                case HumanBodyBones.Hips:
                {
                    m_liftBodyParts[(int)p_localBone].m_targetOffset = m_liftBodyParts[(int)p_localBone].m_sourceTarget.GetMatrix().inverse * m_animator.GetBoneTransform(p_localBone).GetMatrix();
                }
                break;

                case HumanBodyBones.LeftHand:
                case HumanBodyBones.RightHand:
                {
                    m_liftBodyParts[(int)p_localBone].m_targetOffset = m_liftBodyParts[(int)p_localBone].m_sourceTarget.GetMatrix().inverse * m_animator.GetBoneTransform(p_localBone).GetMatrix();
                    m_liftBodyParts[(int)p_localBone].m_reapplyState = RotationReapplyState.Await;
                }
                break;

                case HumanBodyBones.LeftFoot:
                case HumanBodyBones.RightFoot:
                {
                    Transform l_footTransform = null;
                    if(m_trackingMode == BodyTrackingMode.Generic)
                    {
                        // Consider toes
                        l_footTransform = m_animator.GetBoneTransform((p_localBone == HumanBodyBones.LeftFoot) ? HumanBodyBones.LeftToes : HumanBodyBones.RightToes);
                        if(l_footTransform == null)
                            l_footTransform = m_animator.GetBoneTransform(p_localBone);
                        m_solver?.SetLegIKWeight(p_localBone, 1f);
                    }
                    else
                        l_footTransform = m_animator.GetBoneTransform(p_localBone);

                    m_liftBodyParts[(int)p_localBone].m_targetOffset = m_liftBodyParts[(int)p_localBone].m_sourceTarget.GetMatrix().inverse * l_footTransform.GetMatrix();
                }
                break;
            }
        }

        void UnassignRemoteLifter(LifterPlayer p_player, Transform p_remoteTarget)
        {
            for(int i = 0; i < (int)HumanBodyBones.LastBone; i++)
            {
                if((m_liftBodyParts[i].m_source == p_player) && (m_liftBodyParts[i].m_sourceTarget == p_remoteTarget))
                {
                    ResetBone((HumanBodyBones)i);
                    break;
                }
            }
        }

        public void UnassignRemoteLifter(LifterPlayer p_player)
        {
            if(p_player != null)
            {
                for(int i = 0; i < (int)HumanBodyBones.LastBone; i++)
                {
                    if(m_liftBodyParts[i].m_source == p_player)
                    {
                        ResetBone((HumanBodyBones)i);
                    }
                }
            }
        }

        public void ReapplyPermissions()
        {
            if(!m_allowPull && (m_liftBodyParts[(int)HumanBodyBones.Neck].m_source != null))
                ResetBone(HumanBodyBones.Neck);

            if(!m_allowHeadPull && (m_liftBodyParts[(int)HumanBodyBones.Head].m_source != null))
                ResetBone(HumanBodyBones.Head);

            if(!m_allowHipsPull && (m_liftBodyParts[(int)HumanBodyBones.Hips].m_source != null))
                ResetBone(HumanBodyBones.Hips);

            if(!m_allowHandsPull)
            {
                if(m_liftBodyParts[(int)HumanBodyBones.LeftHand].m_source != null)
                    ResetBone(HumanBodyBones.LeftHand);
                if(m_liftBodyParts[(int)HumanBodyBones.RightHand].m_source != null)
                    ResetBone(HumanBodyBones.RightHand);
            }

            if(!m_allowLegsPull)
            {
                if(m_liftBodyParts[(int)HumanBodyBones.LeftFoot].m_source != null)
                    ResetBone(HumanBodyBones.LeftFoot);
                if(m_liftBodyParts[(int)HumanBodyBones.RightFoot].m_source != null)
                    ResetBone(HumanBodyBones.RightFoot);
            }
        }

        void ResetBone(HumanBodyBones p_bone)
        {
            m_liftBodyParts[(int)p_bone].m_source = null;
            m_liftBodyParts[(int)p_bone].m_sourceTarget = null;
            m_liftBodyParts[(int)p_bone].m_targetOffset = Matrix4x4.identity;

            if(p_bone == HumanBodyBones.Neck)
            {
                m_player.prop_VRCPlayerApi_0.Immobilize(false);

                if(m_useVelocity)
                {
                    m_player.field_Private_VRCPlayerApi_0.SetVelocity(m_frameVelocity);
                    m_frameVelocity = Vector3.zero;
                }
            }

            if(m_savePose)
            {
                switch(p_bone)
                {
                    case HumanBodyBones.Head:
                    case HumanBodyBones.Hips:
                    case HumanBodyBones.LeftHand:
                    case HumanBodyBones.RightHand:
                    case HumanBodyBones.LeftFoot:
                    case HumanBodyBones.RightFoot:
                    {
                        m_liftBodyParts[(int)p_bone].m_targetOffset = m_player.transform.GetMatrix().inverse * Matrix4x4.TRS(m_liftBodyParts[(int)p_bone].m_targetPos, m_liftBodyParts[(int)p_bone].m_targetRot, Vector3.one);
                        m_liftBodyParts[(int)p_bone].m_saved = true;
                    }
                    break;
                }
            }
            else
            {
                switch(p_bone)
                {
                    case HumanBodyBones.LeftFoot:
                    case HumanBodyBones.RightFoot:
                    {
                        if(m_trackingMode == BodyTrackingMode.Generic)
                            m_solver?.SetLegIKWeight(p_bone, 0f);
                    }
                    break;
                }
            }

            m_liftBodyParts[(int)p_bone].m_targetPos = Vector3.zero;
            m_liftBodyParts[(int)p_bone].m_targetRot = Quaternion.identity;
            m_liftBodyParts[(int)p_bone].m_reapplyState = RotationReapplyState.None;
        }

        public void ClearSavedPose()
        {
            for(int i = 0; i < (int)HumanBodyBones.LastBone; i++)
            {
                m_liftBodyParts[i].m_saved = false;
                if((m_liftBodyParts[i].m_sourceTarget == null) && (m_trackingMode == BodyTrackingMode.Generic))
                {
                    switch(i)
                    {
                        case (int)HumanBodyBones.LeftFoot:
                        case (int)HumanBodyBones.RightFoot:
                            m_solver?.SetLegIKWeight((HumanBodyBones)i, 0f);
                            break;
                    }
                }
            }
        }

        HumanBodyBones GetNearestBone(Transform p_remoteTarget, bool p_self = false)
        {
            HumanBodyBones l_result = HumanBodyBones.LastBone;
            if(m_animator.isHuman)
            {
                float l_nearestDistance = float.MaxValue;

                foreach(HumanBodyBones l_bone in ms_updateBones)
                {
                    if(p_self && ((l_bone == HumanBodyBones.Neck) || (l_bone == HumanBodyBones.Head)))
                        continue;

                    Transform l_localTransform = m_animator.GetBoneTransform(l_bone);
                    if((l_localTransform != null) && (l_localTransform != p_remoteTarget))
                    {
                        float l_distance = Vector3.Distance(p_remoteTarget.position, l_localTransform.position);
                        if(l_distance < l_nearestDistance)
                        {
                            l_nearestDistance = l_distance;
                            l_result = l_bone;
                        }
                    }
                }
                if(l_nearestDistance > (m_grabDistance * (m_distanceScale ? Utils.GetTrackingScale() : 1f)))
                    l_result = HumanBodyBones.LastBone;
            }
            return l_result;
        }

        void ReapplyRotation(int p_index)
        {
            switch(m_liftBodyParts[p_index].m_reapplyState)
            {
                case RotationReapplyState.Await:
                    m_liftBodyParts[p_index].m_reapplyState = RotationReapplyState.Final;
                    break;
                case RotationReapplyState.Final:
                {
                    m_liftBodyParts[p_index].m_reapplyState = RotationReapplyState.None;

                    if(m_animator != null)
                    {
                        Transform l_boneTransform = m_animator.GetBoneTransform((HumanBodyBones)p_index);
                        if(l_boneTransform != null)
                        {
                            m_liftBodyParts[p_index].m_targetOffset = m_liftBodyParts[p_index].m_targetOffset * (Quaternion.Inverse(l_boneTransform.transform.rotation) * m_liftBodyParts[p_index].m_targetRot).AsMatrix();
                        }
                    }
                }
                break;
            }
        }
    }
}
