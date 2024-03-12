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

namespace Meta.XR.MRUtilityKit
{
    /// <summary>
    /// A struct that can filter certain labels. The default is
    /// to allow all labels.
    /// </summary>
    public struct LabelFilter
    {
        List<string> IncludedLabels;
        List<string> ExcludedLabels;

        public static LabelFilter Included(List<string> included)
        {
            return new LabelFilter { IncludedLabels = included };
        }

        public static LabelFilter Excluded(List<string> excluded)
        {
            return new LabelFilter { ExcludedLabels = excluded };
        }

        public static LabelFilter FromEnum(MRUKAnchor.SceneLabels labels)
        {
            if (labels == ~(MRUKAnchor.SceneLabels)0)
            {
                // All labels included
                return new LabelFilter();
            }
            List<string> result = new();
            foreach (MRUKAnchor.SceneLabels flag in Enum.GetValues(typeof(MRUKAnchor.SceneLabels)))
            {
                if ((labels & flag) != 0)
                {
                    result.Add(flag.ToString());
                }
            }
            return Included(result);
        }

        public bool PassesFilter(List<string> labels)
        {
            if (ExcludedLabels != null)
            {
                foreach (var excludedLabel in ExcludedLabels)
                {
                    if (labels.Contains(excludedLabel))
                    {
                        return false;
                    }
                }
            }
            if (IncludedLabels != null)
            {
                foreach (var includedLabel in IncludedLabels)
                {
                    if (labels.Contains(includedLabel))
                    {
                        return true;
                    }
                }
                return false;
            }
            return true;
        }
    }
}
