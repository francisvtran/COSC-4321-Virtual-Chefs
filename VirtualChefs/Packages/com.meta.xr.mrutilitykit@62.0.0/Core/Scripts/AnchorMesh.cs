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
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("meta.xr.mrutilitykit.tests")]
namespace Meta.XR.MRUtilityKit
{
    public class AnchorMesh
    {
        // Node structure to represent nodes in the mesh
        internal struct Node
        {
            public MRUKAnchor Anchor;
            public Vector2 Position;
            public Node(MRUKAnchor _anchor, Vector2 _position)
            {
                Anchor = _anchor;
                Position = _position;
            }
        };

        // Edge structure to represent edges between points
        internal struct Edge
        {
            public int P1; // Index of the first point
            public int P2; // Index of the second point
            public Edge(int _p1, int _p2)
            {
                P1 = _p1;
                P2 = _p2;
            }
        };

        // Triangle structure to represent triangles formed by points
        internal struct Triangle
        {
            public int P1; // Index of the first point
            public int P2; // Index of the second point
            public int P3; // Index of the third point
            public Triangle(int _p1, int _p2, int _p3)
            {
                P1 = _p1;
                P2 = _p2;
                P3 = _p3;
            }
        };

        internal List<Node> _nodes = new();
        internal List<Triangle> _triangles = new();

        public void CreateMesh(List<MRUKAnchor> anchors)
        {
            _nodes.Clear();
            _triangles.Clear();

            _nodes.Capacity = anchors.Count;
            Bounds bounds = new();
            foreach (var Anchor in anchors)
            {
                if (!Anchor)
                {
                    continue;
                }
                var anchorPosition = Anchor.transform.position;
                Vector2 position2D = new Vector2(anchorPosition.x, anchorPosition.z);
                _nodes.Add(new Node(Anchor, position2D));
                bounds.Encapsulate(anchorPosition);
            }

            if (_nodes.Count == 0)
            {
                return;
            }

            // Delaunay triangulation using Bowyer-Watson algorithm

            // Add a super-triangle that contains all points to ensure a convex hull
            float maxSize = bounds.size.magnitude;
            var center = new Vector2(bounds.center.x, bounds.center.z);

            var p1 = new Vector2(center.x - 20 * maxSize, center.y - maxSize);
            var p2 = new Vector2(center.x, center.y + 20 * maxSize);
            var p3 = new Vector2(center.x + 20 * maxSize, center.y - maxSize);

            int numPoints = _nodes.Count;

            _triangles.Add(new Triangle(numPoints, numPoints + 1, numPoints + 2));

            _nodes.Add(new Node(null, p1));
            _nodes.Add(new Node(null, p2));
            _nodes.Add(new Node(null, p3));

            // Incremental insertion of points
            List<Edge> edges = new();
            for (int i = 0; i < numPoints; ++i)
            {
                edges.Clear();
                for (int j = _triangles.Count - 1; j >= 0; --j)
                {
                    var triangle = _triangles[j];
                    if (IsInsideCircumcircle(triangle, _nodes[i].Position))
                    {
                        AddEdgeToConvexHull(edges, new Edge(triangle.P1, triangle.P2));
                        AddEdgeToConvexHull(edges, new Edge(triangle.P2, triangle.P3));
                        AddEdgeToConvexHull(edges, new Edge(triangle.P3, triangle.P1));
                        _triangles.RemoveAt(j);
                    }
                }

                // Create new Triangles and update edges
                foreach (var edge in edges)
                {
                    _triangles.Add(new Triangle(edge.P1, edge.P2, i));
                }
            }

            // Remove the super-triangle nodes
            _nodes.RemoveRange(numPoints, 3);

            // Remove Triangles that include the super-triangle vertices
            _triangles.RemoveAll(t => t.P1 >= numPoints || t.P2 >= numPoints || t.P3 >= numPoints);
        }

        public void UpdateWorldLock(OVRCameraRig camera)
        {
            if (_nodes.Count == 0 || _triangles.Count == 0)
            {
                return;
            }

            Vector3 headWorldPosition = camera.centerEyeAnchor.position;
            var headPosition2D = new Vector2(headWorldPosition.x, headWorldPosition.z);

            Triangle triangle = FindClosestTriangle(headPosition2D, out var barycentric);

            Vector3 weightedOffset = new();
            Vector2 weightedYawVector = new();

            for (int i = 0; i < 3; i++)
            {
                int triangleIndex;
                float weight;
                switch (i)
                {
                    case 0:
                        triangleIndex = triangle.P1;
                        weight = barycentric.x;
                        break;
                    case 1:
                        triangleIndex = triangle.P2;
                        weight = barycentric.y;
                        break;
                    case 2:
                        triangleIndex = triangle.P3;
                        weight = barycentric.z;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }

                var anchor = _nodes[triangleIndex].Anchor;

                if (anchor.Anchor == OVRAnchor.Null ||
                   !anchor.Anchor.TryGetComponent<OVRLocatable>(out var locatable) ||
                   !locatable.TryGetSceneAnchorPose(out var pose))
                {
                    continue;
                }

                var anchorTransform = Matrix4x4.TRS(pose.Position.Value, pose.Rotation.Value, Vector3.one);

                var adjustment = anchor.transform.localToWorldMatrix * anchorTransform.inverse;

                weightedOffset += adjustment.GetPosition() * weight;

                // Convert angles to 2D unit vectors and sum them up, weighted by their respective weights.
                float radianAngle = adjustment.rotation.eulerAngles.y * Mathf.Deg2Rad;
                Vector2 angleVector = new(Mathf.Cos(radianAngle), Mathf.Sin(radianAngle));
                weightedYawVector += angleVector * weight;
            }

            float weightedYaw = Mathf.Atan2(weightedYawVector.y, weightedYawVector.x) * Mathf.Rad2Deg;
            camera.trackingSpace.localPosition = weightedOffset;
            camera.trackingSpace.localRotation = Quaternion.Euler(0, weightedYaw, 0);
        }

        float CrossProduct(Vector2 p1, Vector2 p2)
        {
            return p1.x * p2.y - p1.y * p2.x;
        }

        bool IsInsideCircumcircle(Triangle triangle, Vector2 p)
        {
            var p1 = _nodes[triangle.P1].Position;
            var p2 = _nodes[triangle.P2].Position;
            var p3 = _nodes[triangle.P3].Position;

            var a = p1 - p;
            var b = p2 - p;
            var c = p3 - p;

            float det = a.sqrMagnitude * CrossProduct(b, c) + b.sqrMagnitude * CrossProduct(c, a) + c.sqrMagnitude * CrossProduct(a, b);

            // Clockwise orientation indicates the point is inside the circumcircle
            return det < 0;
        }

        internal Triangle FindClosestTriangle(Vector2 targetPoint, out Vector3 barycentric)
        {
            float minDistanceSquared = Mathf.Infinity;
            int closestTriangleIndex = -1;
            barycentric = new Vector3();

            for (int i = 0; i < _triangles.Count; ++i)
            {
                var p1 = _nodes[_triangles[i].P1].Position;
                var p2 = _nodes[_triangles[i].P2].Position;
                var p3 = _nodes[_triangles[i].P3].Position;

                Vector3 currentBarycentric = CalculateBarycentricCoordinates(p1, p2, p3, targetPoint);

                var pos = currentBarycentric.x * p1 + currentBarycentric.y * p2 + currentBarycentric.z * p3;

                float distanceSquared = Vector3.SqrMagnitude(pos - targetPoint);

                if (distanceSquared < minDistanceSquared)
                {
                    minDistanceSquared = distanceSquared;
                    closestTriangleIndex = i;
                    barycentric = currentBarycentric;
                }
            }

            if (closestTriangleIndex == -1)
            {
                return new Triangle();
            }

            return _triangles[closestTriangleIndex];
        }

        void AddEdgeToConvexHull(List<Edge> edges, Edge newEdge)
        {
            // If the edge is already in the list then we are going to remove it
            for (int i = 0; i < edges.Count; ++i)
            {
                var edge = edges[i];
                if ((edge.P1 == newEdge.P1 && edge.P2 == newEdge.P2) || (edge.P1 == newEdge.P2 && edge.P2 == newEdge.P1))
                {
                    edges.RemoveAt(i);
                    return;
                }
            }
            // Otherwise add it
            edges.Add(newEdge);
        }

        Vector3 CalculateBarycentricCoordinates(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p)
        {
            var totalArea = 0.5f * CrossProduct(p2 - p1, p3 - p1);

            // Calculate areas of sub-triangles formed by point P and the triangle vertices
            var area1 = 0.5f * CrossProduct(p2 - p, p3 - p);
            var area2 = 0.5f * CrossProduct(p - p1, p3 - p1);
            var area3 = totalArea - area1 - area2;

            Vector3 barycentric;
            // Calculate barycentric coordinates
            barycentric.x = area1 / totalArea;
            barycentric.y = area2 / totalArea;
            barycentric.z = area3 / totalArea;

            if (barycentric.x < 0)
            {
                var edge = p3 - p2;
                float t = Vector2.Dot(p - p2, edge) / edge.sqrMagnitude;
                t = Mathf.Clamp(t, 0.0f, 1.0f);
                barycentric.x = 0.0f;
                barycentric.y = 1.0f - t;
                barycentric.z = t;
            }
            else if (barycentric.y < 0)
            {
                var edge = p1 - p3;
                float t = Vector2.Dot(p - p3, edge) / edge.sqrMagnitude;
                t = Mathf.Clamp(t, 0.0f, 1.0f);
                barycentric.x = t;
                barycentric.y = 0.0f;
                barycentric.z = 1.0f - t;
            }
            else if (barycentric.z < 0)
            {
                var edge = p2 - p1;
                float t = Vector2.Dot(p - p1, edge) / edge.sqrMagnitude;
                t = Mathf.Clamp(t, 0.0f, 1.0f);
                barycentric.x = 1.0f - t;
                barycentric.y = t;
                barycentric.z = 0.0f;
            }

            return barycentric;
        }

        internal void DrawGizmos()
        {
            Gizmos.color = Color.blue;
            foreach (var triangle in _triangles)
            {
                var P1 = new Vector3(_nodes[triangle.P1].Position.x, 0, _nodes[triangle.P1].Position.y);
                var P2 = new Vector3(_nodes[triangle.P2].Position.x, 0, _nodes[triangle.P2].Position.y);
                var P3 = new Vector3(_nodes[triangle.P3].Position.x, 0, _nodes[triangle.P3].Position.y);
                Gizmos.DrawLine(P1, P2);
                Gizmos.DrawLine(P2, P3);
                Gizmos.DrawLine(P3, P1);
            }
        }
    }

}
