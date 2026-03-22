
fpath = r'c:\Hub\DoAn\Client\Assets\Scripts\Player\Combat\PlayerSkillManager.cs'
with open(fpath, 'rb') as f:
    raw = f.read()
content = raw[2:].decode('utf-16-le')

# -----------------------------------------------------------------------
# Fix A: Add debug log to UseSkill
marker_start = '    private void UseSkill(SkillData skill)\r\n    {\r\n        if (skill == null) return;\r\n'
mp_comment_line = '        // ' + '\u2500\u2500' + ' Ki\u1ec3m tra v\xe0 tr\u1eeb MP ' + '\u2500' * 45
marker_end = '\r\n        if (!TryConsumeMP(skill.currentMpCost)) return;'

old_a = marker_start + '\r\n' + mp_comment_line + marker_end
debug_log = '        Debug.Log("[PlayerSkillManager] UseSkill: " + skill.skillName + " | IsOwner=" + IsOwner + " | IsServer=" + IsServer + " | MP=" + dataSync?.networkMp.Value + "/" + dataSync?.networkMaxMp.Value + " | Cost=" + skill.currentMpCost);\r\n'
new_a = marker_start + '\r\n' + debug_log + '\r\n' + mp_comment_line + marker_end

if old_a in content:
    content = content.replace(old_a, new_a)
    print('Fix A (UseSkill debug log) applied OK')
else:
    print('Fix A NOT FOUND')
    idx = content.find('private void UseSkill')
    print(repr(content[idx:idx+300]))

# -----------------------------------------------------------------------
# Fix B: SetOwner after Spawn
old_b = (
    '        // Spawn projectile tr\xean network (ch\u1ec9 server m\u1edbi spawn \u0111\u01b0\u1ee3c)\r\n'
    '        if (IsServer)\r\n'
    '        {\r\n'
    '            projectileNetworkObject.Spawn();\r\n'
    '        }\r\n'
    '        else\r\n'
    '        {'
)
new_b = (
    '        // Spawn projectile tr\xean network (ch\u1ec9 server m\u1edbi spawn \u0111\u01b0\u1ee3c)\r\n'
    '        if (IsServer)\r\n'
    '        {\r\n'
    '            projectileNetworkObject.Spawn();\r\n'
    '\r\n'
    '            // G\xe1n owner \u0111\u1ec3 projectile kh\xf4ng t\u1ef1 g\xe2y damage cho ng\u01b0\u1eddi b\u1eafn\r\n'
    '            ulong ownerId = NetworkObjectId;\r\n'
    '            var fireballDmg = projectile.GetComponent<FireballDamage>();\r\n'
    '            if (fireballDmg != null) fireballDmg.SetOwner(ownerId);\r\n'
    '            var dotDmg = projectile.GetComponent<DotDamage>();\r\n'
    '            if (dotDmg != null) dotDmg.SetOwner(ownerId);\r\n'
    '        }\r\n'
    '        else\r\n'
    '        {'
)

if old_b in content:
    content = content.replace(old_b, new_b)
    print('Fix B (SetOwner after Spawn) applied OK')
else:
    print('Fix B NOT FOUND')
    idx = content.find('projectileNetworkObject.Spawn()')
    print(repr(content[idx-200:idx+200]))

# -----------------------------------------------------------------------
with open(fpath, 'wb') as f:
    f.write(b'\xff\xfe')
    f.write(content.encode('utf-16-le'))
print('File saved.')
