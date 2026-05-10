import struct, zlib, io

K = ' 0123456789+-*=\'"\\/_?.,\u02cb\u02ca~\u02c0:;|<>[]{}!@#$%^&*()a\u00e1\u00e0\u1ea3\u00e3\u1ea1\u00e2\u1ea5\u1ea7\u1ea9\u1eab\u1ead\u0103\u1eaf\u1eb1\u1eb3\u1eb5\u1eb7bcd\u0111e\u00e9\u00e8\u1ebb\u1ebd\u1eb9\u00ea\u1ebf\u1ec1\u1ec3\u1ec5\u1ec7fghi\u00ed\u00ec\u1ec9\u0129\u1ecbjklmno\u00f3\u00f2\u1ecf\u00f5\u1ecd\u00f4\u1ed1\u1ed3\u1ed5\u1ed7\u1ed9\u01a1\u1edb\u1edd\u1edf\u1ee1\u1ee3pqrstu\u00fa\u00f9\u1ee7\u0169\u1ee5\u01b0\u1ee9\u1eeb\u1eed\u1eef\u1ef1vxy\u00fd\u1ef3\u1ef7\u1ef9\u1ef5zwA\u00c1\u00c0\u1ea2\u00c3\u1ea0\u00c2\u1ea4\u1ea6\u1ea8\u1eaa\u1eac\u0102\u1eae\u1eb0\u1eb2\u1eb4\u1eb6BCD\u0110E\u00c9\u00c8\u1eba\u1ebc\u1eb8\u00ca\u1ebe\u1ec0\u1ec2\u1ec4\u1ec6FGHI\u00cd\u00cc\u1ec8\u0128\u1ecaJKLMNO\u00d3\u00d2\u1ece\u00d5\u1ecc\u00d4\u1ed0\u1ed2\u1ed4\u1ed6\u1ed8\u01a0\u1eda\u1edc\u1ede\u1ee0\u1ee2PQRSTU\u00da\u00d9\u1ee6\u0168\u1ee4\u01af\u1ee8\u1eea\u1eec\u1eee\u1ef0VXY\u00dd\u1ef2\u1ef6\u1ef8\u1ef4ZW'

with open(r'C:\Nro\LangLa\LangLaServer\LangLaServer\data\arr_data_game.bin', 'rb') as f:
    raw = f.read()

data = zlib.decompress(raw)
stream = io.BytesIO(data)

def rb(): return struct.unpack('>b', stream.read(1))[0]
def rub(): return struct.unpack('>B', stream.read(1))[0]
def rs(): return struct.unpack('>h', stream.read(2))[0]
def rus(): return struct.unpack('>H', stream.read(2))[0]
def ri(): return struct.unpack('>i', stream.read(4))[0]
def rbool(): return struct.unpack('>B', stream.read(1))[0] != 0

def rutf():
    first = rub()
    if first == 0:
        length = rus()
        return stream.read(length).decode('utf-8', errors='replace')
    else:
        result = ''
        for _ in range(first):
            idx = rub()
            if idx < len(K): result += K[idx]
        return result

# DataIconChar
for _ in range(rub()): rs()
# DataNameClass
for _ in range(rub()): rutf()
# DataNameChar
for _ in range(rub()): rutf(); rb(); rs()
# DataTemplateAchievement
for _ in range(rub()): rb(); rutf(); ri(); ri(); ri(); ri(); ri(); rutf()
# Task
task_count = rs()
for _ in range(task_count):
    rutf(); rs(); rs(); rs(); rs(); rs(); rutf(); rutf(); rutf(); ri(); ri(); ri(); ri(); rutf()
    step_count = rb()
    for _ in range(step_count): rb(); rutf(); rs(); rs(); rs(); rs(); rs(); rs(); rs(); rutf(); rutf()
# DataTaskDay
for _ in range(rub()): rb(); rutf(); rs()
# MapTemplate
map_count = rs()
print(f'Map count: {map_count}')
maps = []
for i in range(map_count):
    name = rutf()
    typeBlockMap = rub()
    mtype = rb()
    maps.append((i, name, typeBlockMap, mtype))
    print(f'Map[{i}]: name={name} typeBlockMap={typeBlockMap} type={mtype}')
