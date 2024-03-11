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

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;
using System;

[assembly: InternalsVisibleTo("meta.xr.mrutilitykit.tests")]
namespace Meta.XR.MRUtilityKit
{
    public class MRUKRoom : MonoBehaviour
    {
        public OVRAnchor Anchor = OVRAnchor.Null;

        // cached list of all anchors
        List<MRUKAnchor> _anchors = new();
        MRUKAnchor _floorAnchor = null;
        MRUKAnchor _ceilingAnchor = null;
        MRUKAnchor _globalMeshAnchor = null;
        List<MRUKAnchor> _wallAnchors = new();

        // a CCW list of bottom-left corner points of each wall
        private List<Vector3> _corners = new List<Vector3>();

        Bounds _roomBounds;

        // a list of seat poses in the room:
        // suggested placements for remote avatars, that exist only on COUCH objects
        // see GetClosestSeatPose for details
        public struct CouchSeat
        {
            public MRUKAnchor couchAnchor;
            public List<Pose> couchPoses;
        };
        List<CouchSeat> _seatPoses = new List<CouchSeat>();

        // A triangle mesh of anchors used for world locking
        internal AnchorMesh _anchorMesh = new AnchorMesh();

        struct Surface
        {
            public MRUKAnchor Anchor;
            public float UsableArea;
            public bool IsPlane;
            public Rect Bounds;
            public Matrix4x4 Transform;
        }

        /// <summary>
        /// Compute further information to the room.
        ///
        /// Important: requires that the room's child <seealso cref="MRUKAnchor"/>
        /// has been properly instantiated and data populated.
        /// </summary>
        internal void ComputeRoomInfo()
        {
            _anchors.Clear();
            _wallAnchors.Clear();

            foreach (Transform child in transform)
            {
                if (child.TryGetComponent(out MRUKAnchor info))
                    _anchors.Add(info);
            }

            float averageRoomHeight = 0.0f;

            for (int i = 0; i < _anchors.Count; i++)
            {
                if (_anchors[i].HasLabel(OVRSceneManager.Classification.WallFace))
                {
                    var wallScale = _anchors[i].PlaneRect.Value.size;
                    averageRoomHeight = (i == 0) ? wallScale.y : averageRoomHeight + wallScale.y;
                    _wallAnchors.Add(_anchors[i]);
                }
                else if (_anchors[i].HasLabel(OVRSceneManager.Classification.Floor))
                {
                    _floorAnchor = _anchors[i];
                }
                else if (_anchors[i].HasLabel(OVRSceneManager.Classification.Ceiling))
                {
                    _ceilingAnchor = _anchors[i];
                }
                else if (_anchors[i].HasLabel(OVRSceneManager.Classification.GlobalMesh))
                {
                    _globalMeshAnchor = _anchors[i];
                }
            }
            averageRoomHeight /= _wallAnchors.Count;

            CalculateRoomOutline();
            CalculateRoomBounds(averageRoomHeight);
            CalculateSeatPoses();
            CalculateHierarchyReferences();

            _anchorMesh.CreateMesh(_wallAnchors);
        }

        /// <summary>
        /// Returns all the Scene objects in the room. <br/>
        /// Useful if you want to do your own calculations within the Mixed Reality Utility Kit framework.
        /// </summary>
        public List<MRUKAnchor> GetRoomAnchors()
        {
            return _anchors;
        }

        /// <summary>
        /// Get the floor anchor of this room
        /// </summary>
        public MRUKAnchor GetFloorAnchor()
        {
            return _floorAnchor;
        }

        /// <summary>
        /// Get the ceiling anchor of this room
        /// </summary>
        public MRUKAnchor GetCeilingAnchor()
        {
            return _ceilingAnchor;
        }

        /// <summary>
        /// Get the global mesh anchor of this room
        /// </summary>
        public MRUKAnchor GetGlobalMeshAnchor()
        {
            return _globalMeshAnchor;
        }

        /// <summary>
        /// Get the wall anchors of this room
        /// </summary>
        public List<MRUKAnchor> GetWallAnchors()
        {
            return _wallAnchors;
        }

        /// <summary>
        /// (this shouldn't be used by non-MRUK code) <br/>
        /// Calculates a world-space outline of the room.
        /// </summary>
        void CalculateRoomOutline()
        {
            if (!_floorAnchor)
            {
                Debug.Log("Floor anchor not found");
                return;
            }

            _corners.Clear();
            _corners.Capacity = _floorAnchor.PlaneBoundary2D.Count;
            foreach (var point in _floorAnchor.PlaneBoundary2D)
            {
                Vector3 pos = _floorAnchor.transform.TransformPoint(new Vector3(point.x, point.y, 0f));
                // We want the corner outline at exactly 0 elevation
                pos.y = 0f;
                _corners.Add(pos);
            }
        }

        /// <summary>
        /// (this shouldn't be used by non-MRUK code) <br/>
        /// Calculates seat poses (free space on a COUCH object) for humans/avatars. <br/>
        /// Y-up is vertical, Z-forward will point away from the closest WALL_FACE
        /// </summary>
        void CalculateSeatPoses()
        {
            _seatPoses.Clear();
            float seatWidth = MRUK.Instance.SceneSettings.SeatWidth;

            for (int i = 0; i < _anchors.Count; i++)
            {
                if (_anchors[i].HasLabel(OVRSceneManager.Classification.Couch))
                {
                    CouchSeat newSeat = new CouchSeat();
                    newSeat.couchAnchor = _anchors[i];
                    newSeat.couchPoses = new List<Pose>();

                    Vector2 surfaceDim = _anchors[i].PlaneRect?.size ?? Vector2.one;
                    float surfaceRatio = surfaceDim.x / surfaceDim.y;
                    Vector3 seatFwd = GetFacingDirection(_anchors[i]);
                    Vector3 seatUp = Vector3.up;
                    Vector3.OrthoNormalize(ref seatFwd, ref seatUp);
                    if (surfaceRatio < 2.0f && surfaceRatio > 0.5f)
                    {
                        // if the surface dimensions are mostly square (likely a chair), just have one centered seat
                        Pose seatPose = new Pose(_anchors[i].transform.position, Quaternion.LookRotation(seatFwd, seatUp));
                        newSeat.couchPoses.Add(seatPose);

                        _seatPoses.Add(newSeat);
                    }
                    else
                    {
                        bool xLong = surfaceDim.x > surfaceDim.y;

                        float longestDim = xLong ? surfaceDim.x : surfaceDim.y;
                        float numSeats = Mathf.Floor(longestDim / seatWidth);
                        float seatBuffer = (longestDim - (numSeats * seatWidth)) / numSeats;
                        for (int k = 0; k < numSeats; k++)
                        {
                            Vector3 seatRight = xLong ? _anchors[i].transform.right : _anchors[i].transform.up;
                            Vector3 seatPos = _anchors[i].transform.position;
                            // start at the edge
                            seatPos -= seatRight * longestDim * 0.5f;
                            seatPos += seatRight * seatBuffer * 0.5f;
                            // the first seat's position
                            seatPos += seatRight * seatWidth * 0.5f;
                            // now we increment
                            seatPos += seatRight * seatWidth * k;
                            seatPos += seatRight * seatBuffer * k;

                            Pose seatPose = new Pose(seatPos, Quaternion.LookRotation(seatFwd, seatUp));
                            newSeat.couchPoses.Add(seatPose);
                            _seatPoses.Add(newSeat);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns a clockwise (when viewed top-down) list of wall corner points, at floor level.
        /// </summary>
        public List<Vector3> GetRoomOutline()
        {
            return _corners;
        }

        /// <summary>
        /// A Key Wall has two requirements: <br/>
        /// It's the longest wall in the room, and more importantly, has NO other room points behind it. <br/>
        /// With a Key Wall, an artist can craft a more "stage-like" environment, avoiding the obligation of procedural art. <br/>
        /// </summary>
        public MRUKAnchor GetKeyWall(out Vector2 wallScale, float tolerance = 0.1f)
        {
            wallScale = Vector3.one;

            // first, sort the walls in order of x length
            // TODO: this is probably expensive, and shouldn't be called every frame. Cache them?
            List<MRUKAnchor> sortedWalls = new List<MRUKAnchor>(_wallAnchors);
            MRUKAnchor keyWall = null;
            sortedWalls = sortedWalls.OrderBy(w => w.PlaneRect.Value.size.x).ToList();

            // second, find the first one with no other walls behind it
            // count down because the default sorting is from shortest to longest
            for (int i = sortedWalls.Count - 1; i >= 0; i--)
            {
                bool noPointsBehind = true;

                // loop through the other corners, making sure none is behind the wall in question
                for (int k = 0; k < _corners.Count; k++)
                {
                    Vector3 vecToCorner = _corners[k] - sortedWalls[i].transform.position;

                    // due to anchor precision, we use a tolerance value
                    // for example, an adjacent wall edge may be just behind the wall, leading to a false result
                    vecToCorner += sortedWalls[i].transform.forward * tolerance;

                    noPointsBehind &= Vector3.Dot(sortedWalls[i].transform.forward, vecToCorner) >= 0.0f;

                    // if any corner is behind this wall, it already fails and we don't need to test other corners
                    if (!noPointsBehind)
                    {
                        break;
                    }
                }

                // early exit upon finding the first one
                if (noPointsBehind)
                {
                    wallScale = sortedWalls[i].PlaneRect.Value.size;
                    keyWall = sortedWalls[i];
                    break;
                }
            }

            return keyWall;
        }

        /// <summary>
        /// Cast a ray against ONLY Scene API objects, returning all results. <br/>
        /// Use as a replacement for Physics.RaycastAll.
        /// </summary>
        public bool RaycastAll(Ray ray, float maxDist, LabelFilter labelFilter, List<RaycastHit> raycastHits, List<MRUKAnchor> anchorList)
        {
            RaycastHit outHit;
            raycastHits.Clear();
            anchorList.Clear();
            for (int i = 0; i < _anchors.Count; i++)
            {
                if (labelFilter.PassesFilter(_anchors[i].AnchorLabels) && _anchors[i].Raycast(ray, maxDist, out outHit))
                {
                    raycastHits.Add(outHit);
                    anchorList.Add(_anchors[i]);
                }
            }

            return raycastHits.Count > 0;
        }

        /// <summary>
        /// Cast a ray against ONLY Scene API objects, returning the closest result. <br/>
        /// Use as a replacement for Physics.Raycast.
        /// </summary>
        public bool Raycast(Ray ray, float maxDist, LabelFilter labelFilter, out RaycastHit hit, out MRUKAnchor anchor)
        {
            hit = new RaycastHit();
            anchor = null;
            bool hitSomething = false;
            float closestDist = maxDist;

            for (int i = 0; i < _anchors.Count; i++)
            {
                if (labelFilter.PassesFilter(_anchors[i].AnchorLabels) && _anchors[i].Raycast(ray, closestDist, out RaycastHit rayHit))
                {
                    closestDist = rayHit.distance;
                    hit = rayHit;
                    anchor = _anchors[i];
                    hitSomething = true;
                }
            }

            return hitSomething;
        }

        /// <summary>
        /// Cast a ray against ONLY Scene API objects, returning the closest result. <br/>
        /// Use as a replacement for Physics.Raycast.
        /// </summary>
        public bool Raycast(Ray ray, float maxDist, out RaycastHit hit, out MRUKAnchor anchor)
        {
            return Raycast(ray, maxDist, new LabelFilter(), out hit, out anchor);
        }

        /// <summary>
        /// Cast a ray against ONLY Scene API objects, returning the closest result. <br/>
        /// Use as a replacement for Physics.Raycast.
        /// </summary>
        public bool Raycast(Ray ray, float maxDist, LabelFilter labelFilter, out RaycastHit hit)
        {
            return Raycast(ray, maxDist, labelFilter, out hit, out var _);
        }

        /// <summary>
        /// Cast a ray against ONLY Scene API objects, returning the closest result. <br/>
        /// Use as a replacement for Physics.Raycast.
        /// </summary>
        public bool Raycast(Ray ray, float maxDist, out RaycastHit hit)
        {
            return Raycast(ray, maxDist, new LabelFilter(), out hit, out var _);
        }

        /// <summary>
        /// Use this if you want a "suggested" transform from hitting a Scene anchor, i.e. how best to "place an app/widget" on a surface. <br/>
        /// When hitting vertical surfaces; Y is world-up, Z is surface normal. <br/>
        /// When hitting horizontal surfaces; Y is world-up, Z is best-guess at player-facing. <br/>
        /// "best guess" = when on ceiling/floor, Z faces player; when on surface (desk/couch), Z faces closest edge to player
        /// </summary>
        public Pose GetBestPoseFromRaycast(Ray ray, float maxDist, LabelFilter labelFilter, out MRUKAnchor sceneAnchor, out Vector3 surfaceNormal, MRUK.PositioningMethod positioningMethod = MRUK.PositioningMethod.DEFAULT)
        {
            sceneAnchor = null;
            Pose bestPose = new Pose();
            surfaceNormal = Vector3.up;

            RaycastHit closestHit;
            if (Raycast(ray, maxDist, labelFilter, out closestHit, out sceneAnchor))
            {
                Vector3 defaultPose = closestHit.point;
                surfaceNormal = closestHit.normal;
                Vector3 poseUp = Vector3.up;
                // by default, use the surface normal for pose forward
                // caution: make sure all the cases of this being "up" are caught below
                Vector3 poseFwd = closestHit.normal;

                if (Vector3.Dot(closestHit.normal, Vector3.up) >= 0.9f && sceneAnchor.HasVolume)
                {
                    // this is a volume object, and the ray has hit the top surface
                    // "snap" the pose Z to align with the closest edge
                    Vector3 toPlane = ray.origin - sceneAnchor.transform.position;
                    Vector3 planeYup = Vector3.Dot(sceneAnchor.transform.up, toPlane) > 0.0f ? sceneAnchor.transform.up : -sceneAnchor.transform.up;
                    Vector3 planeXup = Vector3.Dot(sceneAnchor.transform.right, toPlane) > 0.0f ? sceneAnchor.transform.right : -sceneAnchor.transform.right;
                    Vector3 planeFwd = sceneAnchor.transform.forward;

                    Vector2 anchorScale = sceneAnchor.VolumeBounds.Value.size;
                    Vector3 nearestCorner = sceneAnchor.transform.position + planeXup * anchorScale.x * 0.5f + planeYup * anchorScale.y * 0.5f;
                    Vector3.OrthoNormalize(ref planeFwd, ref toPlane);
                    nearestCorner -= sceneAnchor.transform.position;
                    bool xUp = Vector3.Angle(toPlane, planeYup) > Vector3.Angle(nearestCorner, planeYup);
                    poseFwd = xUp ? planeXup : planeYup;
                    float offset = xUp ? anchorScale.x : anchorScale.y;
                    switch (positioningMethod)
                    {
                        case MRUK.PositioningMethod.CENTER:
                            defaultPose = sceneAnchor.transform.position;
                            break;
                        case MRUK.PositioningMethod.EDGE:
                            defaultPose = sceneAnchor.transform.position + poseFwd * offset * 0.5f;
                            break;
                        case MRUK.PositioningMethod.DEFAULT:
                            break;
                    }
                }
                else if (Mathf.Abs(Vector3.Dot(closestHit.normal, Vector3.up)) >= 0.9f)
                {
                    // This may be the floor, ceiling or any other horizontal plane surface
                    poseFwd = new Vector3(ray.origin.x - closestHit.point.x, 0, ray.origin.z - closestHit.point.z).normalized;
                }
                bestPose.position = defaultPose;
                bestPose.rotation = Quaternion.LookRotation(poseFwd, poseUp);
            }
            else
            {
                Debug.Log("Best pose not found, no surface anchor detected.");
            }

            return bestPose;
        }

        public Pose GetBestPoseFromRaycast(Ray ray, float maxDist, LabelFilter labelFilter, out MRUKAnchor sceneAnchor, MRUK.PositioningMethod positioningMethod = MRUK.PositioningMethod.DEFAULT)
        {
            Pose bestPose = GetBestPoseFromRaycast(ray, maxDist, labelFilter, out sceneAnchor, out Vector3 surfaceNormal, positioningMethod);
            return bestPose;
        }

        /// <summary>
        /// Test if a position is inside the floor outline of the walls. <br/>
        /// Optionally, test floor/ceiling so the room isn't an "infinity column"
        /// </summary>
        public bool IsPositionInRoom(Vector3 queryPosition, bool testVerticalBounds = true)
        {
            var localPos = _floorAnchor.transform.InverseTransformPoint(queryPosition);
            bool isInRoom = _floorAnchor.IsPositionInBoundary(new Vector2(localPos.x, localPos.y));

            // by default, this just tests the bounds when viewed top-down
            // to truly be a 3D test, also check the floor/ceiling
            if (testVerticalBounds)
            {
                isInRoom &= (queryPosition.y <= GetRoomBounds().size.y && queryPosition.y >= 0);
            }
            return isInRoom;
        }

        /// <summary>
        /// Get a world-oriented bounding box of the room
        /// </summary>
        public Bounds GetRoomBounds()
        {
            return _roomBounds;
        }

        void CalculateRoomBounds(float roomHeight)
        {
            Vector3 roomSize = Vector3.one;
            float xMin = 0.0f;
            float xMax = 0.0f;
            float zMin = 0.0f;
            float zMax = 0.0f;
            if (_corners != null)
            {
                for (int i = 0; i < _corners.Count; i++)
                {
                    xMin = Mathf.Min(xMin, _corners[i].x);
                    xMax = Mathf.Max(xMax, _corners[i].x);
                    zMin = Mathf.Min(zMin, _corners[i].z);
                    zMax = Mathf.Max(zMax, _corners[i].z);
                }
            }
            roomSize.x = xMax - xMin;
            roomSize.y = roomHeight;
            roomSize.z = zMax - zMin;

            Vector3 roomCenter = new Vector3((xMax + xMin) * 0.5f, roomHeight * 0.5f, (zMax + zMin) * 0.5f);
            _roomBounds = new Bounds(roomCenter, roomSize);
        }

        /// <summary>
        /// Test if a position is inside of a Scene volume, and optionally return the object. <br/>
        /// To also check if a position is inside the room walls, use IsPositionInRoom(). <br/>
        /// Use distanceBuffer as a cheap way to check volume intersection.
        /// </summary>
        public bool IsPositionInSceneVolume(Vector3 worldPosition, out MRUKAnchor sceneObject, bool testVerticalBounds, float distanceBuffer = 0.0f)
        {
            bool isInObject = false;
            sceneObject = null;
            for (int i = 0; i < _anchors.Count; i++)
            {
                if (_anchors[i].IsPositionInVolume(worldPosition, testVerticalBounds, distanceBuffer))
                {
                    isInObject = true;
                    sceneObject = _anchors[i];
                    break;
                }
            }
            return isInObject;
        }


        /// <summary>
        /// Get a "likely" direction this anchor is facing. For planes, this is always the normal (Z-forward).
        /// For volumes, we use contextual clues; primarily, the closest wall is the "back" of the volume,
        /// and the facing direction is its axis most aligned with this wall normal.
        /// </summary>
        public Vector3 GetFacingDirection(MRUKAnchor anchor)
        {
            // For planes, just use the anchor Z
            if (!anchor.HasVolume)
            {
                return anchor.transform.forward;
            }

            return GetDirectionAwayFromClosestWall(anchor, out var _);
        }

        internal Vector3 GetDirectionAwayFromClosestWall(MRUKAnchor anchor, out int cardinalAxisIndex, List<int> excludedAxes = null)
        {
            float closestWallDistance = Mathf.Infinity;
            // Due to the odd rotation of anchors, we need to use transform.up here instead of transform.forward
            // as forward actually points upwards.
            Vector3 awayFromWall = anchor.transform.up;
            cardinalAxisIndex = 0;
            for (int i = 0; i < 4; i++)
            {
                if (excludedAxes != null && excludedAxes.Contains(i))
                {
                    continue;
                }
                // shoot rays along cardinal directions
                Vector3 cardinalAxis = Quaternion.Euler(0, 90f * i, 0) * -anchor.transform.up;

                foreach (var wallAnchor in _wallAnchors)
                {
                    RaycastHit outHit;
                    if (wallAnchor.Raycast(new Ray(anchor.transform.position, cardinalAxis), closestWallDistance, out outHit))
                    {
                        closestWallDistance = outHit.distance;
                        // whichever wall is closest, point Z-forward away from it
                        cardinalAxisIndex = i;
                        awayFromWall = -cardinalAxis;
                    }
                }
            }
            return awayFromWall;
        }

        /// <summary>
        /// Test if a position is inside of a Scene volume (couch, desk, etc.). <br/>
        /// To also check if a position is inside the room walls, use IsPositionInRoom(). <br/>
        /// Use distanceBuffer as a cheap way to check volume intersection.
        /// </summary>
        public bool IsPositionInSceneVolume(Vector3 worldPosition, float distanceBuffer = 0.0f)
        {
            bool isInObject = IsPositionInSceneVolume(worldPosition, out _, true, distanceBuffer);
            return isInObject;
        }

        /// <summary>
        /// Test if a position is inside of a Scene volume (couch, desk, etc.). <br/>
        /// To also check if a position is inside the room walls, use IsPositionInRoom(). <br/>
        /// Use distanceBuffer as a cheap way to check volume intersection.
        /// </summary>
        public bool IsPositionInSceneVolume(Vector3 worldPosition, bool testVerticalBounds, float distanceBuffer = 0.0f)
        {
            bool isInObject = IsPositionInSceneVolume(worldPosition, out _, testVerticalBounds, distanceBuffer);
            return isInObject;
        }

        /// <summary>
        /// Returns the best-suggested seat, for something like remote caller placement.
        /// </summary>
        public bool TryGetClosestSeatPose(Ray ray, out Pose seatPose, out MRUKAnchor couch)
        {
            Pose bestPose = new Pose();
            couch = null;

            float closestDot = -1.0f;
            for (int i = 0; i < _seatPoses.Count; i++)
            {
                for (int k = 0; k < _seatPoses[i].couchPoses.Count; k++)
                {
                    Vector3 vecToSeat = (_seatPoses[i].couchPoses[k].position - ray.origin).normalized;
                    float thisDot = Vector3.Dot(ray.direction, vecToSeat);
                    if (thisDot > closestDot)
                    {
                        closestDot = thisDot;
                        bestPose.position = _seatPoses[i].couchPoses[k].position;
                        bestPose.rotation = _seatPoses[i].couchPoses[k].rotation;
                        couch = _seatPoses[i].couchAnchor;
                    }
                }
            }
            seatPose.position = bestPose.position;
            seatPose.rotation = bestPose.rotation;

            return (_seatPoses.Count > 0);
        }

        /// <summary>
        /// Returns all seats in the room (a human-spaced position on a COUCH).
        /// </summary>
        public Pose[] GetSeatPoses()
        {
            List<Pose> poses = new List<Pose>();
            for (int i = 0; i < _seatPoses.Count; i++)
            {
                for (int k = 0; k < _seatPoses[i].couchPoses.Count; k++)
                {
                    poses.Add(_seatPoses[i].couchPoses[k]);
                }
            }

            return poses.ToArray();
        }

        /// <summary>
        /// Return the parent of an anchor, if it exists. <br/>
        /// This hierarchical relationship is by reference, not literally in the scene. <br/>
        /// </summary>
        public bool TryGetAnchorParent(MRUKAnchor queryAnchor, out MRUKAnchor parentAnchor)
        {
            parentAnchor = queryAnchor.ParentAnchor;
            return (parentAnchor != null);
        }

        /// <summary>
        /// Returns the logical children of an anchor, if there are any. <br/>
        /// This hierarchical relationship is by reference, not literally in the scene. <br/>
        /// </summary>
        public bool TryGetAnchorChildren(MRUKAnchor queryAnchor, out MRUKAnchor[] childAnchors)
        {
            childAnchors = queryAnchor.ChildAnchors?.ToArray();
            return (childAnchors != null && childAnchors.Length > 0);
        }

        /// <summary>
        /// (internal only) <br/>
        /// One-time calcuation, finds parent-child relationships between anchors. <br/>
        /// Because this relationship isn't a literal scene-graph hierarchy, we can't just use transform.parent or transform.GetChild()
        /// </summary>
        void CalculateHierarchyReferences()
        {
            const float coPlanarTolerance = 0.1f;
            for (int i = 0; i < _anchors.Count; i++)
            {
                _anchors[i].ClearChildReferences();
                if (_anchors[i].HasLabel(OVRSceneManager.Classification.WallFace))
                {
                    // find all _anchors that are a "child" of this wall using heuristics
                    for (int k = 0; k < _anchors.Count; k++)
                    {
                        if (_anchors[k].HasPlane && !_anchors[k].HasVolume)
                        {
                            float angle = Vector3.Angle(_anchors[k].transform.right, _anchors[i].transform.right);
                            // first check if they're co-planar (X-axes closely align)
                            bool alignsWithWall = (angle <= 5.0f);
                            // then check if it's close enough to the wall in local-Z
                            Vector3 localPos = _anchors[i].transform.InverseTransformPoint(_anchors[k].transform.position);
                            bool positionedOnWall = Mathf.Abs(localPos.z) / Mathf.Abs(localPos.x) <= coPlanarTolerance;
                            // then check if the center is within the bounds
                            // (checking each edge should be unnecessary, since they must be created on the wall via Room Setup)
                            float xScale = _anchors[i].PlaneRect.Value.size.x;
                            bool withinWall = Mathf.Abs(localPos.x) < xScale * 0.5f;

                            // through these checks, we should have very high confidence that these anchors are related, even if the individual tolerances are generous
                            if (alignsWithWall && positionedOnWall && withinWall)
                            {
                                // take careful note of the iterators (i,k)
                                _anchors[i].AddChildReference(_anchors[k]);
                                _anchors[k].ParentAnchor = _anchors[i];
                            }
                        }
                    }
                }
                else if (_anchors[i].HasLabel(OVRSceneManager.Classification.Floor))
                {
                    // check volumes that are on the floor (should be all volumes, unless volumes are stacked)
                    for (int k = 0; k < _anchors.Count; k++)
                    {
                        if (_anchors[k].HasVolume)
                        {
                            Vector3 volumeCenterBottom = _anchors[k].transform.position - Vector3.up * _anchors[k].VolumeBounds.Value.size.z;

                            bool volumeOnFloor = Mathf.Abs(_anchors[i].transform.position.y - volumeCenterBottom.y) <= coPlanarTolerance;

                            if (volumeOnFloor)
                            {
                                // take careful note of the iterators (i,k)
                                _anchors[i].AddChildReference(_anchors[k]);
                                _anchors[k].ParentAnchor = _anchors[i];
                            }
                        }
                    }

                }
                else if (_anchors[i].HasVolume)
                {
                    Bounds parentVolumeBounds = _anchors[i].VolumeBounds.Value;

                    // treat this anchor (i) as a parent, and search for a child (k)
                    for (int k = 0; k < _anchors.Count; k++)
                    {
                        if (_anchors[k].HasVolume)
                        {
                            Bounds childVolumeBounds = _anchors[k].VolumeBounds.Value;
                            Vector3 childAnchorBottom = _anchors[k].transform.position - Vector3.up * childVolumeBounds.size.z;

                            // if the child's bottom is coplanar with the parent's top, this is likely a hierarchy
                            bool isOnTop = Mathf.Abs(_anchors[i].transform.position.y - childAnchorBottom.y) <= coPlanarTolerance;

                            if (isOnTop)
                            {
                                // still need to check to ensure at least one corner is within the bounds of the parent candidate bounds
                                bool anyCornerInside = false;
                                for (int c = 0; c < 4; ++c)
                                {
                                    // Get a different corner on each iteration of the loop (height is not important here)
                                    Vector3 cornerPos = new Vector3(i < 2 ? childVolumeBounds.min.x : childVolumeBounds.max.x, i % 2 == 0 ? childVolumeBounds.min.y : childVolumeBounds.max.y, 0.0f);
                                    // convert corner to world space
                                    cornerPos = _anchors[k].transform.TransformPoint(cornerPos);

                                    Vector3 parentRelativeCorner = _anchors[i].transform.InverseTransformPoint(cornerPos);
                                    if (parentRelativeCorner.x >= parentVolumeBounds.min.x && parentRelativeCorner.x <= parentVolumeBounds.max.x &&
                                        parentRelativeCorner.y >= parentVolumeBounds.min.y && parentRelativeCorner.y <= parentVolumeBounds.max.y)
                                    {
                                        anyCornerInside = true;
                                        break;
                                    }
                                }
                                if (anyCornerInside)
                                {
                                    // take careful note of the iterators (i,k)
                                    _anchors[i].AddChildReference(_anchors[k]);
                                    _anchors[k].ParentAnchor = _anchors[i];
                                }
                            }

                        }
                    }
                }
            }
        }

        /// <summary>
        /// See if a room has all the provided Scene API labels.
        /// </summary>
        public bool DoesRoomHave(string[] labels)
        {
            List<string> roomLabels = new List<string>();
            for (int i = 0; i < _anchors.Count; i++)
            {
                foreach (string label in _anchors[i].AnchorLabels)
                {
                    roomLabels.Add(label);
                }
            }

            IEnumerable<string> both = labels.Intersect(roomLabels);

            return labels.Count() > 0 && both.Count() == labels.Count();
        }

        /// <summary>
        /// The closest position on a SceneAPI surface.
        /// </summary>
        public float TryGetClosestSurfacePosition(Vector3 worldPosition, out Vector3 surfacePosition, out MRUKAnchor closestAnchor, LabelFilter labelFilter = new LabelFilter())
        {
            float distance = Mathf.Infinity;
            surfacePosition = Vector3.zero;
            closestAnchor = null;

            for (int i = 0; i < _anchors.Count; i++)
            {
                if (!labelFilter.PassesFilter(_anchors[i].AnchorLabels))
                {
                    continue;
                }

                float dist = _anchors[i].GetClosestSurfacePosition(worldPosition, out Vector3 thisSurfPos);
                if (dist < distance)
                {
                    distance = dist;
                    surfacePosition = thisSurfPos;
                    closestAnchor = _anchors[i].GetComponent<MRUKAnchor>();
                }
            }
            return distance;
        }

        /// <summary>
        /// Returns the anchor with the largest available surface area.
        /// A bit more flexible than HasTableSpace, can be adapted for other usage
        /// </summary>
        public MRUKAnchor FindLargestSurface(string anchorLabel)
        {
            MRUKAnchor largestAnchor = null;
            float largestSurfaceArea = 0;
            foreach (var anchor in _anchors)
            {
                if (!anchor.HasLabel(anchorLabel))
                {
                    continue;
                }

                float thisSurfaceArea = 0f;

                if (anchor.HasPlane)
                {
                    Vector2 quadScale = anchor.PlaneRect.Value.size;
                    thisSurfaceArea = quadScale.x * quadScale.y;
                }
                else if (anchor.HasVolume)
                {
                    Vector3 volumeSize = anchor.VolumeBounds.Value.size;
                    thisSurfaceArea = volumeSize.x * volumeSize.y;
                }
                if (thisSurfaceArea > largestSurfaceArea)
                {
                    largestSurfaceArea = thisSurfaceArea;
                    largestAnchor = anchor;
                }
            }
            return largestAnchor;
        }

        /// <summary>
        /// Generate a random position in a room, while avoiding volume scene
        /// objects and points that are too close to surfaces.
        /// This function uses random sampling and a maximum number of iterations.
        /// </summary>
        /// <param name="minDistanceToSurface">Reject points whose proximity to
        /// a surface is less than the parameter.</param>
        /// <param name="avoidVolumes">Do not allow points to be within volume
        /// scene objects.</param>
        /// <returns>A position that adhers to the constraints, null otherwise.</returns>
        public Vector3? GenerateRandomPositionInRoom(float minDistanceToSurface, bool avoidVolumes)
        {
            if (!_floorAnchor)
            {
                return null;
            }
            Vector3 extents = _roomBounds.extents;
            float minExtent = Mathf.Min(extents.x, extents.y, extents.z);
            if (minDistanceToSurface > minExtent)
            {
                // We can exit early here as we know it's not possible to generate a position in the room that satisfies
                // the minDistanceToSurface requirement
                return null;
            }
            const int maxIterations = 1000;
            // Bail after MaxIteration tries to avoid infinite loop in case MinDistanceToSurface is too large
            // and we can't find a position which does not intersect with the walls and volumes
            for (int i = 0; i < maxIterations; ++i)
            {
                Vector3 spawnPosition = new Vector3(
                    UnityEngine.Random.Range(_roomBounds.min.x + minDistanceToSurface, _roomBounds.max.x - minDistanceToSurface),
                    UnityEngine.Random.Range(_roomBounds.min.y + minDistanceToSurface, _roomBounds.max.y - minDistanceToSurface),
                    UnityEngine.Random.Range(_roomBounds.min.z + minDistanceToSurface, _roomBounds.max.z - minDistanceToSurface)
                );
                if (!IsPositionInRoom(spawnPosition))
                {
                    // Reject points that are outside the room
                    continue;
                }
                LabelFilter filter = LabelFilter.Included(new List<string> { OVRSceneManager.Classification.WallFace });
                float closestDist = TryGetClosestSurfacePosition(spawnPosition, out Vector3 _, out MRUKAnchor _, filter);
                if (closestDist <= minDistanceToSurface)
                {
                    // Reject points that are too close to the walls
                    continue;
                }
                if (avoidVolumes && IsPositionInSceneVolume(spawnPosition, minDistanceToSurface))
                {
                    // Reject points inside volumes if avoid volumes has been enabled
                    continue;
                }
                return spawnPosition;
            }
            return null;
        }

        /// <summary>
        /// Generate a position on any valid surface in the room, limited by the type
        /// of surface and the classification of the object.
        /// </summary>
        /// <param name="surfaceTypes">The type of surface by which to limit
        /// the generation.</param>
        /// <param name="minDistanceToEdge">Limit the generated point to
        /// not being close to a surface's edges and corners.</param>
        /// <param name="labelFilter">The labels to include</param>
        /// <param name="position">The generated position.
        /// <see cref="Vector3.zero"/> is returned if no position was
        /// generated.</param>
        /// <param name="normal">The generated surface normal.
        /// <see cref="Vector3.zero"/> is returned if nothing was
        /// generated.</param>
        /// <returns>True if a position was found, false otherwise.</returns>
        public bool GenerateRandomPositionOnSurface(MRUK.SurfaceType surfaceTypes, float minDistanceToEdge, LabelFilter labelFilter, out Vector3 position, out Vector3 normal)
        {
            List<Surface> surfaces = new();
            float totalUsableSurfaceArea = 0f;
            float minWidth = 2f * minDistanceToEdge;

            // define these as the negative early exit conditions
            position = Vector3.zero;
            normal = Vector3.zero;

            foreach (var anchor in _anchors)
            {
                if (!labelFilter.PassesFilter(anchor.AnchorLabels))
                {
                    continue;
                }
                if (anchor.HasPlane)
                {
                    bool skipPlane = false;
                    if (anchor.transform.forward.y >= Utilities.InvSqrt2)
                    {
                        if ((surfaceTypes & MRUK.SurfaceType.FACING_UP) == 0)
                        {
                            skipPlane = true;
                        }
                    }
                    else if (anchor.transform.forward.y <= -Utilities.InvSqrt2)
                    {
                        if ((surfaceTypes & MRUK.SurfaceType.FACING_DOWN) == 0)
                        {
                            skipPlane = true;
                        }
                    }
                    else if ((surfaceTypes & MRUK.SurfaceType.VERTICAL) == 0)
                    {
                        skipPlane = true;
                    }
                    if (!skipPlane)
                    {
                        var size = anchor.PlaneRect.Value.size;
                        if (size.x > minWidth && size.y > minWidth)
                        {
                            var usableArea = (size.x - minWidth) * (size.y - minWidth);
                            totalUsableSurfaceArea += usableArea;
                            surfaces.Add(new()
                            {
                                Anchor = anchor,
                                UsableArea = usableArea,
                                IsPlane = true,
                                Bounds = anchor.PlaneRect.Value,
                                Transform = anchor.transform.localToWorldMatrix
                            });
                        }
                    }
                }
                if (anchor.HasVolume)
                {
                    for (int i = 0; i < 6; ++i)
                    {
                        Rect bounds;
                        Matrix4x4 faceTransform;
                        if (i == 0)
                        {
                            if ((surfaceTypes & MRUK.SurfaceType.FACING_UP) == 0)
                            {
                                continue;
                            }
                        }
                        else if (i == 1)
                        {
                            if ((surfaceTypes & MRUK.SurfaceType.FACING_DOWN) == 0)
                            {
                                continue;
                            }
                        }
                        else if ((surfaceTypes & MRUK.SurfaceType.VERTICAL) == 0)
                        {
                            continue;
                        }
                        switch (i)
                        {
                            case 0:
                                // +Z face
                                bounds = new()
                                {
                                    xMin = anchor.VolumeBounds.Value.min.x,
                                    xMax = anchor.VolumeBounds.Value.max.x,
                                    yMin = anchor.VolumeBounds.Value.min.y,
                                    yMax = anchor.VolumeBounds.Value.max.y
                                };
                                faceTransform = Matrix4x4.TRS(new Vector3(0f, 0f, anchor.VolumeBounds.Value.max.z), Quaternion.identity, Vector3.one);
                                break;
                            case 1:
                                // -Z face
                                bounds = new()
                                {
                                    xMin = -anchor.VolumeBounds.Value.max.x,
                                    xMax = -anchor.VolumeBounds.Value.min.x,
                                    yMin = anchor.VolumeBounds.Value.min.y,
                                    yMax = anchor.VolumeBounds.Value.max.y
                                };
                                faceTransform = Matrix4x4.TRS(new Vector3(0f, 0f, anchor.VolumeBounds.Value.min.z), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
                                break;
                            case 2:
                                // +X face
                                bounds = new()
                                {
                                    xMin = -anchor.VolumeBounds.Value.max.z,
                                    xMax = -anchor.VolumeBounds.Value.min.z,
                                    yMin = anchor.VolumeBounds.Value.min.y,
                                    yMax = anchor.VolumeBounds.Value.max.y
                                };
                                faceTransform = Matrix4x4.TRS(new Vector3(anchor.VolumeBounds.Value.max.x, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
                                break;
                            case 3:
                                // -X face
                                bounds = new()
                                {
                                    xMin = anchor.VolumeBounds.Value.min.z,
                                    xMax = anchor.VolumeBounds.Value.max.z,
                                    yMin = anchor.VolumeBounds.Value.min.y,
                                    yMax = anchor.VolumeBounds.Value.max.y
                                };
                                faceTransform = Matrix4x4.TRS(new Vector3(anchor.VolumeBounds.Value.min.x, 0f, 0f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
                                break;
                            case 4:
                                // +Y face
                                bounds = new()
                                {
                                    xMin = anchor.VolumeBounds.Value.min.x,
                                    xMax = anchor.VolumeBounds.Value.max.x,
                                    yMin = -anchor.VolumeBounds.Value.max.z,
                                    yMax = -anchor.VolumeBounds.Value.min.z
                                };
                                faceTransform = Matrix4x4.TRS(new Vector3(0f, anchor.VolumeBounds.Value.max.y, 0f), Quaternion.Euler(-90f, 0f, 0f), Vector3.one);
                                break;
                            case 5:
                                // -Y face
                                bounds = new()
                                {
                                    xMin = anchor.VolumeBounds.Value.min.x,
                                    xMax = anchor.VolumeBounds.Value.max.x,
                                    yMin = anchor.VolumeBounds.Value.min.z,
                                    yMax = anchor.VolumeBounds.Value.max.z
                                };
                                faceTransform = Matrix4x4.TRS(new Vector3(0f, anchor.VolumeBounds.Value.min.y, 0f), Quaternion.Euler(90f, 0f, 0f), Vector3.one);
                                break;
                            default:
                                throw new SwitchExpressionException();
                        }

                        var size = bounds.size;
                        if (size.x > minWidth && size.y > minWidth)
                        {
                            var usableArea = (size.x - minWidth) * (size.y - minWidth);
                            totalUsableSurfaceArea += usableArea;
                            surfaces.Add(new()
                            {
                                Anchor = anchor,
                                UsableArea = usableArea,
                                IsPlane = false,
                                Bounds = bounds,
                                Transform = anchor.transform.localToWorldMatrix * faceTransform
                            });
                        }
                    }
                }
            }

            if (surfaces.Count == 0)
                return false;

            const int maxIterations = 1000;
            for (int i = 0; i < maxIterations; ++i)
            {
                // Pick a random surface weighted by surface area (surfaces with a larger
                // area have more chance of being chosen)
                var rand = UnityEngine.Random.Range(0, totalUsableSurfaceArea);
                int index = 0;
                for (; index < surfaces.Count - 1; ++index)
                {
                    rand -= surfaces[index].UsableArea;
                    if (rand <= 0.0f)
                    {
                        break;
                    }
                }

                var surface = surfaces[index];
                var bounds = surface.Bounds;
                Vector2 pos = new Vector2(
                    UnityEngine.Random.Range(bounds.xMin + minDistanceToEdge, bounds.xMax - minDistanceToEdge),
                    UnityEngine.Random.Range(bounds.yMin + minDistanceToEdge, bounds.yMax - minDistanceToEdge)
                );
                if (surface.IsPlane && !surface.Anchor.IsPositionInBoundary(pos))
                {
                    continue;
                }
                position = surface.Transform.MultiplyPoint3x4(new Vector3(pos.x, pos.y, 0f));
                normal = surface.Transform.MultiplyVector(Vector3.forward);
                return true;
            }

            return false;
        }

        internal void UpdateWorldLock(OVRCameraRig camera)
        {
            _anchorMesh.UpdateWorldLock(camera);
        }

        void OnDestroy()
        {
            MRUK.Instance?.OnRoomDestroyed(this);
        }
    }
}
