/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using UnityEngine.AI;

namespace Meta.XR.MRUtilityKit
{
    [RequireComponent(typeof(EventSystem))]
    public class SceneDebugger : MonoBehaviour
    {
        public GameObject debugProjectile;
        public Material visualHelperMaterial;

        public Text logs;
        public Selectable selectionEntryPoint;
        public Dropdown surfaceTypeDropdown;
        public Dropdown positioningMethodDropdown;

        [Tooltip("The delay between the fired selection events")]
        public float inputSelectionDelay = 0.2f;

        [SerializeField, Tooltip("Visualize anchors")]
        public bool ShowDebugAnchors = false;

        public bool shootBall = true;

        float inputDelayCounter = 0;
        Ray shootingRay;

        EventSystem eventSystem;
        OVRCameraRig _cameraRig;

        // For visual debugging of the room
        GameObject debugCube;
        GameObject debugSphere;
        GameObject debugNormal;
        List<GameObject> debugAnchors = new List<GameObject>();
        GameObject debugAnchor;
        Mesh _debugCheckerMesh = null;
        bool previousShowDebugAnchors = false;
        MRUKAnchor previousShownDebugAnchor = null;
        EffectMesh globalMeshEffectMesh = null;
        MRUKAnchor globalMeshAnchor = null;
        GameObject navMeshViz = null;
        NavMeshTriangulation navMeshTriangulation;


        Action debugAction = null;

        private void Start()
        {
            MRUK.Instance?.RegisterSceneLoadedCallback(OnSceneLoaded);
            eventSystem = gameObject.GetComponent<EventSystem>();
#if UNITY_EDITOR
            OVRTelemetry.Start(TelemetryConstants.MarkerId.LoadSceneDebugger).Send();
#endif
            globalMeshEffectMesh = GetGlobalMeshEffectMesh();
            if (!_cameraRig)
            {
                _cameraRig = FindObjectOfType<OVRCameraRig>();
            }
        }
        private void OnDisable()
        {
            debugAction = null;
        }

        void Update()
        {
            if (Application.isEditor)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    shootingRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                    ShootProjectile();
                }
            }

            if (OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger))
            {
                shootingRay = GetRightControllerRay();
                ShootProjectile();
            }
            if (OVRInput.GetDown(OVRInput.RawButton.Start))
            {
                eventSystem.SetSelectedGameObject(selectionEntryPoint.gameObject); // resets the UI focus to the debug canvas
            }

            debugAction?.Invoke();

            // Toggle the anchors debug visuals
            if (ShowDebugAnchors != previousShowDebugAnchors)
            {
                if (ShowDebugAnchors)
                {
                    foreach (var room in MRUK.Instance.GetRooms())
                    {
                        foreach (var anchor in room.GetRoomAnchors())
                        {
                            GameObject anchorVisual = GenerateDebugAnchor(anchor);
                            debugAnchors.Add(anchorVisual);
                        }
                    }
                }
                else
                {
                    foreach (GameObject anchorVisual in debugAnchors)
                    {
                        Destroy(anchorVisual.gameObject);
                    }
                }
                previousShowDebugAnchors = ShowDebugAnchors;
            }
            // Actively listen to the EventSystem to be independent from the Input System
            UINavigationHelper();
            if (OVRInput.GetDown(OVRInput.RawButton.X) || OVRInput.GetDown(OVRInput.RawButton.A))
            {
                ClickUIElement();
            }
        }

        void OnSceneLoaded()
        {
            CreateDebugPrimitives();
        }

        Ray GetRightControllerRay()
        {
            Vector3 rayOrigin = _cameraRig.rightControllerAnchor.position;
            Vector3 rayDirection = _cameraRig.rightControllerAnchor.forward;
            return new Ray(rayOrigin, rayDirection);
        }

        /// <summary>
        /// Shows information about the rooms loaded.
        /// </summary>
        public void ShowRoomDetailsDebugger(bool isOn)
        {
            try
            {
                if (isOn)
                {
                    debugAction = () =>
                    {
                        var currentRoomName = MRUK.Instance?.GetCurrentRoom().name ?? "N/A";
                        var numRooms = MRUK.Instance?.GetRooms().Count ?? 0;
                        SetLogsText("\n[{0}]\nNumber of rooms: {1}\nCurrent room: {2}",
                            nameof(ShowRoomDetailsDebugger),
                            numRooms,
                            currentRoomName
                        );
                    };
                }
                else
                {
                    debugAction = null;
                }
            }
            catch (Exception e)
            {
                SetLogsText("\n[{0}]\n {1}\n{2}",
                    nameof(ShowRoomDetailsDebugger),
                    e.Message,
                    e.StackTrace);
            }
        }

        /// <summary>
        /// Highlights the room's key wall.
        /// </summary>
        public void GetKeyWallDebugger(bool isOn)
        {
            try
            {
                if (isOn)
                {
                    Vector2 wallScale = Vector2.zero;
                    MRUKAnchor keyWall = MRUK.Instance?.GetCurrentRoom()?.GetKeyWall(out wallScale);
                    Vector3 anchorCenter = keyWall.GetAnchorCenter();
                    if (debugCube != null)
                    {
                        debugCube.transform.localScale = new Vector3(wallScale.x, wallScale.y, 0.05f);
                        debugCube.transform.localPosition = anchorCenter;
                        debugCube.transform.localRotation = keyWall.transform.localRotation;
                    }
                    SetLogsText("\n[{0}]\nSize: {1}",
                        nameof(GetKeyWallDebugger),
                        wallScale
                    );
                }
                if (debugCube != null)
                {
                    debugCube.SetActive(isOn);
                }
            }
            catch (Exception e)
            {
                SetLogsText("\n[{0}]\n {1}\n{2}",
                    nameof(GetKeyWallDebugger),
                    e.Message,
                    e.StackTrace);
            }
        }

        /// <summary>
        ///  Highlights the anchor with the largest available surface area.
        /// </summary>
        public void GetLargestSurfaceDebugger(bool isOn)
        {
            try
            {
                if (isOn)
                {
                    string surfaceType = OVRSceneManager.Classification.Table; // using table as the default value
                    if (surfaceTypeDropdown)
                    {
                        surfaceType = surfaceTypeDropdown.options[surfaceTypeDropdown.value].text.ToUpper();
                    }
                    MRUKAnchor largestSurface = MRUK.Instance?.GetCurrentRoom()?.FindLargestSurface(surfaceType);
                    if (largestSurface != null)
                    {
                        if (debugCube != null)
                        {
                            debugCube.transform.localScale = new Vector3(largestSurface.GetAnchorSize().x, largestSurface.GetAnchorSize().y, 0.01f);
                            debugCube.transform.localPosition = largestSurface.transform.position;
                            debugCube.transform.localRotation = largestSurface.transform.rotation;
                        }
                        SetLogsText("\n[{0}]\nAnchor: {1}\nType: {2}",
                            nameof(GetLargestSurfaceDebugger),
                            largestSurface.name,
                            largestSurface.AnchorLabels[0]
                        );
                    }
                    else
                    {
                        SetLogsText("\n[{0}]\n No surface of type {1} found.",
                            nameof(GetLargestSurfaceDebugger),
                            surfaceType
                        );
                    }
                }
                else
                {
                    debugAction = null;
                }
                if (debugCube != null)
                {
                    debugCube.SetActive(isOn);
                }
            }
            catch (Exception e)
            {
                SetLogsText("\n[{0}]\n {1}\n{2}",
                    nameof(GetLargestSurfaceDebugger),
                    e.Message,
                    e.StackTrace
                );
            }
        }

        /// <summary>
        /// Highlights the best-suggested seat, for something like remote caller placement.
        /// </summary>
        public void GetClosestSeatPoseDebugger(bool isOn)
        {
            try
            {
                if (isOn)
                {
                    debugAction = () =>
                    {
                        MRUKAnchor seat = null;
                        Pose seatPose = new Pose();
                        Ray ray = GetRightControllerRay();
                        MRUK.Instance?.GetCurrentRoom()?.TryGetClosestSeatPose(ray, out seatPose, out seat);
                        if (seat)
                        {
                            Vector3 anchorCenter = seat.GetAnchorCenter();
                            if (debugCube != null)
                            {
                                debugCube.transform.localRotation = seat.transform.localRotation;
                                debugCube.transform.position = seatPose.position;
                                debugCube.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                            }
                            SetLogsText("\n[{0}]\nSeat: {1}\nPosition: {2}\nDistance: {3}",
                                nameof(GetClosestSeatPoseDebugger),
                                seat.name,
                                seatPose.position,
                                Vector3.Distance(seatPose.position, ray.origin).ToString("0.##")
                            );
                        }
                        else
                        {
                            SetLogsText("\n[{0}]\n No seat found in the scene.",
                                nameof(GetClosestSeatPoseDebugger)
                            );
                        }
                    };
                }
                else
                {
                    debugAction = null;
                }
                if (debugCube != null)
                {
                    debugCube.SetActive(isOn);
                }
            }
            catch (Exception e)
            {
                SetLogsText("\n[{0}]\n {1}\n{2}",
                    nameof(GetClosestSeatPoseDebugger),
                    e.Message,
                    e.StackTrace);
            }
        }

        /// <summary>
        /// Highlights the closest position on a SceneAPI surface.
        /// </summary>
        public void GetClosestSurfacePositionDebugger(bool isOn)
        {
            try
            {
                if (isOn)
                {
                    debugAction = () =>
                    {
                        Vector3 origin = GetRightControllerRay().origin;
                        Vector3 surfacePosition = Vector3.zero;
                        MRUKAnchor closestAnchor = null;
                        MRUK.Instance?.GetCurrentRoom()?.TryGetClosestSurfacePosition(origin, out surfacePosition, out closestAnchor);
                        if (debugSphere != null)
                        {
                            debugSphere.transform.position = surfacePosition;
                        }
                        SetLogsText("\n[{0}]\nAnchor: {1}\nSurface Position: {2}\nDistance: {3}",
                            nameof(GetClosestSurfacePositionDebugger),
                            closestAnchor.name,
                            surfacePosition,
                            Vector3.Distance(origin, surfacePosition).ToString("0.##")
                        );
                    };
                }
                else
                {
                    debugAction = null;
                }
                if (debugSphere != null)
                {
                    debugSphere.SetActive(isOn);
                }
            }
            catch (Exception e)
            {
                SetLogsText("\n[{0}]\n {1}\n{2}",
                    nameof(GetClosestSurfacePositionDebugger),
                    e.Message,
                    e.StackTrace);
            }
        }

        /// <summary>
        /// Highlights the the best suggested transform to place a widget on a surface.
        /// </summary>
        public void GetBestPoseFromRaycastDebugger(bool isOn)
        {
            try
            {
                if (isOn)
                {
                    debugAction = () =>
                    {
                        Ray ray = GetRightControllerRay();
                        MRUKAnchor sceneAnchor = null;
                        MRUK.PositioningMethod positioningMethod = MRUK.PositioningMethod.DEFAULT;
                        if (positioningMethodDropdown)
                        {
                            positioningMethod = (MRUK.PositioningMethod)positioningMethodDropdown.value;
                        }
                        Pose? bestPose = MRUK.Instance?.GetCurrentRoom()?.GetBestPoseFromRaycast(ray, Mathf.Infinity, new LabelFilter(), out sceneAnchor, positioningMethod);
                        if (bestPose.HasValue && sceneAnchor && debugCube)
                        {
                            debugCube.transform.position = bestPose.Value.position;
                            debugCube.transform.rotation = bestPose.Value.rotation;
                            debugCube.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                            SetLogsText("\n[{0}]\nAnchor: {1}\nPose Position: {2}\nPose Rotation: {3}",
                                nameof(GetBestPoseFromRaycastDebugger),
                                sceneAnchor.name,
                                bestPose.Value.position,
                                bestPose.Value.rotation
                            );
                        }
                    };
                }
                else
                {
                    debugAction = null;
                }
                if (debugCube != null)
                {
                    debugCube.SetActive(isOn);
                }
            }
            catch (Exception e)
            {
                SetLogsText("\n[{0}]\n {1}\n{2}",
                    nameof(GetBestPoseFromRaycastDebugger),
                    e.Message,
                    e.StackTrace);
            }
        }

        /// <summary>
        /// Casts a ray cast forward from the right controller position and draws the normal of the first Scene API object hit.
        /// </summary>
        public void RayCastDebugger(bool isOn)
        {
            try
            {
                if (isOn)
                {
                    debugAction = () =>
                    {
                        Ray ray = GetRightControllerRay();
                        RaycastHit hit = new RaycastHit();
                        MRUKAnchor anchorHit = null;
                        MRUK.Instance?.GetCurrentRoom()?.Raycast(ray, Mathf.Infinity, out hit, out anchorHit);
                        ShowHitNormal(hit);
                        if (anchorHit != null)
                        {
                            SetLogsText("\n[{0}]\nAnchor: {1}\nHit point: {2}\nHit normal: {3}\n",
                                nameof(RayCastDebugger),
                                anchorHit.name,
                                hit.point,
                                hit.normal
                            );
                        }
                    };
                }
                else
                {
                    debugAction = null;
                }
                if (debugNormal != null)
                {
                    debugNormal.SetActive(isOn);
                }
            }
            catch (Exception e)
            {
                SetLogsText("\n[{0}]\n {1}\n{2}",
                    nameof(RayCastDebugger),
                    e.Message,
                    e.StackTrace
                );
            }
        }

        /// <summary>
        /// Moves the debug sphere to the controller position and colors it in green if its position is in the room,
        /// red otherwise.
        /// </summary>
        public void IsPositionInRoomDebugger(bool isOn)
        {
            try
            {
                if (isOn)
                {
                    debugAction = () =>
                    {
                        Ray ray = GetRightControllerRay();
                        if (debugSphere != null)
                        {
                            bool? isInRoom = MRUK.Instance?.GetCurrentRoom()?.IsPositionInRoom(debugSphere.transform.position);
                            debugSphere.transform.position = ray.GetPoint(0.2f); // add some offset
                            debugSphere.GetComponent<Renderer>().material.color = (isInRoom.HasValue && isInRoom.Value) ? Color.green : Color.red;
                            SetLogsText("\n[{0}]\nPosition: {1}\nIs inside the Room: {2}\n",
                                nameof(IsPositionInRoomDebugger),
                                debugSphere.transform.position,
                                isInRoom
                            );
                        }
                    };
                }
                if (debugSphere != null)
                {
                    debugSphere.SetActive(isOn);
                }
            }
            catch (Exception e)
            {
                SetLogsText("\n[{0}]\n {1}\n{2}",
                    nameof(IsPositionInRoomDebugger),
                    e.Message,
                    e.StackTrace);
            }
        }

        /// <summary>
        /// Shows the debug anchor visualization mode for the anchor being pointed at.
        /// </summary>
        public void ShowDebugAnchorsDebugger(bool isOn)
        {
            try
            {
                if (isOn)
                {
                    debugAction = () =>
                    {
                        Ray ray = GetRightControllerRay();
                        RaycastHit hit = new RaycastHit();
                        MRUKAnchor anchorHit = null;
                        MRUK.Instance?.GetCurrentRoom()?.Raycast(ray, Mathf.Infinity, out hit, out anchorHit);
                        if (previousShownDebugAnchor != anchorHit && anchorHit != null)
                        {
                            Destroy(debugAnchor);
                            debugAnchor = GenerateDebugAnchor(anchorHit);
                            previousShownDebugAnchor = anchorHit;
                        }
                        ShowHitNormal(hit);
                        SetLogsText("\n[{0}]\nHit point: {1}\nHit normal: {2}\n",
                            nameof(ShowDebugAnchorsDebugger),
                            hit.point,
                            hit.normal
                        );
                    };
                }
                else
                {
                    debugAction = null;
                    Destroy(debugAnchor);
                    debugAnchor = null;
                }
                if (debugNormal != null)
                {
                    debugNormal.SetActive(isOn);
                }
            }
            catch (Exception e)
            {
                SetLogsText("\n[{0}]\n {1}\n{2}",
                    nameof(ShowDebugAnchorsDebugger),
                    e.Message,
                    e.StackTrace);
            }
        }

        /// <summary>
        ///  Displays the global mesh anchor if one is found in the scene.
        /// </summary>
        public void DisplayGlobalMesh(bool isOn)
        {
            try
            {
                LabelFilter filter = LabelFilter.Included(new System.Collections.Generic.List<string>() { OVRSceneManager.Classification.GlobalMesh });
                if (MRUK.Instance && MRUK.Instance.GetCurrentRoom() && !globalMeshAnchor)
                {
                    globalMeshAnchor = MRUK.Instance.GetCurrentRoom().GetGlobalMeshAnchor();
                }
                if (!globalMeshAnchor)
                {
                    SetLogsText("\n[{0}]\nNo global mesh anchor found in the scene.\n",
                            nameof(DisplayGlobalMesh)
                        );
                    return;
                }
                if (isOn)
                {
                    if (!globalMeshEffectMesh)
                    {
                        globalMeshEffectMesh = new GameObject("_globalMeshViz", typeof(EffectMesh)).GetComponent<EffectMesh>();
                        globalMeshEffectMesh.Labels = MRUKAnchor.SceneLabels.GLOBAL_MESH;
                        if (visualHelperMaterial)
                            globalMeshEffectMesh.MeshMaterial = visualHelperMaterial;
                        globalMeshEffectMesh.CreateMesh();
                    }
                    else
                    {
                        globalMeshEffectMesh.ToggleEffectMeshVisibility(true, filter, visualHelperMaterial);
                    }
                }
                else
                {
                    if (!globalMeshEffectMesh)
                        return;
                    globalMeshEffectMesh.ToggleEffectMeshVisibility(false, filter, globalMeshEffectMesh.MeshMaterial);
                }
            }
            catch (Exception e)
            {
                SetLogsText("\n[{0}]\n {1}\n{2}",
                    nameof(DisplayGlobalMesh),
                    e.Message,
                    e.StackTrace
                );
            }
        }

        /// <summary>
        /// Toggles the global mesh anchor's collision.
        /// </summary>
        public void ToggleGlobalMeshCollisions(bool isOn)
        {
            try
            {
                LabelFilter filter = LabelFilter.Included(new System.Collections.Generic.List<string>() { OVRSceneManager.Classification.GlobalMesh });
                if (MRUK.Instance && MRUK.Instance.GetCurrentRoom() && !globalMeshAnchor)
                {
                    globalMeshAnchor = MRUK.Instance.GetCurrentRoom().GetGlobalMeshAnchor();
                }
                if (!globalMeshAnchor)
                {
                    SetLogsText("\n[{0}]\nNo global mesh anchor found in the scene.\n",
                            nameof(ToggleGlobalMeshCollisions)
                        );
                    return;
                }
                if (isOn)
                {
                    if (!globalMeshEffectMesh)
                    {
                        globalMeshEffectMesh = new GameObject("_globalMeshViz", typeof(EffectMesh)).GetComponent<EffectMesh>();
                        globalMeshEffectMesh.Labels = MRUKAnchor.SceneLabels.GLOBAL_MESH;
                        if (visualHelperMaterial)
                            globalMeshEffectMesh.MeshMaterial = visualHelperMaterial;
                        globalMeshEffectMesh.HideMesh = true;
                        globalMeshEffectMesh.CreateMesh();
                    }
                    globalMeshEffectMesh.AddColliders();
                }
                else
                {
                    if (!globalMeshEffectMesh)
                        return;
                    globalMeshEffectMesh.DestroyColliders(filter);
                }
            }
            catch (Exception e)
            {
                SetLogsText("\n[{0}]\n {1}\n{2}",
                    nameof(ToggleGlobalMeshCollisions),
                    e.Message,
                    e.StackTrace
                );
            }
        }

        /// <summary>
        /// Displays the nav mesh, if present.
        /// </summary>
        public void DisplayNavMesh(bool isOn)
        {
            try
            {
                if (isOn)
                {
                    debugAction = () =>
                    {
                        var triangulation = NavMesh.CalculateTriangulation();
                        if (triangulation.areas.Length == 0 && navMeshTriangulation.Equals(triangulation))
                            return;

                        triangulation.Equals(triangulation);
                        MeshRenderer navMeshRenderer = null;
                        MeshFilter navMeshFilter = null;
                        if (!navMeshViz)
                        {
                            navMeshViz = new GameObject("_navMeshViz");
                            navMeshRenderer = navMeshViz.AddComponent<MeshRenderer>();
                            navMeshFilter = navMeshViz.AddComponent<MeshFilter>();
                        }
                        else
                        {
                            navMeshRenderer = navMeshViz.GetComponent<MeshRenderer>();
                            navMeshFilter = navMeshViz.GetComponent<MeshFilter>();
                            DestroyImmediate(navMeshFilter.mesh);
                            navMeshFilter.mesh = null;
                        }
                        var navMesh = new Mesh();

                        navMesh.SetVertices(triangulation.vertices);
                        navMesh.SetIndices(triangulation.indices, MeshTopology.Triangles, 0);
                        navMeshRenderer.material = visualHelperMaterial;
                        navMeshRenderer.material.color = Color.cyan;
                        navMeshFilter.mesh = navMesh;
                        navMeshTriangulation = triangulation;
                    };
                }
                else
                {
                    DestroyImmediate(navMeshViz);
                    debugAction = null;
                }
            }
            catch (Exception e)
            {
                SetLogsText("\n[{0}]\n {1}\n{2}",
                    nameof(DisplayNavMesh),
                    e.Message,
                    e.StackTrace
                );
            }

        }


        private EffectMesh GetGlobalMeshEffectMesh()
        {
            EffectMesh[] effectMeshes = FindObjectsByType<EffectMesh>(FindObjectsSortMode.None);
            foreach (EffectMesh effectMesh in effectMeshes)
            {
                if ((effectMesh.Labels & MRUKAnchor.SceneLabels.GLOBAL_MESH) != 0)
                {
                    return effectMesh;
                }
            }
            return null;
        }

        /// <summary>
        /// Creates an object to help visually debugging a specific anchor.
        /// </summary>
        GameObject GenerateDebugAnchor(MRUKAnchor anchor)
        {
            GameObject debugPlanePrefab = CreateDebugPrefabSource(true);
            GameObject debugVolumePrefab = CreateDebugPrefabSource(false);

            Vector3 anchorScale;
            if (anchor.HasVolume)
            {
                // Volumes
                debugAnchor = CloneObject(debugVolumePrefab, anchor.transform);
                anchorScale = anchor.GetAnchorSize();
            }
            else
            {
                // Quads
                debugAnchor = CloneObject(debugPlanePrefab, anchor.transform);
                Vector2 quadScale = anchor.PlaneRect.Value.size;
                anchorScale = new Vector3(quadScale.x, quadScale.y, 1.0f);
            }
            ScaleChildren(debugAnchor.transform, anchorScale);
            debugAnchor.transform.parent = null;
            debugAnchor.SetActive(true);

            Destroy(debugPlanePrefab);
            Destroy(debugVolumePrefab);

            return debugAnchor;
        }

        void ShootProjectile()
        {
            if (!shootBall)
            {
                return;
            }

            debugProjectile.SetActive(true);
            Rigidbody ball = debugProjectile.GetComponent<Rigidbody>();
            if (ball)
            {
                debugProjectile.transform.parent = null;
                ball.velocity = Vector3.zero;
                // Position slightly ahead, since controller collision may intersect
                ball.transform.position = shootingRay.origin + shootingRay.direction * 0.1f;
                ball.AddForce(shootingRay.direction * 200.0f);
            }
        }

        GameObject CloneObject(GameObject prefabObj, Transform refObject)
        {
            GameObject newObj = Instantiate(prefabObj);
            newObj.name = "Debug_" + refObject.name;
            newObj.transform.position = refObject.position;
            newObj.transform.rotation = refObject.rotation;

            return newObj;
        }

        void ScaleChildren(Transform transform, Vector3 localScale)
        {
            foreach (Transform child in transform)
                child.localScale = localScale;
        }

        /// <summary>
        /// By creating our reference PLANE and VOLUME prefabs in code, we can avoid linking them via Inspector.
        /// </summary>
        GameObject CreateDebugPrefabSource(bool isPlane)
        {
            string prefabName = isPlane ? "PlanePrefab" : "VolumePrefab";
            GameObject prefabObject = new GameObject(prefabName);

            GameObject meshParent = new GameObject("MeshParent");
            meshParent.transform.SetParent(prefabObject.transform);
            meshParent.SetActive(false);

            GameObject prefabMesh = isPlane ? GameObject.CreatePrimitive(PrimitiveType.Quad) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            prefabMesh.name = "Mesh";
            prefabMesh.transform.SetParent(meshParent.transform);
            if (isPlane)
            {
                // Unity quad's normal doesn't align with transform's Z-forward
                prefabMesh.transform.localRotation = Quaternion.Euler(0, 180, 0);
            }
            else
            {
                // Anchor cubes don't have a center pivot
                prefabMesh.transform.localPosition = new Vector3(0, 0, -0.5f);
            }
            SetMaterialProperties(prefabMesh.GetComponent<MeshRenderer>());
            DestroyImmediate(prefabMesh.GetComponent<Collider>());

            GameObject prefabPivot = new GameObject("Pivot");
            prefabPivot.transform.SetParent(prefabObject.transform);

            CreateGridPattern(prefabPivot.transform, Vector3.zero, Quaternion.identity);
            if (!isPlane)
            {
                CreateGridPattern(prefabPivot.transform, new Vector3(0, 0, -1), Quaternion.Euler(180, 0, 0));
                CreateGridPattern(prefabPivot.transform, new Vector3(0, -0.5f, -0.5f), Quaternion.Euler(90, 0, 0));
                CreateGridPattern(prefabPivot.transform, new Vector3(0, 0.5f, -0.5f), Quaternion.Euler(-90, 0, 0));
                CreateGridPattern(prefabPivot.transform, new Vector3(-0.5f, 0, -0.5f), Quaternion.Euler(0, -90, 90));
                CreateGridPattern(prefabPivot.transform, new Vector3(0.5f, 0, -0.5f), Quaternion.Euler(180, -90, 90));
            }
            return prefabObject;
        }

        void SetMaterialProperties(MeshRenderer refMesh)
        {
            refMesh.material.SetColor("_Color", new Color(0.5f, 0.9f, 1.0f, 0.75f));
            refMesh.material.SetOverrideTag("RenderType", "Transparent");
            refMesh.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            refMesh.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            refMesh.material.SetInt("_ZWrite", 0);
            refMesh.material.SetInt("_Cull", 2); // "Back"
            refMesh.material.DisableKeyword("_ALPHATEST_ON");
            refMesh.material.EnableKeyword("_ALPHABLEND_ON");
            refMesh.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            refMesh.material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        // The grid pattern on each anchor is actually a mesh, to avoid a texture
        void CreateGridPattern(Transform parentTransform, Vector3 localOffset, Quaternion localRotation)
        {
            GameObject newGameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            newGameObject.name = "Checker";
            newGameObject.transform.SetParent(parentTransform, false);
            newGameObject.transform.localPosition = localOffset;
            newGameObject.transform.localRotation = localRotation;
            DestroyImmediate(newGameObject.GetComponent<Collider>());

            // offset the debug grid the smallest amount to avoid z-fighting
            const float NORMAL_OFFSET = 0.001f;

            // the mesh is used on every prefab, but only needs to be created once
            if (_debugCheckerMesh == null)
            {
                _debugCheckerMesh = new Mesh();
                const int gridWidth = 10;
                float cellWidth = 1.0f / gridWidth;
                float xPos = -0.5f;
                float yPos = -0.5f;

                int totalTiles = gridWidth * gridWidth / 2;
                int totalVertices = totalTiles * 4;
                int totalIndices = totalTiles * 6;

                Vector3[] MeshVertices = new Vector3[totalVertices];
                Vector2[] MeshUVs = new Vector2[totalVertices];
                Color32[] MeshColors = new Color32[totalVertices];
                Vector3[] MeshNormals = new Vector3[totalVertices];
                Vector4[] MeshTangents = new Vector4[totalVertices];
                int[] MeshTriangles = new int[totalIndices];

                int vertCounter = 0;
                int indexCounter = 0;
                int quadCounter = 0;

                for (int x = 0; x < gridWidth; x++)
                {
                    bool createQuad = (x % 2 == 0);
                    for (int y = 0; y < gridWidth; y++)
                    {
                        if (createQuad)
                        {
                            for (int V = 0; V < 4; V++)
                            {
                                Vector3 localVertPos = new Vector3(xPos, yPos + y * cellWidth, NORMAL_OFFSET);
                                switch (V)
                                {
                                    case 1:
                                        localVertPos += new Vector3(0, cellWidth, 0);
                                        break;
                                    case 2:
                                        localVertPos += new Vector3(cellWidth, cellWidth, 0);
                                        break;
                                    case 3:
                                        localVertPos += new Vector3(cellWidth, 0, 0);
                                        break;
                                }
                                MeshVertices[vertCounter] = localVertPos;
                                MeshUVs[vertCounter] = Vector2.zero;
                                MeshColors[vertCounter] = Color.black;
                                MeshNormals[vertCounter] = Vector3.forward;
                                MeshTangents[vertCounter] = Vector3.right;

                                vertCounter++;
                            }

                            int baseCount = quadCounter * 4;
                            MeshTriangles[indexCounter++] = baseCount;
                            MeshTriangles[indexCounter++] = baseCount + 2;
                            MeshTriangles[indexCounter++] = baseCount + 1;
                            MeshTriangles[indexCounter++] = baseCount;
                            MeshTriangles[indexCounter++] = baseCount + 3;
                            MeshTriangles[indexCounter++] = baseCount + 2;

                            quadCounter++;
                        }
                        createQuad = !createQuad;
                    }
                    xPos += cellWidth;
                }

                _debugCheckerMesh.Clear();
                _debugCheckerMesh.name = "CheckerMesh";
                _debugCheckerMesh.vertices = MeshVertices;
                _debugCheckerMesh.uv = MeshUVs;
                _debugCheckerMesh.colors32 = MeshColors;
                _debugCheckerMesh.triangles = MeshTriangles;
                _debugCheckerMesh.normals = MeshNormals;
                _debugCheckerMesh.tangents = MeshTangents;
                _debugCheckerMesh.RecalculateNormals();
                _debugCheckerMesh.RecalculateTangents();
            }

            newGameObject.GetComponent<MeshFilter>().mesh = _debugCheckerMesh;

            Material material = newGameObject.GetComponent<MeshRenderer>().material;
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_ZWrite", 0);
            material.SetInt("_Cull", 2); // "Back"
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        /// <summary>
        /// Creates the debug primitives for visual debugging purposes and to avoid inspector linking.
        /// </summary>
        void CreateDebugPrimitives()
        {
            debugCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            debugCube.GetComponent<Renderer>().material.color = Color.green;
            debugCube.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            debugCube.GetComponent<Collider>().enabled = false;
            debugCube.SetActive(false);

            debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            debugSphere.GetComponent<Renderer>().material.color = Color.green;
            debugSphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            debugSphere.GetComponent<Collider>().enabled = false;
            debugSphere.SetActive(false);

            debugNormal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            debugNormal.GetComponent<Renderer>().material.color = Color.green;
            debugNormal.transform.localScale = new Vector3(0.02f, 0.1f, 0.02f);
            debugNormal.GetComponent<Collider>().enabled = false;
            debugNormal.SetActive(false);
        }

        /// <summary>
        /// Convenience method to show the normal of a hit collision.
        /// </summary>
        /// <param name="hit"></param>
        void ShowHitNormal(RaycastHit hit)
        {
            if (debugNormal != null && hit.point != Vector3.zero && hit.distance != 0)
            {
                debugNormal.SetActive(true);
                debugNormal.transform.position = hit.point + (-debugNormal.transform.up * debugNormal.transform.localScale.y);
                debugNormal.transform.rotation = Quaternion.FromToRotation(-Vector3.up, hit.normal);
            }
            else
            {
                debugNormal.SetActive(false);
            }
        }

        /// <summary>
        ///  Selects a menu option if available.
        /// </summary>
        void ClickUIElement()
        {
            if (eventSystem != null && eventSystem.currentSelectedGameObject != null)
            {
                Selectable selectable = eventSystem.currentSelectedGameObject.GetComponent<Selectable>();
                ExecuteEvents.Execute(selectable.gameObject, new BaseEventData(eventSystem), ExecuteEvents.submitHandler);
            }
        }

        /// <summary>
        /// Navigates the Unity UI using a given move direction.
        /// </summary>
        void NavigateUI(MoveDirection direction)
        {
            AxisEventData data = new AxisEventData(eventSystem);
            data.moveDir = direction;
            data.selectedObject = eventSystem.currentSelectedGameObject;
            ExecuteEvents.Execute(data.selectedObject, data, ExecuteEvents.moveHandler);
        }

        /// <summary>
        /// Helper function to handle user navigation input using Oculus Touch thumbsticks.
        /// Detects user input on primary and secondary thumbsticks and triggers navigation events accordingly.
        /// This to make the option selection independent from the Project's Input System configuration.
        /// </summary>
        void UINavigationHelper()
        {
            if (inputDelayCounter <= 0)
            {
                Vector3 secondaryThumbstick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
                Vector3 primaryThumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
                if (secondaryThumbstick.y > 0.5f || primaryThumbstick.y > 0.5f)
                {
                    NavigateUI(MoveDirection.Up);
                    inputDelayCounter = inputSelectionDelay;
                }
                else if (secondaryThumbstick.y < -0.5f || primaryThumbstick.y < -0.5f)
                {
                    NavigateUI(MoveDirection.Down);
                    inputDelayCounter = inputSelectionDelay;
                }
                else if (secondaryThumbstick.x > 0.5f || primaryThumbstick.x > 0.5f)
                {
                    NavigateUI(MoveDirection.Right);
                }
                else if (secondaryThumbstick.x < -0.5f || primaryThumbstick.x < -0.5f)
                {
                    NavigateUI(MoveDirection.Left);
                }
            }
            else
            {
                inputDelayCounter -= Time.deltaTime;
            }
        }

        void SetLogsText(string logsText, params object[] args)
        {
            if (logs)
            {
                logs.text = String.Format(logsText, args);
            }
        }
    }
}
