using UnityEngine;

public class ResourceConstant
{
    public const string PREFAB_ROOT = "Prefabs/";
    public const string TILEMAP_ROOT = "Tilemap/";
    public const string IMAGE_ROOT = "Images/";
    public const string DATA_ROOT = "Data/";
    // tag
    public const string UI_TAG_ROOT = "UIRoot";
    public const string CHARACTER_TAG_ROOT = "CharacterRoot";
    public const string TILEMAP_TAG = "TileMap";
    public const string RESOURCE_MAP_TAG = "ResourceMap";
    public const string GLOBAL_LIGHT_TAG = "GlobalLight";
    public const string MINIMAP_TAG = "MiniMap";
    public const string ACTION_UI_TAG = "ActionUI";
    //
    public static readonly Vector3 VECTOR3_DEFAULT = new Vector3(-999.0f,0.0f,0.0f);
    public static readonly Vector3Int VECTOR3INT_DEFAULT = new Vector3Int(-999,0,0);
}
