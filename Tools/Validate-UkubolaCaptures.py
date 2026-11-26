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
    results.append({'file': str(path), 'width': pixels.shape[1], 'height': pixels.shape[0],
                    'standardDeviation': float(pixels.std()), 'nonBlackFraction': float((pixels.max(axis=2) > 12).mean())})
valid = len(results) >= 6 and all(r['standardDeviation'] > 5 and r['nonBlackFraction'] > .05 for r in results)
print(json.dumps({'success': valid, 'captures': results}, indent=2))
raise SystemExit(0 if valid else 1)
