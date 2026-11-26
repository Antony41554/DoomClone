"""Bind successful executions of both extracted players to the release manifest."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser()
parser.add_argument('manifest',type=Path)
parser.add_argument('--preview-report',required=True,type=Path)
parser.add_argument('--legacy-report',required=True,type=Path)
args=parser.parse_args()
manifest_path=args.manifest.resolve()
if not manifest_path.is_relative_to(ROOT/'Builds'): raise SystemExit('Local package manifest required')
manifest=json.loads(manifest_path.read_text(encoding='utf-8-sig'))
extracted=(ROOT/manifest['extractionRoot']).resolve()
if not extracted.is_relative_to(ROOT/'Evidence/Evolution') or manifest.get('extractedExecutionPassed'):
    raise SystemExit('Expected a new, unfinalized extracted preview')

def sha(path):
    value=hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda:stream.read(1024*1024),b''):value.update(chunk)
    return value.hexdigest()

proof={}
for label,report_path,subfolder,expected_hash in (
    ('preview',args.preview_report,'OutpostPreview',manifest['previewExeSha256']),
    ('legacy',args.legacy_report,'LegacyCampaign',manifest['legacyExeSha256'])):
    path=report_path.resolve()
    if not path.is_relative_to(ROOT/'Evidence/Evolution'): raise SystemExit('Unexpected QA report location')
    report=json.loads(path.read_text(encoding='utf-8-sig'))
    launch=json.loads((path.parent/'launch.json').read_text(encoding='utf-8-sig'))
    executable=extracted/subfolder/'UKUBOLA.exe'
    if Path(launch['executable']).resolve()!=executable or launch['executableSha256']!=expected_hash or sha(executable)!=expected_hash:
        raise SystemExit('QA did not execute the packaged player')
    if not report['success'] or not report['snapshotRoundTrip'] or not launch.get('validationPassed') or launch.get('exitCode')!=0:
        raise SystemExit('Extracted-player QA failed')
    if label=='preview' and (report['mode']!='route' or report['scene']!='OutpostSlice'):
        raise SystemExit('Extracted preview requires a complete live route')
    if label=='legacy' and (report['mode']!='legacy-load' or report['scene'] not in ('E1M1_Save','E1M2_Save','E1M3_Save')):
        raise SystemExit('Extracted legacy player requires a copied v1 save load')
    report['captures']=[Path(p).name for p in report['captures']]
    proof[label]={'reportSha256':sha(path),'report':report,'executableSha256':expected_hash}
summary_path=ROOT/manifest['summaryPath']
summary=json.loads(summary_path.read_text(encoding='utf-8-sig'))
summary['extractedExecutionPassed']=True
summary['extractedQa']=proof
summary_path.write_text(json.dumps(summary,indent=2),encoding='utf-8')
manifest['extractedExecutionPassed']=True
for asset in manifest['assets']:
    asset['sha256']=sha(ROOT/asset['path'])
manifest_path.write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print(json.dumps({'success':True,'manifest':str(manifest_path),'extractedExecutionPassed':True,'players':list(proof)},indent=2))
