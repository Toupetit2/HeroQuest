"""Offline geometry/graph checks against the uploaded Unity prefab. Does not run Unity."""
from pathlib import Path
from collections import deque
from random import Random
import re
root=Path(__file__).resolve().parents[1]
scene=(root/'Prefabs/HeroQuest/Plateau.prefab').read_text()
walls=set()
for chunk in re.split(r'(?=^--- !u!\d+ &\d+)', scene, flags=re.M):
    if not chunk.startswith('--- !u!1001'): continue
    coords={a:float(v) for a,v in re.findall(r'propertyPath: m_LocalPosition\.(x|y|z)\s+value: ([^\n]+)',chunk)}
    if 'x' in coords and 'z' in coords: walls.add((round(coords['x']),round(coords['z'])))
rows=[r.split() for r in (root/'Data/PCG/board_prefab.txt').read_text().strip().splitlines()]
assert len(rows)==19 and all(len(r)==26 for r in rows)
zone={(x,18-y): (int(s[1:]) if s!='.' else 0) for y,r in enumerate(rows) for x,s in enumerate(r)}
assert set(zone.values())==set(range(23))
edges=[]
for (x,y),a in zone.items():
    for dx,dy in ((1,0),(0,1)):
        nxt=(x+dx,y+dy)
        if nxt not in zone or zone[nxt]==a: continue
        key=(-25+2*x+dx, -18+2*y+dy)
        if key in walls: edges.append((a,zone[nxt],key))
assert len(edges)>200

def check_one(seed, rooms, extra, secret):
    rng=Random(seed)
    candidates=edges[:];rng.shuffle(candidates)
    active={0};opened=set();secret_edges=set();pairs=set();room_ids=set()
    while len(room_ids)<rooms:
        frontier=[e for e in candidates if (e[0] in active)!=(e[1] in active)]
        assert frontier, f'Cannot expand at {seed}, {rooms}'
        a,b,k=rng.choice(frontier)
        room=b if a in active else a
        active.add(room);room_ids.add(room);opened.add(k)
        pairs.add(tuple(sorted((a,b))))
    for a,b,k in candidates:
        pair=tuple(sorted((a,b)))
        if a in active and b in active and pair not in pairs and len(opened)<21 and rng.random()<extra:
            opened.add(k);pairs.add(pair)
            if rng.random()<secret: secret_edges.add(k)
    assert len(room_ids)==rooms and len(opened)<=21
    # Secret doors must never be a required path: verify connectivity without them.
    start=next(pt for pt,v in zone.items() if v==0)
    seen={start};q=deque([start])
    while q:
        x,y=q.popleft()
        for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)):
            nxt=(x+dx,y+dy)
            if nxt not in zone or nxt in seen or zone[nxt] not in active: continue
            key=(-25+2*x+dx,-18+2*y+dy)
            if key in walls and (key not in opened or key in secret_edges): continue
            seen.add(nxt);q.append(nxt)
    assert len(seen)==sum(z in active for z in zone.values())
    # A room chosen for stairs needs space for a 2x2 footprint.
    assert any(all(zone.get((x+dx,y+dy))==rid for dy in (0,1) for dx in (0,1))
               for (x,y),rid in zone.items() if rid in room_ids)

for rooms in (1,2,3,6,12,18,21):
    for seed in range(250):
        check_one(seed+rooms*10000,rooms,0.22,0.35)
print('PASS: 1,750 fixed-board / door-limit / secret-door / stairs-availability graph cases')
print('Physical supply from rulebook p. 4: 31 monsters / 15 furniture / 21 doors.')
print('NOTE: No Unity C# compiler/scene execution is involved in these tests.')
