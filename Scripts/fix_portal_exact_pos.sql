-- Fix portal spawn positions: exact world coordinates of EdgeLeft/EdgeRight triggers
-- Computed from scene YAML: world_pos = parent_transform + local_override

-- ============================================================
-- RIGHT portals: player arrives at EdgeLeft of DESTINATION map
-- EdgeLeft world_x = -7.46 in ALL maps (same parent + local offset)
-- ============================================================
-- Portal 65: Map99(Map00) right → Map100(Map01) — EdgeLeft of Map01 = (-7.46, 4.877)
UPDATE map_portal SET dest_x=-7.46, dest_y=4.88 WHERE portal_id=65;
-- Portal 67: Map100(Map01) right → Map101(Map02) — EdgeLeft of Map02 = (-7.46, 2.577)
UPDATE map_portal SET dest_x=-7.46, dest_y=2.58 WHERE portal_id=67;
-- Portal 4: Map101(Map02) right → Map102(Map03) — EdgeLeft of Map03 = (-7.46, -1.883)
UPDATE map_portal SET dest_x=-7.46, dest_y=-1.88 WHERE portal_id=4;
-- Portal 69: Map102(Map03) right → Map103(Map04) — EdgeLeft of Map04 = (-7.46, -2.643)
UPDATE map_portal SET dest_x=-7.46, dest_y=-2.64 WHERE portal_id=69;
-- Portal 71: Map103(Map04) right → Map104(Map05) — EdgeLeft of Map05 = (-7.46, 2.177)
UPDATE map_portal SET dest_x=-7.46, dest_y=2.18 WHERE portal_id=71;

-- ============================================================
-- LEFT portals: player arrives at EdgeRight of DESTINATION map
-- EdgeRight world positions per map:
--   Map00: (30.38, -1.335)  Map01: (23.48, 6.257)  Map02: (23.35, 12.407)
--   Map03: (32.92, -1.483)  Map04: (49.40, -2.403)
-- ============================================================
-- Portal 66: Map100(Map01) left → Map99(Map00) — EdgeRight of Map00 = (30.38, -1.335)
UPDATE map_portal SET dest_x=30.38, dest_y=-1.34 WHERE portal_id=66;
-- Portal 3: Map101(Map02) left → Map100(Map01) — EdgeRight of Map01 = (23.48, 6.257)
UPDATE map_portal SET dest_x=23.48, dest_y=6.26 WHERE portal_id=3;
-- Portal 68: Map102(Map03) left → Map101(Map02) — EdgeRight of Map02 = (23.35, 12.407)
UPDATE map_portal SET dest_x=23.35, dest_y=12.41 WHERE portal_id=68;
-- Portal 70: Map103(Map04) left → Map102(Map03) — EdgeRight of Map03 = (32.92, -1.483)
UPDATE map_portal SET dest_x=32.92, dest_y=-1.48 WHERE portal_id=70;
-- Portal 72: Map104(Map05) left → Map103(Map04) — EdgeRight of Map04 = (49.40, -2.403)
UPDATE map_portal SET dest_x=49.40, dest_y=-2.40 WHERE portal_id=72;

-- ============================================================
-- Also sync src_x/src_y to match trigger world positions
-- (server skips distance check for edge portals, but good for consistency)
-- ============================================================
UPDATE map_portal SET src_x=-7.46, src_y=-1.58 WHERE portal_id=64;  -- Map00 left trigger
UPDATE map_portal SET src_x=-7.46, src_y=4.88  WHERE portal_id=66;  -- Map01 left trigger
UPDATE map_portal SET src_x=-7.46, src_y=2.58  WHERE portal_id=3;   -- Map02 left trigger
UPDATE map_portal SET src_x=-7.46, src_y=-1.88 WHERE portal_id=68;  -- Map03 left trigger
UPDATE map_portal SET src_x=-7.46, src_y=-2.64 WHERE portal_id=70;  -- Map04 left trigger
UPDATE map_portal SET src_x=-7.46, src_y=2.18  WHERE portal_id=72;  -- Map05 left trigger

UPDATE map_portal SET src_x=30.38, src_y=-1.34 WHERE portal_id=65;  -- Map00 right trigger
UPDATE map_portal SET src_x=23.48, src_y=6.26  WHERE portal_id=67;  -- Map01 right trigger
UPDATE map_portal SET src_x=23.35, src_y=12.41 WHERE portal_id=4;   -- Map02 right trigger
UPDATE map_portal SET src_x=32.92, src_y=-1.48 WHERE portal_id=69;  -- Map03 right trigger
UPDATE map_portal SET src_x=49.40, src_y=-2.40 WHERE portal_id=71;  -- Map04 right trigger
