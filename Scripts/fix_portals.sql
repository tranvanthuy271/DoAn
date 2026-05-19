-- Fix portal spawn positions
-- Right portals: dest_x=-28 → -5 (player arrives on ground near left edge of destination map)
-- Portal 67 also fix dest_y=2.12 → 0 (arrive on main floor, not elevated platform)
UPDATE map_portal SET dest_x=-5, dest_y=0 WHERE portal_id IN (65, 4, 69, 71);
UPDATE map_portal SET dest_x=-5, dest_y=0 WHERE portal_id=67;

-- Left portals: src_x=-28 → -9 (trigger is now reachable, just past left ground edge)
UPDATE map_portal SET src_x=-9, src_y=0 WHERE portal_id IN (64, 66, 3, 68, 70, 72) AND portal_direction='left';
