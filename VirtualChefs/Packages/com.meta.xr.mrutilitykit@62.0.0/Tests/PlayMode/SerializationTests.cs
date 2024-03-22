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


using System.Collections;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine.TestTools.Utils;

namespace Meta.XR.MRUtilityKit.Tests
{
    public class SerializationTests : MonoBehaviour
    {
        private const int DefaultTimeoutMs = 10000;
        const string unityExpectedSerializedScene = @"{
  ""CoordinateSystem"": ""Unity"",
  ""Rooms"": [
    {
      ""UUID"": ""287A2B1E21D342D2B72333C69A3D856F"",
      ""RoomLayout"": {
        ""FloorUuid"": ""681D49700642440FA543B84C5E6075CE"",
        ""CeilingUuid"": ""5D2518B44391491DACEF6047E0A396BE"",
        ""WallsUUid"": [
          ""F76A570A94604D8F9E64CA33E060AD71"",
          ""3DF644AB8EF643D9BC0815B37F3C509D"",
          ""BE7370BF58444E6D9065F1C27E17AE8C"",
          ""363ACF20934C4099B0C6461AC59ADCF9"",
          ""DFEC541D7CDA42C1BF676AF849F22F93"",
          ""568A9000907A4BEF88584F3063F1C835"",
          ""794092B10A684918AE76BB11F4A8923F"",
          ""0E3CD9D109A449EDB6BCA03B02DAF307""
        ]
      },
      ""Anchors"": [
        {
          ""UUID"": ""F76A570A94604D8F9E64CA33E060AD71"",
          ""SemanticClassifications"": [
            ""WALL_FACE""
          ],
          ""Transform"": {
            ""Translation"": [2.01866651,1.5,2.28662467],
            ""Rotation"": [0.0,269.502716,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""PlaneBounds"": {
            ""Min"": [-1.83889234,-1.5],
            ""Max"": [1.83889234,1.5]
          },
          ""PlaneBoundary2D"": [
            [-1.83889234,-1.5],
            [1.83889234,-1.5],
            [1.83889234,1.5],
            [-1.83889234,1.5]
          ]
        },
        {
          ""UUID"": ""3DF644AB8EF643D9BC0815B37F3C509D"",
          ""SemanticClassifications"": [
            ""WALL_FACE""
          ],
          ""Transform"": {
            ""Translation"": [0.341732264,1.5,4.269563],
            ""Rotation"": [0.0,184.9589,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""PlaneBounds"": {
            ""Min"": [-1.66721463,-1.5],
            ""Max"": [1.66721463,1.5]
          },
          ""PlaneBoundary2D"": [
            [-1.66721463,-1.5],
            [1.66721463,-1.5],
            [1.66721463,1.5],
            [-1.66721463,1.5]
          ]
        },
        {
          ""UUID"": ""BE7370BF58444E6D9065F1C27E17AE8C"",
          ""SemanticClassifications"": [
            ""WALL_FACE""
          ],
          ""Transform"": {
            ""Translation"": [-2.69884,1.5,4.469511],
            ""Rotation"": [0.0,182.31749,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""PlaneBounds"": {
            ""Min"": [-1.38072717,-1.5],
            ""Max"": [1.38072717,1.5]
          },
          ""PlaneBoundary2D"": [
            [-1.38072717,-1.5],
            [1.38072717,-1.5],
            [1.38072717,1.5],
            [-1.38072717,1.5]
          ]
        },
        {
          ""UUID"": ""363ACF20934C4099B0C6461AC59ADCF9"",
          ""SemanticClassifications"": [
            ""WALL_FACE""
          ],
          ""Transform"": {
            ""Translation"": [-4.159371,1.5,2.96420383],
            ""Rotation"": [0.0,92.96769,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""PlaneBounds"": {
            ""Min"": [-1.56323612,-1.5],
            ""Max"": [1.56323612,1.5]
          },
          ""PlaneBoundary2D"": [
            [-1.56323612,-1.5],
            [1.56323612,-1.5],
            [1.56323612,1.5],
            [-1.56323612,1.5]
          ]
        },
        {
          ""UUID"": ""DFEC541D7CDA42C1BF676AF849F22F93"",
          ""SemanticClassifications"": [
            ""WALL_FACE""
          ],
          ""Transform"": {
            ""Translation"": [-2.85284019,1.5,1.33746612],
            ""Rotation"": [0.0,2.70687151,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""PlaneBounds"": {
            ""Min"": [-1.38901365,-1.5],
            ""Max"": [1.38901365,1.5]
          },
          ""PlaneBoundary2D"": [
            [-1.38901365,-1.5],
            [1.38901365,-1.5],
            [1.38901365,1.5],
            [-1.38901365,1.5]
          ]
        },
        {
          ""UUID"": ""568A9000907A4BEF88584F3063F1C835"",
          ""SemanticClassifications"": [
            ""WALL_FACE""
          ],
          ""Transform"": {
            ""Translation"": [-1.66822469,1.5,-0.307902753],
            ""Rotation"": [0.0,97.31695,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""PlaneBounds"": {
            ""Min"": [-1.592741,-1.5],
            ""Max"": [1.592741,1.5]
          },
          ""PlaneBoundary2D"": [
            [-1.592741,-1.5],
            [1.592741,-1.5],
            [1.592741,1.5],
            [-1.592741,1.5]
          ]
        },
        {
          ""UUID"": ""794092B10A684918AE76BB11F4A8923F"",
          ""SemanticClassifications"": [
            ""WALL_FACE""
          ],
          ""Transform"": {
            ""Translation"": [0.0330624,1.5,-1.99241328],
            ""Rotation"": [0.0,3.14845777,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""PlaneBounds"": {
            ""Min"": [-1.90701377,-1.5],
            ""Max"": [1.90701377,1.5]
          },
          ""PlaneBoundary2D"": [
            [-1.90701377,-1.5],
            [1.90701377,-1.5],
            [1.90701377,1.5],
            [-1.90701377,1.5]
          ]
        },
        {
          ""UUID"": ""0E3CD9D109A449EDB6BCA03B02DAF307"",
          ""SemanticClassifications"": [
            ""WALL_FACE""
          ],
          ""Transform"": {
            ""Translation"": [1.98591208,1.5,-0.82467556],
            ""Rotation"": [0.0,272.192383,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""PlaneBounds"": {
            ""Min"": [-1.27340925,-1.5],
            ""Max"": [1.27340925,1.5]
          },
          ""PlaneBoundary2D"": [
            [-1.27340925,-1.5],
            [1.27340925,-1.5],
            [1.27340925,1.5],
            [-1.27340925,1.5]
          ]
        },
        {
          ""UUID"": ""4FA37C4FDF9F459785A16B7615297D19"",
          ""SemanticClassifications"": [
            ""OTHER""
          ],
          ""Transform"": {
            ""Translation"": [1.61697006,1.0874939,2.351218],
            ""Rotation"": [270.0,5.7210083,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""VolumeBounds"": {
            ""Min"": [-0.398986936,-0.432495117,-1.08197021],
            ""Max"": [0.398986936,0.432495117,0.0]
          }
        },
        {
          ""UUID"": ""E0AA955D4D0744C2AECE933C126CA851"",
          ""SemanticClassifications"": [
            ""OTHER""
          ],
          ""Transform"": {
            ""Translation"": [0.491380483,0.474517822,3.643852],
            ""Rotation"": [270.0,274.152954,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""VolumeBounds"": {
            ""Min"": [-0.433013916,-0.410492,-0.468994141],
            ""Max"": [0.433013916,0.410492,0.0]
          }
        },
        {
          ""UUID"": ""6A7A54AF3A904034A91CCCC43C3DF8CA"",
          ""SemanticClassifications"": [
            ""OTHER""
          ],
          ""Transform"": {
            ""Translation"": [-1.333756,1.27151489,-1.750792],
            ""Rotation"": [270.0,96.27296,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""VolumeBounds"": {
            ""Min"": [-0.199005172,-0.401000977,-1.26599121],
            ""Max"": [0.199005172,0.401000977,0.0]
          }
        },
        {
          ""UUID"": ""3EE1685ADB444088916F40E589F583C2"",
          ""SemanticClassifications"": [
            ""OTHER""
          ],
          ""Transform"": {
            ""Translation"": [-2.335,1.0874939,4.381],
            ""Rotation"": [270.0,1.8795377,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""VolumeBounds"": {
            ""Min"": [-0.633471549,-0.08175456,-1.08197021],
            ""Max"": [0.633471549,0.08175456,0.0]
          }
        },
        {
          ""UUID"": ""DD55C0F2B95545A9885D667BE6BFB8B5"",
          ""SemanticClassifications"": [
            ""TABLE""
          ],
          ""Transform"": {
            ""Translation"": [-3.72,0.6,2.47],
            ""Rotation"": [270.0,4.0065403,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""PlaneBounds"": {
            ""Min"": [-0.398986936,-1.0687387],
            ""Max"": [0.398986936,1.0687387]
          },
          ""VolumeBounds"": {
            ""Min"": [-0.398986936,-1.0687387,-0.6],
            ""Max"": [0.398986936,1.0687387,0.0]
          },
          ""PlaneBoundary2D"": [
            [-0.398986936,-1.0687387],
            [0.398986936,-1.0687387],
            [0.398986936,1.0687387],
            [-0.398986936,1.0687387]
          ]
        },
        {
          ""UUID"": ""A95BA1EAA5B34A54A5B4D07080F882CA"",
          ""SemanticClassifications"": [
            ""COUCH""
          ],
          ""Transform"": {
            ""Translation"": [0.74,0.5,-1.02],
            ""Rotation"": [270.0,1.23901379,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""PlaneBounds"": {
            ""Min"": [-1.20065725,-0.9039919],
            ""Max"": [1.20065725,0.9039919]
          },
          ""VolumeBounds"": {
            ""Min"": [-1.20065725,-0.9039919,-0.5],
            ""Max"": [1.20065725,0.9039919,0.0]
          },
          ""PlaneBoundary2D"": [
            [-1.20065725,-0.9039919],
            [1.20065725,-0.9039919],
            [1.20065725,0.9039919],
            [-1.20065725,0.9039919]
          ]
        },
        {
          ""UUID"": ""EFFE221DAA1E4EF7A8DAD702C9F8ED15"",
          ""SemanticClassifications"": [
            ""WINDOW_FRAME""
          ],
          ""Transform"": {
            ""Translation"": [-1.71,1.576,-0.696],
            ""Rotation"": [0.0,97.4,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""PlaneBounds"": {
            ""Min"": [-0.646113634,-0.456154764],
            ""Max"": [0.646113634,0.456154764]
          },
          ""PlaneBoundary2D"": [
            [-0.646113634,-0.456154764],
            [0.646113634,-0.456154764],
            [0.646113634,0.456154764],
            [-0.646113634,0.456154764]
          ]
        },
        {
          ""UUID"": ""53EB0859E26F4B1A9F6BF91FF25F6972"",
          ""SemanticClassifications"": [
            ""DOOR_FRAME""
          ],
          ""Transform"": {
            ""Translation"": [-1.54,1.03,0.61],
            ""Rotation"": [0.0,97.4,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""PlaneBounds"": {
            ""Min"": [-0.378175974,-1.00632],
            ""Max"": [0.378175974,1.00632]
          },
          ""PlaneBoundary2D"": [
            [-0.378175974,-1.00632],
            [0.378175974,-1.00632],
            [0.378175974,1.00632],
            [-0.378175974,1.00632]
          ]
        },
        {
          ""UUID"": ""681D49700642440FA543B84C5E6075CE"",
          ""SemanticClassifications"": [
            ""FLOOR""
          ],
          ""Transform"": {
            ""Translation"": [-1.06952024,0.0,1.2340889],
            ""Rotation"": [270.0,273.148438,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""PlaneBounds"": {
            ""Min"": [-3.16107464,-3.18514252],
            ""Max"": [3.16107464,3.18514252]
          },
          ""PlaneBoundary2D"": [
            [-0.614610434,3.14264679],
            [-3.1610744,3.185142],
            [-3.16107464,-0.628885269],
            [0.0159805864,-0.3973337],
            [-0.00542993844,-3.17527843],
            [3.121027,-3.18514252],
            [3.16107488,-0.4239784],
            [3.05573153,2.90878654]
          ]
        },
        {
          ""UUID"": ""5D2518B44391491DACEF6047E0A396BE"",
          ""SemanticClassifications"": [
            ""CEILING""
          ],
          ""Transform"": {
            ""Translation"": [-1.06952024,3.0,1.2340889],
            ""Rotation"": [90.0,93.14846,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""PlaneBounds"": {
            ""Min"": [-3.16107464,-3.18514252],
            ""Max"": [3.16107464,3.18514252]
          },
          ""PlaneBoundary2D"": [
            [-3.05573153,2.90878654],
            [-3.16107488,-0.423978329],
            [-3.121027,-3.18514252],
            [0.00542993844,-3.17527819],
            [-0.0159805864,-0.397333622],
            [3.16107464,-0.628885269],
            [3.1610744,3.18514228],
            [0.614610434,3.14264679]
          ]
        }
      ]
    }
  ]
}";
        const string unrealExpectedSerializedScene = @"{
  ""CoordinateSystem"": ""Unreal"",
  ""Rooms"": [
    {
      ""UUID"": ""2C968DDD83B3411FA4C43515BCAE4FD5"",
      ""RoomLayout"": {
        ""FloorUuid"": ""469D6D16896A45858ADC4BB05B2D1268"",
        ""CeilingUuid"": ""4401B7317760425E9FE197733A427F76"",
        ""WallsUUid"": [
          ""40213D13552A4A21AE7A2457C6F27937"",
          ""12ADD8A452914E0B92B54A3BB45D0192"",
          ""E0A9495F8CA94F0F98D078EACCA3ECF5"",
          ""C478BBB05CEB47D2B4BE3C62C8A68EA4"",
          ""1608E6EDEE2E4003844193513D27E6E7"",
          ""0C239875708A4535A40D537666A2BE6F"",
          ""F492A68E773542EE8B3732DC04B4C9C4"",
          ""F5AEB1DAE14B47BEAE93684F0B405222""
        ]
      },
      ""Anchors"": [
        {
          ""UUID"": ""40213D13552A4A21AE7A2457C6F27937"",
          ""SemanticClassifications"": [
            ""WALL_FACE""
          ],
          ""Transform"": {
            ""Translation"": [228.66246,201.866653,150.0],
            ""Rotation"": [0.0,449.502716,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""PlaneBounds"": {
            ""Min"": [-183.889236,-150.0],
            ""Max"": [183.889236,150.0]
          },
          ""PlaneBoundary2D"": [
            [183.889236,150.0],
            [-183.889236,150.0],
            [-183.889236,-150.0],
            [183.889236,-150.0]
          ]
        },
        {
          ""UUID"": ""12ADD8A452914E0B92B54A3BB45D0192"",
          ""SemanticClassifications"": [
            ""WALL_FACE""
          ],
          ""Transform"": {
            ""Translation"": [426.956329,34.1732254,150.0],
            ""Rotation"": [0.0,364.9589,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""PlaneBounds"": {
            ""Min"": [-166.721466,-150.0],
            ""Max"": [166.721466,150.0]
          },
          ""PlaneBoundary2D"": [
            [166.721466,150.0],
            [-166.721466,150.0],
            [-166.721466,-150.0],
            [166.721466,-150.0]
          ]
        },
        {
          ""UUID"": ""E0A9495F8CA94F0F98D078EACCA3ECF5"",
          ""SemanticClassifications"": [
            ""WALL_FACE""
          ],
          ""Transform"": {
            ""Translation"": [446.9511,-269.884,150.0],
            ""Rotation"": [0.0,362.3175,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""PlaneBounds"": {
            ""Min"": [-138.072723,-150.0],
            ""Max"": [138.072723,150.0]
          },
          ""PlaneBoundary2D"": [
            [138.072723,150.0],
            [-138.072723,150.0],
            [-138.072723,-150.0],
            [138.072723,-150.0]
          ]
        },
        {
          ""UUID"": ""C478BBB05CEB47D2B4BE3C62C8A68EA4"",
          ""SemanticClassifications"": [
            ""WALL_FACE""
          ],
          ""Transform"": {
            ""Translation"": [296.42038,-415.9371,150.0],
            ""Rotation"": [0.0,272.967682,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""PlaneBounds"": {
            ""Min"": [-156.323608,-150.0],
            ""Max"": [156.323608,150.0]
          },
          ""PlaneBoundary2D"": [
            [156.323608,150.0],
            [-156.323608,150.0],
            [-156.323608,-150.0],
            [156.323608,-150.0]
          ]
        },
        {
          ""UUID"": ""1608E6EDEE2E4003844193513D27E6E7"",
          ""SemanticClassifications"": [
            ""WALL_FACE""
          ],
          ""Transform"": {
            ""Translation"": [133.746613,-285.284027,150.0],
            ""Rotation"": [0.0,182.706879,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""PlaneBounds"": {
            ""Min"": [-138.901367,-150.0],
            ""Max"": [138.901367,150.0]
          },
          ""PlaneBoundary2D"": [
            [138.901367,150.0],
            [-138.901367,150.0],
            [-138.901367,-150.0],
            [138.901367,-150.0]
          ]
        },
        {
          ""UUID"": ""0C239875708A4535A40D537666A2BE6F"",
          ""SemanticClassifications"": [
            ""WALL_FACE""
          ],
          ""Transform"": {
            ""Translation"": [-30.7902756,-166.822464,150.0],
            ""Rotation"": [0.0,277.316956,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""PlaneBounds"": {
            ""Min"": [-159.274109,-150.0],
            ""Max"": [159.274109,150.0]
          },
          ""PlaneBoundary2D"": [
            [159.274109,150.0],
            [-159.274109,150.0],
            [-159.274109,-150.0],
            [159.274109,-150.0]
          ]
        },
        {
          ""UUID"": ""F492A68E773542EE8B3732DC04B4C9C4"",
          ""SemanticClassifications"": [
            ""WALL_FACE""
          ],
          ""Transform"": {
            ""Translation"": [-199.241333,3.30623984,150.0],
            ""Rotation"": [0.0,183.148453,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""PlaneBounds"": {
            ""Min"": [-190.70137,-150.0],
            ""Max"": [190.70137,150.0]
          },
          ""PlaneBoundary2D"": [
            [190.70137,150.0],
            [-190.70137,150.0],
            [-190.70137,-150.0],
            [190.70137,-150.0]
          ]
        },
        {
          ""UUID"": ""F5AEB1DAE14B47BEAE93684F0B405222"",
          ""SemanticClassifications"": [
            ""WALL_FACE""
          ],
          ""Transform"": {
            ""Translation"": [-82.46756,198.5912,150.0],
            ""Rotation"": [0.0,452.192383,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""PlaneBounds"": {
            ""Min"": [-127.340927,-150.0],
            ""Max"": [127.340927,150.0]
          },
          ""PlaneBoundary2D"": [
            [127.340927,150.0],
            [-127.340927,150.0],
            [-127.340927,-150.0],
            [127.340927,-150.0]
          ]
        },
        {
          ""UUID"": ""14C790B5B8284A218E1D61EAFF85A803"",
          ""SemanticClassifications"": [
            ""OTHER""
          ],
          ""Transform"": {
            ""Translation"": [235.1218,161.697,108.74939],
            ""Rotation"": [270.0,185.721008,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""VolumeBounds"": {
            ""Min"": [0.0,-39.8986931,-43.24951],
            ""Max"": [108.197021,39.8986931,43.24951]
          }
        },
        {
          ""UUID"": ""21C21CCA002D4C08995DAE046DF389B5"",
          ""SemanticClassifications"": [
            ""OTHER""
          ],
          ""Transform"": {
            ""Translation"": [364.3852,49.13805,47.4517822],
            ""Rotation"": [270.0,454.152954,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""VolumeBounds"": {
            ""Min"": [0.0,-43.30139,-41.049202],
            ""Max"": [46.8994141,43.30139,41.049202]
          }
        },
        {
          ""UUID"": ""BB467FDFCBDA4780A6A1DAAAB20000A1"",
          ""SemanticClassifications"": [
            ""OTHER""
          ],
          ""Transform"": {
            ""Translation"": [-175.079208,-133.3756,127.151489],
            ""Rotation"": [270.0,276.272949,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""VolumeBounds"": {
            ""Min"": [0.0,-19.9005165,-40.1000977],
            ""Max"": [126.599121,19.9005165,40.1000977]
          }
        },
        {
          ""UUID"": ""0C035F658E57426B8A279F265057EB05"",
          ""SemanticClassifications"": [
            ""OTHER""
          ],
          ""Transform"": {
            ""Translation"": [438.1,-233.5,108.74939],
            ""Rotation"": [270.0,181.879532,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""VolumeBounds"": {
            ""Min"": [0.0,-63.3471565,-8.175456],
            ""Max"": [108.197021,63.3471565,8.175456]
          }
        },
        {
          ""UUID"": ""A78991A353DF49669E096451DD0DDCBB"",
          ""SemanticClassifications"": [
            ""TABLE""
          ],
          ""Transform"": {
            ""Translation"": [247.0,-372.0,60.0000038],
            ""Rotation"": [270.0,184.006546,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""PlaneBounds"": {
            ""Min"": [-39.8986931,-106.873871],
            ""Max"": [39.8986931,106.873871]
          },
          ""VolumeBounds"": {
            ""Min"": [0.0,-39.8986931,-106.873871],
            ""Max"": [60.0000038,39.8986931,106.873871]
          },
          ""PlaneBoundary2D"": [
            [39.8986931,106.873871],
            [-39.8986931,106.873871],
            [-39.8986931,-106.873871],
            [39.8986931,-106.873871]
          ]
        },
        {
          ""UUID"": ""184355B165A842DF92FE2F038E0AFCEC"",
          ""SemanticClassifications"": [
            ""COUCH""
          ],
          ""Transform"": {
            ""Translation"": [-102.0,74.0,50.0],
            ""Rotation"": [270.0,181.239014,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""PlaneBounds"": {
            ""Min"": [-120.065727,-90.3991852],
            ""Max"": [120.065727,90.3991852]
          },
          ""VolumeBounds"": {
            ""Min"": [0.0,-120.065727,-90.3991852],
            ""Max"": [50.0,120.065727,90.3991852]
          },
          ""PlaneBoundary2D"": [
            [120.065727,90.3991852],
            [-120.065727,90.3991852],
            [-120.065727,-90.3991852],
            [120.065727,-90.3991852]
          ]
        },
        {
          ""UUID"": ""DD532251CB134F7C9D46E47C82178DD2"",
          ""SemanticClassifications"": [
            ""WINDOW_FRAME""
          ],
          ""Transform"": {
            ""Translation"": [-69.6,-171.0,157.599991],
            ""Rotation"": [0.0,277.4,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""PlaneBounds"": {
            ""Min"": [-64.61137,-45.6154747],
            ""Max"": [64.61137,45.6154747]
          },
          ""PlaneBoundary2D"": [
            [64.61137,45.6154747],
            [-64.61137,45.6154747],
            [-64.61137,-45.6154747],
            [64.61137,-45.6154747]
          ]
        },
        {
          ""UUID"": ""FD9D93764CC140DFBF7BFD96DDE7278F"",
          ""SemanticClassifications"": [
            ""DOOR_FRAME""
          ],
          ""Transform"": {
            ""Translation"": [61.0,-154.0,103.0],
            ""Rotation"": [0.0,277.4,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""PlaneBounds"": {
            ""Min"": [-37.8175964,-100.632],
            ""Max"": [37.8175964,100.632]
          },
          ""PlaneBoundary2D"": [
            [37.8175964,100.632],
            [-37.8175964,100.632],
            [-37.8175964,-100.632],
            [37.8175964,-100.632]
          ]
        },
        {
          ""UUID"": ""469D6D16896A45858ADC4BB05B2D1268"",
          ""SemanticClassifications"": [
            ""FLOOR""
          ],
          ""Transform"": {
            ""Translation"": [123.40889,-106.952026,0.0],
            ""Rotation"": [270.0,453.148438,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""PlaneBounds"": {
            ""Min"": [-316.107452,-318.514252],
            ""Max"": [316.107452,318.514252]
          },
          ""PlaneBoundary2D"": [
            [-305.573151,290.878662],
            [-316.107483,-42.39784],
            [-312.1027,-318.514252],
            [0.542993844,-317.527832],
            [-1.5980587,-39.73337],
            [316.107452,-62.8885269],
            [316.107452,318.5142],
            [61.4610443,314.264679]
          ]
        },
        {
          ""UUID"": ""4401B7317760425E9FE197733A427F76"",
          ""SemanticClassifications"": [
            ""CEILING""
          ],
          ""Transform"": {
            ""Translation"": [123.40889,-106.952026,300.0],
            ""Rotation"": [90.0,273.148468,0.0],
            ""Scale"": [1.0,1.0,1.0]
          },
          ""PlaneBounds"": {
            ""Min"": [-316.107452,-318.514252],
            ""Max"": [316.107452,318.514252]
          },
          ""PlaneBoundary2D"": [
            [-61.4610443,314.264679],
            [-316.107452,318.514221],
            [-316.107452,-62.8885269],
            [1.5980587,-39.73336],
            [-0.542993844,-317.527832],
            [312.1027,-318.514252],
            [316.107483,-42.3978348],
            [305.573151,290.878662]
          ]
        }
      ]
    }
  ]
}
";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Packages\\com.meta.xr.mrutilitykit\\Tests\\RayCastTests.unity",
                new LoadSceneParameters(LoadSceneMode.Additive));
            yield return new WaitUntil(() => MRUK.Instance.IsInitialized);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int i = SceneManager.sceneCount - 1; i >= 1; i--)
            {
                var asyncOperation = SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(i).name); // Clear/reset scene
                yield return new WaitUntil(() => asyncOperation.isDone);
            }
        }

        /// <summary>
        /// Test that serialization to the Unity coordinate system works as expected.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator SerializationToUnity()
        {
            var json = MRUK.Instance.SaveSceneToJsonString(SerializationHelpers.CoordinateSystem.Unity);

            var splitJson = json.Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            var splitExpected = unityExpectedSerializedScene.Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < splitExpected.Length && i < splitJson.Length; i++)
            {
                if (Regex.IsMatch(splitExpected[i], "[A-F0-9]{32}") &&
                    Regex.IsMatch(splitJson[i], "[A-F0-9]{32}"))
                {
                    // Ignore GUIDs because they change every time
                    continue;
                }
                Assert.AreEqual(splitExpected[i], splitJson[i], "Line {0}", i + 1);
            }
            Assert.AreEqual(splitExpected.Length, splitJson.Length, "Number of lines");
            yield return null;
        }

        /// <summary>
        /// Test that serialization to the Unreal coordinate system works as expected.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator SerializationToUnreal()
        {
            var json = MRUK.Instance.SaveSceneToJsonString(SerializationHelpers.CoordinateSystem.Unreal);

            var splitJson = json.Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            var splitExpected = unrealExpectedSerializedScene.Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < splitExpected.Length && i < splitJson.Length; i++)
            {
                if (Regex.IsMatch(splitExpected[i], "[A-F0-9]{32}") &&
                    Regex.IsMatch(splitJson[i], "[A-F0-9]{32}"))
                {
                    // Ignore GUIDs because they change every time
                    continue;
                }
                Assert.AreEqual(splitExpected[i], splitJson[i], "Line {0}", i + 1);
            }
            Assert.AreEqual(splitExpected.Length, splitJson.Length, "Number of lines");
            yield return null;
        }

        /// <summary>
        /// Test that deserialization from the Unity coordinate system works as expected.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator DeserializationFromUnity()
        {
            ValidateLoadedScene(unityExpectedSerializedScene);
            yield return null;
        }

        /// <summary>
        /// Test that deserialization from the Unreal coordinate system works as expected.
        /// </summary>
        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator DeserializationFromUnreal()
        {
            ValidateLoadedScene(unrealExpectedSerializedScene);
            yield return null;
        }

        void ValidateLoadedScene(string sceneJson)
        {
            MRUK.Instance.LoadSceneFromJsonString(sceneJson);
            Assert.NotNull(MRUK.Instance.GetCurrentRoom());
            var loadedRoom = MRUK.Instance.GetCurrentRoom();
            MRUK.Instance.LoadSceneFromPrefab(MRUK.Instance.SceneSettings.RoomPrefabs[0], false);
            var expectedRoom = MRUK.Instance.GetRooms()[1];
            Assert.IsNotNull(expectedRoom);
            var loadedAnchors = loadedRoom.GetRoomAnchors();
            var expectedAnchors = expectedRoom.GetRoomAnchors();
            Assert.AreEqual(expectedAnchors.Count, loadedAnchors.Count);
            for (int i = 0; i < loadedAnchors.Count; i++)
            {
                var loadedAnchor = loadedAnchors[i];
                var expectedAnchor = expectedAnchors[i];
                // Skip UUID check as they could change every time
                if (loadedAnchor.PlaneRect.HasValue)
                {
                    Assert.That(loadedAnchor.PlaneRect.Value.position, Is.EqualTo(expectedAnchor.PlaneRect.Value.position).Using(Vector2EqualityComparer.Instance));
                    Assert.That(loadedAnchor.PlaneRect.Value.size, Is.EqualTo(expectedAnchor.PlaneRect.Value.size).Using(Vector2EqualityComparer.Instance));
                }
                if (loadedAnchor.VolumeBounds.HasValue)
                {
                    Assert.That(loadedAnchor.VolumeBounds.Value.extents, Is.EqualTo(expectedAnchor.VolumeBounds.Value.extents).Using(Vector3EqualityComparer.Instance));
                    Assert.That(loadedAnchor.VolumeBounds.Value.center, Is.EqualTo(expectedAnchor.VolumeBounds.Value.center).Using(Vector3EqualityComparer.Instance));
                }
                Assert.That(loadedAnchor.transform.position, Is.EqualTo(expectedAnchor.transform.position).Using(Vector3EqualityComparer.Instance));
                Assert.That(loadedAnchor.transform.rotation.eulerAngles, Is.EqualTo(expectedAnchor.transform.rotation.eulerAngles).Using(Vector3EqualityComparer.Instance));
                Assert.That(loadedAnchor.transform.localScale, Is.EqualTo(expectedAnchor.transform.localScale).Using(Vector3EqualityComparer.Instance));
                Assert.That(loadedAnchor.GetAnchorCenter(), Is.EqualTo(expectedAnchor.GetAnchorCenter()).Using(Vector3EqualityComparer.Instance));
                Assert.That(loadedAnchor.GetAnchorSize(), Is.EqualTo(expectedAnchor.GetAnchorSize()).Using(Vector3EqualityComparer.Instance));
                if (loadedAnchor.PlaneBoundary2D != null)
                {
                    var loadedPlaneBoundary2D = loadedAnchor.PlaneBoundary2D;
                    var expectedPlaneBoundary2D = expectedAnchor.PlaneBoundary2D;
                    for (int j = 0; j < loadedAnchor.AnchorLabels.Count; j++)
                    {
                        Assert.That(loadedPlaneBoundary2D[j], Is.EqualTo(expectedPlaneBoundary2D[j]).Using(Vector2EqualityComparer.Instance));
                    }
                }
                for (int j = 0; j < loadedAnchor.AnchorLabels.Count; j++)
                {
                    Assert.AreEqual(expectedAnchor.AnchorLabels[j], loadedAnchor.AnchorLabels[j]);
                }
                var loadedBoundsFaceCenters = loadedAnchor.GetBoundsFaceCenters();
                var expectedBoundsFaceCenters = expectedAnchor.GetBoundsFaceCenters();
                for (int j = 0; j < loadedAnchor.GetBoundsFaceCenters().Length; j++)
                {
                    Assert.That(loadedBoundsFaceCenters[j], Is.EqualTo(expectedBoundsFaceCenters[j]).Using(Vector3EqualityComparer.Instance));
                }
            }
        }
    }
}
