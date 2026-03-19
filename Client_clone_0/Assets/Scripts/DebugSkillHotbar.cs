using UnityEngine;
public class DebugSkillHotbar : MonoBehaviour {
    void Update() {
        if (Input.GetKeyDown(KeyCode.F9)) {
            var ui = FindObjectOfType<SkillHotbarUI>();
            if (ui != null) {
                for(int i=0; i<ui.slots.Count; i++) {
                    var s = ui.slots[i];
                    var bound = (SkillData)s.GetType().GetField("boundSkill", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(s);
                    if (bound != null) {
                        Debug.LogError($"Slot {i}: {bound.skillName} | canUse: {bound.CanUse()} | cd: {bound.GetCooldownRemaining()} | type: {bound.skillType}");
                    } else {
                        Debug.LogError($"Slot {i}: NULL");
                    }
                }
            }
        }
    }
}
