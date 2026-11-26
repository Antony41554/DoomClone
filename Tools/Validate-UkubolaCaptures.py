"""Reject empty/black QA output; this is a sanity check, not an artistic score."""
from pathlib import Path
import json
import sys
import numpy as np
from PIL import Image

report = json.loads(Path(sys.argv[1]).read_text(encoding='utf-8-sig'))
results = []
for name in report.get('captures', []):
    path = Path(name)
    pixels = np.asarray(Image.open(path).convert('RGB'))
    # The independent HUD can be valid while every Lit variant is missing.
    world=pixels[:int(pixels.shape[0]*.8)]
    quantized=(world.astype(np.int32)>>3)
    buckets=quantized[:,:,0]*1024+quantized[:,:,1]*32+quantized[:,:,2]
    dominant=float(np.bincount(buckets.ravel(),minlength=32768).max()/buckets.size)
    results.append({'file': str(path), 'width': pixels.shape[1], 'height': pixels.shape[0],
                    'standardDeviation': float(pixels.std()), 'nonBlackFraction': float((pixels.max(axis=2) > 12).mean()),'dominantWorldColorFraction':dominant})
valid = len(results) >= 6 and all(r['standardDeviation'] > 5 and r['nonBlackFraction'] > .05 for r in results)
if report.get('scene') in ('ArtRoom','OutpostSlice'): valid=valid and all(r['dominantWorldColorFraction']<.92 for r in results)
print(json.dumps({'success': valid, 'captures': results}, indent=2))
raise SystemExit(0 if valid else 1)
