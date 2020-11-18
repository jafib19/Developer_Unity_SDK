﻿
/*===============================================================================
Copyright (C) 2020 ARWAY Ltd. All Rights Reserved.

This file is part of ARwayKit AR SDK

The ARwayKit SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of ARWAY Ltd.

===============================================================================*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Siccity.GLTFUtility;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Arway
{
    public class ContentManager : MonoBehaviour
    {
        public ArwaySDK m_Sdk = null;

        /// <summary>
        /// The map identifier.
        /// </summary>
        public string map_id;

        public GameObject m_ARSpace;

        public GameObject wayPoint;
        public GameObject destination;
        public GameObject imagesPOI;
        public GameObject textPOI;

        private NavController navController;
        private Node[] Map = new Node[0];
        public List<GameObject> nodeList;

        private string filePath = "";

        public TMP_Dropdown dropdown;

        // -------------------------  
        [Header("Localization")]
        public string mapped_Cloud_Id;

        [SerializeField]
        private GameObject poseSetterGO;

        [SerializeField]
        private GameObject RunButton;

        [SerializeField]
        private GameObject loaderPanel;

        [SerializeField]
        private Text loaderText;

        public bool vibrateOnLocalize;

        public TMP_Text loc_attempts_txt;

        private int counts = 0;
        private int requestCount = 0;

        private Texture2D m_Texture;
   


        void Start()
        {

            dropdown = dropdown.GetComponent<TMP_Dropdown>();
            dropdown.options.Clear();
            dropdown.options.Add(new TMP_Dropdown.OptionData("Select Destination"));
          
            filePath = $"{Application.persistentDataPath}/Files/test.glb";

            navController = this.GetComponent<NavController>();


            if (map_id.Length>0)
            {

                m_Sdk = ArwaySDK.Instance;

                if (m_Sdk.developerToken != null && m_Sdk.developerToken.Length > 0)
                {
                    StartCoroutine(GetMapData(map_id));
                }
                else
                {
                    Debug.Log("***********\tDeveloper Token not valid!\t***********");
                }
            }
            else
            {
                Debug.Log(" Map Id null..");
                NotificationManager.Instance.GenerateWarning("Error: " + "Map Id is null!!");
            }

            if (m_ARSpace == null)
            {
                m_ARSpace = new GameObject("ARSpace");
                if (m_ARSpace == null)
                {
                    Debug.Log("No AR Space found");
                }
            }

        }


        /// <summary>
        /// Gets the map data.
        /// </summary>
        /// <returns>The map data.</returns>
        /// <param name="map_id">Map identifier.</param>
        IEnumerator GetMapData(string map_id)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(m_Sdk.ContentServer + EndPoint.MAP_DATA + "index.php?map_id=" + map_id))
            {
                www.SetRequestHeader("dev-token", m_Sdk.developerToken);

                yield return www.SendWebRequest();

                if (www.isNetworkError || www.isHttpError)
                {
                    Debug.Log("****************\t" + www.error + "\t****************");
                    NotificationManager.Instance.GenerateWarning("Error: " + www.error);
                }
                else
                {
                    try
                    {
                        string jsonResult = System.Text.Encoding.UTF8.GetString(www.downloadHandler.data);
                        MapAssetData mapAssetData = JsonUtility.FromJson<MapAssetData>(jsonResult);

                        if (mapAssetData != null)
                        {
                            if (mapAssetData.Waypoints != null)
                            {   //   Debug.Log("Total waypoints: " + mapAssetData.Waypoints.Length);
                                for (int i = 0; i < mapAssetData.Waypoints.Length; i++)
                                {
                                    Vector3 pos = new Vector3((float)mapAssetData.Waypoints[i].Position.posX * -1f
                                        , (float)mapAssetData.Waypoints[i].Position.posY - 1.3f
                                        , (float)mapAssetData.Waypoints[i].Position.posZ);

                                    Vector3 rot = new Vector3((float)mapAssetData.Waypoints[i].Rotation.rotX
                                        , (float)mapAssetData.Waypoints[i].Rotation.rotY
                                        , (float)mapAssetData.Waypoints[i].Rotation.rotZ);


                                    StartCoroutine(CreatePrefab(wayPoint, mapAssetData.Waypoints[i].name, pos, rot));
                                }
                            }

                            if (mapAssetData.Destinations != null)
                            {
                                Debug.Log("Total Destinations: " + mapAssetData.Destinations.Length);
                                for (int i = 0; i < mapAssetData.Destinations.Length; i++)
                                {
                                    Vector3 pos = new Vector3((float)mapAssetData.Destinations[i].Position.posX * -1f
                                        , (float)mapAssetData.Destinations[i].Position.posY - 1.2f
                                        , (float)mapAssetData.Destinations[i].Position.posZ);

                                    Vector3 rot = new Vector3((float)mapAssetData.Destinations[i].Rotation.rotX
                                        , (float)mapAssetData.Destinations[i].Rotation.rotY
                                        , (float)mapAssetData.Destinations[i].Rotation.rotZ);

                                    StartCoroutine(CreatePrefab(destination, mapAssetData.Destinations[i].name, pos, rot));

                                    dropdown.options.Add(new TMP_Dropdown.OptionData(mapAssetData.Destinations[i].name));

                                }


                            }

                            if (mapAssetData.GlbModels != null)
                            {
                                for (int i = 0; i < mapAssetData.GlbModels.Length; i++)
                                {
                                    Vector3 pos = new Vector3((float)mapAssetData.GlbModels[i].Position.posX * -1f
                                       , (float)mapAssetData.GlbModels[i].Position.posY
                                       , (float)mapAssetData.GlbModels[i].Position.posZ);

                                    Vector3 rot = new Vector3((float)mapAssetData.GlbModels[i].Rotation.rotX
                                        , (float)mapAssetData.GlbModels[i].Rotation.rotY
                                        , (float)mapAssetData.GlbModels[i].Rotation.rotZ);



                                    CreateGlbPrefab(mapAssetData.GlbModels[i].name, pos, rot, mapAssetData.GlbModels[i].link);
                                }
                            }

                            if (mapAssetData.FloorImages != null)
                            {
                                for (int i = 0; i < mapAssetData.FloorImages.Length; i++)
                                {
                                    Vector3 pos = new Vector3((float)mapAssetData.FloorImages[i].Position.posX * -1f
                                          , (float)mapAssetData.FloorImages[i].Position.posY
                                          , (float)mapAssetData.FloorImages[i].Position.posZ);

                                    Vector3 rot = new Vector3((float)mapAssetData.FloorImages[i].Rotation.rotX
                                        , (float)mapAssetData.FloorImages[i].Rotation.rotY
                                        , (float)mapAssetData.FloorImages[i].Rotation.rotZ);

                                    string imageUrl = mapAssetData.FloorImages[i].link;

                                    if (!string.IsNullOrEmpty(imageUrl))
                                        StartCoroutine(loadPoiImage(imageUrl, pos, rot, mapAssetData.FloorImages[i].name));
                                    else
                                        Debug.Log("Image URL is empty!!");
                                }
                            }

                            for (int i = 0; i < mapAssetData.FloorPlans.Length; i++)
                            {

                            }

                            if (mapAssetData.Texts != null)
                            {
                                for (int i = 0; i < mapAssetData.Texts.Length; i++)
                                {
                                    Vector3 pos = new Vector3((float)mapAssetData.Texts[i].Position.posX * -1f
                                          , (float)mapAssetData.Texts[i].Position.posY
                                          , (float)mapAssetData.Texts[i].Position.posZ);

                                    Vector3 rot = new Vector3((float)mapAssetData.Texts[i].Rotation.rotX
                                        , (float)mapAssetData.Texts[i].Rotation.rotY
                                        , (float)mapAssetData.Texts[i].Rotation.rotZ);

                                    string text = mapAssetData.Texts[i].name;

                                    if (text != null)
                                        loadPoiText(text, pos, rot, text);
                                    else
                                        Debug.Log("Text is empty!!");
                                }
                            }
                        }


                        RunButton.SetActive(true);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e, this);
                    }
                }
            }
        }



        public unsafe void RequestLocalization()
        {
            XRCameraImage image;
            XRCameraIntrinsics intr;
            ARCameraManager cameraManager = m_Sdk.cameraManager;
            var cameraSubsystem = cameraManager.subsystem;

            if (cameraSubsystem != null && cameraSubsystem.TryGetLatestImage(out image) && cameraSubsystem.TryGetIntrinsics(out intr))
            {
                loaderText.text = "Localizing...";
                loaderPanel.SetActive(true);

                LocalizationRequest lr = new LocalizationRequest();
                lr.cloud_Id = int.Parse(mapped_Cloud_Id);
                lr.width = image.width;
                lr.height = image.height;
                lr.channel = 3;
                lr.Camera_fx = intr.focalLength.x;
                lr.Camera_fy = intr.focalLength.y;
                lr.Camera_px = intr.principalPoint.x;
                lr.Camera_py = intr.principalPoint.y;


                var format = TextureFormat.RGB24;

                if (m_Texture == null || m_Texture.width != image.width || m_Texture.height != image.height)
                {
                    m_Texture = new Texture2D(image.width, image.height, format, false);
                }

                // Convert the image to format, flipping the image across the Y axis.
                // We can also get a sub rectangle, but we'll get the full image here.
                var conversionParams = new XRCameraImageConversionParams(image, format, CameraImageTransformation.MirrorX);

                // Texture2D allows us write directly to the raw texture data
                // This allows us to do the conversion in-place without making any copies.
                var rawTextureData = m_Texture.GetRawTextureData<byte>();
                try
                {
                    image.Convert(conversionParams, new IntPtr(rawTextureData.GetUnsafePtr()), rawTextureData.Length);
                }
                finally
                {
                    // We must dispose of the XRCameraImage after we're finished
                    // with it to avoid leaking native resources.
                    image.Dispose();
                }

                // Apply the updated texture data to our texture
                m_Texture.Apply();

                byte[] _bytesjpg = m_Texture.EncodeToJPG();
                lr.image = Convert.ToBase64String(_bytesjpg);
                lr.timestamp = image.timestamp;

                //show requeset counts..

                loc_attempts_txt.GetComponent<TMP_Text>().enabled = true;

                string output = JsonUtility.ToJson(lr);
                StartCoroutine(sendCameraImages(output));

            }

        }

        /// <summary>
        /// Sends the camera images.
        /// </summary>
        /// <returns>The camera images.</returns>
        /// <param name="rawdata">Rawdata.</param>
        IEnumerator sendCameraImages(string rawdata)
        {
            using (UnityWebRequest www = UnityWebRequest.Put(m_Sdk.localizationServer + m_Sdk.developerToken + "/" + EndPoint.REQ_POSE, rawdata))
            {
                www.method = UnityWebRequest.kHttpVerbPOST;
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Accept", "application/json");
                yield return www.SendWebRequest();
                Debug.Log("***************");

                if (www.error != null)
                {
                    loaderPanel.SetActive(false);
                    Debug.Log("Error: " + www.error);
                }
                else
                {
                    loaderPanel.SetActive(false);

                    Debug.Log("All OK");
                    Debug.Log("Status Code: " + www.downloadHandler.text);

                    requestCount++;

                    LocalizationResponse localization = JsonUtility.FromJson<LocalizationResponse>(www.downloadHandler.text);
                    Debug.Log(localization);

                    if (localization.poseAvailable == true)
                    {
                        counts += 1;
                        poseSetterGO.GetComponent<PoseSetter>().poseHandler(localization);

                        if (vibrateOnLocalize)
                            Handheld.Vibrate();
                    }

                    loc_attempts_txt.text = "Localization attempts:  " + counts + " / " + requestCount;
                }
            }
        }


        // update drop down for destinations... 

        public void HangleDestinationSelection(int val)
        {
            if (val > 0)
            {
                Debug.Log("selection >> " + val + " " + dropdown.options[val].text);
            }
        }



        IEnumerator CreatePrefab(GameObject gameObject, string name, Vector3 pos, Vector3 rot)
        {
            var temp = Instantiate(gameObject);
            temp.name = name;
            temp.transform.parent = m_ARSpace.transform;
            temp.transform.localPosition = pos;
            temp.transform.localEulerAngles = rot;

            nodeList.Add(temp);

            Map = new Node[nodeList.Count];
            int i = 0;
            foreach (var node in nodeList)
            {
                Map[i] = node.GetComponent<Node>();
                i = i + 1;
            }
            navController.allnodes = Map;
            yield return name;
        }

        public IEnumerator loadPoiImage(String url, Vector3 pos, Vector3 rot, String name)
        {
            Debug.Log("URL>>>" + url);

            var imgpoi = Instantiate(imagesPOI);
            imgpoi.transform.parent = m_ARSpace.transform;
            imgpoi.transform.localPosition = pos;
            imgpoi.transform.localEulerAngles = rot;
            imgpoi.name = name;


            UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
            }
            else
            {
                Texture myTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                imgpoi.GetComponentInChildren<RawImage>().texture = myTexture;
            }
        }

        public void loadPoiText(String textcon, Vector3 pos, Vector3 rot, String name)
        {
            var temp = Instantiate(textPOI);
            temp.transform.parent = m_ARSpace.transform;
            temp.transform.localPosition = pos;
            temp.transform.localEulerAngles = rot;
            temp.name = name;
            temp.GetComponentInChildren<TMP_Text>().text = textcon;

        }


        // import GLB model
        private void CreateGlbPrefab(string glb_name, Vector3 pos, Vector3 rot, string url)
        {
            string path = GetFilePath(url);
            //Debug.Log("GLB_URL >> " + url+"\n path>> "+path);
            if (File.Exists(path))
            {
                Debug.Log("Found file locally, loading...");
                LoadModel(path, glb_name, pos, rot);
                return;
            }

            StartCoroutine(GetFileRequest(url, (UnityWebRequest req) =>
            {
                if (req.isNetworkError || req.isHttpError)
                {
                    // Log any errors that may happen
                    Debug.Log($"{req.error} : {req.downloadHandler.text}");
                }
                else
                {
                    // Save the model into a new wrapper
                    LoadModel(path, glb_name, pos, rot);
                }
            }));
        }

        string GetFilePath(string url)
        {
            string[] pieces = url.Split('/');
            string filename = pieces[pieces.Length - 1];

            return $"{filePath}{filename}";
        }

        void LoadModel(string path, string glb_name, Vector3 pos, Vector3 rot)
        {
            GameObject model = Importer.LoadFromFile(path);

            model.name = glb_name;
            model.transform.parent = m_ARSpace.transform;
            model.transform.localPosition = pos;
            // model.transform.localEulerAngles = rot;

        }

        IEnumerator GetFileRequest(string url, Action<UnityWebRequest> callback)
        {
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.downloadHandler = new DownloadHandlerFile(GetFilePath(url));
                yield return req.SendWebRequest();
                callback(req);
            }
        }

        /// <summary>
        /// Backs the button click.
        /// </summary>
        /// <param name="sceneName">Scene name.</param>
        public void BackButtonClick(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

    }
}