import sys
import mysql.connector
sys.stdout.reconfigure(encoding='utf-8')

conn = mysql.connector.connect(
    host='127.0.0.1', user='root', database='gamedb', charset='utf8mb4'
)
c = conn.cursor()

fixes = [
    (1, 'Cấp Độ'),
    (2, 'Nhiệm Vụ'),
    (3, 'Chuyên Cần'),
    (4, 'Phó Bản'),
    (5, 'Vàng'),
]
for id, name in fixes:
    c.execute("UPDATE leaderboard_caches SET Name=%s WHERE Id=%s", (name, id))
conn.commit()

c.execute("SELECT Id, Name FROM leaderboard_caches")
for r in c.fetchall():
    print(r)
conn.close()
print("Leaderboard names fixed!")
