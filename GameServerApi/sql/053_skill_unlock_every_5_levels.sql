-- Align player skill unlocks with the runtime rule:
-- slot 1 = NORMAL_ATTACK at level 1, then combat skills unlock every 5 player levels.
-- DASH stays level 1 because it is a movement utility, not part of the combat skill chain.

UPDATE skill_template SET level_to_unlock = 1 WHERE skill_code IN ('NORMAL_ATTACK', 'DASH');

UPDATE skill_template SET level_to_unlock = 5  WHERE skill_code IN ('FIRE_BOLT', 'WATER_BOLT', 'METAL_STRIKE', 'EARTH_AURA', 'WIND_STRIKE', 'WOOD_VINE');
UPDATE skill_template SET level_to_unlock = 10 WHERE skill_code IN ('FIRE_BURST', 'WATER_PILLAR', 'METAL_BLADE', 'EARTH_BOOMERANG', 'WIND_BLADE', 'WOOD_ARROW');
UPDATE skill_template SET level_to_unlock = 15 WHERE skill_code IN ('FIRE_RAIN', 'WATER_ARMOR', 'METAL_SHIELD', 'EARTH_BLINK', 'WIND_STEP', 'WOOD_HEAL');

-- Hybrid/fusion skills unlock by fusion gene availability, not player level.
UPDATE skill_template SET level_to_unlock = 1 WHERE skill_code IN (
    'HYBRID_FIRE_EARTH_LAVA_AURA',
    'HYBRID_WATER_WOOD_VENOM',
    'HYBRID_METAL_WIND_BARRAGE'
);
