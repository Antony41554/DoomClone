"""Technical PCM checks of reused preview cues; this is not a listening review."""
from array import array
import hashlib
import json
from pathlib import Path
import sys
import uuid
import wave

root=Path(__file__).resolve().parents[1]
records=[]
for path in sorted((root/'Assets/_UKUBOLA/Audio/Phase09').glob('*.wav')):
    with wave.open(str(path),'rb') as audio:
        width=audio.getsampwidth();frames=audio.getnframes();rate=audio.getframerate();channels=audio.getnchannels()
        if width not in (1,2,4): raise SystemExit('Unsupported PCM width; no perceptual acceptance: '+str(path))
        raw=audio.readframes(frames);samples=array({1:'B',2:'h',4:'i'}[width],raw)
        if sys.byteorder!='little' and width>1:samples.byteswap()
        offset=128 if width==1 else 0;full=128 if width==1 else 2**(width*8-1)
        peak=max(abs(sample-offset) for sample in samples)/full
        rms=(sum(((sample-offset)/full)**2 for sample in samples)/len(samples))**.5
        clipped=sum(abs(sample-offset)>=full-1 for sample in samples)/len(samples)
        records.append({'file':path.relative_to(root).as_posix(),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'seconds':frames/rate,'sampleRate':rate,'channels':channels,'bits':width*8,'peak':peak,'rms':rms,'clippedFraction':clipped,'passed':frames/rate>.015 and peak>.0001 and clipped<.0001})
success=len(records)>=10 and all(r['passed'] for r in records)
output=root/'Evidence/Evolution'/('audio-technical-'+uuid.uuid4().hex)/'result.json'
output.parent.mkdir(parents=True)
output.write_text(json.dumps({'success':success,'kind':'Technical PCM validation only; listening review pending','clips':records},indent=2),encoding='utf-8')
print(json.dumps({'success':success,'clips':len(records),'evidence':str(output),'listeningReview':'pending'},indent=2))
raise SystemExit(0 if success else 1)
