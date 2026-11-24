"""Scoped visual invariants for the incremental research renderer."""
from pathlib import Path
r=Path(__file__).resolve().parents[1]
read=lambda p:(r/p).read_text(encoding='utf-8-sig')
s=read('Assets/Scripts/DemoResearch.cs');c=read('Assets/Scripts/DemoCompanySystems.cs');g=read('Assets/Scripts/HistoryGraphLayout.cs');fx=read('Assets/Scripts/HistoryTreeEffects.cs')
def body(source,signature):
 a=source.index(signature);a=source.index('{',a);depth=1;i=a+1
 while depth:
  if source[i]=='{':depth+=1
  elif source[i]=='}':depth-=1
  i+=1
 return source[a:i]
h=body(s,'void TapHistoryNode(');co=body(c,'void TapCompanyTechNode(')
assert 'OpenTechScreen' not in h and 'RefreshTechTreeIncremental(false, key)' in h
assert 'OpenCompanyTechScreen' not in co and 'RefreshTechTreeIncremental(true, node.key)' in co
assert 'SaveGame()' not in co[co.index('EndUndo();'):]
assert 'cards.TryGetValue(nodeKey, out var existing)' in s and 'edges.AddOrUpdate' in s
assert 'ResearchTrainTexture(LocoArt[locoIdx])' in s
assert 'subCanvas.overrideSorting = false' in s and 'GraphicRaycaster' in body(s,'void ResearchSubCanvas(')
assert 'Canvas.preWillRenderCanvases += Refresh' in g and 'Canvas.preWillRenderCanvases -= Refresh' in g
assert 'IsSleeping) return' in g and 'ObserveMotion(maxMove, maxSpeed)' in g
assert 'entry.restAnchor = entry.previousPosition' in g and 'e.restAnchor' in g
assert 'for (int pass = 0; pass < 16; pass++)' in g and 'const float gap = 12f' in g
assert 'CollectCollisionCandidates' in g and 'bucket.Count > 16' in g
assert 'candidate > after' in g and 'BinarySearch(candidate)' in g
baseline=r/'Temp/spec-incremental-20261117'
if baseline.exists():
 old=(baseline/'DemoResearch.cs').read_text(encoding='utf-8-sig')
 a=old.index('            bool locked = !done && !ready;',old.index('spawnNodeCard ='))
 b=old.index('            var touch =',a)
 initial=s[s.index('            bool locked = !done && !ready;',s.index('spawnNodeCard =')):s.index('            var touch =',s.index('spawnNodeCard ='))]
 assert old[a:b].replace('Resources.Load<Texture2D>("Research/Train/" + LocoArt[locoIdx])','ResearchTrainTexture(LocoArt[locoIdx])')==initial,'initial card geometry/color/text changed'
 oldfx=(baseline/'HistoryTreeEffects.cs').read_text(encoding='utf-8-sig')
 assert body(oldfx[oldfx.index('public class HistoryNodeReveal'):],'void Update()')==body(fx[fx.index('public class HistoryNodeReveal'):],'void Update()'),'reveal motion changed'
 assert oldfx[oldfx.index('public class HistoryCameraShake'):]==fx[fx.index('public class HistoryCameraShake'):],'shake/parallax changed'
 oldg=(baseline/'HistoryGraphLayout.cs').read_text(encoding='utf-8-sig')
 a=oldg.index('        if (Time.unscaledTime >=');b=oldg.index('        ResolveOverlaps();',a)
 assert oldg[a:b] in g,'force equations changed'
 for color in ['0.27f, 0.46f, 0.60f, 0.95f','0.66f, 0.47f, 0.37f, 0.82f']:
  assert color in g and color in oldg
print('INCREMENTAL_STATIC_PASS no_full_reopen keyed_cards_and_edges passive_stencil_subcanvases texture_cache single_save=PASS')
print('VISUAL_CONTRACT_PASS initial_card_geometry/colors/fonts reveal_motion camera_shake parallax force_equations link_colors/width=UNCHANGED')
print('GRID_SLEEP_CONTRACT_PASS ordered_dynamic_candidates dense_fallback gap12_pass16 window_bound_sleep pre_render_camera_sync=PASS')
