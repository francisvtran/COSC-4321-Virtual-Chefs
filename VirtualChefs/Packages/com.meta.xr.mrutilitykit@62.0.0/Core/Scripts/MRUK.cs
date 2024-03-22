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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.XR.MRUtilityKit
{

    /// <summary>
    /// This class contains convenience functions that allow you to
    /// query your scene.
    ///
    /// Use together with <seealso cref="MRUKLoader"/> to
    /// load data via link, fake data, or on-device data.
    /// </summary>
    public class MRUK : MonoBehaviour
    {
        // when interacting specifically with tops of volumes, this can be used to
        // specify where the return position should be aligned on the surface
        // e.g. some apps might want a position right in the center of the table (chess)
        // for others, the edge may be more important (piano or pong)
        public enum PositioningMethod
        {
            DEFAULT,
            CENTER,
            EDGE
        }

        /// <summary>
        /// Specify the source of the scene data.
        /// </summary>
        public enum SceneDataSource
        {
            /// <summary>
            /// Load scene data from the device.
            /// </summary>
            Device,
            /// <summary>
            /// Load scene data from prefabs.
            /// </summary>
            Prefab,
            /// <summary>
            /// First try to load data from the device and if none can be found
            /// fall back to loading from a prefab.
            /// </summary>
            DeviceWithPrefabFallback,
        }

        [Flags]
        public enum SurfaceType
        {
            FACING_UP = 1 << 0,
            FACING_DOWN = 1 << 1,
            VERTICAL = 1 << 2,
        };

        [Flags]
        private enum AnchorRepresentation
        {
            PLANE = 1 << 0,
            VOLUME = 1 << 1,
        }

        public bool IsInitialized { get; private set; } = false;

        public UnityEvent SceneLoadedEvent = new UnityEvent();

        /// <summary>
        /// When world locking is enabled the position of the camera rig will be adjusted each frame to ensure
        /// the room anchors are where they should be relative to the camera position.This is necessary to
        /// ensure the position of the virtual objects in the world do not get out of sync with the real world.
        /// </summary>
        public bool EnableWorldLock
        {
            get { return _enableWorldLock; }
            set
            {
                if (!value && _enableWorldLock)
                {
                    _cameraRig.trackingSpace.localPosition = Vector3.zero;
                    _cameraRig.trackingSpace.localRotation = Quaternion.identity;
                }
                _enableWorldLock = value;
            }
        }

        private OVRCameraRig _cameraRig;
        private bool _enableWorldLock = true;

        /// <summary>
        /// This is the final event that tells developer code that Scene API and MR Utility Kit have been initialized, and that the room can be queried.
        /// </summary>
        void InitializeScene()
        {
            try
            {
                SceneLoadedEvent.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            IsInitialized = true;
        }

        /// <summary>
        /// Register to receive a callback when the scene is loaded. If the scene is already loaded
        /// at the time this is called, the callback will be invoked immediatly.
        /// </summary>
        /// <param name="callback"></param>
        public void RegisterSceneLoadedCallback(UnityAction callback)
        {
            SceneLoadedEvent.AddListener(callback);
            if (IsInitialized)
            {
                callback();
            }
        }

        /// <summary>
        /// Get a list of all the rooms in the scene.
        /// </summary>
        public List<MRUKRoom> GetRooms() => _sceneRooms;

        /// <summary>
        /// Returns the current room the headset is in. If the headset is not in any given room
        /// then it will return the room the headset was last in when this function was called.
        /// If the headset hasn't been in a valid room yet then return the first room in the list.
        /// If no rooms have been loaded yet then return null.
        /// </summary>
        public MRUKRoom GetCurrentRoom()
        {
            // This is a rather expensive operation, we should only do it at most once per frame.
            if (_cachedCurrentRoomFrame != Time.frameCount)
            {
                if (_cameraRig?.centerEyeAnchor.position is Vector3 eyePos)
                {
                    foreach (var room in _sceneRooms)
                    {
                        if (room.IsPositionInRoom(eyePos, false))
                        {
                            _cachedCurrentRoom = room;
                            _cachedCurrentRoomFrame = Time.frameCount;
                            return room;
                        }
                    }
                }
            }

            if (_cachedCurrentRoom != null)
            {
                return _cachedCurrentRoom;
            }

            if (_sceneRooms.Count > 0)
            {
                return _sceneRooms[0];
            }

            return null;
        }

        /// <summary>
        /// Ensures whether the runtime permission for USE_SCENE has been
        /// granted or not. If the user has not granted this permission,
        /// a request is made.
        /// </summary>
        public static void EnsureSceneRuntimePermissions()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
        var permission = "com.oculus.permission.USE_SCENE";
        if (UnityEngine.Android.Permission.HasUserAuthorizedPermission(permission))
            UnityEngine.Android.Permission.RequestUserPermission(permission);
#endif
        }

        /// <summary>
        /// Checks whether any anchors can be loaded.
        /// </summary>
        /// <returns>Returns a task-based bool, which is true if
        /// there are any scene anchors in the system, and false
        /// otherwise. If false is returned, then either
        /// the scene permission needs to be set, or the user
        /// has to run Scene Capture.</returns>
        public static async Task<bool> HasSceneModel()
        {
            var rooms = new List<OVRAnchor>();
            if (!await OVRAnchor.FetchAnchorsAsync<OVRRoomLayout>(rooms))
                return false;
            return rooms.Count > 0;
        }

        [Serializable]
        public class MRUKSettings
        {
            [SerializeField, Tooltip("Where to load the data from.")]
            public SceneDataSource DataSource = SceneDataSource.Device;
            [SerializeField, Tooltip("Which room to use; -1 is random.")]
            public int RoomIndex = -1;
            [SerializeField, Tooltip("The list of prefab rooms to use.")]
            public GameObject[] RoomPrefabs;
            [SerializeField, Tooltip("Trigger a scene load on startup.")]
            public bool LoadSceneOnStartup = true;
            [SerializeField, Tooltip("The width of a seat. Use to calculate seat positions")]
            public float SeatWidth = 0.6f;
        }

        [Tooltip("Contains all the information regarding data loading.")]
        public MRUKSettings SceneSettings;

        List<MRUKRoom> _sceneRooms = new List<MRUKRoom>();
        MRUKRoom _cachedCurrentRoom = null;
        int _cachedCurrentRoomFrame = 0;

        public static MRUK Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.Assert(false, "There should be only one instance of MRUK!");
                Destroy(this);
            }
            else
            {
                Instance = this;
            }

            if (SceneSettings.LoadSceneOnStartup)
            {
                // We can't await for the result because Awake is not async, silence the warning
#pragma warning disable CS4014
                LoadScene();
#pragma warning restore CS4014
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            if (!_cameraRig)
            {
                _cameraRig = FindObjectOfType<OVRCameraRig>();
            }
        }

        private void Update()
        {
            if (_enableWorldLock && _cameraRig)
            {
                var room = GetCurrentRoom();
                if (room)
                {
                    room.UpdateWorldLock(_cameraRig);
                }
            }
        }

        /// <summary>
        /// Load the scene asynchronously using the settings from SceneSettings
        /// </summary>
        async Task LoadScene()
        {
            try
            {
                if (SceneSettings.DataSource == SceneDataSource.Device ||
                    SceneSettings.DataSource == SceneDataSource.DeviceWithPrefabFallback)
                {
                    await LoadSceneFromDevice();
                }
                if (SceneSettings.DataSource == SceneDataSource.Prefab ||
                    (SceneSettings.DataSource == SceneDataSource.DeviceWithPrefabFallback && _sceneRooms.Count == 0))
                {
                    if (SceneSettings.RoomPrefabs.Length == 0)
                    {
                        Debug.LogWarning($"Failed to load room from prefab because prefabs list is empty");
                        return;
                    }

                    // Clone the roomPrefab, but essentially replace all its content
                    // if -1 or out of range, use a random one
                    var roomIndex = SceneSettings.RoomIndex;
                    if (roomIndex == -1)
                        roomIndex = UnityEngine.Random.Range(0, SceneSettings.RoomPrefabs.Length);

                    Debug.Log($"Loading prefab room {roomIndex}");

                    GameObject roomPrefab = SceneSettings.RoomPrefabs[roomIndex];
                    LoadSceneFromPrefab(roomPrefab);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                throw;
            }
        }

        /// <summary>
        /// Called when the room is destroyed
        /// </summary>
        /// <remarks>
        /// This is used to keep the list of active rooms up to date.
        /// So there should never be any null entries in the list.
        /// </remarks>
        /// <param name="room"></param>
        internal void OnRoomDestroyed(MRUKRoom room)
        {
            _sceneRooms.Remove(room);
            if (_cachedCurrentRoom == room)
            {
                _cachedCurrentRoom = null;
            }
        }

        public void ClearScene()
        {
            // destroy the rooms and all children
            foreach (var room in _sceneRooms)
            {
                foreach (Transform child in room.transform)
                    Destroy(child.gameObject);
                Destroy(room.gameObject);
            }
            _sceneRooms.Clear();
        }

        /// <summary>
        /// Loads the scene from the data stored on the device.
        /// </summary>
        public async Task LoadSceneFromDevice(bool clearSceneFirst = true)
        {
            if (clearSceneFirst)
            {
                ClearScene();
            }
            var rooms = new List<OVRAnchor>();
            await OVRAnchor.FetchAnchorsAsync<OVRRoomLayout>(rooms);

#if UNITY_EDITOR
            OVRTelemetry.Start(TelemetryConstants.MarkerId.LoadSceneFromDevice)
                .AddAnnotation(TelemetryConstants.AnnotationType.NumRooms, rooms.Count.ToString())
                .SetResult(rooms.Count == 0 ? OVRPlugin.Qpl.ResultType.Fail : OVRPlugin.Qpl.ResultType.Success)
                .Send();
#endif

            if (rooms.Count == 0)
            {
                Debug.Log("MRUKLoader couldn't load any scene data. Ensure " +
                    "that Scene Capture has been run and that the runtime " +
                    "permission for Scene Data has been granted.");
                return;
            }

            var roomInfos = new List<MRUKRoom>();
            foreach (var roomAnchor in rooms)
            {
                var room = roomAnchor.GetComponent<OVRAnchorContainer>();

                var childAnchors = new List<OVRAnchor>();
                await room.FetchChildrenAsync(childAnchors);

                var roomObject = new GameObject($"Room - {roomAnchor.Uuid}");
                var roomInfo = roomObject.AddComponent<MRUKRoom>();
                roomInfo.Anchor = roomAnchor;
                roomInfos.Add(roomInfo);

                // Enable locatable on all the child anchors
                var tasks = new List<OVRTask<bool>>();
                foreach (var child in childAnchors)
                {
                    if (!child.TryGetComponent<OVRLocatable>(out var locatable))
                        continue;
                    tasks.Add(locatable.SetEnabledSafeAsync(true));
                }
                foreach (var task in tasks)
                {
                    await task;
                }

                // Once they are all enabled, get their position in a batch
                foreach (var child in childAnchors)
                {
                    if (!child.TryGetComponent<OVRLocatable>(out var locatable))
                        continue;

                    var label = "none";
                    var splitLabels = new List<string>();
                    if (child.TryGetComponent<OVRSemanticLabels>(out var labels) && labels.IsEnabled)
                    {
                        label = labels.Labels;
                        splitLabels.AddRange(label.Split(','));
                    }

                    var childObject = new GameObject(label);
                    childObject.transform.parent = roomObject.transform;

                    // first position it
                    if (locatable.TryGetSceneAnchorPose(out var pose))
                    {
                        var position = pose.ComputeWorldPosition(Camera.main);
                        var rotation = pose.ComputeWorldRotation(Camera.main);
                        childObject.transform.localPosition = position ?? Vector3.zero;
                        childObject.transform.localRotation = rotation ?? Quaternion.identity;
                    }

                    // provide anchor info
                    var info = childObject.AddComponent<MRUKAnchor>();
                    info.PopulateData(splitLabels, child);
                }
            }

            // this thing calculates everything, but first we need to have all
            // the toolkitanchor infos populated
            foreach (var roomInfo in roomInfos)
            {
                roomInfo.ComputeRoomInfo();
                _sceneRooms.Add(roomInfo);
            }

            // now that we're done, updated the global state for the Update()
            InitializeScene();
        }

        /// <summary>
        /// This simulates the creation of a scene in the Editor, using transforms and names from our prefab rooms.
        /// </summary>
        public void LoadSceneFromPrefab(GameObject roomPrefab, bool clearSceneFirst = true)
        {
#if UNITY_EDITOR
            OVRTelemetry.Start(TelemetryConstants.MarkerId.LoadSceneFromPrefab)
                .AddAnnotation(TelemetryConstants.AnnotationType.RoomName, roomPrefab.name)
                .Send();
#endif

            if (clearSceneFirst)
            {
                ClearScene();
            }

            List<GameObject> walls = new List<GameObject>();
            List<GameObject> volumes = new List<GameObject>();
            List<GameObject> planes = new List<GameObject>();

            GameObject sceneRoom = new GameObject(roomPrefab.name);
            MRUKRoom roomInfo = sceneRoom.AddComponent<MRUKRoom>();

            // walls ordered sequentially, CW when viewed top-down
            List<MRUKAnchor> orderedWalls = new List<MRUKAnchor>();

            List<MRUKAnchor> unorderedWalls = new List<MRUKAnchor>();
            List<Vector3> floorCorners = new List<Vector3>();

            FindObjects(MRUKAnchor.SceneLabels.WALL_FACE.ToString(), roomPrefab.transform, ref walls);
            FindObjects(MRUKAnchor.SceneLabels.OTHER.ToString(), roomPrefab.transform, ref volumes);
            FindObjects(MRUKAnchor.SceneLabels.TABLE.ToString(), roomPrefab.transform, ref volumes);
            FindObjects(MRUKAnchor.SceneLabels.COUCH.ToString(), roomPrefab.transform, ref volumes);
            FindObjects(MRUKAnchor.SceneLabels.WINDOW_FRAME.ToString(), roomPrefab.transform, ref planes);
            FindObjects(MRUKAnchor.SceneLabels.DOOR_FRAME.ToString(), roomPrefab.transform, ref planes);
            FindObjects(MRUKAnchor.SceneLabels.WALL_ART.ToString(), roomPrefab.transform, ref planes);
            FindObjects(MRUKAnchor.SceneLabels.PLANT.ToString(), roomPrefab.transform, ref volumes);
            FindObjects(MRUKAnchor.SceneLabels.SCREEN.ToString(), roomPrefab.transform, ref volumes);
            FindObjects(MRUKAnchor.SceneLabels.BED.ToString(), roomPrefab.transform, ref volumes);
            FindObjects(MRUKAnchor.SceneLabels.LAMP.ToString(), roomPrefab.transform, ref volumes);
            FindObjects(MRUKAnchor.SceneLabels.STORAGE.ToString(), roomPrefab.transform, ref volumes);

            float wallHeight = 0.0f;

            for (int i = 0; i < walls.Count; i++)
            {
                if (i == 0)
                {
                    wallHeight = walls[i].transform.localScale.y;
                }

                MRUKAnchor objData = CreateAnchorFromRoomObject(walls[i].transform, walls[i].transform.localScale, AnchorRepresentation.PLANE);

                objData.transform.parent = sceneRoom.transform;
                objData.transform.Rotate(0, 180, 0);

                unorderedWalls.Add(objData);
            }

            // There may be imprecision between the prefab walls (misaligned edges)
            // so, we shift them so the edges perfectly match up:
            // bottom left corner of wall is fixed, right corner matches left corner of wall to the right
            int seedId = 0;
            for (int i = 0; i < unorderedWalls.Count; i++)
            {
                MRUKAnchor wall = GetAdjacentWall(ref seedId, unorderedWalls);
                orderedWalls.Add(wall);

                Rect wallRect = wall.PlaneRect.Value;
                Vector3 leftCorner = wall.transform.TransformPoint(new Vector3(wallRect.max.x, wallRect.min.y, 0.0f));
                floorCorners.Add(leftCorner);
            }
            for (int i = 0; i < orderedWalls.Count; i++)
            {
                Rect planeRect = orderedWalls[i].PlaneRect.Value;
                Vector3 corner1 = floorCorners[i];
                int nextID = (i == orderedWalls.Count - 1) ? 0 : i + 1;
                Vector3 corner2 = floorCorners[nextID];

                Vector3 wallRight = (corner1 - corner2);
                wallRight.y = 0.0f;
                float wallWidth = wallRight.magnitude;
                wallRight /= wallWidth;
                Vector3 wallUp = Vector3.up;
                Vector3 wallFwd = Vector3.Cross(wallRight, wallUp);
                Vector3 newPosition = (corner1 + corner2) * 0.5f + Vector3.up * (planeRect.height * 0.5f);
                Quaternion newRotation = Quaternion.LookRotation(wallFwd, wallUp);
                Rect newRect = new Rect(-0.5f * wallWidth, planeRect.y, wallWidth, planeRect.height);

                orderedWalls[i].transform.position = newPosition;
                orderedWalls[i].transform.rotation = newRotation;
                orderedWalls[i].PlaneRect = newRect;
                orderedWalls[i].PlaneBoundary2D = new List<Vector2>
                {
                    new Vector2(newRect.xMin, newRect.yMin),
                    new Vector2(newRect.xMax, newRect.yMin),
                    new Vector2(newRect.xMax, newRect.yMax),
                    new Vector2(newRect.xMin, newRect.yMax),
                };
            }

            for (int i = 0; i < volumes.Count; i++)
            {
                Vector3 cubeScale = new Vector3(volumes[i].transform.localScale.x, volumes[i].transform.localScale.z, volumes[i].transform.localScale.y);
                var representation = AnchorRepresentation.VOLUME;
                // Table and couch are special. They also have a plane attached to them.
                if (volumes[i].transform.name == MRUKAnchor.SceneLabels.TABLE.ToString() ||
                    volumes[i].transform.name == MRUKAnchor.SceneLabels.COUCH.ToString())
                {
                    representation |= AnchorRepresentation.PLANE;
                }
                MRUKAnchor objData = CreateAnchorFromRoomObject(volumes[i].transform, cubeScale, representation);
                objData.transform.parent = sceneRoom.transform;

                // in the prefab rooms, the cubes are more Unity-like and default: Y is up, pivot is centered
                // this needs to be converted to Scene format, in which the pivot is on top of the cube and Z is up
                objData.transform.position += cubeScale.z * 0.5f * Vector3.up;
                objData.transform.Rotate(new Vector3(-90, 0, 0), Space.Self);
            }
            for (int i = 0; i < planes.Count; i++)
            {
                MRUKAnchor objData = CreateAnchorFromRoomObject(planes[i].transform, planes[i].transform.localScale, AnchorRepresentation.PLANE);
                objData.transform.parent = sceneRoom.transform;

                // Unity quads have a surface normal facing the opposite direction
                // Rather than have "backwards" walls in the room prefab, we just rotate them here
                objData.transform.Rotate(0, 180, 0);
            }

            // mimic OVRSceneManager: floor/ceiling anchor aligns with longest wall, scaled to room size
            MRUKAnchor longestWall = null;
            float longestWidth = 0.0f;
            foreach (var wall in orderedWalls)
            {
                float wallWidth = wall.PlaneRect.Value.size.x;
                if (wallWidth > longestWidth)
                {
                    longestWidth = wallWidth;
                    longestWall = wall;
                }
            }

            // calculate the room bounds, relative to the longest wall
            float zMin = 0.0f;
            float zMax = 0.0f;
            float xMin = 0.0f;
            float xMax = 0.0f;
            for (int i = 0; i < floorCorners.Count; i++)
            {
                Vector3 localPos = longestWall.transform.InverseTransformPoint(floorCorners[i]);

                zMin = i == 0 ? localPos.z : Mathf.Min(zMin, localPos.z);
                zMax = i == 0 ? localPos.z : Mathf.Max(zMax, localPos.z);
                xMin = i == 0 ? localPos.x : Mathf.Min(xMin, localPos.x);
                xMax = i == 0 ? localPos.x : Mathf.Max(xMax, localPos.x);
            }
            Vector3 localRoomCenter = new Vector3((xMin + xMax) * 0.5f, 0, (zMin + zMax) * 0.5f);
            Vector3 roomCenter = longestWall.transform.TransformPoint(localRoomCenter);
            roomCenter -= Vector3.up * wallHeight * 0.5f;
            Vector3 floorScale = new Vector3(zMax - zMin, xMax - xMin, 1);

            for (int i = 0; i < 2; i++)
            {
                string anchorName = (i == 0 ? "FLOOR" : "CEILING");

                var position = roomCenter + Vector3.up * wallHeight * i;
                float anchorFlip = i == 0 ? 1 : -1;
                var rotation = Quaternion.LookRotation(longestWall.transform.up * anchorFlip, longestWall.transform.right);
                MRUKAnchor objData = CreateAnchor(anchorName, position, rotation, floorScale, AnchorRepresentation.PLANE);
                objData.transform.parent = sceneRoom.transform;

                objData.PlaneBoundary2D = new(floorCorners.Count);
                foreach (var corner in floorCorners)
                {
                    var localCorner = objData.transform.InverseTransformPoint(corner);
                    objData.PlaneBoundary2D.Add(new Vector2(localCorner.x, localCorner.y));
                }

                if (i == 1)
                {
                    objData.PlaneBoundary2D.Reverse();
                }
            }

            // after everything, we need to let the room computation run
            roomInfo.ComputeRoomInfo();
            _sceneRooms.Add(roomInfo);

            InitializeScene();
        }

        /// <summary>
        /// Serializes the current scene into a JSON string using the specified coordinate system for serialization.
        /// </summary>
        /// <param name="coordinateSystem">The coordinate system to be used for serialization (Unity/Unreal).</param>
        /// <returns>A JSON string representing the serialized scene data.</returns>
        public string SaveSceneToJsonString(SerializationHelpers.CoordinateSystem coordinateSystem)
        {
            return SerializationHelpers.Serialize(coordinateSystem);
        }

        /// <summary>
        /// Loads the scene from a JSON string representing the scene data.
        /// </summary>
        /// <param name="jsonString">The JSON string containing the serialized scene data.</param>
        /// <param name="clearSceneFirst">If set to true, the current scene will be cleared before loading the new data. Defaults to true.</param>
        public void LoadSceneFromJsonString(string jsonString, bool clearSceneFirst = true)
        {
            if (clearSceneFirst)
            {
                ClearScene();
            }

            _sceneRooms =  SerializationHelpers.Deserialize(jsonString);
            foreach (var room in _sceneRooms)
            {
                // after everything, we need to let the room computation run
                room.ComputeRoomInfo();
            }
#if UNITY_EDITOR
            OVRTelemetry.Start(TelemetryConstants.MarkerId.LoadSceneFromJson)
                .AddAnnotation(TelemetryConstants.AnnotationType.NumRooms, _sceneRooms.Count.ToString())
                .Send();
#endif
            InitializeScene();
        }

        MRUKAnchor CreateAnchorFromRoomObject(Transform refObject, Vector3 objScale, AnchorRepresentation representation)
        {
            return CreateAnchor(refObject.name, refObject.position, refObject.rotation, objScale, representation);
        }

        MRUKAnchor CreateAnchor(string name, Vector3 position, Quaternion rotation, Vector3 objScale, AnchorRepresentation representation)
        {
            var realAnchor = new GameObject(name);
            realAnchor.transform.position = position;
            realAnchor.transform.rotation = rotation;
            MRUKAnchor objData = realAnchor.AddComponent<MRUKAnchor>();
            objData.AnchorLabels.Add(realAnchor.name);
            if ((representation & AnchorRepresentation.PLANE) != 0)
            {
                var size2d = new Vector2(objScale.x, objScale.y);
                var rect = new Rect(-0.5f * size2d, size2d);
                objData.PlaneRect = rect;
                objData.PlaneBoundary2D = new List<Vector2>
                {
                    new Vector2(rect.xMin, rect.yMin),
                    new Vector2(rect.xMax, rect.yMin),
                    new Vector2(rect.xMax, rect.yMax),
                    new Vector2(rect.xMin, rect.yMax),
                };
            }
            if ((representation & AnchorRepresentation.VOLUME) != 0)
            {
                Vector3 offsetCenter = new Vector3(0, 0, -objScale.z * 0.5f);
                objData.VolumeBounds = new Bounds(offsetCenter, objScale);
            }
            return objData;
        }

        void FindObjects(string objName, Transform rootTransform, ref List<GameObject> objList)
        {
            if (rootTransform.name.Equals(objName))
            {
                objList.Add(rootTransform.gameObject);
            }

            foreach (Transform child in rootTransform)
            {
                FindObjects(objName, child, ref objList);
            }
        }

        MRUKAnchor GetAdjacentWall(ref int thisID, List<MRUKAnchor> randomWalls)
        {
            Vector2 thisWallScale = randomWalls[thisID].PlaneRect.Value.size;

            Vector3 halfScale = thisWallScale * 0.5f;
            Vector3 bottomRight = randomWalls[thisID].transform.position - randomWalls[thisID].transform.up * halfScale.y - randomWalls[thisID].transform.right * halfScale.x;
            float closestCornerDistance = Mathf.Infinity;
            // When searching for a matching corner, the correct one should match positions. If they don't, assume there's a crack in the room.
            // This should be an impossible scenario and likely means broken data from Room Setup.
            int rightWallID = 0;
            for (int i = 0; i < randomWalls.Count; i++)
            {
                // compare to bottom left point of other walls
                if (i != thisID)
                {
                    Vector2 testWallHalfScale = randomWalls[i].PlaneRect.Value.size * 0.5f;
                    Vector3 bottomLeft = randomWalls[i].transform.position - randomWalls[i].transform.up * testWallHalfScale.y + randomWalls[i].transform.right * testWallHalfScale.x;
                    float thisCornerDistance = Vector3.Distance(bottomLeft, bottomRight);
                    if (thisCornerDistance < closestCornerDistance)
                    {
                        closestCornerDistance = thisCornerDistance;
                        rightWallID = i;
                    }
                }
            }
            thisID = rightWallID;
            return randomWalls[thisID];
        }
    }
}
