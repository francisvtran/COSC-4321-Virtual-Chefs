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
using System.Collections.Generic;

namespace Meta.XR.MRUtilityKit
{
    public static class Triangulator
    {
        static bool IsConvex(Vector2 prevPoint, Vector2 currPoint, Vector2 nextPoint)
        {
            Vector2 edge1 = prevPoint - currPoint;
            Vector2 edge2 = nextPoint - currPoint;

            float crossProductZ = edge1.x * edge2.y - edge1.y * edge2.x;

            return crossProductZ <= 0;
        }

        static bool PointInTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
        {
            Vector2 ab = b - a;
            Vector2 bc = c - b;
            Vector2 ca = a - c;

            Vector2 ap = p - a;
            Vector2 bp = p - b;
            Vector2 cp = p - c;

            float crossProductZ1 = ab.x * ap.y - ab.y * ap.x;
            float crossProductZ2 = bc.x * bp.y - bc.y * bp.x;
            float crossProductZ3 = ca.x * cp.y - ca.y * cp.x;

            return (crossProductZ1 >= 0) && (crossProductZ2 >= 0) && (crossProductZ3 >= 0);
        }

        static bool IsEar(List<Vector2> vertices, List<int> indices, int prevIndex, int currIndex, int nextIndex)
        {
            int numPoints = indices.Count;

            Vector2 prevPoint = vertices[prevIndex];
            Vector2 currPoint = vertices[currIndex];
            Vector2 nextPoint = vertices[nextIndex];

            for (int i = 0; i < numPoints; ++i)
            {
                int index = indices[i];
                if (index != prevIndex && index != currIndex && index != nextIndex)
                {
                    Vector2 testPoint = vertices[index];

                    if (PointInTriangle(prevPoint, currPoint, nextPoint, testPoint))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // Ear clipping algorithm to triangulate the boundary
        public static List<int> TriangulatePoints(List<Vector2> vertices)
        {
            List<int> indices = new();
            List<int> triangles = new();
            int numTriangles = Mathf.Max(vertices.Count - 2, 0);
            triangles.Capacity = 3 * numTriangles;

            indices.Capacity = vertices.Count;
            for (int i = 0; i < vertices.Count; ++i)
            {
                indices.Add(i);
            }

            while (indices.Count > 3)
            {
                bool earFound = false;

                for (int i = 0; i < indices.Count; ++i)
                {
                    int prevIndex = indices[(i + indices.Count - 1) % indices.Count];
                    int currIndex = indices[i];
                    int nextIndex = indices[(i + 1) % indices.Count];

                    if (IsConvex(vertices[prevIndex], vertices[currIndex], vertices[nextIndex]) && IsEar(vertices, indices, prevIndex, currIndex, nextIndex))
                    {
                        triangles.Add(prevIndex);
                        triangles.Add(currIndex);
                        triangles.Add(nextIndex);

                        indices.RemoveAt(i);
                        earFound = true;
                        break;
                    }
                }

                if (!earFound)
                {
                    Debug.LogError("Failed to triangulate points.");
                    break;
                }
            }

            if (indices.Count == 3)
            {
                triangles.AddRange(indices);
            }

            return triangles;
        }
    }
}
