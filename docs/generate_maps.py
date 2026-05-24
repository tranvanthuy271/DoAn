#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
generate_maps.py
Generates:
  1. Unity .unity scene files for all LangLa world maps
  2. .meta files for each scene
  3. SQL file (maps_full.sql) with map_config + map_portal INSERT statements

Run:
  python generate_maps.py
"""

import os
import re
import uuid

SCENES_DIR    = r"C:\Hub\DoAn\Client\Assets\Scenes"
SQL_OUTPUT    = r"C:\Hub\DoAn\maps_full.sql"
ICON_TREE_DIR = r"C:\Hub\DoAn\Client\Assets\Art\iconTree"

# ─────────────────────────────────────────────────────────────
# Load iconTree sprite GUIDs from .meta files
# ─────────────────────────────────────────────────────────────
def _load_icon_tree_guids():
    guids = {}
    if not os.path.isdir(ICON_TREE_DIR):
        return guids
    for fname in os.listdir(ICON_TREE_DIR):
        if not fname.endswith('.png.meta'):
            continue
        num = fname.replace('.png.meta', '')
        try:
            with open(os.path.join(ICON_TREE_DIR, fname), 'r', encoding='utf-8') as fh:
                m = re.search(r'guid: ([a-f0-9]+)', fh.read())
                if m:
                    guids[num] = m.group(1)
        except Exception:
            pass
    return guids

ICON_TREE_GUIDS = _load_icon_tree_guids()

def tree_guid(num):
    """Return GUID for iconTree/{num}.png, fallback to 147."""
    return ICON_TREE_GUIDS.get(str(num), ICON_TREE_GUIDS.get('147', '00000000000000000000000000000000'))

# typeBlockMap → (bg_sprite_index, overlay_sprite_index)
TYPE_BLOCK_SPRITES = {
    0: (147, 148),   # normal
    1: (195, 196),   # sand
    2: (371, 372),   # forest/rain
    3: (123, 124),   # rocky
    6: (3,   4),     # heavy rain
}

# ─────────────────────────────────────────────────────────────
# Map data: map_id → (name_vi, scene_name, typeBlockMap, bg_sprite, overlay_sprite)
#   typeBlockMap: 0=normal, 1=sand, 2=rain/forest, 3=rock, 6=heavy_rain
#   bg_sprite / overlay_sprite: unique iconTree sprite index per map
# ─────────────────────────────────────────────────────────────
ALL_MAPS = {
    #           name_vi                   scene_name          type  bg    ov
    0:   ("Làng Khởi Đầu",          "GameScene",          0,  147, 148),  # existing
    # World chain maps
    56:  ("Đồi trung tâm",           "Map56",              0,  179, 180),
    57:  ("Thánh Địa Thất Kiếm",     "Map57",              0,  181, 182),
    59:  ("Làng Cát",                "Map59",              1,  197, 198),  # sand
    60:  ("Làng Sương Mù",           "Map60",              0,  377, 378),  # misty
    61:  ("Vách Chigiri",            "Map61",              0,  159, 160),
    62:  ("Núi Kirigakure",          "Map62",              0,  373, 374),
    63:  ("Cánh Đồng Kaminari",      "Map63",              0,  161, 162),
    64:  ("Thung Lũng Chết",         "Map64",              0,  375, 376),  # dark
    65:  ("Đồi Hoang",               "Map65",              0,  165, 166),
    66:  ("Hẻm Núi Mizu",            "Map66",              0,  167, 168),
    68:  ("Làng Cỏ",                 "Map68",              0,  171, 172),
    69:  ("Làng Mây",                "Map69",              0,  383, 384),  # cloudy
    70:  ("Vách Đá Ngang",           "Map70",              0,  125, 126),  # rocky
    71:  ("Miếu Iwagakure",          "Map71",              0,  127, 128),  # stone
    72:  ("Chân Núi Tsuchi",         "Map72",              0,  129, 130),  # mountain
    73:  ("Rừng Nấm",                "Map73",              0,  173, 174),
    74:  ("Dòng Sông Kusagakure",    "Map74",              0,  379, 380),  # river
    75:  ("Làng Lá",                 "Map75",              2,  371, 372),  # forest/rain
    76:  ("Đồng Cỏ Tenchi",          "Map76",              0,  151, 152),
    77:  ("Rừng Kumogakure",         "Map77",              0,  153, 154),
    78:  ("Nghĩa Địa Bỏ Hoang",      "Map78",              0,  369, 370),  # haunted
    79:  ("Chiến Trường Cổ",         "Map79",              0,  155, 156),
    80:  ("Đồi Cát",                 "Map80",              0,  157, 158),
    81:  ("Sa mạc Sunagakure",       "Map81",              1,  195, 196),  # sand
    82:  ("Núi Hokage",              "Map82",              0,  149, 150),
    83:  ("Thung lũng Tận Cùng",     "Map83",              0,  385, 386),
    85:  ("Làng Đá",                 "Map85",              3,  123, 124),  # rock/storm
    86:  ("Trường Konoha",           "Map86",              0,  175, 176),
    87:  ("Hang Khỉ",                "Map87",              0,  381, 382),
    88:  ("Cầu Kannabi",             "Map88",              0,  177, 178),
    98:  ("Chiến trường",            "Map98",              0,  183, 184),
    99:  ("Cửa phía tây",            "Map99",              0,  185, 186),
    100: ("Cửa phía đông",           "Map100",             0,  187, 188),
    102: ("Làng Mưa",                "Map102",             6,    3,   4),  # heavy rain
    103: ("Pháo đài Amega",          "Map103",             6,    6,   7),
    104: ("Vùng trũng Kusa",         "Map104",             6,    8,   9),
    105: ("Lãnh địa thiên thần",     "Map105",             6,   10,  11),
    106: ("Căn cứ Akatsuki",         "Map106",             6,   12,  13),
    # Side / optional maps
    58:  ("Hang Vĩ Thú",             "Map58",              0,  387, 388),
    67:  ("Hầm bí mật",              "Map67",              0,    0,   1),  # dark/cave
    84:  ("Khu luyện tập",           "Map84",              0,  189, 190),
    89:  ("Vòng Lặp Ảo Tưởng",       "Map89",              0,  393, 394),
    90:  ("Hang Vĩ Thú (cấp 1)",     "Map90",              0,  389, 390),
    91:  ("Hang Vĩ Thú (cấp 2)",     "Map91",              0,  391, 392),
    92:  ("Hang Vĩ Thú (cấp 3)",     "Map92",              0,   14,  15),
    93:  ("Hang Gamaken",            "Map93",              0,   16,  17),
    94:  ("Hang Gamatatsu",          "Map94",              0,   18,  19),
    95:  ("Hang Gama Armored",       "Map95",              0,  191, 192),
    96:  ("Hang Gamabunta",          "Map96",              0,  193, 194),
    97:  ("Hang Gamahiro",           "Map97",              0,  395, 396),
    101: ("Chiến trường phó bản",    "Map101",             0,  397, 398),
    # Dungeons (accessible from starting map via dungeon portals)
    6:   ("Địa cung (sơ cấp)",       "Map6",               0,   20,  21),
    7:   ("Địa cung (trung cấp)",    "Map7",               0,   22,  23),
    18:  ("Địa cung (cao cấp)",      "Map18",              0,   24,  25),
    19:  ("Địa cung (thượng cấp)",   "Map19",              0,   26,  27),
    # Keep existing DoAn dungeon maps
    110: ("Vòng lặp vô tận",         "DungeonWaveScene",   0,  399, 400),
    111: ("Địa Cung",                "DungeonPartyScene",  0,  401, 402),
}

# ─────────────────────────────────────────────────────────────
# Main world chain (linear, sequential portals left↔right)
# ─────────────────────────────────────────────────────────────
MAIN_CHAIN = [
    0,   # GameScene (existing) → right to Map75
    75,  # Làng Lá — starting hub
    76,  # Đồng Cỏ Tenchi
    77,  # Rừng Kumogakure
    78,  # Nghĩa Địa Bỏ Hoang
    79,  # Chiến Trường Cổ
    80,  # Đồi Cát
    81,  # Sa mạc Sunagakure
    59,  # Làng Cát (Sand Village)
    61,  # Vách Chigiri
    62,  # Núi Kirigakure
    63,  # Cánh Đồng Kaminari
    64,  # Thung Lũng Chết
    65,  # Đồi Hoang
    66,  # Hẻm Núi Mizu
    60,  # Làng Sương Mù (Mist Village)
    68,  # Làng Cỏ (Grass Village)
    70,  # Vách Đá Ngang
    71,  # Miếu Iwagakure
    72,  # Chân Núi Tsuchi
    73,  # Rừng Nấm
    74,  # Dòng Sông Kusagakure
    69,  # Làng Mây (Cloud Village)
    82,  # Núi Hokage
    85,  # Làng Đá (Stone Village)
    86,  # Trường Konoha
    87,  # Hang Khỉ
    88,  # Cầu Kannabi
    56,  # Đồi trung tâm
    57,  # Thánh Địa Thất Kiếm
    83,  # Thung lũng Tận Cùng
    98,  # Chiến trường
    99,  # Cửa phía tây
    100, # Cửa phía đông
    102, # Làng Mưa (Rain Village)
    103, # Pháo đài Amega
    104, # Vùng trũng Kusa
    105, # Lãnh địa thiên thần
    106, # Căn cứ Akatsuki (final)
]

# Side maps: map_id → source_map_id (they connect back via 'none' portal)
SIDE_AREAS = {
    84:  75,   # Training area ← Leaf Village
    67:  63,   # Secret dungeon ← Kaminari Field
    58:  56,   # Tailed Beast cave ← Central Hill
    89:  88,   # Illusion Loop ← Kannabi Bridge
    90:  58,   # Beast cave var 1 ← Map58
    91:  58,   # Beast cave var 2 ← Map58
    92:  58,   # Beast cave var 3 ← Map58
    93:  87,   # Gamaken ← Monkey cave
    94:  87,   # Gamatatsu ← Monkey cave
    95:  87,   # Gama Armored ← Monkey cave
    96:  87,   # Gamabunta ← Monkey cave
    97:  87,   # Gamahiro ← Monkey cave
    101: 100,  # Battle arena ← East Gate
    6:   0,    # Dungeon I ← Starting map
    7:   0,    # Dungeon II ← Starting map
    18:  0,    # Dungeon III ← Starting map
    19:  0,    # Dungeon IV ← Starting map
}

# ─────────────────────────────────────────────────────────────
# Level ranges: index in chain → (min_level, max_level)
# ─────────────────────────────────────────────────────────────
def chain_levels(idx):
    """Return (min_level, max_level) for a map at position idx in the main chain."""
    step = 5
    base = idx * step
    return (max(1, base - 4), base + step + 10)


# ─────────────────────────────────────────────────────────────
# Unity scene template helpers
# ─────────────────────────────────────────────────────────────

SCENE_HEADER = """\
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!29 &1
OcclusionCullingSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_OcclusionBakeSettings:
    smallestOccluder: 5
    smallestHole: 0.25
    backfaceThreshold: 100
  m_SceneGUID: 00000000000000000000000000000000
  m_OcclusionCullingData: {{fileID: 0}}
--- !u!104 &2
RenderSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 9
  m_Fog: 0
  m_FogColor: {{r: 0.5, g: 0.5, b: 0.5, a: 1}}
  m_FogMode: 3
  m_FogDensity: 0.01
  m_LinearFogStart: 0
  m_LinearFogEnd: 300
  m_AmbientSkyColor: {{r: 0.212, g: 0.227, b: 0.259, a: 1}}
  m_AmbientEquatorColor: {{r: 0.114, g: 0.125, b: 0.133, a: 1}}
  m_AmbientGroundColor: {{r: 0.047, g: 0.043, b: 0.035, a: 1}}
  m_AmbientIntensity: 1
  m_AmbientMode: 3
  m_SubtractiveShadowColor: {{r: 0.42, g: 0.478, b: 0.627, a: 1}}
  m_SkyboxMaterial: {{fileID: 0}}
  m_HaloStrength: 0.5
  m_FlareStrength: 1
  m_FlareFadeSpeed: 3
  m_HaloTexture: {{fileID: 0}}
  m_SpotCookie: {{fileID: 10001, guid: 0000000000000000e000000000000000, type: 0}}
  m_DefaultReflectionMode: 0
  m_DefaultReflectionResolution: 128
  m_ReflectionBounces: 1
  m_ReflectionIntensity: 1
  m_CustomReflection: {{fileID: 0}}
  m_Sun: {{fileID: 0}}
  m_UseRadianceAmbientProbe: 0
--- !u!157 &3
LightmapSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 12
  m_GIWorkflowMode: 1
  m_GISettings:
    serializedVersion: 2
    m_BounceScale: 1
    m_IndirectOutputScale: 1
    m_AlbedoBoost: 1
    m_EnvironmentLightingMode: 0
    m_EnableBakedLightmaps: 0
    m_EnableRealtimeLightmaps: 0
  m_LightmapEditorSettings:
    serializedVersion: 12
    m_Resolution: 2
    m_BakeResolution: 40
    m_AtlasSize: 1024
    m_AO: 0
    m_AOMaxDistance: 1
    m_CompAOExponent: 1
    m_CompAOExponentDirect: 0
    m_ExtractAmbientOcclusion: 0
    m_Padding: 2
    m_LightmapParameters: {{fileID: 0}}
    m_LightmapsBakeMode: 1
    m_TextureCompression: 1
    m_FinalGather: 0
    m_FinalGatherFiltering: 1
    m_FinalGatherRayCount: 256
    m_ReflectionCompression: 2
    m_MixedBakeMode: 2
    m_BakeBackend: 1
    m_PVRSampling: 1
    m_PVRDirectSampleCount: 32
    m_PVRSampleCount: 512
    m_PVRBounces: 2
    m_PVREnvironmentSampleCount: 256
    m_PVREnvironmentReferencePointCount: 2048
    m_PVRFilteringMode: 1
    m_PVRDenoiserTypeDirect: 1
    m_PVRDenoiserTypeIndirect: 1
    m_PVRDenoiserTypeAO: 1
    m_PVRFilterTypeDirect: 0
    m_PVRFilterTypeIndirect: 0
    m_PVRFilterTypeAO: 0
    m_PVREnvironmentMIS: 1
    m_PVRCulling: 1
    m_PVRFilteringGaussRadiusDirect: 1
    m_PVRFilteringGaussRadiusIndirect: 5
    m_PVRFilteringGaussRadiusAO: 2
    m_PVRFilteringAtrousPositionSigmaDirect: 0.5
    m_PVRFilteringAtrousPositionSigmaIndirect: 2
    m_PVRFilteringAtrousPositionSigmaAO: 1
    m_ExportTrainingData: 0
    m_TrainingDataDestination: TrainingData
    m_LightProbeSampleCountMultiplier: 4
  m_LightingDataAsset: {{fileID: 0}}
  m_LightingSettings: {{fileID: 0}}
--- !u!196 &4
NavMeshSettings:
  serializedVersion: 2
  m_ObjectHideFlags: 0
  m_BuildSettings:
    serializedVersion: 3
    agentTypeID: 0
    agentRadius: 0.5
    agentHeight: 2
    agentSlope: 45
    agentClimb: 0.4
    ledgeDropHeight: 0
    maxJumpAcrossDistance: 0
    minRegionArea: 2
    manualCellSize: 0
    cellSize: 0.16666667
    manualTileSize: 0
    tileSize: 256
    buildHeightMesh: 0
    maxJobWorkers: 0
    preserveTilesOutsideBounds: 0
    debug:
      m_Flags: 0
  m_NavMeshData: {{fileID: 0}}
"""

# Inline Camera (main camera with CameraFollow MonoBehaviour)
# CameraFollow script GUID: 8bfae4097a7f7974fae076be36de2aa6
CAMERA_TEMPLATE = """\
--- !u!1 &1000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 1001}}
  - component: {{fileID: 1002}}
  - component: {{fileID: 1003}}
  - component: {{fileID: 1004}}
  m_Layer: 0
  m_Name: Main Camera
  m_TagString: MainCamera
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!81 &1003
AudioListener:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 1000}}
  m_Enabled: 1
--- !u!20 &1002
Camera:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 1000}}
  m_Enabled: 1
  serializedVersion: 2
  m_ClearFlags: 1
  m_BackGroundColor: {{r: 0.19215687, g: 0.3019608, b: 0.4745098, a: 0}}
  m_projectionMatrixMode: 1
  m_GateFitMode: 2
  m_FOVAxisMode: 0
  m_SensorSize: {{x: 36, y: 24}}
  m_LensShift: {{x: 0, y: 0}}
  m_NormalizedViewPortRect:
    serializedVersion: 2
    x: 0
    y: 0
    width: 1
    height: 1
  near clip plane: 0.3
  far clip plane: 1000
  field of view: 60
  orthographic: 1
  orthographic size: 5
  m_Depth: -1
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingPath: -1
  m_TargetTexture: {{fileID: 0}}
  m_TargetDisplay: 0
  m_TargetEye: 3
  m_HDR: 1
  m_AllowMSAA: 1
  m_AllowDynamicResolution: 0
  m_ForceIntoRT: 0
  m_OcclusionCulling: 1
  m_StereoConvergence: 10
  m_StereoSeparation: 0.022
--- !u!4 &1001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 1000}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: -10}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!114 &1004
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 1000}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 8bfae4097a7f7974fae076be36de2aa6, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  target: {{fileID: 0}}
  offset: {{x: 0, y: 2, z: -10}}
  smoothSpeed: 30
  instantFollow: 1
  followX: 1
  followY: 1
  followLocalPlayerOnly: 1
  useBounds: 0
  autoDetectMaxMap: 1
  minBounds: {{x: 0, y: 0}}
  maxBounds: {{x: 0, y: 0}}
"""

# Ground container (child of Map at fileID 5001) + Floor (BoxCollider2D + PlatformEffector2D)
GROUND_TEMPLATE = """\
--- !u!1 &2000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 2001}}
  m_Layer: 6
  m_Name: Ground
  m_TagString: Ground
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &2001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 2000}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {{fileID: 2011}}
  m_Father: {{fileID: 5001}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!1 &2010
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 2011}}
  - component: {{fileID: 2012}}
  - component: {{fileID: 2013}}
  m_Layer: 6
  m_Name: Floor
  m_TagString: Ground
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &2011
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 2010}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: -4.5, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 2001}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!61 &2012
BoxCollider2D:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 2010}}
  m_Enabled: 1
  m_Density: 1
  m_Material: {{fileID: 0}}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_ForceSendLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_ForceReceiveLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_ContactCaptureLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_CallbackLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_IsTrigger: 0
  m_UsedByEffector: 1
  m_UsedByComposite: 0
  m_Offset: {{x: 0, y: 0}}
  m_SpriteTilingProperty:
    border: {{x: 0, y: 0, z: 0, w: 0}}
    pivot: {{x: 0.5, y: 0.5}}
    oldSize: {{x: 0, y: 0}}
    newSize: {{x: 0, y: 0}}
    adaptiveTilingThreshold: 0.5
    drawMode: 0
    adaptiveTiling: 0
  m_AutoTiling: 0
  serializedVersion: 2
  m_Size: {{x: 200, y: 1}}
  m_EdgeRadius: 0
--- !u!251 &2013
PlatformEffector2D:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 2010}}
  m_Enabled: 1
  m_UseColliderMask: 1
  m_ColliderMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RotationalOffset: 0
  m_UseOneWay: 1
  m_UseOneWayGrouping: 0
  m_SurfaceArc: 180
  m_UseSideFriction: 0
  m_UseSideBounce: 0
  m_SideArc: 1
"""

def mapui_template(bg_guid, overlay_guid):
    """
    MapUI (root, scale 1.3) ─> Map (SpriteRenderer bg + children: Overlay, Ground, MaxMap)
    fileIDs:
      MapUI:    GO=4000, TR=4001
      Map:      GO=5000, TR=5001, SR=5002
      Overlay:  GO=6000, TR=6001, SR=6002
    Ground (2001) and MaxMap (7001) are listed as children of Map (5001).
    """
    out = """\
--- !u!1 &4000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 4001}
  m_Layer: 0
  m_Name: MapUI
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &4001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 4000}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 1.81, y: 0.67, z: -9.975066}
  m_LocalScale: {x: 1.3, y: 1.3, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 5001}
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
"""
    out += f"""\
--- !u!1 &5000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 5001}}
  - component: {{fileID: 5002}}
  m_Layer: 0
  m_Name: Map
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &5001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 5000}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: -0.11, y: 3.8, z: 9.975066}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {{fileID: 6001}}
  - {{fileID: 2001}}
  - {{fileID: 7001}}
  m_Father: {{fileID: 4001}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!212 &5002
SpriteRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 5000}}
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 0
  m_RayTraceProcedural: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {{fileID: 10754, guid: 0000000000000000f000000000000000, type: 0}}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 0
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: -1
  m_Sprite: {{fileID: 21300000, guid: {bg_guid}, type: 3}}
  m_Color: {{r: 1, g: 1, b: 1, a: 1}}
  m_FlipX: 0
  m_FlipY: 0
  m_DrawMode: 0
  m_Size: {{x: 1, y: 1}}
  m_AdaptiveModeThreshold: 0.5
  m_SpriteTileMode: 0
  m_WasSpriteAssigned: 1
  m_MaskInteraction: 0
  m_SpriteSortPoint: 0
--- !u!1 &6000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 6001}}
  - component: {{fileID: 6002}}
  m_Layer: 0
  m_Name: MapOverlay
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &6001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 6000}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 13.1, y: 0.06, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 5001}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!212 &6002
SpriteRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 6000}}
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 0
  m_RayTraceProcedural: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {{fileID: 10754, guid: 0000000000000000f000000000000000, type: 0}}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 0
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: -1
  m_Sprite: {{fileID: 21300000, guid: {overlay_guid}, type: 3}}
  m_Color: {{r: 1, g: 1, b: 1, a: 1}}
  m_FlipX: 0
  m_FlipY: 0
  m_DrawMode: 0
  m_Size: {{x: 15, y: 9.12}}
  m_AdaptiveModeThreshold: 0.5
  m_SpriteTileMode: 0
  m_WasSpriteAssigned: 1
  m_MaskInteraction: 0
  m_SpriteSortPoint: 0
"""
    return out


def _maxmap_collider(fid_go, fid_tr, fid_col, name, parent_fid, pos_x, pos_y, size_x, size_y):
    """Standalone Layer-10 BoxCollider2D game object for MaxMap camera bounds."""
    return f"""\
--- !u!1 &{fid_go}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {fid_tr}}}
  - component: {{fileID: {fid_col}}}
  m_Layer: 10
  m_Name: {name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{fid_tr}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {fid_go}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: {pos_x}, y: {pos_y}, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {parent_fid}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!61 &{fid_col}
BoxCollider2D:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {fid_go}}}
  m_Enabled: 1
  m_Density: 1
  m_Material: {{fileID: 0}}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_ForceSendLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_ForceReceiveLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_ContactCaptureLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_CallbackLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_IsTrigger: 0
  m_UsedByEffector: 0
  m_UsedByComposite: 0
  m_Offset: {{x: 0, y: 0}}
  m_SpriteTilingProperty:
    border: {{x: 0, y: 0, z: 0, w: 0}}
    pivot: {{x: 0.5, y: 0.5}}
    oldSize: {{x: 0, y: 0}}
    newSize: {{x: 0, y: 0}}
    adaptiveTilingThreshold: 0.5
    drawMode: 0
    adaptiveTiling: 0
  m_AutoTiling: 0
  serializedVersion: 2
  m_Size: {{x: {size_x}, y: {size_y}}}
  m_EdgeRadius: 0
"""


def maxmap_template():
    """
    MaxMap container (child of Map at fileID 5001) + 3 boundary BoxCollider2D on Layer 10.
    CameraFollow.cs reads these to determine min/maxBounds:
      Left wall  x=-45  (tall)   → camera minX bound
      Right wall x=+45  (tall)   → camera maxX bound
      Top ceil   y=+15  (wide)   → camera maxY bound
    """
    out = """\
--- !u!1 &7000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 7001}
  m_Layer: 0
  m_Name: MaxMap
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &7001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 7000}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 7011}
  - {fileID: 7021}
  - {fileID: 7031}
  m_Father: {fileID: 5001}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
"""
    out += _maxmap_collider(7010, 7011, 7012, "MaxMap (left)",  7001, -45,   0,  5, 60)
    out += _maxmap_collider(7020, 7021, 7022, "MaxMap (right)", 7001, +45,   0,  5, 60)
    out += _maxmap_collider(7030, 7031, 7032, "MaxMap (top)",   7001,   0, +15, 90,  5)
    return out


# MapEdges container
def map_edges_template(has_left, has_right):
    """Generate the MapEdges container. The portal stripped transforms are listed as children."""
    children = []
    if has_left:
        children.append("  - {fileID: 3011}")
    if has_right:
        children.append("  - {fileID: 3021}")
    children_yaml = "\n".join(children) if children else ""
    return f"""\
--- !u!1 &3000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 3001}}
  m_Layer: 0
  m_Name: MapEdges
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &3001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 3000}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
{children_yaml}
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
"""

# Right portal (EdgeRight) prefab instance
# guid: 3b5457cef5d7f9447b8a62333afae39c
# fileIDs within the prefab (known from existing scenes):
#   3607581679510828760 → m_Name
#   5410273480956339150 → Transform
#   5455916139836146813 → currentMapId
#   8876727282495522588 → m_text (label)
#   6165142847357503239 → added component
def right_portal_template(map_id, map_name, pos_x=33.0, pos_y=0.0):
    # Escape Vietnamese name for YAML (use unicode escapes or raw UTF-8)
    return f"""\
--- !u!1001 &3020
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {{fileID: 3001}}
    m_Modifications:
    - target: {{fileID: 3607581679510828760, guid: 3b5457cef5d7f9447b8a62333afae39c, type: 3}}
      propertyPath: m_Name
      value: EdgeRight
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5410273480956339150, guid: 3b5457cef5d7f9447b8a62333afae39c, type: 3}}
      propertyPath: m_LocalPosition.x
      value: {pos_x}
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5410273480956339150, guid: 3b5457cef5d7f9447b8a62333afae39c, type: 3}}
      propertyPath: m_LocalPosition.y
      value: {pos_y}
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5410273480956339150, guid: 3b5457cef5d7f9447b8a62333afae39c, type: 3}}
      propertyPath: m_LocalPosition.z
      value: -0.47571796
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5410273480956339150, guid: 3b5457cef5d7f9447b8a62333afae39c, type: 3}}
      propertyPath: m_LocalRotation.w
      value: 1
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5410273480956339150, guid: 3b5457cef5d7f9447b8a62333afae39c, type: 3}}
      propertyPath: m_LocalRotation.x
      value: -0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5410273480956339150, guid: 3b5457cef5d7f9447b8a62333afae39c, type: 3}}
      propertyPath: m_LocalRotation.y
      value: -0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5410273480956339150, guid: 3b5457cef5d7f9447b8a62333afae39c, type: 3}}
      propertyPath: m_LocalRotation.z
      value: -0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5410273480956339150, guid: 3b5457cef5d7f9447b8a62333afae39c, type: 3}}
      propertyPath: m_LocalEulerAnglesHint.x
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5410273480956339150, guid: 3b5457cef5d7f9447b8a62333afae39c, type: 3}}
      propertyPath: m_LocalEulerAnglesHint.y
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5410273480956339150, guid: 3b5457cef5d7f9447b8a62333afae39c, type: 3}}
      propertyPath: m_LocalEulerAnglesHint.z
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5455916139836146813, guid: 3b5457cef5d7f9447b8a62333afae39c, type: 3}}
      propertyPath: currentMapId
      value: {map_id}
      objectReference: {{fileID: 0}}
    - target: {{fileID: 8876727282495522588, guid: 3b5457cef5d7f9447b8a62333afae39c, type: 3}}
      propertyPath: m_text
      value: {map_name}
      objectReference: {{fileID: 0}}
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents:
    - targetCorrespondingSourceObject: {{fileID: 6165142847357503239, guid: 3b5457cef5d7f9447b8a62333afae39c, type: 3}}
      insertIndex: -1
      addedObject: {{fileID: 0}}
  m_SourcePrefab: {{fileID: 100100000, guid: 3b5457cef5d7f9447b8a62333afae39c, type: 3}}
--- !u!4 &3021 stripped
Transform:
  m_CorrespondingSourceObject: {{fileID: 5410273480956339150, guid: 3b5457cef5d7f9447b8a62333afae39c, type: 3}}
  m_PrefabInstance: {{fileID: 3020}}
  m_PrefabAsset: {{fileID: 0}}
"""

# Left portal (EdgeLeft) prefab instance
# guid: 009f854fc0d81c444b4a42a03d251105
# fileIDs within the prefab:
#   452233973403747226 → direction + currentMapId
#   2069323437285224715 → m_Name
#   5638256326072347985 → Transform (rotation Y=-1 → faces left)
#   8682287568831445787 → added component
def left_portal_template(map_id, pos_x=-33.0, pos_y=0.0):
    return f"""\
--- !u!1001 &3010
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {{fileID: 3001}}
    m_Modifications:
    - target: {{fileID: 452233973403747226, guid: 009f854fc0d81c444b4a42a03d251105, type: 3}}
      propertyPath: direction
      value: left
      objectReference: {{fileID: 0}}
    - target: {{fileID: 452233973403747226, guid: 009f854fc0d81c444b4a42a03d251105, type: 3}}
      propertyPath: currentMapId
      value: {map_id}
      objectReference: {{fileID: 0}}
    - target: {{fileID: 2069323437285224715, guid: 009f854fc0d81c444b4a42a03d251105, type: 3}}
      propertyPath: m_Name
      value: EdgeLeft
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5638256326072347985, guid: 009f854fc0d81c444b4a42a03d251105, type: 3}}
      propertyPath: m_LocalPosition.x
      value: {pos_x}
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5638256326072347985, guid: 009f854fc0d81c444b4a42a03d251105, type: 3}}
      propertyPath: m_LocalPosition.y
      value: {pos_y}
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5638256326072347985, guid: 009f854fc0d81c444b4a42a03d251105, type: 3}}
      propertyPath: m_LocalPosition.z
      value: -0.12225317
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5638256326072347985, guid: 009f854fc0d81c444b4a42a03d251105, type: 3}}
      propertyPath: m_LocalRotation.w
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5638256326072347985, guid: 009f854fc0d81c444b4a42a03d251105, type: 3}}
      propertyPath: m_LocalRotation.x
      value: -0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5638256326072347985, guid: 009f854fc0d81c444b4a42a03d251105, type: 3}}
      propertyPath: m_LocalRotation.y
      value: -1
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5638256326072347985, guid: 009f854fc0d81c444b4a42a03d251105, type: 3}}
      propertyPath: m_LocalRotation.z
      value: -0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5638256326072347985, guid: 009f854fc0d81c444b4a42a03d251105, type: 3}}
      propertyPath: m_LocalEulerAnglesHint.x
      value: 180
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5638256326072347985, guid: 009f854fc0d81c444b4a42a03d251105, type: 3}}
      propertyPath: m_LocalEulerAnglesHint.y
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 5638256326072347985, guid: 009f854fc0d81c444b4a42a03d251105, type: 3}}
      propertyPath: m_LocalEulerAnglesHint.z
      value: 180
      objectReference: {{fileID: 0}}
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents:
    - targetCorrespondingSourceObject: {{fileID: 8682287568831445787, guid: 009f854fc0d81c444b4a42a03d251105, type: 3}}
      insertIndex: -1
      addedObject: {{fileID: 0}}
  m_SourcePrefab: {{fileID: 100100000, guid: 009f854fc0d81c444b4a42a03d251105, type: 3}}
--- !u!4 &3011 stripped
Transform:
  m_CorrespondingSourceObject: {{fileID: 5638256326072347985, guid: 009f854fc0d81c444b4a42a03d251105, type: 3}}
  m_PrefabInstance: {{fileID: 3010}}
  m_PrefabAsset: {{fileID: 0}}
"""

def scene_roots():
    return """\
--- !u!1660057539 &9223372036854775807
SceneRoots:
  m_ObjectHideFlags: 0
  m_Roots:
  - {fileID: 1001}
  - {fileID: 4001}
  - {fileID: 3001}
"""


def generate_scene(map_id, has_left=True, has_right=True):
    """Generate a complete Unity scene with MapUI/background/MaxMap bounds."""
    name, scene_name, typeBlockMap, bg_num, overlay_num = ALL_MAPS[map_id]

    bg_guid      = tree_guid(bg_num)
    overlay_guid = tree_guid(overlay_num)

    parts = [
        SCENE_HEADER,
        CAMERA_TEMPLATE,
        mapui_template(bg_guid, overlay_guid),
        GROUND_TEMPLATE,
        maxmap_template(),
        map_edges_template(has_left, has_right),
    ]
    if has_left:
        parts.append(left_portal_template(map_id))
    if has_right:
        parts.append(right_portal_template(map_id, name))
    parts.append(scene_roots())
    return "".join(parts)


def generate_meta(guid_str):
    """Generate a .meta file for a Unity scene asset."""
    return f"""\
fileFormatVersion: 2
guid: {guid_str}
DefaultImporter:
  externalObjects: {{}}
  userData: 
  labels: []
  assetBundleName: 
  assetBundleVariant: 
"""


# ─────────────────────────────────────────────────────────────
# SQL generation
# ─────────────────────────────────────────────────────────────

def escape_sql_str(s):
    """Escape a string for SQL INSERT."""
    return s.replace("'", "\\'")


def generate_sql():
    lines = []
    lines.append("-- ========================================================")
    lines.append("-- LangLa World Maps — Auto-generated by generate_maps.py")
    lines.append("-- ========================================================\n")

    # ── map_config ──
    lines.append("-- Delete old test map entries (keep 0=starting, 110, 111)")
    lines.append("DELETE FROM `map_config` WHERE `map_id` NOT IN (0, 110, 111);")
    lines.append("")
    lines.append("-- Insert or update all LangLa world maps")
    lines.append("INSERT INTO `map_config` (`map_id`, `map_name`, `scene_name`, `spawn_points_json`, `min_level`, `max_level`) VALUES")

    rows = []
    # Update map 0 to match LangLa (keep scene GameScene but update name)
    rows.append(f"(0, 'Làng Khởi Đầu', 'GameScene', '[{{\"x\":0,\"y\":0}}]', 1, 10)")

    # Add all world maps (sorted by id for readability)
    for chain_idx, mid in enumerate(MAIN_CHAIN):
        if mid == 0:
            continue  # already added above
        name, scene, *_ = ALL_MAPS[mid]
        min_lv, max_lv = chain_levels(chain_idx)
        row = f"({mid}, '{escape_sql_str(name)}', '{scene}', '[{{\"x\":0,\"y\":0}}]', {min_lv}, {max_lv})"
        rows.append(row)

    # Side maps
    side_levels = {
        84: (1, 15),
        67: (50, 70),
        58: (135, 160),
        89: (130, 155),
        90: (135, 160), 91: (145, 170), 92: (155, 180),
        93: (125, 150), 94: (135, 160), 95: (145, 170), 96: (155, 180), 97: (165, 190),
        101: (160, 185),
        6: (10, 30), 7: (30, 60), 18: (80, 110), 19: (110, 140),
    }
    for mid in sorted(SIDE_AREAS.keys()):
        if mid in ALL_MAPS:
            name, scene, *_ = ALL_MAPS[mid]
            min_lv, max_lv = side_levels.get(mid, (1, 999))
            row = f"({mid}, '{escape_sql_str(name)}', '{scene}', '[{{\"x\":0,\"y\":0}}]', {min_lv}, {max_lv})"
            rows.append(row)

    lines.append(",\n".join(rows) + "\nON DUPLICATE KEY UPDATE")
    lines.append("  `map_name`   = VALUES(`map_name`),")
    lines.append("  `scene_name` = VALUES(`scene_name`),")
    lines.append("  `min_level`  = VALUES(`min_level`),")
    lines.append("  `max_level`  = VALUES(`max_level`);")
    lines.append("")

    # ── map_portal ──
    lines.append("-- Delete ALL old portals")
    lines.append("DELETE FROM `map_portal`;")
    lines.append("")
    lines.append("-- Reset AUTO_INCREMENT")
    lines.append("ALTER TABLE `map_portal` AUTO_INCREMENT = 1;")
    lines.append("")
    lines.append("INSERT INTO `map_portal` (`portal_id`, `portal_name`, `source_map_id`, `src_x`, `src_y`, `src_radius`, `dest_map_id`, `dest_scene_name`, `dest_x`, `dest_y`, `portal_type`, `portal_direction`, `required_item_id`, `dungeon_id`, `is_active`) VALUES")

    portal_rows = []
    pid = 1

    # Chain portals: right and left for each adjacent pair
    for i in range(len(MAIN_CHAIN) - 1):
        src_id = MAIN_CHAIN[i]
        dst_id = MAIN_CHAIN[i + 1]
        _, src_scene, *_ = ALL_MAPS[src_id]
        _, dst_scene, *_ = ALL_MAPS[dst_id]

        src_name_raw, *_ = ALL_MAPS[src_id]
        dst_name_raw, *_ = ALL_MAPS[dst_id]
        src_name = escape_sql_str(src_name_raw)
        dst_name = escape_sql_str(dst_name_raw)

        # right portal: src → dst  (right edge of src_id leads to dst_id)
        portal_rows.append(
            f"({pid}, '{src_name} → {dst_name}', {src_id}, 30, 0, 2.5, "
            f"{dst_id}, '{dst_scene}', -28, 0, 'world_travel', 'right', NULL, NULL, 1)"
        )
        pid += 1

        # left portal: dst → src  (left edge of dst_id leads back to src_id)
        portal_rows.append(
            f"({pid}, '{dst_name} ← {src_name}', {dst_id}, -28, 0, 2.5, "
            f"{src_id}, '{src_scene}', 28, 0, 'world_travel', 'left', NULL, NULL, 1)"
        )
        pid += 1

    # Side area portals (direction='none' — accessed by MapPortalTrigger)
    for side_id, src_id in sorted(SIDE_AREAS.items()):
        if side_id not in ALL_MAPS or src_id not in ALL_MAPS:
            continue
        _, src_scene, *_ = ALL_MAPS[src_id]
        _, side_scene, *_ = ALL_MAPS[side_id]
        side_name_raw, *_ = ALL_MAPS[side_id]
        src_name_raw, *_ = ALL_MAPS[src_id]
        side_name = escape_sql_str(side_name_raw)
        src_name = escape_sql_str(src_name_raw)

        # Entry: src_map → side_map
        portal_rows.append(
            f"({pid}, '{src_name} → {side_name}', {src_id}, 0, 0, 3, "
            f"{side_id}, '{side_scene}', 0, 0, 'world_travel', 'none', NULL, NULL, 1)"
        )
        pid += 1

        # Exit: side_map → src_map
        portal_rows.append(
            f"({pid}, '{side_name} ← {src_name}', {side_id}, 0, 0, 3, "
            f"{src_id}, '{src_scene}', 5, 0, 'world_travel', 'none', NULL, NULL, 1)"
        )
        pid += 1

    # Dungeon enter/exit portals (keep existing DungeonWaveScene + DungeonPartyScene)
    portal_rows.append(
        f"({pid}, 'Vào Vòng lặp vô tận', 0, 5, 0, 3, "
        f"110, 'DungeonWaveScene', 0, 0, 'enter_dungeon', 'none', NULL, 110, 1)"
    )
    pid += 1
    portal_rows.append(
        f"({pid}, 'Vào Địa Cung', 0, -5, 0, 3, "
        f"111, 'DungeonPartyScene', 0, 0, 'enter_dungeon', 'none', NULL, 111, 1)"
    )
    pid += 1

    lines.append(",\n".join(portal_rows) + ";")
    lines.append("")
    lines.append("-- Reset AUTO_INCREMENT to next available id")
    lines.append(f"ALTER TABLE `map_portal` AUTO_INCREMENT = {pid + 1};")
    lines.append("")

    return "\n".join(lines)


# ─────────────────────────────────────────────────────────────
# Main execution
# ─────────────────────────────────────────────────────────────

def main():
    skip_scenes = {
        "GameScene", "DungeonWaveScene", "DungeonPartyScene",
        "Map1", "Map2", "Map3", "Map4", "Map6",
        "Map01", "Map02", "Map03", "Map04",
    }

    has_right_portal = set()
    has_left_portal  = set()
    for i in range(len(MAIN_CHAIN) - 1):
        has_right_portal.add(MAIN_CHAIN[i])
    for i in range(1, len(MAIN_CHAIN)):
        has_left_portal.add(MAIN_CHAIN[i])
    for side_id in SIDE_AREAS:
        has_left_portal.add(side_id)
    for d in [6, 7, 18, 19]:
        has_left_portal.discard(d)
        has_right_portal.discard(d)

    print("=== Generating Unity scenes (v2 – MapUI+Background+MaxMap) ===")
    scenes_written = 0
    scenes_skipped = 0

    for map_id, (name, scene_name, typeBlockMap, bg_num, _) in sorted(ALL_MAPS.items()):
        if scene_name in skip_scenes:
            print(f"  SKIP  {scene_name:<30} (pre-existing)")
            scenes_skipped += 1
            continue

        unity_path = os.path.join(SCENES_DIR, f"{scene_name}.unity")
        meta_path  = unity_path + ".meta"

        # Re-use existing GUID so EditorBuildSettings references survive
        existing_guid = None
        if os.path.isfile(meta_path):
            with open(meta_path, 'r', encoding='utf-8') as fh:
                m = re.search(r'guid: ([a-f0-9]+)', fh.read())
                if m:
                    existing_guid = m.group(1)
        guid_str = existing_guid or uuid.uuid4().hex

        content = generate_scene(
            map_id,
            has_left  = map_id in has_left_portal,
            has_right = map_id in has_right_portal,
        )
        with open(unity_path, "w", encoding="utf-8") as f:
            f.write(content)

        if not existing_guid:
            with open(meta_path, "w", encoding="utf-8") as f:
                f.write(generate_meta(guid_str))

        print(f"  WRITE {scene_name:<30} map={map_id:3d} type={typeBlockMap} bg=iconTree/{bg_num}")
        scenes_written += 1

    print(f"\n  Written: {scenes_written}  Skipped: {scenes_skipped}")

    # ── Generate SQL ──
    print(f"\n=== Generating SQL → {SQL_OUTPUT} ===")
    sql = generate_sql()
    with open(SQL_OUTPUT, "w", encoding="utf-8") as f:
        f.write(sql)
    chain_portals = 2 * (len(MAIN_CHAIN) - 1)
    side_portals  = 2 * len([s for s in SIDE_AREAS if s in ALL_MAPS])
    print(f"  SQL written.  Chain portals: {chain_portals}  Side portals: {side_portals}")


if __name__ == "__main__":
    main()
