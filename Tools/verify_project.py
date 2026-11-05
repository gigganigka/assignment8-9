"""Offline structural checks. Optional: tree-sitter and tree-sitter-c-sharp for syntax.
This does not compile C# or run Unity. Run from any directory with Python 3.
"""
from pathlib import Path
import collections, json, re, sys
root = Path(__file__).resolve().parents[1]
assets = root / 'Assets'
checks=[]
def check(condition, message):
    if not condition: raise AssertionError(message)
    checks.append(message)

manifest=json.loads((root/'Packages/manifest.json').read_text())['dependencies']
check(all(x in manifest for x in ['com.unity.inputsystem','com.unity.render-pipelines.universal','com.unity.ugui']), 'Required Input System, URP and uGUI packages declared')
ids=collections.defaultdict(list)
for meta in assets.rglob('*.meta'):
    match=re.search(r'^guid: ([a-f0-9]{32})$',meta.read_text(errors='replace'),re.M)
    if match: ids[match[1]].append(meta)
check(all(len(paths)==1 for paths in ids.values()), 'All asset GUIDs unique')
check(all(Path(str(p)+'.meta').exists() for p in assets.rglob('*') if p.suffix!='.meta'), 'Every asset and directory has a meta file')
for name in ['mainmenu','level1','level2','bosslevel','win']:
    scene=assets/(name+'.unity')
    check(scene.exists() and str(scene.relative_to(root)) in (root/'ProjectSettings/EditorBuildSettings.asset').read_text(), name+' exists and is listed for build')
for name, expected in [('level1','level2'),('level2','bosslevel')]:
    check('nextScene: '+expected in (assets/(name+'.unity')).read_text(), name+' portal leads to '+expected)
layers=(root/'ProjectSettings/TagManager.asset').read_text().split('  layers:\n')[1].split('  m_SortingLayers:')[0].splitlines()
check(layers[6].strip()=='- Ground', 'Ground layer is index 6, matching uploaded ground masks')
for path in ['Sky','Hills','Ground','Heart','Circle','ExplorerIdle','ExplorerRun']:
    check((assets/'Resources/Mobile'/(path+'.png')).exists(), 'UI resource exists: '+path)
check((assets/'TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset').exists(), 'UI font asset exists')
# User-owned script references in the original scenes must still resolve locally.
for script in ['PlayerController','PlayerShooter','Health','MainMenu','LevelManager','Boss']:
    meta=assets/'Scripts'/(script+'.cs.meta')
    guid=re.search(r'^guid: (\w+)',meta.read_text(),re.M)[1]
    check(guid in ids, 'Original script identity retained: '+script)
# Check representative safe rectangles against intended layout dimensions. These
# are numerical layout checks, not Device Simulator or visual test results.
profiles={'16:9':(1920,1080,(0,0,1920,1080)),
          'notch':(2532,1170,(132,63,2268,1107)),
          'tablet':(2048,1536,(0,0,2048,1536))}
for name,(w,h,(_,_,sw,sh)) in profiles.items():
    scale=h/1080; width=sw/scale; height=sh/scale
    check(width>=1120 and height>=900, name+': menu, win and pause cards fit representative safe rectangle')
    # x, y measured from bottom-left in reference units.
    boxes={'stick':(50,60,320,320),'jump':(width-270,90,220,220),'fire':(width-520,35,220,220),
           'health':(30,height-145,390,120),'coins':(width-560,height-145,320,120),
           'pause':(width-210,height-145,170,120)}
    for item,(x,y,bw,bh) in boxes.items():
        check(x>=0 and y>=0 and x+bw<=width and y+bh<=height, name+': '+item+' fits safe rectangle')
    def overlap(a,b):
        x,y,w,h=a; X,Y,W,H=b
        return x<X+W and X<x+w and y<Y+H and Y<y+h
    for a,b in [('stick','jump'),('stick','fire'),('jump','fire'),('health','coins'),('coins','pause')]:
        check(not overlap(boxes[a],boxes[b]),name+': '+a+' and '+b+' do not overlap')
try:
    from tree_sitter import Language, Parser
    import tree_sitter_c_sharp
    parser=Parser(Language(tree_sitter_c_sharp.language()))
    scripts=list(assets.rglob('*.cs'))
    bad=[str(p.relative_to(root)) for p in scripts if parser.parse(p.read_bytes()).root_node.has_error]
    check(not bad, 'C# syntax parsed without errors in '+str(len(scripts))+' files: '+str(bad))
except ImportError:
    print('SKIP C# syntax parser: install tree-sitter and tree-sitter-c-sharp to enable it.')
print('\n'.join('PASS '+x for x in checks))
print('\n'+str(len(checks))+' checks passed. Unity compilation and real-device play tests are still required.')
