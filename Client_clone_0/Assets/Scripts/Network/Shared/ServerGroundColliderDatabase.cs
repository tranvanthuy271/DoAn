using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ServerGroundColliderDatabase", menuName = "DoAn/Server Ground Collider Database")]
public class ServerGroundColliderDatabase : ScriptableObject
{
    public const string ResourcesPath = "ScriptableObjects/ServerGroundColliderDatabase";
    public MapGroundData[] maps = Array.Empty<MapGroundData>();

    public bool TryGetMap(int mapId, out MapGroundData mapData)
    {
        if (maps != null)
        {
            foreach (var map in maps)
            {
                if (map != null && map.mapId == mapId)
                {
                    mapData = map;
                    return true;
                }
            }
        }

        mapData = null;
        return false;
    }

    [Serializable]
    public class MapGroundData
    {
        public int mapId;
        public string sceneName;
        public GroundColliderData[] colliders = Array.Empty<GroundColliderData>();
    }

    [Serializable]
    public struct GroundColliderData
    {
        public string name;
        public string layerName;
        public Vector2 position;
        public float rotationZ;
        public Vector2 scale;
        public Vector2 offset;
        public Vector2 size;
        public float edgeRadius;
        public bool isTrigger;
        public bool usedByEffector;
        public bool hasPlatformEffector;
        public bool useOneWay;
        public bool useOneWayGrouping;
        public float surfaceArc;
        public float sideArc;
        public float rotationalOffset;
        public bool useSideFriction;
        public bool useSideBounce;
    }
}
