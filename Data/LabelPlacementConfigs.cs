﻿using System.Collections.Generic;
using UnityEngine;

namespace SimpleLabels.Data
{
    public class LabelPlacementConfigs
    {
        public static readonly Dictionary<string, List<LabelPlacement>> LabelPlacementConfigsDictionary =
            new Dictionary<string, List<LabelPlacement>>
            {
                {
                    "StorageRack_Small",
                    new List<LabelPlacement>
                    {
                        new LabelPlacement(
                            new Vector3(0f, 0.75f, -0.254f),
                            Vector3.zero,
                            new Vector2(1.8f, 0.5f)
                        ),
                        new LabelPlacement(
                            new Vector3(0.5f, 0.75f, 0f),
                            new Vector3(0, -90, 0),
                            new Vector2(1.8f, 0.5f)
                        ),
                        new LabelPlacement(
                            new Vector3(0f, 0.75f, 0.254f),
                            new Vector3(0, 180, 0),
                            new Vector2(1.8f, 0.5f)
                        ),
                        new LabelPlacement(
                            new Vector3(-0.5f, 0.75f, 0f),
                            new Vector3(0, 90, 0),
                            new Vector2(1.8f, 0.5f)
                        )
                    }
                },
                {
                    "StorageRack_Medium",
                    new List<LabelPlacement>
                    {
                        new LabelPlacement(
                            new Vector3(0f, 0.75f, -0.254f),
                            Vector3.zero,
                            new Vector2(2.3f, 0.5f)
                        ),
                        new LabelPlacement(
                            new Vector3(0.75f, 0.75f, 0f),
                            new Vector3(0, -90, 0),
                            new Vector2(2.3f, 0.5f)
                        ),
                        new LabelPlacement(
                            new Vector3(0f, 0.75f, 0.254f),
                            new Vector3(0, 180, 0),
                            new Vector2(2.3f, 0.5f)
                        ),
                        new LabelPlacement(
                            new Vector3(-0.75f, 0.75f, 0f),
                            new Vector3(0, 90, 0),
                            new Vector2(2.3f, 0.5f)
                        )
                    }
                },
                {
                    "StorageRack_Large",
                    new List<LabelPlacement>
                    {
                        new LabelPlacement(
                            new Vector3(0f, 0.75f, -0.254f),
                            Vector3.zero,
                            new Vector2(2.8f, 0.5f)
                        ),
                        new LabelPlacement(
                            new Vector3(1.006f, 0.75f, 0f),
                            new Vector3(0, -90, 0),
                            new Vector2(2.8f, 0.5f)
                        ),
                        new LabelPlacement(
                            new Vector3(0f, 0.75f, 0.254f),
                            new Vector3(0, 180, 0),
                            new Vector2(2.8f, 0.5f)
                        ),
                        new LabelPlacement(
                            new Vector3(-1.006f, 0.75f, 0f),
                            new Vector3(0, 90, 0),
                            new Vector2(2.8f, 0.5f)
                        )
                    }
                },
                {
                    "WallMountedShelf",
                    new List<LabelPlacement>
                    {
                        new LabelPlacement(
                            new Vector3(0f, 0.255f, 0.43f),
                            new Vector3(0, 180, 0),
                            new Vector2(0.8f, 0.3f)
                        )
                    }
                },
                {
                    "MixingStationMk2",
                    new List<LabelPlacement>
                    {
                        new LabelPlacement(
                            new Vector3(-0.7f, 1.08f, 0.178f),
                            new Vector3(19, 180, 0),
                            new Vector2(1.2f, 0.4f)
                        )
                    }
                },
                {
                    "MixingStation",
                    new List<LabelPlacement>
                    {
                        new LabelPlacement(
                            new Vector3(-0.6f, 0.925f, 0.5f),
                            new Vector3(0, 180, 0),
                            new Vector2(1.0f, 0.4f)
                        )
                    }
                },
                {
                    "PackagingStation_Mk2",
                    new List<LabelPlacement>
                    {
                        new LabelPlacement(
                            new Vector3(0, 0.925f, 0.504f),
                            new Vector3(0, 180, 0),
                            new Vector2(1.2f, 0.4f)
                        )
                    }
                },
                {
                    "PackagingStation",
                    new List<LabelPlacement>
                    {
                        new LabelPlacement(
                            new Vector3(0, 0.925f, 0.504f),
                            new Vector3(0, 180, 0),
                            new Vector2(1.2f, 0.4f)
                        )
                    }
                },
                {
                    "BrickPress",
                    new List<LabelPlacement>
                    {
                        new LabelPlacement(
                            new Vector3(0, 0.96f, 0.405f),
                            new Vector3(0, 180, 0),
                            new Vector2(1.0f, 0.4f)
                        )
                    }
                },
                {
                    "Cauldron",
                    new List<LabelPlacement>
                    {
                        new LabelPlacement(
                            new Vector3(0, 0.305f, 0.425f),
                            new Vector3(90, 180, 0),
                            new Vector2(0.6f, 0.3f)
                        )
                    }
                },
                {
                    "ChemistryStation",
                    new List<LabelPlacement>
                    {
                        new LabelPlacement(
                            new Vector3(0, 0.925f, 0.504f),
                            new Vector3(0, 180, 0),
                            new Vector2(1.2f, 0.4f)
                        )
                    }
                },
                {
                    "LabOven",
                    new List<LabelPlacement>
                    {
                        new LabelPlacement(
                            new Vector3(0, 0.925f, 0.504f),
                            new Vector3(0, 180, 0),
                            new Vector2(1.2f, 0.4f)
                        )
                    }
                },
                {
                    "DryingRack",
                    new List<LabelPlacement>
                    {
                        new LabelPlacement(
                            new Vector3(0, 1.92f, -0.03f),
                            Vector3.zero,
                            new Vector2(1.5f, 0.4f)
                        ),
                        new LabelPlacement(
                            new Vector3(0, 1.92f, 0.03f),
                            new Vector3(0, 180, 0),
                            new Vector2(1.5f, 0.4f)
                        )
                    }
                },
                {
                    "MetalSquareTable",
                    new List<LabelPlacement>
                    {
                        // Front
                        new LabelPlacement(
                            new Vector3(0, 0.45f, 0.505f),
                            new Vector3(0, 180, 0),
                            new Vector2(1.0f, 0.4f)
                        ),
                        // Back
                        new LabelPlacement(
                            new Vector3(0, 0.45f, -0.505f),
                            new Vector3(0, 0, 0),
                            new Vector2(1.0f, 0.4f)
                        ),
                        // Right
                        new LabelPlacement(
                            new Vector3(0.505f, 0.45f, 0),
                            new Vector3(0, -90, 0),
                            new Vector2(1.0f, 0.4f)
                        ),
                        // Left
                        new LabelPlacement(
                            new Vector3(-0.505f, 0.45f, 0),
                            new Vector3(0, 90, 0),
                            new Vector2(1.0f, 0.4f)
                        )
                    }
                },
                {
                    "WoodSquareTable",
                    new List<LabelPlacement>
                    {
                        // Front
                        new LabelPlacement(
                            new Vector3(0, 0.45f, 0.505f),
                            new Vector3(0, 180, 0),
                            new Vector2(1.0f, 0.4f)
                        ),
                        // Back
                        new LabelPlacement(
                            new Vector3(0, 0.45f, -0.505f),
                            new Vector3(0, 0, 0),
                            new Vector2(1.0f, 0.4f)
                        ),
                        // Right
                        new LabelPlacement(
                            new Vector3(0.505f, 0.45f, 0),
                            new Vector3(0, -90, 0),
                            new Vector2(1.0f, 0.4f)
                        ),
                        // Left
                        new LabelPlacement(
                            new Vector3(-0.505f, 0.45f, 0),
                            new Vector3(0, 90, 0),
                            new Vector2(1.0f, 0.4f)
                        )
                    }
                },
                {
                    "DisplayCabinet",
                    new List<LabelPlacement>
                    {
                        new LabelPlacement(
                            new Vector3(0f, 1.4f, -0.355f),
                            Vector3.zero,
                            new Vector2(2.8f, 0.5f)
                        ),
                        new LabelPlacement(
                            new Vector3(0.605f, 1.4f, 0f),
                            new Vector3(0, -90, 0),
                            new Vector2(2.8f, 0.5f)
                        ),
                        new LabelPlacement(
                            new Vector3(0f, 1.4f, 0.355f),
                            new Vector3(0, 180, 0),
                            new Vector2(2.8f, 0.5f)
                        ),
                        new LabelPlacement(
                            new Vector3(-0.605f, 1.4f, 0f),
                            new Vector3(0, 90, 0),
                            new Vector2(2.8f, 0.5f)
                        )
                    }
                },
                {
                    "Safe",
                    new List<LabelPlacement>
                    {
                        new LabelPlacement(
                            new Vector3(0, 0.2f, 0.255f),
                            new Vector3(0, 180, 0),
                            new Vector2(1.2f, 0.4f)
                        )
                    }
                },
                {
                    "CoffeeTable",
                    new List<LabelPlacement>
                    {
                        new LabelPlacement(
                            new Vector3(0f, 0.5f, -0.405f),
                            Vector3.zero,
                            new Vector2(2.8f, 0.5f)
                        ),
                        new LabelPlacement(
                            new Vector3(.805f, .5f, 0f),
                            new Vector3(0, -90, 0),
                            new Vector2(2.8f, 0.5f)
                        ),
                        new LabelPlacement(
                            new Vector3(0f, .5f, 0.405f),
                            new Vector3(0, 180, 0),
                            new Vector2(2.8f, 0.5f)
                        ),
                        new LabelPlacement(
                            new Vector3(-0.805f, .025f, 0f),
                            new Vector3(0, 90, 0),
                            new Vector2(2.8f, 0.5f)
                        )
                    }
                }
            };
    }

    public struct LabelPlacement
    {
        public readonly Vector3 LocalPosition;
        public readonly Vector3 EulerRotation;
        public readonly Vector2 Dimensions;

        public LabelPlacement(Vector3 position, Vector3 rotation, Vector2 dimensions)
        {
            LocalPosition = position;
            EulerRotation = rotation;
            Dimensions = dimensions;
        }

        public Quaternion Rotation => Quaternion.Euler(EulerRotation);
    }
}