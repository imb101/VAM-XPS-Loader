using System.Collections.Generic;
using UnityEngine;

namespace XNALara
{
    public delegate void ArmatureEventDelegate();

    public class Armature
    {
        public int rowcount = 0;
        public bool hasTangents = false;
        public bool hasVariableWeights = false;
        private Vector3 worldTranslation = Vector3.zero;
        private Vector3 worldScale = new Vector3(1.0f, 1.0f, 1.0f);
        private Matrix4x4 worldMatrix = Matrix4x4.identity;

        private Bone[] bones = new Bone[0];
        private Matrix4x4[] boneMatrices = new Matrix4x4[0];

        private Dictionary<string, Bone> boneDict = new Dictionary<string, Bone>();
        private List<Bone> rootBones = new List<Bone>();

        public event ArmatureEventDelegate ArmatureEvent;
        public bool hasHeader = false;

        public Armature() {
           
        }

    

        public Vector3 WorldTranslation {
            set {
                worldTranslation = value;
            //    WorldMatrix = 
              //      Matrix.CreateScale(worldScale) * 
                //    Matrix.CreateTranslation(worldTranslation);
            }
            get { return worldTranslation; }
        }

        public Vector3 WorldScale {
            set {
                worldScale = value;
        //        WorldMatrix =
          //          Matrix.CreateScale(worldScale) *
            //        Matrix.CreateTranslation(worldTranslation);
            }
            get { return worldScale; }
        }

        public Matrix4x4 WorldMatrix {
            private set {
                worldMatrix = value;
                if (ArmatureEvent != null) {
                    ArmatureEvent();
                }
            }
            get { return worldMatrix; }
        }

        public Bone[] Bones {
            set {
                bones = new Bone[value.Length];
                boneMatrices = new Matrix4x4[bones.Length];
                boneDict.Clear();
                rootBones.Clear();
                for (int boneID = 0; boneID < bones.Length; boneID++) {
                    Bone bone = value[boneID];
                    bones[boneID] = bone;
                    boneDict[bone.name] = bone;
                    if (bone.parent == null) {
                        rootBones.Add(bone);
                    }
                }
                InitMatrices();
            }

            get { return bones; }
        }

        public Bone GetBone(string boneName) {
            Bone bone;
            if (boneDict.TryGetValue(boneName, out bone)) {
                return bone;
            }
            return null;
        }

        public Matrix4x4[] GetBoneMatrices(Mesh mesh) {
            return mesh.bindposes;
        }

        public Matrix4x4[] BoneMatrices {
            get { return boneMatrices; }
        }

        public void SetBoneTransform(string boneName, Matrix4x4 transform) {
            boneDict[boneName].relTransform = transform;
        }

        public void UpdateBoneMatrices() {
            foreach (Bone root in rootBones) {
                CalcAbsTransform(root);
            }
            for (int boneID = 0; boneID < bones.Length; boneID++) {
                Bone bone = bones[boneID];
                boneMatrices[boneID] = bone.invMove * bone.absTransform;
            }
            if (ArmatureEvent != null) {
                ArmatureEvent();
            }
        }

        private void InitMatrices() {
            foreach (Bone bone in bones) {
                bone.relTransform = Matrix4x4.identity;//.Identity;
                bone.invMove = Matrix4x4.TRS(-bone.absPosition,Quaternion.identity,Vector3.one);
             //   bone.relMove = bone.parent != null ? 
               //     Matrix.CreateTranslation(bone.absPosition - bone.parent.absPosition) : 
                 //   Matrix.Identity;
            }
            UpdateBoneMatrices();
        }

        private void CalcAbsTransform(Bone bone) {
            Bone parent = bone.parent;
            if (parent != null) {
                bone.absTransform = (bone.relTransform * bone.relMove) * parent.absTransform;
            }
            else {
                bone.absTransform = bone.relTransform;
            }
            foreach (Bone child in bone.children) {
                CalcAbsTransform(child);
            }
        }

        public class Bone
        {
            public Armature armature;

            public int id;
            public string name;
            public Vector3 absPosition;
            public Bone parent;
            public List<Bone> children = new List<Bone>();

            public Matrix4x4 invMove;
            public Matrix4x4 relMove;

            public Matrix4x4 relTransform;
            public Matrix4x4 absTransform;
        }
    }
}
