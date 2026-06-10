-- Migration 052: Normalize invalid map/portal quest requirements.
-- required_quest_id = 0 does not point to any quest_config row and locks travel forever.

UPDATE map_config
SET required_quest_id = NULL
WHERE required_quest_id = 0;

UPDATE map_portal
SET required_quest_id = NULL
WHERE required_quest_id = 0;
