"""Persistence wiring regression audit; pairs with managed and native round trips."""
from pathlib import Path
import re,json,hashlib,sys
root=Path(__file__).resolve().parents[1]
s=(root/'Assets/Scripts/DemoSave.cs').read_text(encoding='utf-8-sig')
capture=s[s.index('    SaveData CaptureSaveData()'):s.index('    void SaveGame()')]
restore=s[s.index('    void ApplySaveData('):s.index('    string savedRegion;')]
schema=s[s.index('sealed class SaveData'):s.index('    bool saveReady')]
fields=set()
for decl in re.findall(r'public\s+[\w<>]+\s+([^;]+);',schema):
    for item in decl.split(','):
        fields.add(re.match(r'\s*(\w+)',item)[1])
assert len(fields)==61,(len(fields),fields)
for f in fields-{'version'}:
    assert re.search(r'\b'+f+r'\s*=',capture) or 'data.'+f in capture,('capture',f)
    assert 'data.'+f in restore,('restore',f)
assert 'EnsureCompanyTechMigrated();' in restore
save=s[s.index('    void SaveGame()'):s.index('    void TickAutosave()')]
assert 'RestoreGame()' not in save and 'CaptureSaveData()' in save
reset=s[s.index('    void ResetLoadedSessionState()'):s.index('    bool LoadBundledTestSave()')]
for term in ['coTechSessionUnlocked.Clear()', 'coTechJustUnlocked = null','researchProfitEstimates.Clear()', 'frozenTraffic.Clear()', 'lineForecasts.Clear()', 'operatingRates.Clear()', 'storyQueue.Clear()','pendingCheckin.Clear()']:
    assert term in reset,term
load=s[s.index('    bool LoadBundledTestSave()'):]
assert 'DevTestSaveSyncEnabled = true' in load and load.index('saveReady = false')<load.index('ClosePanel(true)')
m=(root/'Assets/Scripts/DemoMobile.cs').read_text(encoding='utf-8-sig')
new=m[m.index('var resetBtn ='):m.index('    AudioClip MakeTone')]
assert 'DevTestSaveSyncEnabled = false' not in new
assert new.index('saveReady = false; saveBlocked = true')<new.index('PlayerPrefs.DeleteKey')<new.index('SceneManager.LoadScene')
assert 'SaveGame();' in m[m.index('    void QueueStory('):m.index('    void TickStory()')]
u=(root/'Assets/Scripts/DemoUndo.cs').read_text(encoding='utf-8-sig')
for term in ['CopyUndoContract(c)','state.ownedThemes','state.plaques','state.tickets','state.externalProgress != UndoExternalProgressSignature()']:
    assert term in u,term
print('SAVE_STATIC_PASS fields=61 symmetric_capture_restore coTech_migration sync_resume reset_teardown_guard empty_network undo_external_guard=PASS')
a=root/'Assets/Resources/TestSaves/railway2_local_v1.json'
if '--baseline' in sys.argv:
    b=Path(sys.argv[sys.argv.index('--baseline')+1])
    assert a.read_bytes()==b.read_bytes(),'live fixture changed since the explicitly supplied baseline'
    print('LIVE_TEST_SAVE_UNCHANGED sha256='+hashlib.sha256(a.read_bytes()).hexdigest())
else:
    json.loads(a.read_text(encoding='utf-8-sig'))
    print('LIVE_TEST_SAVE_VALID_JSON sha256='+hashlib.sha256(a.read_bytes()).hexdigest())
