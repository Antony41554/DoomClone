"""Create and extract a local two-build preview; publication remains a separate step."""
import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import shutil
import uuid
import zipfile

ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser()
parser.add_argument('--build',type=Path,required=True)
parser.add_argument('--qa',type=Path,action='append',required=True)
parser.add_argument('--commit',required=True)
args=parser.parse_args()
build=args.build.resolve()
if not build.is_relative_to(ROOT/'Builds') or not (build/'BUILD_COMPLETE.json').is_file():
    raise SystemExit('A completed local build is required')
if len(args.commit)!=40 or any(c not in '0123456789abcdef' for c in args.commit):
    raise SystemExit('Full source commit required')
reports=[]
current_executable_sha=hashlib.sha256((build/'UKUBOLA.exe').read_bytes()).hexdigest()
for report_path in args.qa:
    path=report_path.resolve()
    if not path.is_relative_to(ROOT/'Evidence'): raise SystemExit('QA evidence must be local')
    data=json.loads(path.read_text(encoding='utf-8-sig'))
    launch=json.loads((path.parent/'launch.json').read_text(encoding='utf-8-sig'))
    if not data['success'] or data.get('environment')!='WindowsPlayer' or data['scene']!='OutpostSlice' or not data['snapshotRoundTrip'] or not launch.get('validationPassed') or launch.get('exitCode')!=0:
        raise SystemExit('Outpost native QA has not passed')
    if Path(launch['executable']).resolve()!=build/'UKUBOLA.exe' or launch['executableSha256']!=current_executable_sha:
        raise SystemExit('QA ran another executable')
    data['captures']=[Path(p).name for p in data['captures']]
    reports.append(data)
if not any(r['mode']=='route' for r in reports) or not any(r['mode']=='capture' and len(r['behaviorChecks'])>=9 for r in reports):
    raise SystemExit('Both live-route and component QA are required')

def sha(path):
    value=hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda:stream.read(1024*1024),b''): value.update(block)
    return value.hexdigest()

legacy=ROOT/'Builds/UKUBOLA_Legacy_baee6d2_QA2.zip'
if sha(legacy)!='047e30d7d1da16b5a83f5f9740c5db8c139343bdf3dfdb03e35ef2ee5f1cb961':
    raise SystemExit('Conserved legacy ZIP differs from the accepted snapshot')
stem='UKUBOLA_EvolutionPreview_2026-11-26_Windows_x64'
output=ROOT/'Builds'/f'{stem}.zip'
manifest_path=ROOT/'Builds'/f'{stem}.release-assets.json'
summary_path=ROOT/'Builds'/f'{stem}.validation.json'
checksum_path=ROOT/'Builds'/f'{stem}.sha256.txt'
for path in (output,manifest_path,summary_path,checksum_path):
    if path.exists(): raise SystemExit(f'Preserving existing artifact: {path}')
records=[]
names=set()
def allowed(name):
    return not any('dontship' in p.lower().replace('_','') or 'donotship' in p.lower().replace('_','') for p in PurePosixPath(name).parts) and Path(name).suffix.lower() not in ('.pdb','.mdb','.log')
def entry(archive,name,source):
    path=PurePosixPath(name)
    if path.is_absolute() or '..' in path.parts or '\\' in name or name.casefold() in names:
        raise ValueError('Unsafe or duplicate ZIP member: '+name)
    names.add(name.casefold());digest=hashlib.sha256();size=0
    info=zipfile.ZipInfo(name,date_time=(2026,11,26,0,0,0));info.compress_type=zipfile.ZIP_DEFLATED
    with archive.open(info,'w',force_zip64=True) as destination:
        while block:=source.read(1024*1024): destination.write(block);digest.update(block);size+=len(block)
    records.append({'path':name,'bytes':size,'sha256':digest.hexdigest()})
def text_entry(archive,name,value):
    import io
    entry(archive,name,io.BytesIO(value.encode('utf-8')))

guide='''UKUBOLA — EVOLUTION PREVIEW — WINDOWS x64

1. Extraer completamente este ZIP.
2. Play_Outpost.cmd: nueva sección industrial, siete enemigos, pistola/escopeta,
   elevador, secreto, refrigerante, objetivos y guardado v2.
3. Play_Original_Campaign.cmd: campaña anterior de tres misiones conservada.

La pantalla inicial mantiene el juego en pausa hasta ENTER THE OUTPOST.
DISPLAY AND CONTROLS permite calidad, aspecto, FOV y sensibilidad.
Clic inicial captura ratón. WASD mueve; Shift corre; ratón gira; clic dispara.
E/Espacio usa. 2/3 pistola/escopeta. F5 guarda/F9 carga en Outpost.
Esc pausa. Alt+F4 cierra. No hay salto ni agachado.

No mezclar archivos de OutpostPreview y LegacyCampaign.
Outpost guarda en UKUBOLA/EvolutionPreview dentro de los datos persistentes
de Unity. La campaña original conserva su ubicación de guardados v1.

Esta es una preview jugable, no la reconstrucción final de las tres misiones.
Se aplazan Foundry/Core nuevos, acabado del arsenal completo, reasignación,
revisión auditiva y validación exhaustiva de rendimiento/campaña.
El recorrido automatizado conoce la ruta; no acredita 5–8 min de exploración.

Fuentes y estado: https://github.com/Antony41554/DoomClone
'''
credits='''UKUBOLA — Créditos y procedencia

Proyecto y recursos preexistentes: repositorio UKUBOLA/DoomClone del propietario.
Evolución industrial: generadores Unity y Python incluidos en el repositorio;
meshes, texturas PBR y fuente geométrica 5x7 creados para esta evolución.
Audio y retratos reutilizados: Assets/_UKUBOLA/Audio/Phase09 y Art/Phase15.
Referencia de dirección: opción C aportada por el usuario. No es una captura.
Motor y paquetes: Unity y sus proveedores. Se adjuntan los avisos disponibles
de los paquetes instalados. No se establece una nueva licencia abierta para
el contenido del proyecto con esta preview.
'''
with zipfile.ZipFile(output,'x',compression=zipfile.ZIP_DEFLATED,compresslevel=6) as archive:
    for path in sorted(build.rglob('*')):
        if path.is_file() and allowed(path.relative_to(build).as_posix()):
            if not path.resolve().is_relative_to(build): raise ValueError('External build link')
            with path.open('rb') as stream: entry(archive,'OutpostPreview/'+path.relative_to(build).as_posix(),stream)
    with zipfile.ZipFile(legacy) as previous:
        for info in previous.infolist():
            if not info.is_dir() and allowed(info.filename):
                with previous.open(info) as stream: entry(archive,'LegacyCampaign/'+info.filename,stream)
    license_index=[]
    for path in sorted((ROOT/'Library/PackageCache').rglob('*')):
        if path.is_file() and path.name.lower() in ('license','license.md','license.txt','thirdparty notices.md','third-party notices.md','thirdpartynotices.md','thirdpartynotices.txt'):
            relative=path.relative_to(ROOT/'Library/PackageCache').as_posix()
            notice_name='LICENSES/'+relative.split('/')[0].split('@')[0]+'-'+hashlib.sha256(relative.encode()).hexdigest()[:12]+'-'+path.name
            with path.open('rb') as stream: entry(archive,notice_name,stream)
            license_index.append({'source':relative,'packaged':notice_name})
    text_entry(archive,'LICENSES/INDEX.json',json.dumps(license_index,indent=2))
    text_entry(archive,'START_HERE.txt',guide)
    text_entry(archive,'CREDITS.txt',credits)
    text_entry(archive,'Play_Outpost.cmd','@echo off\r\nstart "" "%~dp0OutpostPreview\\UKUBOLA.exe"\r\n')
    text_entry(archive,'Play_Original_Campaign.cmd','@echo off\r\nstart "" "%~dp0LegacyCampaign\\UKUBOLA.exe"\r\n')
    text_entry(archive,'MANIFEST.json',json.dumps({'sourceCommit':args.commit,'files':records},indent=2))

extracted=ROOT/'Evidence/Evolution'/('extracted-preview-'+uuid.uuid4().hex)
extracted.mkdir(parents=True)
with zipfile.ZipFile(output) as archive:
    bad=archive.testzip()
    if bad: raise ValueError('ZIP CRC failure: '+bad)
    for info in archive.infolist():
        target=(extracted/info.filename).resolve()
        if not target.is_relative_to(extracted): raise ValueError('ZIP member escapes extraction')
        target.parent.mkdir(parents=True,exist_ok=True)
        with archive.open(info) as source,target.open('xb') as destination: shutil.copyfileobj(source,destination)
for record in records:
    if sha(extracted/record['path'])!=record['sha256']: raise ValueError('Extracted hash mismatch')
summary={'kind':'Deadline-scoped preview, not P27 final acceptance','sourceCommit':args.commit,'nativeQa':reports,'extractedExecutionPassed':False,'packagedFiles':len(records),'excluded':'Unity DontShip/DoNotShip folders, symbols and logs'}
summary_path.write_text(json.dumps(summary,indent=2),encoding='utf-8')
checksum_path.write_text(f'{sha(output)}  {output.name}\n',encoding='utf-8')
assets=[{'path':p.relative_to(ROOT).as_posix(),'name':p.name,'sha256':sha(p),'contentType':mime} for p,mime in ((output,'application/zip'),(checksum_path,'text/plain'),(summary_path,'application/json'))]
manifest={'verifiedExtraction':True,'nativeQaPassed':True,'extractedExecutionPassed':False,'sourceCommit':args.commit,'extractionRoot':extracted.relative_to(ROOT).as_posix(),'previewExeSha256':sha(extracted/'OutpostPreview/UKUBOLA.exe'),'legacyExeSha256':sha(extracted/'LegacyCampaign/UKUBOLA.exe'),'summaryPath':summary_path.relative_to(ROOT).as_posix(),'assets':assets}
manifest_path.write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print(json.dumps({'success':True,'manifest':str(manifest_path),'extraction':str(extracted),'files':len(records),'zipBytes':output.stat().st_size,'requiresExtractedPlayerQa':True},indent=2))
