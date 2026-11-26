"""Original, deterministic industrial pixel materials. Run before BuildArtRoom."""
from pathlib import Path
import json
import hashlib
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/_UKUBOLA/Art/Evolution/Textures'
OUT.mkdir(parents=True, exist_ok=True)
SIZE = 128
PALETTES = [(72, 83, 82), (83, 67, 54), (57, 60, 73)]
manifest = []
manifest_path=OUT.parent/'SOURCE_MANIFEST.json'
if manifest_path.exists():
    previous=json.loads(manifest_path.read_text(encoding='utf-8'))
    for entry in previous.get('files',[]):
        path=ROOT/entry['path']
        if not path.is_relative_to(OUT) or not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest()!=entry['sha256']:
            raise SystemExit(f'Generated texture changed or removed; preserving outputs: {path}')

def material(name, kind, base, seed):
    rng = np.random.default_rng(seed)
    height = Image.new('L', (SIZE, SIZE), 160)
    image = Image.new('RGB', (SIZE, SIZE), base)
    draw, depth = ImageDraw.Draw(image), ImageDraw.Draw(height)
    dark = tuple(int(c * .38) for c in base)
    edge = tuple(min(255, int(c * 1.55)) for c in base)
    if kind == 'Armor':
        # Broad painted plates; the articulated mesh supplies the panel boundaries.
        # Avoid architectural vents and bolts repeating across small body segments.
        draw.line((0,104,127,104),fill=tuple(int(c*.88) for c in base))
        draw.line((0,106,127,106),fill=tuple(int(c*1.08) for c in base))
    elif kind in ('Wall', 'Trim', 'Weapon'):
        for x0, x1 in [(0, 62), (65, 127)]:
            draw.rectangle((x0, 1, x1, 126), fill=base, outline=dark, width=2)
            draw.line((x0+3, 4, x1-3, 4), fill=edge, width=1)
            depth.rectangle((x0, 1, x1, 126), outline=50, width=2)
            for x in (x0+6, x1-6):
                for y in (8, 119):
                    draw.rectangle((x-1, y-1, x+1, y+1), fill=dark)
                    draw.point((x-1, y-1), fill=edge)
                    depth.rectangle((x-1, y-1, x+1, y+1), fill=70)
        if kind == 'Wall':
            for y in range(77, 110, 5):
                draw.rectangle((12, y, 51, y+2), fill=dark)
                draw.line((13, y-1, 51, y-1), fill=edge)
                depth.rectangle((12, y, 51, y+2), fill=60)
            draw.rectangle((77, 20, 115, 23), fill=(110, 133, 130))
    elif kind == 'Floor':
        dark = tuple(int(c * .72) for c in base)
        edge = tuple(min(255, int(c * 1.16)) for c in base)
        for y in range(0, 128, 64):
            for x in range(0, 128, 64):
                draw.rectangle((x+1, y+1, x+62, y+62), fill=base, outline=dark, width=1)
                draw.line((x+3,y+3,x+60,y+3), fill=edge)
                depth.rectangle((x, y, x+63, y+63), outline=55, width=2)
                for j in (6, 56):
                    draw.rectangle((x+j,y+6,x+j+1,y+7), fill=(115,121,115))
    elif kind == 'Ceiling':
        draw.rectangle((0,0,127,127), outline=dark, width=3)
        depth.rectangle((0,0,127,127), outline=45, width=3)
        for y in range(18, 112, 7):
            draw.rectangle((27,y,101,y+3), fill=dark)
            depth.rectangle((27,y,101,y+3), fill=50)
            draw.line((28,y-1,100,y-1),fill=edge)
    elif kind == 'Crate':
        draw.rectangle((3,3,124,124), outline=edge, width=3)
        for x in range(16, 120, 20):
            draw.rectangle((x,9,x+7,118), fill=dark)
            depth.rectangle((x,9,x+7,118),fill=55)
        draw.rectangle((10,90,118,109), fill=(28,33,34))
        for x in range(11,118,14): draw.polygon([(x,90),(x+8,90),(x+20,109),(x+12,109)],fill=(177,128,43))
    elif kind == 'Console':
        draw.rectangle((0,0,127,127),outline=dark,width=4)
        draw.rectangle((9,12,118,86),fill=(9,21,25),outline=edge,width=3)
        for y in range(22,76,9):
            draw.line((18,y,18+int(rng.integers(30,85)),y),fill=(48,171,184),width=2)
        for x in range(15,116,17): draw.rectangle((x,99,x+7,106),fill=(115,119,106))
        depth.rectangle((9,12,118,86), fill=95)
    elif kind == 'Door':
        for y in range(0,128,22):
            draw.rectangle((0,y,127,y+18),fill=base,outline=dark,width=2)
            draw.line((1,y+3,126,y+3),fill=edge)
            depth.rectangle((0,y,127,y+18),outline=45,width=2)
    else:
        yy,xx = np.mgrid[:SIZE,:SIZE]
        wave=(np.sin(xx*.12+np.cos(yy*.09)*2)+np.sin(yy*.16-xx*.04))*.2+.5
        array=np.clip(np.array(base)[None,None,:]*(.5+wave[:,:,None]),0,255).astype('uint8')
        image=Image.fromarray(array); height=Image.fromarray((wave*150+30).astype('uint8'))
    rgb=np.asarray(image).astype(np.float32)
    noise=rng.integers(-3,4,(SIZE,SIZE,1)) if kind=='Armor' else rng.integers(-8,9,(SIZE,SIZE,1))
    rgb=np.clip(rgb+noise,0,255)
    # Scratches are short directional marks; wear follows edges rather than uniform static.
    image=Image.fromarray(rgb.astype('uint8')); draw=ImageDraw.Draw(image)
    for _ in range(18 if kind=='Armor' else 75):
        x,y=rng.integers(4,124,2); length=int(rng.integers(1,6))
        draw.line((int(x),int(y),int(min(x+length,126)),int(y)),fill=tuple(int(c*(.66 if kind=='Armor' else .78)) for c in edge))
    h=np.asarray(height,dtype=np.float32)/255
    dx=(np.roll(h,-1,axis=1)-np.roll(h,1,axis=1))*2.5
    dy=(np.roll(h,-1,axis=0)-np.roll(h,1,axis=0))*2.5
    normal=np.stack((-dx,dy,np.ones_like(h)),axis=-1); normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
    normal=np.clip((normal*.5+.5)*255,0,255).astype('uint8')
    mask=np.zeros((SIZE,SIZE,4),dtype='uint8'); mask[:,:,0]=155; mask[:,:,1]=np.clip(h*220+30,0,255); mask[:,:,3]=95
    if kind=='Floor':
        yy,xx=np.mgrid[:SIZE,:SIZE]; wet=np.sin(xx*.09)+np.cos(yy*.07+xx*.03)>.3
        mask[:,:,3]=np.where(wet,225,105); mask[:,:,0]=110
    if kind in ('HazardToxic','HazardHot','HazardElectric'): mask[:,:,0]=10; mask[:,:,3]=185
    emission=np.zeros((SIZE,SIZE,3),dtype='uint8')
    if kind.startswith('Hazard'): emission=np.clip(np.asarray(image)*.65,0,255).astype('uint8')
    outputs={'Base':image,'Normal':Image.fromarray(normal),'Mask':Image.fromarray(mask),'Emission':Image.fromarray(emission)}
    for suffix,bitmap in outputs.items():
        path=OUT/f'{name}_{suffix}.png'; bitmap.save(path)
        manifest.append({'path':path.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'seed':seed,'source':'Tools/Generate-UkubolaArt.py','kind':suffix})

for stage,base in enumerate(PALETTES):
    for index,kind in enumerate(['Wall','Floor','Ceiling','Trim','Crate','Console']):
        color=tuple(int(c*(.85 if kind=='Floor' else .7 if kind=='Ceiling' else 1)) for c in base)
        material(f'{stage}_{kind}',kind,color,1600+stage*100+index)
for i,(kind,color) in enumerate([('Door',(67,77,80)),('Weapon',(98,78,61)),('Armor',(74,91,85)),('HazardToxic',(44,115,45)),('HazardHot',(189,67,15)),('HazardElectric',(35,88,170))]):
    material(kind,kind,color,2000+i)

# Original 5x7 digit atlas, each cell 16x24, white glyphs tinted by the HUD.
patterns=['11111/10001/10011/10101/11001/10001/11111','00100/01100/00100/00100/00100/00100/01110','11110/00001/00001/01110/10000/10000/11111','11110/00001/00001/01110/00001/00001/11110','10010/10010/10010/11111/00010/00010/00010','11111/10000/10000/11110/00001/00001/11110','01111/10000/10000/11110/10001/10001/01110','11111/00001/00010/00100/01000/01000/01000','01110/10001/10001/01110/10001/10001/01110','01110/10001/10001/01111/00001/00001/11110','11001/11010/00010/00100/01000/01011/10011','00000/00000/00000/11111/00000/00000/00000']
atlas=Image.new('RGBA',(256,24)); d=ImageDraw.Draw(atlas)
for i,pattern in enumerate(patterns):
    for y,row in enumerate(pattern.split('/')):
        for x,on in enumerate(row):
            if on=='1':
                px,py=i*16+2+x*2,4+y*2
                d.rectangle((px+1,py+2,px+2,py+3), fill=(50,50,50,255))
                d.rectangle((px,py,px+1,py+1),fill=(255,255,255,255))
atlas.save(OUT/'Digits.png')
panel=Image.new('RGB',(256,96),(20,27,30)); d=ImageDraw.Draw(panel)
for inset,color in [(0,(112,127,129)),(2,(44,54,57)),(5,(9,14,17)),(7,(35,44,47))]: d.rectangle((inset,inset,255-inset,95-inset),outline=color,width=2)
for x in (12,243):
    for y in (12,83): d.rectangle((x-2,y-2,x+2,y+2),fill=(92,106,106));d.line((x-1,y,x+1,y),fill=(10,15,16))
panel.save(OUT/'HudPanel.png')
yy,xx=np.mgrid[:64,:64];radius=((xx-31.5)**2+(yy-31.5)**2)**.5/32
steam=np.ones((64,64,4),dtype='uint8')*255
steam[:,:,3]=(np.clip(1-radius,0,1)**2*210).astype('uint8')
Image.fromarray(steam).save(OUT/'Steam.png')
for name in ('Digits.png','HudPanel.png','Steam.png'):
    path=OUT/name
    manifest.append({'path':path.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'source':'Tools/Generate-UkubolaArt.py','kind':'UI' if name!='Steam.png' else 'VFX'})
manifest_path.write_text(json.dumps({'generatorVersion':2,'generatorSha256':hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),'license':'Original UKUBOLA resources','files':manifest},indent=2),encoding='utf-8')
print(json.dumps({'materialFamilies':24,'maps':96,'uiTextures':2,'vfxTextures':1,'output':str(OUT)}))
