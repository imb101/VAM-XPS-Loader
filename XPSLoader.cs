using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using XNALara;
using MVR.FileManagementSecure;
using UnityEngine.UI;
using SimpleJSON;

namespace XPSLoader
{
    class XPSRenderGroup
    {
        public bool hasAlpha = false;
        public bool hasSpecular = false;
        public bool hasEmission = false;

        public float specIntensity = 0f;
        public int texCount = 1;
        public int[] alphaGroups = new int[] { 12, 18, 15, 19, 21, 7, 9, 6, 8, 20, 23, 25, 27, 29, 31, 33, 37, 39, 41 };
        public int[] specGroups = new int[] { 11, 12, 14, 15, 6, 4, 2, 8, 1, 20, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 36, 37, 38, 39, 40, 41 };
        
        
        public int[] specIntensityGroups = new int[] { 26, 27, 28, 29, 30, 31, 36, 37, 38, 39 };
        public Dictionary<int, string> maps;

        public int[] normalTwo = new int[] { 11,12,14,15,6,4,26,27,30,31,36,37,38,39,40,41 };
        public int[] aoTwo = new int[] { 17,19,3,9,2,8,1,20,22,23,24,25,28,29};

        public int[] normalThree = new int[] { 2, 8, 1, 20, 22, 23, 24, 25, 28, 29 };
        public int[] envThree = new int[] { 26, 27};
        public int[] emissionThree = new int[] { 30,31,36,37 };
        public int[] specularThree = new int[] { 38,39,40,41 };

        public int[] specularFour = new int[] { 24,25 };
        public int[] maskFour = new int[] { 1,20,22,23,28,29 };
        public int[] emissionFour = new int[] { 38,39 };

        int renderGroupNum;

        public XPSRenderGroup(int renderGroupNum_, string[] param)
        {
            renderGroupNum = renderGroupNum_;

            if (alphaGroups.Contains(renderGroupNum))
            {                
                hasAlpha = true;
            }

            if (specGroups.Contains(renderGroupNum))
                hasSpecular = true;

            processMaps();
        }

        void processMaps()
        {
            maps = new Dictionary<int, string>();
            maps.Add(0, DIFFUSEMAP); //all groups have diffuse as 0;

            if (normalTwo.Contains(renderGroupNum))
                maps.Add(1, NORMALMAP);
            else if (aoTwo.Contains(renderGroupNum))
                maps.Add(1, OCCLUSIONMAP); //this doesn't do anything as the shader doesnt support this keyword!

            if (normalThree.Contains(renderGroupNum))
                maps.Add(2, NORMALMAP);
            else if (envThree.Contains(renderGroupNum))
                maps.Add(2, ENVMAP);
            else if (emissionThree.Contains(renderGroupNum))
                maps.Add(2, EMISSIONMAP);
            else if (specularThree.Contains(renderGroupNum))
                maps.Add(2, SPECULARMAP);

            if (maskFour.Contains(renderGroupNum))
            { //maps.Add(2, "_mask");  need to fix this
            }
            else if (emissionFour.Contains(renderGroupNum))
                maps.Add(3, EMISSIONMAP);
            else if (specularFour.Contains(renderGroupNum))
                maps.Add(3, SPECULARMAP);


        }

        public static Material constructFallbackMaterial(MeshDesc.Texture[] textures, string path)
        {
            Material m = new Material(Shader.Find(STANDARD_SHADER_NAME));

            if(textures.Length > 0)
            { 
             m.SetTexture(DIFFUSEMAP, ImageUtilities.getTex(path, textures[0].filename));
             m.SetTextureScale(DIFFUSEMAP, new Vector2(1, -1));
            }
            m.SetFloat(SPECULARPARAM, DEFAULT_SPEC_INT);
            return m;
        }

        public Material constructMaterial(MeshDesc.Texture[] textures, string path)
        {
            Material m = null;
            Texture2D diffuse;

            if (textures.Length > 0)
            {
                diffuse = ImageUtilities.getTex(path, textures[0].filename);
            
            
            if (hasEmission)
                m = new Material(Shader.Find(EMISSIVE_SHADER_NAME));
            else if(hasAlpha && ImageUtilities.HasAlpha(diffuse.GetPixels()))
                m = new Material(Shader.Find(ALPHA_SHADER_NAME));
            else
                m = new Material(Shader.Find(STANDARD_SHADER_NAME));

            

            foreach (KeyValuePair<int, string> texMap in maps)
            {                
                    if(textures.Length > texMap.Key) 
                    {
                        m.SetTexture(texMap.Value, ImageUtilities.getTex(path, textures[texMap.Key].filename));
                       m.SetTextureScale(texMap.Value, new Vector2(1, -1));
                    }
            }
            
            m.SetFloat(SPECULARPARAM, DEFAULT_SPEC_INT);

            }
          
            return m;
        }

        public static string DIFFUSEMAP = "_MainTex";
        public static string NORMALMAP = "_BumpMap";
        public static string ENVMAP = "_SpecCubeIBL";
        public static string EMISSIONMAP = "_MKGlowColor";
        public static string SPECULARMAP = "_SpecColor";
        public const string OCCLUSIONMAP = "_FuzzOcc";
        public static string SPECULARPARAM = "_SpecInt";
        public static string EMISSIONPARAM = "_MKGlowPower";
        //"_IBLFilter"
        //"_SpecOffset"
        //"_Shininess"
        //"_Fresnel"
        //"Custom/Subsurface/GlossNMCull"
        public static float DEFAULT_SPEC_INT = 0.2f;

        public static string EMISSIVE_SHADER_NAME = "Custom/Subsurface/EmissiveGlow";
        public static string ALPHA_SHADER_NAME = "Marmoset/Transparent/Specular IBL";
        //  public static string STANDARD_SHADER_NAME = "Marmoset/Specular IBL";
        public static string STANDARD_SHADER_NAME = "Custom/Subsurface/GlossNMCull";
        //public static string STANDARD_SHADER_NAME = "Marmoset/Beta/Skin IBL SoftComputeBuff";

    }

    class MeshData
    {
        public Transform[] boneList;
        public Matrix4x4[] bindposes;
        public GameObject armature;

        public MeshData(Transform[] boneList_, Matrix4x4[] bindposes_, GameObject armature_)
        {
            boneList = boneList_;
            bindposes = bindposes_;
            armature = armature_;
        }
    }

    class XPSLoader : MVRScript
    {
        bool debug = false;
        bool debugSRM = false;
        bool debug2 = false;
        string prevFolder = "";
        public string pathtofile;
        public int rowcounter = 0;
        JSONStorableString filePath;
        JSONStorableBool loadedModel;
        List<SkinnedMeshRenderer> smr;
        Dictionary<Material, XPSRenderGroup> renderGroups;
        GameObject rootObj;
        List<UIDynamic> uicomp;
        List<Material> materials;
        JSONClass pluginJson;
        bool subscene = false;
        public const string xpsRootObjName = "XPS_ROOT";
        Dictionary<string, Transform> transforms;
        List<string> transformIds;

        public void ModelLoadComplete(List<object> bindings)
        {
            bindings.Add(loadedModel.val);
        }

        public void RestoreModel()
        {
            cleanupTransforms();

            if (!loadedModel.val && filePath.val != null && filePath.val.Length > 0)
            {
                loadModel(filePath.val, true);
                loadedModel.val = true;
                prevFolder = FileManagerSecure.GetDirectoryName(filePath.val);
            }
        }

        public GameObject getActualContainingGOM(bool forceSS = false)
        {
            if (this.containingAtom.isSubSceneRestore || subscene || forceSS)
            {
                return this.containingAtom.containingSubScene.containingAtom.gameObject;
            }
            else
            {
                return containingAtom.gameObject;
            }
        }

        private Transform getRescaleObject(Atom atom, bool isSubscene)
        {
            if (!isSubscene)
            {
                foreach (Transform tt in atom.GetComponentsInChildren<Transform>())
                {
                    if (tt.name.Equals("rescaleObject"))
                        return tt;
                }
            }else
            {
                return atom.transform.Find("reParentObject").Find("object").Find("reParentObject").Find("object").Find("rescaleObject");                              
            }

            throw new Exception("No rescaleObject on atom.");
        }
     
        public Atom getActualContainingAtom(bool forceSS = false)
        {
            if (this.containingAtom.isSubSceneRestore || subscene || forceSS)
                return SuperController.singleton.GetAtomByUid(this.containingAtom.subScenePath.Split('/')[0]);
            else
                return containingAtom;
        }

        public override void Init()
        {
            if (this.containingAtom.isSubSceneRestore || containingAtom.name.Contains('/'))
                subscene = true;
            else
                subscene = false;

            renderGroups = new Dictionary<Material, XPSRenderGroup>();

            loadedModel = new JSONStorableBool("loadedModel", false);
            //RegisterBool(loadedModel);

            filePath = new JSONStorableString("filePath", null);//, RestoreModel);
            RegisterString(filePath);

            rootObj = null;

            CreateButton("Select XNALara File").button.onClick.AddListener(() =>
            {
                if (prevFolder == "")
                    prevFolder = FileManagerSecure.GetDirectoryName("Custom/xps"); ;// SuperController.singleton.dir + "Custom\\XPS";
                SuperController.singleton.GetMediaPathDialog((string path) =>
                {
                    if (!path.Equals(""))
                    {

                        loadModel(path, false);
                        loadedModel.val = true;
                        prevFolder = FileManagerSecure.GetDirectoryName(path);
                    }
                    SuperController.singleton.BroadcastMessage("OnActionsProviderAvailable", this, SendMessageOptions.DontRequireReceiver);
                }, "mesh|ascii|xps", prevFolder, true);


            });

            CreateButton("Clean up").button.onClick.AddListener(() =>
            {
                cleanupTransforms();
                cleanupUI();
                loadedModel.val = false;
            });

            CreateButton("Create Autocollider").button.onClick.AddListener(() =>
            {
                foreach (SkinnedMeshRenderer sm in smr)
                {
                         makeCollider(sm);// XXXX
                }
            });

 
        }

        private JSONClass extractPluginJSON(JSONNode file, string id)
        {
            JSONClass retJson = null;

            JSONNode sceneFile = file.AsObject["atoms"];

            foreach (JSONNode st in sceneFile.Childs)
            {
                if (st["id"].ToString().Equals("\"" + id + "\""))
                {

                    foreach (JSONNode subSt in st["storables"].Childs)
                    {
                        if (subSt["id"].ToString().Equals("\"" + storeId + "\""))
                        {
                            retJson = subSt.AsObject;
                            break;
                        }
                    }
                    break;
                }
            }

            return retJson;
        }

        public override void PostRestore()
        {
            pluginJson = null;

            if (!subscene)
            {
                pluginJson = extractPluginJSON(SuperController.singleton.loadJson, this.AtomUidToStoreAtomUid(this.containingAtom.uid));
                RestoreFromJSON((JSONClass)pluginJson);
            }
            else
            {

                JSONNode subsceneSave = SuperController.singleton.GetSaveJSON(this.containingAtom.parentAtom).AsObject["atoms"]; ;
                string ssPath = null;

                foreach (JSONNode st in subsceneSave.Childs)
                {                    
                    if (st["id"].ToString().Equals("\"" + this.containingAtom.subScenePath.TrimEnd('/') + "\""))
                    {
                        
                        foreach (JSONNode subSt in st["storables"].Childs)
                        {

                            if (subSt["id"].ToString().Equals("\"" + this.containingAtom.containingSubScene.storeId + "\""))
                            {
                                pluginJson = subSt.AsObject;                               
                                ssPath = subSt["storePath"];
                               
                                break;
                            }
                        }
                        break;
                    }
                }

                //if ss path!=null and it doesn't contain a / it means its just been made.. have to goto the UI to get where.
   
                if (ssPath != null && !ssPath.Contains("/"))
                {
                    SubScene subSceneComp = this.containingAtom.containingSubScene;
                    SubSceneUI subSceneUI = subSceneComp.UITransform.GetComponentInChildren<SubSceneUI>();
                    ssPath = "Custom/SubScene/" + subSceneUI.creatorNameInputField.text + "/" + subSceneUI.signatureInputField.text + "/" + ssPath;
                }

                if (ssPath != null && ssPath.Contains("/"))
                {
                    try
                    {
                        JSONNode subsceneNode = SuperController.singleton.LoadJSON(ssPath);
                        pluginJson = extractPluginJSON(subsceneNode, this.AtomUidToStoreAtomUid(this.containingAtom.uid).Split('/')[1]);
                    }
                    catch (Exception e)
                    {
                        SuperController.LogMessage("Unable to load stored JSON: " + ssPath);
                    }

                    if (pluginJson != null)
                        RestoreFromJSON((JSONClass)pluginJson);
                }

            }

            base.PostRestore();

            RestoreModel();
        }

        protected Transform getBoneByName(Transform root, string name)
        {
            foreach(Transform kids in root.GetComponentsInChildren<Transform>())
            {
                if (kids.name.Equals(name))
                    return kids;
            }

            return null;
        }

        public void loadModel(string path, bool restore)
        {
            rootObj = new GameObject(xpsRootObjName);
            rootObj.transform.position = containingAtom.freeControllers[0].transform.position;
            rootObj.transform.rotation = containingAtom.freeControllers[0].transform.rotation;
            try
            {
                filePath.val = path;
                Load(path, true, rootObj);
                
                Transform rescaleObj = getRescaleObject(getActualContainingAtom(), subscene);

                rootObj.transform.parent = rescaleObj;

                foreach (SkinnedMeshRenderer sm in smr)
                {
                         //makeCollider(sm);// XXXX
                }

                refreshTransforms(subscene);
                restoreBoneAdjustments();
                CreateXPSModelUI(restore);

                loadedModel.val = true;
            }
            catch (Exception e)
            {
                SuperController.LogError(e.Message);
                SuperController.LogError(e.StackTrace);
                Destroy(rootObj);
            }

         //   rootObj.transform.parent = containingAtom.freeControllers[0].transform;
        }

        private void cleanupTransforms()
        {
            if (rootObj != null)
                Destroy(rootObj);
            else
            {
                Transform[] trans = getActualContainingGOM().GetComponentsInChildren<Transform>();
                foreach (Transform tt in trans) { if (tt.name.Equals(xpsRootObjName)) Destroy(tt.gameObject); }
            }
        }

        public void cleanupUI()
        {
            cleanupUIItems(uicomp);
        }

        public void cleanupUIItems(List<UIDynamic> items)
        {
            if (items != null)
            {
                foreach (UIDynamic uid in items)
                {
                    if (uid.GetType().Equals(typeof(UIDynamicTextField)))
                    {
                        this.RemoveTextField((UIDynamicTextField)uid);
                        Destroy(uid);
                    }
                    else if (uid.GetType().Equals(typeof(UIDynamicToggle)))
                    {
                        this.RemoveToggle((UIDynamicToggle)uid);
                        Destroy(uid);
                    }
                    else if (uid.GetType().Equals(typeof(UIDynamicSlider)))
                    {
                        this.RemoveSlider((UIDynamicSlider)uid);
                        Destroy(uid);
                    }
                    else if (uid.GetType().Equals(typeof(UIDynamicPopup)))
                    {
                        this.RemovePopup((UIDynamicPopup)uid);
                        Destroy(uid);
                    }
                    else if (uid.GetType().Equals(typeof(UIDynamic)))
                    {
                        this.RemoveSpacer((UIDynamic)uid);
                        Destroy(uid);
                    }
                }
            }

            items = null;
        }

        protected void flipAllFaces()
        {
            foreach(SkinnedMeshRenderer sm in smr)
            {
                sm.sharedMesh.triangles = sm.sharedMesh.triangles.Reverse<int>().ToArray();
            }
        }

        public void makeCollider(SkinnedMeshRenderer rr)
        {
            Transform[] tt = rr.bones;

            BoneWeight[] weights = rr.sharedMesh.boneWeights;
            Dictionary<int, Transform> weightedBones = new Dictionary<int, Transform>();
            foreach (BoneWeight bb in weights)
            {
                if (!weightedBones.ContainsKey(bb.boneIndex0))
                    weightedBones.Add(bb.boneIndex0, tt[bb.boneIndex0]);
                if (!weightedBones.ContainsKey(bb.boneIndex1))
                    weightedBones.Add(bb.boneIndex1, tt[bb.boneIndex1]);
                if (!weightedBones.ContainsKey(bb.boneIndex0))
                    weightedBones.Add(bb.boneIndex2, tt[bb.boneIndex2]);
                if (!weightedBones.ContainsKey(bb.boneIndex0))
                    weightedBones.Add(bb.boneIndex3, tt[bb.boneIndex3]);
            }

            foreach (KeyValuePair<int, Transform> etc in weightedBones)
            {
                //   Debug.DrawLine(etc.position, etc.parent.position, Color.red, 10000f); 
                if (etc.Value.gameObject.GetComponent<CapsuleCollider>() == null)
                {

                    //cap.direction = 1;

                    Vector3 direction = (etc.Value.position - etc.Value.parent.position);

                    // First move center and bounds from bone space to local space

                    // if(!direction.Equals(Vector3.zero))
                    {
                        GameObject newColl = new GameObject(etc.Value.name + "collider");
                        newColl.transform.position = etc.Value.position;
                        newColl.transform.rotation = GetBoneFix(etc.Value, direction);
                        newColl.transform.parent = etc.Value;

                        CapsuleCollider cap = newColl.AddComponent<CapsuleCollider>();
                        //Debug.DrawLine(etc.Value.position, etc.Value.parent.position, Color.red, 100000f);
                        float dist = Vector3.Distance(etc.Value.position, etc.Value.parent.position) * etc.Value.worldToLocalMatrix.lossyScale.x;
                        /*
                                                if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y) && Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
                                                    cap.direction = 0;
                                                else if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x) && Mathf.Abs(direction.y) > Mathf.Abs(direction.z))
                                                    cap.direction = 1;
                                                else if (Mathf.Abs(direction.z) > Mathf.Abs(direction.x) && Mathf.Abs(direction.z) > Mathf.Abs(direction.y))
                                                    cap.direction = 2;
                                                    */
                        cap.height = dist;
                        cap.radius = dist / 4;
                    }
                }
            }
        }

        public Quaternion GetBoneFix(Transform bone, Vector3 boneDirection)
        {
            // This function gives a rotation to align the bone to the positive X axis
            Quaternion boneFixRot = Quaternion.identity;

            Vector3 boneRotVec = new Vector3(boneDirection.x, boneDirection.y, boneDirection.z);
            Vector3 boneRotVecAbs = new Vector3(Math.Abs(boneDirection.x), Math.Abs(boneDirection.y), Math.Abs(boneDirection.z));

            // If the abs X axis is longest we remove Z rotation before aligning to X
            if (boneRotVecAbs.x > boneRotVecAbs.y && boneRotVecAbs.x > boneRotVecAbs.z)
            {
                boneRotVec.z = 0f;
                boneFixRot = Quaternion.FromToRotation(boneRotVec, Vector3.right);
            }

            // If the abs Y axis is longest we want to undo the Y before aligning to X
            if (boneRotVecAbs.y > boneRotVecAbs.x && boneRotVecAbs.y > boneRotVecAbs.z)
            {
                if (boneDirection.z < 0)
                {
                    boneFixRot = Quaternion.FromToRotation(boneDirection, Vector3.forward);
                    boneFixRot = Quaternion.Euler(-boneFixRot.eulerAngles.x, 0f, 0f) * Quaternion.FromToRotation(boneDirection, Vector3.right);
                }
                else
                {
                    boneFixRot = Quaternion.FromToRotation(boneDirection, Vector3.forward);
                    boneFixRot = Quaternion.Euler(boneFixRot.eulerAngles.x, 0f, 0f) * Quaternion.FromToRotation(boneDirection, Vector3.right);
                }
            }

            // If the abs Z axis is longest we remove X rotation before aligning to Z
            if (boneRotVecAbs.z > boneRotVecAbs.x && boneRotVecAbs.z > boneRotVecAbs.y)
            {
                boneRotVec.x = 0f;
                boneFixRot = Quaternion.FromToRotation(boneRotVec, Vector3.right);
            }

            return boneFixRot;
        }

        public MeshData createRig(Armature arma, GameObject rootObj)
        {
            int boneCount = arma.Bones.Length;

            Dictionary<XNALara.Armature.Bone, GameObject> bones = new Dictionary<Armature.Bone, GameObject>();
            Transform[] bonesList = new Transform[arma.Bones.Length];
            Matrix4x4[] bindposes = new Matrix4x4[arma.Bones.Length];

            GameObject armature = new GameObject("Armature");
            armature.transform.position = Vector3.zero;
            armature.transform.rotation = Quaternion.identity;
            //armature.transform.localScale = new Vector3(100f, 100f, 100f);


            foreach (XNALara.Armature.Bone bn in arma.Bones)
            {
                GameObject gbn = new GameObject(bn.name);

                bones.Add(bn, gbn);


                gbn.transform.position = new Vector3(-bn.absPosition.x,  bn.absPosition.y , bn.absPosition.z);
             //   gbn.transform.rotation =  bn.absTransform.GetRotation(); //Quaternion.identity;// 
                gbn.transform.localScale = bn.relTransform.GetScale();



                if (bn.id == 0)
                {
                    gbn.transform.parent = armature.transform;
                    //  gbn.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                }else if (bn.parent != null && bones.ContainsKey(bn.parent))
                {
                    gbn.transform.parent = bones[bn.parent].transform;
                }
                else if(bonesList[bn.id-1]!=null)  // try and parent to last bone in the list
                {
                    gbn.transform.parent = bonesList[bn.id - 1];
                }
                else  //fine parent to the root bone.
                {
                    gbn.transform.parent = bonesList[0];
                }

/*
                if (bn.id != 0)
                {
                    Vector3 direction = (gbn.transform.position - gbn.transform.parent.position);
                    gbn.transform.rotation = GetBoneFix(gbn.transform, direction);
                }*/

                bonesList[bn.id] = gbn.transform;



                Matrix4x4 invertBindPose = new Matrix4x4();

//                if (bn.id == 0)
  //                  invertBindPose = gbn.transform.worldToLocalMatrix * armature.transform.localToWorldMatrix;// * rootObj.transform.localToWorldMatrix;// * rootObj.transform.localToWorldMatrix;// * rootObj.transform.localToWorldMatrix;
    //            else
                invertBindPose = gbn.transform.worldToLocalMatrix * armature.transform.localToWorldMatrix;// * rootObj.transform.localToWorldMatrix;// * rootObj.transform.localToWorldMatrix;// * gbn.transform.root.localToWorldMatrix;// ;

                bindposes[bn.id] = invertBindPose;
            }

            armature.transform.position = rootObj.transform.position;
            armature.transform.rotation = rootObj.transform.rotation;
            armature.transform.parent = rootObj.transform;

            // rootObj.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            return new MeshData(bonesList, bindposes, armature);
        }

        public void debugTransformView(Transform root)
        {

            LineRenderer lr;
            List<Vector3> line = new List<Vector3>();

            if (this.GetComponent<LineRenderer>() != null)
                lr = this.gameObject.GetComponent<LineRenderer>();
            else
                lr = this.gameObject.AddComponent<LineRenderer>();

            lr.material = new Material(Shader.Find("Oculus/Unlit Transparent Color"));
            lr.material.SetColor("_Color", Color.red);
            lr.material.color = Color.red;
            lr.startWidth = 0.01f;
            lr.endWidth = 0.01f;

            foreach (Transform tt in root.GetComponentsInChildren<Transform>())
            {
                line.Add(tt.transform.position);
            }

            lr.positionCount = line.Count;
            lr.SetPositions(line.ToArray());
        }

        public Material processTexture(MeshDesc mesh, string path)
        {
            //  

            Material m = null;// new Material(Shader.Find("Marmoset/Transparent/Specular IBL"));

            string[] param = mesh.name.Split('_');

            int rgNum = 0;

            if (int.TryParse(param[0], out rgNum))
            {
                XPSRenderGroup rg = new XPSRenderGroup(rgNum, param);                
                m = rg.constructMaterial(mesh.textures, path);
                renderGroups.Add( m, rg);
                setDefaultMaterialParams(ref m);
            }
            else
            {
                m = XPSRenderGroup.constructFallbackMaterial(mesh.textures, path);
            }



            return m;
        }

        public List<SkinnedMeshRenderer> createSkinnedMeshes(List<MeshDesc> meshes, MeshData meshData, GameObject rootObjXPS, string path)
        {
            List<SkinnedMeshRenderer> smr_ = new List<SkinnedMeshRenderer>();

            Dictionary<string, List<SkinnedMeshRenderer>> smrDic = new Dictionary<string, List<SkinnedMeshRenderer>>();

            foreach (MeshDesc mesh in meshes)
            {
               
                SkinnedMeshRenderer skinnedMesh = createSkinnedMesh(mesh, meshData.boneList, meshData.bindposes, meshData.armature.transform.parent.gameObject, path);
                //skinnedMesh.name = mesh.name;
        /*        if(skinnedMesh.name.ToLower().Contains("!") || skinnedMesh.name.ToLower().Contains("part"))
                {
                    string newmeshname = mesh.name;
                    if (skinnedMesh.name.ToLower().Contains("!"))
                        newmeshname = skinnedMesh.name.Split('!')[0];
                    else if (skinnedMesh.name.ToLower().Contains("part"))
                        newmeshname = skinnedMesh.name.Substring(0, skinnedMesh.name.ToLower().IndexOf("part"));

                    SuperController.LogMessage("create skined mesh " + mesh.name+" key is "+newmeshname);

                    if (smrDic.ContainsKey(newmeshname))
                        smrDic[newmeshname].Add(skinnedMesh);
                    else
                    {
                        List<SkinnedMeshRenderer> smlocal = new List<SkinnedMeshRenderer>();
                        smlocal.Add(skinnedMesh);
                        smrDic.Add(newmeshname,smlocal);
                    }
                }
                else*/
                {
                    smr_.Add(skinnedMesh);
                }
            }


            foreach (KeyValuePair<string, List<SkinnedMeshRenderer>> smrr in smrDic)
            {
                if (smrr.Value != null)
                {

                  //  SkinnedMeshRenderer comb = MeshCombiner.Combine(smrr.Value, smrr.Value[0].transform.parent.gameObject, smrr.Key).GetComponent<SkinnedMeshRenderer>();
                 //   smr_.Add(comb);
                    //Unwrapping.GenerateSecondaryUVSet(comb.sharedMesh);
                }

            }

            return smr_;
        }

        public SkinnedMeshRenderer createSkinnedMesh(MeshDesc mesh, Transform[] bonesList, Matrix4x4[] bindposes, GameObject rootObj, string path)
        {

            Mesh MN = new Mesh();

            Vector3[] verts = new Vector3[mesh.vertices.Length];
            Vector3[] norms = new Vector3[mesh.vertices.Length];
            Vector2[] uv = new Vector2[mesh.vertices.Length];
            Vector2[] uv2 = new Vector2[mesh.vertices.Length];
            Vector2[] uv3 = new Vector2[mesh.vertices.Length];
            Vector2[] uv4 = new Vector2[mesh.vertices.Length];

            BoneWeight[] weights = new BoneWeight[mesh.vertices.Length];

            int[] tris = new int[mesh.indices.Length];

            int i = 0;
            int adjusted = 0;
            int overridedd = 0;
            int xbones = 0;
            int biggestBoneCount = 0;
            foreach (MeshDesc.Vertex vin in mesh.vertices)
            {
                // verts[i] = bonesList[0].TransformPoint(new Vector3(vin.position.x, vin.position.y, vin.position.z));
                //norms[i] = bonesList[0].TransformPoint(new Vector3(vin.normal.x, vin.normal.y, vin.normal.z));
                verts[i] = new Vector3(-vin.position.x, vin.position.y, vin.position.z);
                norms[i] = new Vector3(vin.normal.x, vin.normal.y, vin.normal.z);
                uv[i] = new Vector2(vin.texCoords[0].x, vin.texCoords[0].y);

                if (vin.texCoords.Length > 1) uv2[i] = new Vector2(vin.texCoords[1].x, vin.texCoords[1].y);
                if (vin.texCoords.Length > 2) uv3[i] = new Vector2(vin.texCoords[2].x, vin.texCoords[2].y);
                if (vin.texCoords.Length > 3) uv4[i] = new Vector2(vin.texCoords[3].x, vin.texCoords[3].y);


                weights[i] = new BoneWeight();
              
                biggestBoneCount = vin.boneIndicesGlobal.Length > biggestBoneCount ? vin.boneIndicesGlobal.Length : biggestBoneCount;
                             
                weights[i].boneIndex0 = vin.boneIndicesGlobal.Length > 0 ? vin.boneIndicesGlobal[0] : 0;
                weights[i].boneIndex1 = vin.boneIndicesGlobal.Length > 1 ? vin.boneIndicesGlobal[1] : 0;
                weights[i].boneIndex2 = vin.boneIndicesGlobal.Length > 2 ? vin.boneIndicesGlobal[2] : 0;
                weights[i].boneIndex3 = vin.boneIndicesGlobal.Length > 3 ? vin.boneIndicesGlobal[3] : 0;


                weights[i].weight0 = vin.boneWeights.Length > 0 ? vin.boneWeights[0] : 0;
                weights[i].weight1 = vin.boneWeights.Length > 1 ? vin.boneWeights[1] : 0;
                weights[i].weight2 = vin.boneWeights.Length > 2 ? vin.boneWeights[2] : 0;
                weights[i].weight3 = vin.boneWeights.Length > 3 ? vin.boneWeights[3] : 0;

                float weightSum = (weights[i].weight0 + weights[i].weight1 + weights[i].weight2 + weights[i].weight3);


                i++;
            }

            string meshNewName = mesh.name.Split('_').Length > 2 ? mesh.name.Split('_')[1] : mesh.name;

            if (biggestBoneCount > 4) SuperController.LogMessage("Loading unsupported mesh "+meshNewName+" which has more than 4 boneweights. May not render correctly.");

            i = 0;

            foreach (ushort face in mesh.indices)
            {
                tris[i] = face;
                i++;
            }

            MN.vertices = verts;
            MN.normals = norms;
            MN.bindposes = bindposes;
            MN.boneWeights = weights;
            MN.triangles = tris;//.Reverse().ToArray();
            MN.uv = uv;
                       

            GameObject gom = new GameObject(meshNewName);
            //GameObject gom = new GameObject(mesh.name);
            
            // gom.transform.position = bonesList[0].transform.position;
            // gom.transform.rotation = bonesList[0].transform.rotation;
            gom.transform.parent = rootObj.transform;
            SkinnedMeshRenderer mf = gom.AddComponent<SkinnedMeshRenderer>();
            mf.sharedMesh = MN;
            mf.bones = bonesList;            

            mf.rootBone = bonesList[0];
            try
            {
                Material m = processTexture(mesh, path);
                mf.sharedMaterial = m;
                mf.material = m;
            }
            catch (Exception e) { SuperController.LogMessage(e.Message); }
            MN.RecalculateBounds();

            //SuperController.LogMessage("GOM Parent " + gom.transform.parent.name);

            return mf;


        }

        public List<SkinnedMeshRenderer> Load(string path, bool hasTangents, GameObject rootObj)
        {
            string[] extensions = path.Split('.');
            string exe = extensions[extensions.Length - 1];

            Armature arma = null;
            smr = new List<SkinnedMeshRenderer>();

            MeshData meshData = null;

            if (exe.ToLower().Equals("ascii"))
            {
                String raw = FileManagerSecure.ReadAllText(path);

                String[] fileSplit = Regex.Split(raw, "\n");
                arma = LoadArmatureASCII(fileSplit);
                meshData = createRig(arma, rootObj);
                int boneCount = arma.Bones.Length;
                rowcounter = (1 + (boneCount * 3));
                int meshCount = int.Parse(fileSplit[rowcounter].Split('#')[0]);
                rowcounter++;
                List<MeshDesc> meshes = new List<MeshDesc>();
                for (int meshID = 0; meshID < meshCount; meshID++)
                {
                    meshes.Add(LoadMeshASCII(fileSplit, arma != null));                    
                }
               
                smr = createSkinnedMeshes(meshes, meshData, rootObj, path);
            }
            else if (exe.ToLower().Equals("mesh") || exe.ToLower().Equals("xps"))
            {
                int rr = 0;
                byte[] fileData = FileManagerSecure.ReadAllBytes(path);
                arma = LoadArmatureBinary(fileData);

                if (arma != null)
                {
                    rr = arma.rowcount;

                    meshData = createRig(arma, rootObj);
                    int boneCount = arma.Bones.Length;
                    uint meshCount = BitConverter.ToUInt32(fileData, rr);
                    rr = rr + 4;
                    List<MeshDesc> meshes = new List<MeshDesc>();
                    for (int meshID = 0; meshID < meshCount; meshID++)
                    {
                        MeshDesc mesh = null;
                        int currentCount = rr;

                        mesh = LoadMeshBinary(fileData, arma != null, rr, arma.hasTangents, arma.hasVariableWeights);
                        meshes.Add(mesh);
                          rr = mesh.rowcount;
                    }
                    
                    smr = createSkinnedMeshes(meshes, meshData, rootObj, path);

                }
            }
           // debugTransformView(meshData.boneList[0]);

            if (meshData != null && meshData.boneList != null && meshData.boneList[0] != null)
            {
             //   meshData.boneList[0].localPosition = Vector3.zero;
          //      meshData.boneList[0].localRotation = Quaternion.identity;
            }
           
            // meshData.boneList[0].parent = meshData.armature.transform;
            return smr;
        }

        private void refreshTransforms(bool forceSS)
        {
            refreshTransforms(getActualContainingGOM(forceSS));
        }

        private void refreshTransforms(GameObject root)
        {
            SkinnedMeshRenderer[] smr = root.GetComponentsInChildren<SkinnedMeshRenderer>();

            transformIds = new List<string>();
            transforms = new Dictionary<string, Transform>();

            foreach (SkinnedMeshRenderer sm in smr)
            {

                Transform[] tt = sm.bones;


                foreach (Transform trans in tt)
                {
                    if (trans != null)
                    {
                        if (trans.gameObject.GetComponent<Atom>() != null || trans.gameObject.GetComponent<RectTransform>() != null || trans.gameObject.GetComponent<FreeControllerV3>() != null || trans.gameObject.GetComponent<SubAtom>() != null)
                        {
                            continue;
                        }


                        if (transforms.ContainsKey(trans.name))
                        {
                            if (transforms[trans.name].Equals(trans)) //this is the same bone.. ignore it.
                                continue;
                            else //a different bone with the same name, add a uniq version of it.
                            {
                                String uniqName = trans.name;
                                int count = 0;
                                while (transforms.ContainsKey(uniqName))
                                {
                                    uniqName = trans.name + "_" + count;
                                    count++;
                                }
                                transforms.Add(uniqName, trans);
                                transformIds.Add(uniqName);
                            }
                        }
                        else
                        {
                            transforms.Add(trans.name, trans);
                            transformIds.Add(trans.name);
                        }

                    }
                }
            }
        }

        private void restoreBoneAdjustments()
        {
            Dictionary<string, Vector3> boneRotations = new Dictionary<string, Vector3>();
            Dictionary<string, Vector3> boneTransforms = new Dictionary<string, Vector3>();
            Dictionary<string, float> boneScales = new Dictionary<string, float>();

            if (pluginJson != null)
            {
                JSONClass arr = pluginJson.AsObject;
                
                foreach(string key in arr.Keys)
                {
                    string adjKet = key.Split('_')[0];
                    string boneName;
                    switch (adjKet)
                    {
                        case "xRotBone":
                            boneName = key.Split('_')[1];
                            JSONStorableFloat xRotBone = new JSONStorableFloat(key, transforms[boneName].localRotation.x, -180f, 180f, true, true);
                            RegisterFloat(xRotBone);
                            boneRotations[boneName] = boneRotations.ContainsKey(boneName) ? new Vector3(arr[key].AsFloat, boneRotations[boneName].y, boneRotations[boneName].z) : new Vector3(arr[key].AsFloat, 0f, 0f);
                            xRotBone.RestoreFromJSON(pluginJson);
                            break;
                        case "yRotBone":
                            boneName = key.Split('_')[1];
                            JSONStorableFloat yRotBone = new JSONStorableFloat(key, transforms[boneName].localRotation.y, -180f, 180f, true, true);
                            RegisterFloat(yRotBone);
                            boneRotations[boneName] = boneRotations.ContainsKey(boneName) ? new Vector3(boneRotations[boneName].x, arr[key].AsFloat, boneRotations[boneName].z) : new Vector3(0f, arr[key].AsFloat, 0f);
                            yRotBone.RestoreFromJSON(pluginJson);
                            break;
                        case "zRotBone":
                            boneName = key.Split('_')[1];
                            JSONStorableFloat zRotBone = new JSONStorableFloat(key, transforms[boneName].localRotation.z, -180f, 180f, true, true);
                            RegisterFloat(zRotBone);
                            boneRotations[boneName] = boneRotations.ContainsKey(boneName) ? new Vector3(boneRotations[boneName].x, boneRotations[boneName].y, arr[key].AsFloat) : new Vector3(0f, 0f, arr[key].AsFloat);
                            zRotBone.RestoreFromJSON(pluginJson);
                            break;

                        case "xPosBone":
                            boneName = key.Split('_')[1];
                            JSONStorableFloat xPosBone = new JSONStorableFloat(key, transforms[boneName].localPosition.x, -5f, 5f, true, true);
                            RegisterFloat(xPosBone);
                            boneTransforms[boneName] = boneTransforms.ContainsKey(boneName) ? new Vector3(arr[key].AsFloat, boneTransforms[boneName].y, boneTransforms[boneName].z) : new Vector3(arr[key].AsFloat, transforms[boneName].localPosition.y, transforms[boneName].localPosition.z);
                            xPosBone.RestoreFromJSON(pluginJson);
                            break;
                        case "yPosBone":
                            boneName = key.Split('_')[1];
                            JSONStorableFloat yPosBone = new JSONStorableFloat(key, transforms[boneName].localPosition.y, -5f, 5f, true, true);
                            RegisterFloat(yPosBone);
                            boneTransforms[boneName] = boneTransforms.ContainsKey(boneName) ? new Vector3(boneTransforms[boneName].x, arr[key].AsFloat, boneTransforms[boneName].z) : new Vector3(transforms[boneName].localPosition.x, arr[key].AsFloat, transforms[boneName].localPosition.z);
                            yPosBone.RestoreFromJSON(pluginJson);
                            break;
                        case "zPosBone":
                            boneName = key.Split('_')[1];
                            JSONStorableFloat zPosBone = new JSONStorableFloat(key, transforms[boneName].localPosition.z, -5f, 5f, true, true);
                            RegisterFloat(zPosBone);
                            boneTransforms[boneName] = boneTransforms.ContainsKey(boneName) ? new Vector3(boneTransforms[boneName].x, boneTransforms[boneName].y, arr[key].AsFloat) : new Vector3(transforms[boneName].localPosition.x, transforms[boneName].localPosition.y, arr[key].AsFloat);
                            zPosBone.RestoreFromJSON(pluginJson);
                            break;

                        case "scaleBone":
                            boneName = key.Split('_')[1];
                            JSONStorableFloat scaleBone = new JSONStorableFloat(key, 1f, 0f, 5f, true, true);
                            RegisterFloat(scaleBone);
                            boneScales[boneName] = arr[key].AsFloat;
                            scaleBone.RestoreFromJSON(pluginJson);
                            break;
                    }

                }

                foreach(KeyValuePair<string, Vector3> rot in boneRotations)                
                    transforms[rot.Key].localRotation = Quaternion.Euler(rot.Value);

                foreach (KeyValuePair<string, Vector3> trans in boneTransforms)
                    transforms[trans.Key].localPosition = trans.Value;

                foreach (KeyValuePair<string, float> scal in boneScales)
                    transforms[scal.Key].localScale = new Vector3(scal.Value, scal.Value, scal.Value);

            }
        }

        public List<UIDynamic> createBoneUI(Transform selectedBone)
        {
            List<UIDynamic> uiBonecomp = new List<UIDynamic>();

            if (selectedBone != null)
            {
                if (uiBonecomp.Count > 0)
                    cleanupUIItems(uiBonecomp);

                uiBonecomp = new List<UIDynamic>();

                UIDynamicSlider xSlid = createFloatSlider("xRotBone_" + selectedBone.name, "X Bone Rotation", selectedBone.localRotation.x, -180f, 180f, (float val) => { }, false, false);
                UIDynamicSlider ySlid = createFloatSlider("yRotBone_" + selectedBone.name, "Y Bone Rotation", selectedBone.localRotation.y, -180f, 180f, (float val) => { }, false, false);
                UIDynamicSlider zSlid = createFloatSlider("zRotBone_" + selectedBone.name, "Z Bone Rotation", selectedBone.localRotation.y, -180f, 180f, (float val) => { }, false, false);

                sliderToJSONStorableFloat[xSlid].setJSONCallbackFunction += delegate (JSONStorableFloat js) { selectedBone.localRotation = Quaternion.Euler(js.val, ySlid.slider.value, zSlid.slider.value);  };
                sliderToJSONStorableFloat[ySlid].setJSONCallbackFunction += delegate (JSONStorableFloat js) { selectedBone.localRotation = Quaternion.Euler(xSlid.slider.value, js.val, zSlid.slider.value); };
                sliderToJSONStorableFloat[zSlid].setJSONCallbackFunction += delegate (JSONStorableFloat js) { selectedBone.localRotation = Quaternion.Euler(xSlid.slider.value, ySlid.slider.value, js.val); };

                UIDynamicSlider boneScale = createFloatSlider("scaleBone_"+ selectedBone.name, "Bone Scale", selectedBone.localScale.x, 0f, 5f, (float val) => { }, false, false);
                boneScale.slider.onValueChanged.AddListener((float val) => { selectedBone.localScale = new Vector3(val, val, val); });

                UIDynamicSlider xPos = createFloatSlider("xPosBone_" + selectedBone.name, "X Bone Position", selectedBone.localPosition.x, -5f, 5f, (float val) => { }, false, false);
                UIDynamicSlider yPos = createFloatSlider("yPosBone_" + selectedBone.name, "Y Bone Position", selectedBone.localPosition.y, -5f, 5f, (float val) => { }, false, false);
                UIDynamicSlider zPos = createFloatSlider("zPosBone_" + selectedBone.name, "Z Bone Position", selectedBone.localPosition.z, -5f, 5f, (float val) => { }, false, false);
            
                xPos.slider.onValueChanged.AddListener((float val) => { selectedBone.localPosition = new Vector3(val, yPos.slider.value, zPos.slider.value); });
                yPos.slider.onValueChanged.AddListener((float val) => { selectedBone.localPosition = new Vector3(xPos.slider.value, val, zPos.slider.value); });
                zPos.slider.onValueChanged.AddListener((float val) => { selectedBone.localPosition = new Vector3(xPos.slider.value, yPos.slider.value, val); });

                uiBonecomp.Add(xSlid);
                uiBonecomp.Add(ySlid);
                uiBonecomp.Add(zSlid);
                uiBonecomp.Add(boneScale);
                uiBonecomp.Add(xPos);
                uiBonecomp.Add(yPos);
                uiBonecomp.Add(zPos);

                uiBonecomp.Add(CreateSpacer());
            }

            return uiBonecomp;
        }

        protected void setTexturesDirection(bool up)
        {
            if (up)
            {
                foreach (Material m in materials)
                {
                    if (renderGroups.ContainsKey(m))
                    {
                        XPSRenderGroup rg = renderGroups[m];

                        foreach (KeyValuePair<int, string> map in rg.maps)
                        {
                            m.SetTextureScale(map.Value, new Vector2(1, -1));
                        }
                    }
                }
            }
            else
            {
                foreach (Material m in materials)
                {
                    if (renderGroups.ContainsKey(m))
                    {
                        XPSRenderGroup rg = renderGroups[m];

                        foreach (KeyValuePair<int, string> map in rg.maps)
                        {
                            m.SetTextureScale(map.Value, new Vector2(1, 1));
                        }
                    }
                }
            }
        }

        const float default_specularIntensityGlobal = 0.2f;
        const float default_specularSharpnessGlobal = 4f;
        const float default_specularFresnelGlobal = 0.7f;
        const float default_diffuseOffsetGlobal = 0f;
        const float default_specularOffsetlGlobal = 0f;
        const float default_glossOffsetGlobal = 0.8f;
        const float default_iblFilterGlobal = 0f;
        const float default_diffuseBumpinessGlobal = 0.5f;
        const float default_specularBumpinessGlobal = 0.5f;

        protected void setDefaultMaterialParams(ref Material m)
        {
            SetSpecIntensity(m, default_specularIntensityGlobal);
            SetSpecSharpness(m, default_specularSharpnessGlobal);
          SetSpecFresnel(m, default_specularFresnelGlobal); 
         SetDiffOffset(m, default_diffuseOffsetGlobal);
           SetSpecOffset(m, default_specularOffsetlGlobal); 
        SetGlossOffset(m, default_glossOffsetGlobal); 
         SetIBLFilter(m, default_iblFilterGlobal);
         setMaterialParam(m, "_DiffuseBumpiness", default_diffuseBumpinessGlobal);
        setMaterialParam(m, "_SpecularBumpiness", default_specularBumpinessGlobal);

        }

        public void CreateXPSModelUI(bool restore)
        {
            if (uicomp == null)
                uicomp = new List<UIDynamic>();


            materials = new List<Material>();

            uicomp.Add(CreateLabel("Mesh/Material Settings", true));
            Dictionary<string, Material> matmap = new Dictionary<string, Material>();

            foreach (SkinnedMeshRenderer sm in smr)
            {
                string storableName = "meshEnabled" + sm.name;
                bool defaultVal = true;
                if (sm.name.StartsWith("-"))
                {
                    defaultVal = false;
                    sm.enabled = false;
                }

                uicomp.Add(createToggle(storableName, sm.name, defaultVal, (bool val) => { sm.enabled = val; }, true, restore));

                int matcount = 0;
                foreach (Material mat in sm.sharedMaterials)
                {
                    materials.Add(mat);

                    if (!matmap.ContainsKey(mat.name))
                        matmap.Add(mat.name, mat);

                    matcount++;
                }

            }

            uicomp.Add(CreateLabel("Transform Settings", false));

            Transform xps = rootObj.transform;


            uicomp.Add(createFloatSlider("scaleAdjust", "Scale", rootObj.transform.localScale.x, 0f,100f,(float val) => { rootObj.transform.localScale = new Vector3(val, val, val); }, false, restore));
            uicomp.Add(createFloatSlider("xRot", "X Rotation", rootObj.transform.localRotation.x,0f,360f, (float val) => { rootObj.transform.localRotation = Quaternion.Euler(val, rootObj.transform.localRotation.y, rootObj.transform.localRotation.z); }, false, restore));
            uicomp.Add(createFloatSlider("yRot", "Y Rotation", rootObj.transform.localRotation.y, 0f, 360f, (float val) => { rootObj.transform.localRotation = Quaternion.Euler(rootObj.transform.localRotation.x, val, rootObj.transform.localRotation.z); }, false, restore));
            uicomp.Add(createFloatSlider("zRot", "Z Rotation", rootObj.transform.localRotation.z, 0f, 360f, (float val) => { rootObj.transform.localRotation = Quaternion.Euler(rootObj.transform.localRotation.x, rootObj.transform.localRotation.y, val); }, false, restore));

            uicomp.Add(CreateLabel("Global Material Settings", false));

            uicomp.Add(createToggle("flipTextures", "Flip All Textures", true, (bool val) =>{ setTexturesDirection(val); }, false, restore));

            uicomp.Add(createToggle("flipTexturesNormals", "Flip Normals Only", true, (bool val) =>
            {
                if (val)
                {
                    foreach (Material m in materials)
                    {
                        if (renderGroups.ContainsKey(m))
                        {
                            XPSRenderGroup rg = renderGroups[m];

                            foreach (KeyValuePair<int, string> map in rg.maps)
                            {
                                if (map.Value.Equals(XPSRenderGroup.NORMALMAP))
                                {
                                    m.SetTextureScale(map.Value, new Vector2(1, -1));
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    foreach (Material m in materials)
                    {
                        if (renderGroups.ContainsKey(m))
                        {
                            XPSRenderGroup rg = renderGroups[m];

                            foreach (KeyValuePair<int, string> map in rg.maps)
                            {
                                if (map.Value.Equals(XPSRenderGroup.NORMALMAP))
                                {
                                    m.SetTextureScale(map.Value, new Vector2(1, 1));
                                    break;
                                }
                            }
                        }
                    }
                }
            }, false, restore));

            uicomp.Add(createFloatSlider("specularIntensityGlobal", "Specular Intensity", 0.2f, (float val) => { foreach (Material m in materials) SetSpecIntensity(m, val); }, false, restore));
            uicomp.Add(createFloatSlider("specularSharpnessGlobal", "Specular Sharpness", 4f, (float val) => { foreach (Material m in materials) SetSpecSharpness(m, val); }, false, restore));
            uicomp.Add(createFloatSlider("specularFresnelGlobal", "Specular Fresnel", 0.7f, (float val) => { foreach (Material m in materials) SetSpecFresnel(m, val); }, false, restore));
            uicomp.Add(createFloatSlider("diffuseOffsetGlobal", "Diffuse Offset", 0f, (float val) => { foreach (Material m in materials) SetDiffOffset(m, val); }, false, restore));
            uicomp.Add(createFloatSlider("specularOffsetlGlobal", "Specular Offset", 0f, (float val) => { foreach (Material m in materials) SetSpecOffset(m, val); }, false, restore));
            uicomp.Add(createFloatSlider("glossOffsetGlobal", "Gloss Offset", 0.8f, (float val) => { foreach (Material m in materials) SetGlossOffset(m, val); }, false, restore));
            uicomp.Add(createFloatSlider("iblFilterGlobal", "IBL Filter", 0f, (float val) => { foreach (Material m in materials) SetIBLFilter(m, val); }, false, restore));
            uicomp.Add(createFloatSlider("diffuseBumpinessGlobal", "Diffuse Bumpiness", 0.5f, (float val) => { foreach (Material m in materials) setMaterialParam(m, "_DiffuseBumpiness", val); }, false, restore));
            uicomp.Add(createFloatSlider("specularBumpinessGlobal", "Specular Bumpiness", 0.5f, (float val) => { foreach (Material m in materials) setMaterialParam(m, "_SpecularBumpiness", val); }, false, restore));

            uicomp.Add(CreateLabel("Rig Settings", false));
            
            JSONStorableString editedBones = new JSONStorableString("editedBones", "Edited Bones: ");// null, null,"");
            RegisterString(editedBones);
            
            JSONStorableStringChooser boneAdj = new JSONStorableStringChooser("BoneAdj", null, null, "Select a Bone");
            //RegisterStringChooser(boneAdj);

            boneAdj.choices = transformIds;
            //boneAdj.val = boneName;

            UIDynamicPopup fp = CreateFilterablePopup(boneAdj);
            fp.popupPanelHeight = 700f;
            uicomp.Add(fp);

            uicomp.Add(CreateSpacer());

            List<UIDynamic> uiBonecomp = new List<UIDynamic>();

            fp.popup.onValueChangeHandlers += delegate (string value)
            {
                if(transforms.ContainsKey(boneAdj.val))
                { 
                Transform selectedBone = transforms[boneAdj.val];

                    if (uiBonecomp.Count > 0)
                        cleanupUIItems(uiBonecomp);

                    uiBonecomp = createBoneUI(selectedBone);
                    
                    uicomp.AddRange(uiBonecomp);

                    editedBones.val = editedBones.val + "\n" + value;
                }
            };

                     
            uicomp.Add(CreateLabel("Material count " + materials.Count, true));            
            uicomp.Add(CreateTextField(editedBones, true));
            uicomp.Add(CreateSpacer(true));
        }


        protected void SetSpecIntensity(Material m, float val)
        {
            setMaterialParam(m, "_SpecInt", val);
        }

        protected void SetSpecFresnel(Material m, float val)
        {
            setMaterialParam(m, "_Fresnel", val);
        }

        protected void SetSpecSharpness(Material m, float val)
        {
            setMaterialParam(m, "_Shininess", val);

        }

        protected void SetDiffOffset(Material m, float val)
        {
            setMaterialParam(m, "_DiffOffset", val);
        }

        protected void SetSpecBumpiness(Material m, float val)
        {
            setMaterialParam(m, "_SpecularBumpiness", val);

        }

        protected void SetDiffBumpiness(Material m, float val)
        {
            setMaterialParam(m, "_DiffuseBumpiness", val);

        }

        protected void SetSpecOffset(Material m, float val)
        {
            setMaterialParam(m, "_SpecOffset", val);

        }

        protected void SetGlossOffset(Material m, float val)
        {
            setMaterialParam(m, "_GlossOffset", val);
        }

        protected void SetIBLFilter(Material m, float val)
        {
            setMaterialParam(m, "_IBLFilter", val);
        }

        void setMaterialParam(Material m, string param, float val)
        {
            m.SetFloat(param, val);
        }


        public Armature LoadArmatureASCII(String[] fileSplit)
        {
            int boneCount = int.Parse(fileSplit[0].Split('#')[0]);

            if (boneCount == 0)
            {
                return null;
            }

            Armature armature = new Armature();
            Armature.Bone[] bones = new Armature.Bone[boneCount];
            int[] parentIDs = new int[boneCount];

            int boneCounter = 0;
            for (int rowcount = 1; rowcount < boneCount * 3; rowcount = rowcount + 3)
            {
                Armature.Bone bone = new Armature.Bone();
                bone.armature = armature;
                bone.id = boneCounter;
                bone.name = fileSplit[rowcount];

                parentIDs[boneCounter] = int.Parse(fileSplit[rowcount + 1].Split('#')[0]);

                float absPosX = float.Parse(fileSplit[rowcount + 2].Split(' ')[0]);
                float absPosY = float.Parse(fileSplit[rowcount + 2].Split(' ')[1]);
                float absPosZ = float.Parse(fileSplit[rowcount + 2].Split(' ')[2]);

                bone.absPosition = new Vector3(absPosX, absPosY, absPosZ);

                bones[boneCounter] = bone;
                boneCounter++;

            }

            for (int boneID = 0; boneID < boneCount; boneID++)
            {
                Armature.Bone bone = bones[boneID];
                int parentID = parentIDs[boneID];
                if (parentID >= 0)
                {
                    Armature.Bone parent = bones[parentID];
                    bone.parent = parent;
                    parent.children.Add(bone);
                }
            }

            armature.Bones = bones;
            return armature;
        }

        public MeshDesc LoadMeshASCII(String[] fileSplit, bool hasArmature)
        {
            MeshDesc mesh = new MeshDesc();
            mesh.name = fileSplit[rowcounter];
            rowcounter++;

            int uvLayerCount = int.Parse(fileSplit[rowcounter].Split('#')[0].Trim());

            rowcounter++;
            mesh.uvLayerCount = (uint)uvLayerCount;

            int textureCount = int.Parse(fileSplit[rowcounter].Split('#')[0].Trim());

            rowcounter++;
            mesh.textures = new MeshDesc.Texture[textureCount];

            for (int textureID = 0; textureID < textureCount; textureID++)
            {
                MeshDesc.Texture texture = new MeshDesc.Texture();
                texture.filename = fileSplit[rowcounter];
                rowcounter++;

                texture.uvLayerIndex = uint.Parse(fileSplit[rowcounter].Split('#')[0].Trim());
                rowcounter++;
                mesh.textures[textureID] = texture;
            }

            Dictionary<short, short> boneIndexDict = new Dictionary<short, short>();
            List<short> boneIndexMap = new List<short>();

            int vertexCount = int.Parse(fileSplit[rowcounter].Split('#')[0].Trim());


            rowcounter++;
            mesh.vertices = new MeshDesc.Vertex[vertexCount];

            for (int vertexID = 0; vertexID < vertexCount; vertexID++)
            {

                MeshDesc.Vertex vertex = new MeshDesc.Vertex();
                string[] pos = fileSplit[rowcounter].Split(' ');

                float positionX = pos.Length > 0 ? float.Parse(pos[0].Trim()) : 0f;
                float positionY = pos.Length > 1 ? float.Parse(pos[1].Trim()) : 0f;
                float positionZ = pos.Length > 2 ? float.Parse(pos[2].Trim()) : 0f;

                rowcounter++;
                vertex.position = new Vector3(positionX, positionY, positionZ);


                string[] norms = fileSplit[rowcounter].Split(' ');
                float normalX = norms.Length > 0 ? float.Parse(norms[0].Trim()) : 0f;
                float normalY = norms.Length > 1 ? float.Parse(norms[1].Trim()) : 0f;
                float normalZ = norms.Length > 2 ? float.Parse(norms[2].Trim()) : 0f;
                rowcounter++;
                vertex.normal = new Vector3(normalX, normalY, normalZ);

                string[] cols = fileSplit[rowcounter].Split(' ');
                float colorR = cols.Length > 0 ? float.Parse(cols[0].Trim()) / 255.0f : 0f;
                float colorG = cols.Length > 1 ? float.Parse(cols[1].Trim()) / 255.0f : 0f;
                float colorB = cols.Length > 2 ? float.Parse(cols[2].Trim()) / 255.0f : 0f;
                float colorA = cols.Length > 3 ? float.Parse(cols[3].Trim()) / 255.0f : 0f;
                rowcounter++;
                vertex.color = new Vector4(colorR, colorG, colorB, 1.0f);
                vertex.texCoords = new Vector2[uvLayerCount];
                for (int uvLayerID = 0; uvLayerID < uvLayerCount; uvLayerID++)
                {
                    float texCoordX = float.Parse(fileSplit[rowcounter].Split(' ')[0].Trim());
                    float texCoordY = float.Parse(fileSplit[rowcounter].Split(' ')[1].Trim());
                    rowcounter++;
                    vertex.texCoords[uvLayerID] = new Vector2(texCoordX, texCoordY);
                }

                vertex.tangents = new Vector4[uvLayerCount];

                if (hasArmature)
                {

                    string[] bones = fileSplit[rowcounter].Split(' ');
                    rowcounter++;

                    int boneWeightCount = bones.Length;// > 4 ? bones.Length : 4;

                    vertex.boneIndicesGlobal = new short[boneWeightCount]; //or 4.
                    vertex.boneIndicesLocal = new short[boneWeightCount];

                    int boneCount = bones.Length;

                    for (int i = 0; i < boneCount; i++)
                    {
                        vertex.boneIndicesGlobal[i] = (short)float.Parse(bones[i]);
                    }


                    vertex.boneWeights = new float[bones.Length];
                    float weightSum = 0;

                    string[] boneWeights = fileSplit[rowcounter].Trim().Split(' ');
                    rowcounter++;

                    for (int i = 0; i < boneWeightCount; i++)
                    {
                        float weight = float.Parse(boneWeights[i]);
                        vertex.boneWeights[i] = weight;
                        weightSum += weight;
                    }
                    if (weightSum == 0)
                    {
                        vertex.boneWeights[0] = 1.0f;
                    }
                    else
                    {
                        if (weightSum != 1.0f)
                        {
                          for (int i = 0; i < boneWeightCount; i++)
                            {
                                vertex.boneWeights[i] /= weightSum;
                            }
                        }
                    }
                    short indexDefault = 0;

                    for (int i = 0; i < boneWeightCount; i++)
                    {
                        if (vertex.boneWeights[i] > 0)
                        {
                            indexDefault = vertex.boneIndicesGlobal[i];
                            break;
                        }
                    }
                    for (int i = 0; i < boneWeightCount; i++)
                    {
                        short indexGlobal = vertex.boneIndicesGlobal[i];
                        if (vertex.boneWeights[i] == 0)
                        {
                            indexGlobal = indexDefault;
                        }
                        short indexLocal;
                        if (!boneIndexDict.TryGetValue(indexGlobal, out indexLocal))
                        {
                            indexLocal = (short)boneIndexMap.Count;
                            boneIndexDict[indexGlobal] = indexLocal;
                            boneIndexMap.Add(indexGlobal);
                        }
                        vertex.boneIndicesLocal[i] = indexLocal;
                    }
                }
                mesh.vertices[vertexID] = vertex;
            }

            mesh.boneIndexMap = boneIndexMap.ToArray();
            int faceCount = int.Parse(fileSplit[rowcounter].Split('#')[0].Trim());

            rowcounter++;

            mesh.indices = new ushort[faceCount * 3];
            int index = 0;
            for (int faceID = 0; faceID < faceCount; faceID++)
            {
                mesh.indices[index + 0] = ushort.Parse(fileSplit[rowcounter].Split(' ')[0].Trim());
                mesh.indices[index + 1] = ushort.Parse(fileSplit[rowcounter].Split(' ')[1].Trim());
                mesh.indices[index + 2] = ushort.Parse(fileSplit[rowcounter].Split(' ')[2].Trim());
                index += 3;
                rowcounter++;
            }

            return mesh;
        }

        public int[] LoadHeaderBinary(byte[] file)
        {
            int rr = 0;

            int magicnumer = BitConverter.ToInt32(file, rr);
            rr = rr + 4;

            uint version_major = BitConverter.ToUInt16(file, rr);
            rr = rr + 2;
            uint version_minor = BitConverter.ToUInt16(file, rr);
            rr = rr + 2;

            int slength = file[rr]; rr++;
            string xnalara = Encoding.UTF8.GetString(file, rr, slength);
            rr = rr + slength;

            uint settingsLen = BitConverter.ToUInt32(file, rr);
            rr = rr + 4;

            slength = file[rr]; rr++;
            string machineName = Encoding.UTF8.GetString(file, rr, slength);
            rr = rr + slength;


            slength = file[rr]; rr++;
            string userName = Encoding.UTF8.GetString(file, rr, slength);
            rr = rr + slength;


            int leng = 0; int slength2 = 0;
            slength = file[rr]; rr++; slength2 = 0;

            if (slength > 128) { slength2 = file[rr]; rr++; }

            leng = (slength % 128) + (slength2 * 128);

            string filesString = Encoding.UTF8.GetString(file, rr, leng);
            rr = rr + leng;

            Debug.Log(version_major + "|" + version_minor + "|" + xnalara + "|");
            Debug.Log(settingsLen + "|" + machineName + "|" + userName + "|" + filesString);

            if (version_major <= 2 && version_minor <= 12)
            {
                rr = rr + ((int)settingsLen * 4);
            }
            else
            {
                int valuesRead = 0;
                uint hash = BitConverter.ToUInt32(file, rr);
                rr = rr + 4;
                valuesRead++;

                uint items = BitConverter.ToUInt32(file, rr);
                rr = rr + 4;
                valuesRead++;

                for (int i = 0; i < items; i++)
                {
                    uint optType = BitConverter.ToUInt32(file, rr);
                    rr = rr + 4;
                    valuesRead++;

                    uint optcount = BitConverter.ToUInt32(file, rr);
                    rr = rr + 4;
                    valuesRead++;

                    uint optInfo = BitConverter.ToUInt32(file, rr);
                    rr = rr + 4;
                    valuesRead++;

                                      
                    if (optType == 0)
                    {
                        rr = rr +((int)optcount * 4);
                        valuesRead += (int)optcount;
                    }
                    else if (optType == 1) // pose
                    {
                        if (optcount > 0)
                        {
                            int bones;
                            for (bones = 0; bones < optInfo; bones++)
                            {

                                StringBuilder boneline = new StringBuilder();
                                string letter = Encoding.UTF8.GetString(file, rr, 2);
                                rr += 2;
                                boneline.Append(letter);
                                int counter = 0;

                                while (!letter.Contains("\r") && !letter.Contains("\n"))
                                {
                                    letter = Encoding.UTF8.GetString(file, rr, 2);
                                    rr += 2;
                                    boneline.Append(letter);
                                    counter++;
                                }

                            }
                        

                        uint roundMult = optcount % 4 > 0 ? (optcount - (optcount % 4)) + 4 : optcount;
                        if (optcount % 2 > 0 ) roundMult = roundMult - 1;
                 
                        uint addBytes = roundMult - optcount;
                        rr = rr + (int)addBytes;

                      //  if (optcount == 0) rr = rr - 1;

                        }
                    }
                    else if (optType == 2) // flag
                    {
                        for (int op = 0; op < optcount; op++)
                        {
                            uint flag = BitConverter.ToUInt32(file, rr);
                            rr = rr + 4; 
                            valuesRead += 1;

                            uint value = BitConverter.ToUInt32(file, rr);
                            rr = rr + 4; 
                            valuesRead += 1;

                        }
                    }
                    else //waste read till end of settings.
                    {
                        int loopstart = valuesRead;
                        int loopfinis = (int)settingsLen;
                        int ldiff = (int)settingsLen - valuesRead;

                         rr = rr + (ldiff * 4);

                    }
                }
            }

            return new int[] { rr, (int)version_major, (int)version_minor };
        }

        public Armature LoadArmatureBinary(byte[] file)
        {
            Armature armature = new Armature();
            int rr = 0;
            uint boneCount = BitConverter.ToUInt32(file, rr);
            rr = rr + 4;

    

            if (boneCount == 323232)
            {
                int[] rets = LoadHeaderBinary(file);
                rr = rets[0];

                if (rets[1] <= 2 && rets[2] <= 12)
                {
                    armature.hasTangents = true;
                }

                if (rets[1] >= 3)
                {
                    armature.hasVariableWeights = true;
                }

                boneCount = BitConverter.ToUInt32(file, rr);
                rr = rr + 4;             

            }
            else
                armature.hasTangents = true;


            if (boneCount == 0)
            {
                return armature;
            }
            
            Armature.Bone[] bones = new Armature.Bone[boneCount];
            int[] parentIDs = new int[boneCount];
            for (int boneID = 0; boneID < boneCount; boneID++)
            {
                Armature.Bone bone = new Armature.Bone();

                bone.armature = armature;
                bone.id = boneID;


                int slength = file[rr]; rr++;
                if (slength > 127) rr++;

                bone.name = Encoding.UTF8.GetString(file, rr, slength);
                rr = rr + slength;

                
                parentIDs[boneID] = BitConverter.ToInt16(file, rr);
                rr = rr + 2;

                float absPosX = BitConverter.ToSingle(file, rr); rr = rr + 4;
                float absPosY = BitConverter.ToSingle(file, rr); rr = rr + 4;
                float absPosZ = BitConverter.ToSingle(file, rr); rr = rr + 4;

                bone.absPosition = new Vector3(absPosX, absPosY, absPosZ);
                bones[boneID] = bone;
            }
            
            for (int boneID = 0; boneID < boneCount; boneID++)
            {
                Armature.Bone bone = bones[boneID];
                int parentID = parentIDs[boneID];
                if (parentID >= 0)
                {
                    Armature.Bone parent = bones[parentID];
                    bone.parent = parent;
                    parent.children.Add(bone);
                }
            }
            armature.Bones = bones;
            armature.rowcount = rr;
            
            return armature;
        }

        public MeshDesc LoadMeshBinary(byte[] file, bool hasArmature, int rowcounter_, bool hasTangents = false, bool hasVariableWeights = false)
        {
            
            int rr = rowcounter_;
            MeshDesc mesh = new MeshDesc();
            
            int slength = file[rr]; rr++; if (slength > 127) rr++;
            mesh.name = Encoding.UTF8.GetString(file, rr, slength);
            rr = rr + slength;
            
            uint uvLayerCount = BitConverter.ToUInt32(file, rr); rr = rr + 4;
            mesh.uvLayerCount = uvLayerCount;
            uint textureCount = BitConverter.ToUInt32(file, rr); rr = rr + 4;
            mesh.textures = new MeshDesc.Texture[textureCount];
            
            for (int textureID = 0; textureID < textureCount; textureID++)
            {
                MeshDesc.Texture texture = new MeshDesc.Texture();
                slength = file[rr]; rr++; if (slength > 127) rr++;
                texture.filename = Encoding.UTF8.GetString(file, rr, slength);
                rr = rr + slength;

                texture.uvLayerIndex = BitConverter.ToUInt32(file, rr); rr = rr + 4;
                mesh.textures[textureID] = texture;

            }
            
            Dictionary<short, short> boneIndexDict = new Dictionary<short, short>();
            List<short> boneIndexMap = new List<short>();
            int vertexCount = BitConverter.ToInt32(file, rr); rr = rr + 4;
            mesh.vertices = new MeshDesc.Vertex[vertexCount];
            
            for (int vertexID = 0; vertexID < vertexCount; vertexID++)
            {
                MeshDesc.Vertex vertex = new MeshDesc.Vertex();
                float positionX = BitConverter.ToSingle(file, rr); rr = rr + 4;
                float positionY = BitConverter.ToSingle(file, rr); rr = rr + 4;
                float positionZ = BitConverter.ToSingle(file, rr); rr = rr + 4;
                vertex.position = new Vector3(positionX, positionY, positionZ);
                float normalX = BitConverter.ToSingle(file, rr); rr = rr + 4;
                float normalY = BitConverter.ToSingle(file, rr); rr = rr + 4;
                float normalZ = BitConverter.ToSingle(file, rr); rr = rr + 4;
                vertex.normal = (new Vector3(normalX, normalY, normalZ));
                float colorR = file[rr] / 255.0f; rr++;
                float colorG = file[rr] / 255.0f; rr++;
                float colorB = file[rr] / 255.0f; rr++;
                float colorA = file[rr] / 255.0f; rr++;
                vertex.color = new Vector4(colorR, colorG, colorB, 1.0f);
                vertex.texCoords = new Vector2[uvLayerCount];
                for (int uvLayerID = 0; uvLayerID < uvLayerCount; uvLayerID++)
                {
                    float texCoordX = BitConverter.ToSingle(file, rr); rr = rr + 4;
                    float texCoordY = BitConverter.ToSingle(file, rr); rr = rr + 4;
                    vertex.texCoords[uvLayerID] = new Vector2(texCoordX, texCoordY);
                }
                vertex.tangents = new Vector4[uvLayerCount];

                if (hasTangents)
                {
                    for (int uvLayerID = 0; uvLayerID < uvLayerCount; uvLayerID++)
                    {
                        float tangentX = BitConverter.ToSingle(file, rr); rr = rr + 4;
                        float tangentY = BitConverter.ToSingle(file, rr); rr = rr + 4;
                        float tangentZ = BitConverter.ToSingle(file, rr); rr = rr + 4;
                        float tangentW = BitConverter.ToSingle(file, rr); rr = rr + 4;
                        vertex.tangents[uvLayerID] = new Vector4(tangentX, tangentY, tangentZ, tangentW);
                    }
                }

                if (hasArmature)
                {
                    int weightCount = 4;

                    if (hasVariableWeights)
                    {
                        weightCount = BitConverter.ToInt16(file, rr);
                        rr = rr + 2;
                    }
                

                     vertex.boneIndicesGlobal = new short[weightCount];
                    vertex.boneIndicesLocal = new short[weightCount];
                    for (int i = 0; i < weightCount; i++)
                    {
                        vertex.boneIndicesGlobal[i] = BitConverter.ToInt16(file, rr); rr = rr + 2;
                    }
                    vertex.boneWeights = new float[weightCount];
                    float weightSum = 0;
                    for (int i = 0; i < weightCount; i++)
                    {
                        float weight = BitConverter.ToSingle(file, rr); rr = rr + 4;
                        vertex.boneWeights[i] = weight;
                        weightSum += weight;
                    }
                    if (weightSum == 0)
                    {
                        vertex.boneWeights[0] = 1f;
                    }
                    else
                    {
                        if (weightSum != 1.0f)
                        {
                            for (int i = 0; i < weightCount; i++)
                            {
                                vertex.boneWeights[i] /= weightSum;
                            }
                        }
                    }
                    short indexDefault = -1;
                    for (int i = 0; i < weightCount; i++)
                    {
                        if (vertex.boneWeights[i] > 0)
                        {
                            indexDefault = vertex.boneIndicesGlobal[i];
                            break;
                        }
                    }
                    for (int i = 0; i < weightCount; i++)
                    {
                        short indexGlobal = vertex.boneIndicesGlobal[i];
                        if (vertex.boneWeights[i] == 0)
                        {
                            indexGlobal = indexDefault;
                        }
                        short indexLocal;
                        if (!boneIndexDict.TryGetValue(indexGlobal, out indexLocal))
                        {
                            indexLocal = (short)boneIndexMap.Count;
                            boneIndexDict[indexGlobal] = indexLocal;
                            boneIndexMap.Add(indexGlobal);
                        }
                        vertex.boneIndicesLocal[i] = indexLocal;
                    }
                }
                mesh.vertices[vertexID] = vertex;
            }
            

            mesh.boneIndexMap = boneIndexMap.ToArray();
            uint faceCount = BitConverter.ToUInt32(file, rr); rr = rr + 4;

            mesh.indices = new ushort[faceCount * 3];
            int index = 0;
            for (int faceID = 0; faceID < faceCount; faceID++)
            {
                mesh.indices[index + 0] = (ushort)BitConverter.ToUInt32(file, rr); rr = rr + 4;
                mesh.indices[index + 1] = (ushort)BitConverter.ToUInt32(file, rr); rr = rr + 4;
                mesh.indices[index + 2] = (ushort)BitConverter.ToUInt32(file, rr); rr = rr + 4;
                index += 3;
            }
            
            mesh.rowcount = rr;

            return mesh;
        }

        public UIDynamicTextField CreateLabel(string label, bool rhs, int height = 40)
        {
            JSONStorableString jsonLabel = new JSONStorableString(label, label);
            UIDynamicTextField labelField = CreateTextField(jsonLabel, rhs);
            SetTextFieldHeight(labelField, height);

            return labelField;
        }

        public static void SetTextFieldHeight(UIDynamicTextField textField, int height)
        {
            LayoutElement component = textField.GetComponent<LayoutElement>();
            if (component != null)
            {
                component.minHeight = height;
                component.preferredHeight = height;
            }
            textField.height = height;
        }

        protected UIDynamicSlider createFloatSlider(string name, string displayName, float initialVal, Action<float> settable, bool right, bool restore)
        {
            return createFloatSlider(name,  displayName,  initialVal,0f,1f,settable,  right,  restore);
        }

        protected UIDynamicSlider createFloatSlider(string name, string displayName, float initialVal, float min, float max,Action<float> settable, bool right, bool restore)
        {
            JSONStorableFloat settableVal;
            if (GetFloatJSONParam(name) == null)
            {
                settableVal = new JSONStorableFloat(name, initialVal, min, max, false, true);
                RegisterFloat(settableVal);                
            }
            else
            {                
                settableVal = GetFloatJSONParam(name);             
                //settableVal.val = initialVal;
            }

            if (restore && pluginJson != null)
            {
                settableVal.RestoreFromJSON(pluginJson);
                if (settableVal != null) { settable(settableVal.val); }          
            }
            settableVal.setJSONCallbackFunction += delegate (JSONStorableFloat js) { settable(js.val); };

            UIDynamicSlider solverPositionWeightslider = CreateSlider(settableVal, right);
            solverPositionWeightslider.labelText.text = displayName;
            return solverPositionWeightslider;
        }

        protected UIDynamic createToggle(string name, string displayName, bool initialVal, Action<bool> settable, bool right, bool restore)
        {
            JSONStorableBool solverFixTransforms = new JSONStorableBool(name, initialVal);
            RegisterBool(solverFixTransforms);
            if (restore && pluginJson != null)
            {
                solverFixTransforms.RestoreFromJSON(pluginJson);
         
                if (solverFixTransforms!=null) settable(solverFixTransforms.val);
            }             

            solverFixTransforms.setJSONCallbackFunction += (delegate (JSONStorableBool js) { settable(js.val); });
            UIDynamicToggle tog = CreateToggle(solverFixTransforms, right);
            tog.labelText.text = displayName;
            return tog;
        }

        protected JSONStorableBool createToggleStorable(string name, bool initialVal, Action<bool> settable, bool right, bool restore)
        {
            JSONStorableBool solverFixTransforms = new JSONStorableBool(name, initialVal);
            RegisterBool(solverFixTransforms);
     
            if (restore && pluginJson != null)
            {
                solverFixTransforms.RestoreFromJSON(pluginJson);
                if (solverFixTransforms != null) settable(solverFixTransforms.val);                
            }
            solverFixTransforms.setJSONCallbackFunction += (delegate (JSONStorableBool js) { settable(js.val); });
            return solverFixTransforms;
        }
    }



}
