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
    public static class Utilities
    {
        static Dictionary<GameObject, Bounds?> prefabBoundsCache = new();

        static public readonly float Sqrt2 = Mathf.Sqrt(2f);
        static public readonly float InvSqrt2 = 1f / Mathf.Sqrt(2f);

        static public Bounds? GetPrefabBounds(GameObject prefab)
        {
            if (prefabBoundsCache.TryGetValue(prefab, out Bounds? cachedBounds))
            {
                return cachedBounds;
            }
            Bounds? bounds = CalculateBoundsRecursively(prefab.transform);
            prefabBoundsCache.Add(prefab, bounds);
            return bounds;
        }

        static Bounds? CalculateBoundsRecursively(Transform transform)
        {
            Bounds? bounds = null;
            Renderer renderer = transform.GetComponent<Renderer>();

            if (renderer != null && renderer.bounds.size != Vector3.zero)
            {
                // If the current GameObject has a renderer component, include its bounds
                bounds = renderer.bounds;
            }

            // Recursively process children
            foreach (Transform child in transform.transform)
            {
                Bounds? childBounds = CalculateBoundsRecursively(child);
                if (childBounds != null)
                {
                    if (bounds != null)
                    {
                        var boundsValue = bounds.Value;
                        boundsValue.Encapsulate(childBounds.Value);
                        bounds = boundsValue;
                    }
                    else
                    {
                        bounds = childBounds;
                    }
                }
            }

            return bounds;
        }
    }
}
