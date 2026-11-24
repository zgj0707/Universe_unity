using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HistoryGraphLayout : MonoBehaviour
{
    class Entry { public string key; public RectTransform rect; public int era; public string[] parents; public Vector2 velocity, size, workingPosition, previousPosition, restAnchor; }
    readonly List<Entry> entries = new List<Entry>();
    readonly Dictionary<string, Entry> byKey = new Dictionary<string, Entry>();
    [NonSerialized] public Dictionary<string, Vector2> positions;
    public HistoryGraphEdges edges;
    readonly Dictionary<string, float> angleTargets = new Dictionary<string, float>();
    readonly Dictionary<Vector2Int, List<int>> nearby = new Dictionary<Vector2Int, List<int>>();
    readonly Dictionary<Vector2Int, List<int>> collisionNearby = new Dictionary<Vector2Int, List<int>>();
    float collisionCellSize = 382f;
    int stableFrames;
    public bool IsSleeping { get; private set; }
    public long CollisionChecks { get; private set; }
    public void Wake(bool restart = false)
    {
        IsSleeping = false; stableFrames = 0; accumulatedTime = 0f;
        if (restart) { foreach (var e in entries) e.velocity = Vector2.zero; angleTargets.Clear(); nextAngleUpdate = 0f; }
    }
    public void SetOrder(List<string> keys)
    {
        var order = new Dictionary<string, int>();
        for (int i = 0; i < keys.Count; i++) order[keys[i]] = i;
        entries.Sort((a,b) => (order.TryGetValue(a.key, out int ai) ? ai : -1).CompareTo(order.TryGetValue(b.key, out int bi) ? bi : -1));
    }
    Vector2Int CollisionCell(Vector2 p) => new Vector2Int(Mathf.FloorToInt(p.x / collisionCellSize), Mathf.FloorToInt(p.y / collisionCellSize));
    void AddCollisionCell(int index)
    {
        var cell = CollisionCell(entries[index].workingPosition);
        if (!collisionNearby.TryGetValue(cell, out var bucket)) collisionNearby[cell] = bucket = new List<int>();
        bucket.Add(index);
    }
    void MoveCollisionCell(int index, Vector2 previous)
    {
        var before = CollisionCell(previous); var after = CollisionCell(entries[index].workingPosition);
        if (before == after) return;
        collisionNearby[before].Remove(index); AddCollisionCell(index);
    }
    readonly List<int> collisionCandidates = new List<int>();
    int[] candidateMarks = Array.Empty<int>();
    int candidateStamp;
    void CollectCollisionCandidates(int index, int after)
    {
        var cell = CollisionCell(entries[index].workingPosition);
        for (int dx=-1; dx<=1; dx++) for (int dy=-1; dy<=1; dy++)
            if (collisionNearby.TryGetValue(new Vector2Int(cell.x+dx, cell.y+dy), out var bucket))
                foreach (int candidate in bucket)
                    if (candidate > after && candidateMarks[candidate] != candidateStamp)
                    {
                        candidateMarks[candidate] = candidateStamp;
                        int insert = collisionCandidates.BinarySearch(candidate);
                        collisionCandidates.Insert(~insert, candidate);
                    }
    }
    float nextAngleUpdate;
    public float updatesPerSecond = 120f;
    float accumulatedTime;
    public void Add(string key, RectTransform rect, int era, string[] parents)
    {
        var e = new Entry { key = key, rect = rect, era = era, parents = parents, size = rect ? rect.sizeDelta : new Vector2(200, 120) };
        entries.Add(e); byKey.Add(key, e);
        if (!initialLayoutComplete) Wake();
        else { localPending.Add(e); Wake(false); }
    }
    bool initialLayoutComplete;
    readonly List<Entry> localPending = new List<Entry>();
    int localCursor, localPass;
    bool localMoved;
    void UpdateLocalLayout()
    {
        var budget = System.Diagnostics.Stopwatch.StartNew();
        while (localPending.Count > 0 && budget.Elapsed.TotalMilliseconds < 1.0)
        {
            var e = localPending[0];
            if (!e.rect || !positions.ContainsKey(e.key)) { localPending.RemoveAt(0); localCursor = localPass = 0; localMoved = false; continue; }
            Vector2 p = positions[e.key];
            if (localCursor < entries.Count)
            {
                var other = entries[localCursor++];
                if (other == e || !positions.TryGetValue(other.key, out var q)) continue;
                Vector2 delta = p - q;
                float ox = (e.size.x + other.size.x) * 0.5f + 12f - Mathf.Abs(delta.x);
                float oy = (e.size.y + other.size.y) * 0.5f + 12f - Mathf.Abs(delta.y);
                if (ox > 0f && oy > 0f && e.era >= 0)
                {
                    if (ox < oy) p.x += (delta.x >= 0 ? 1 : -1) * (ox + 0.01f);
                    else p.y += (delta.y >= 0 ? 1 : -1) * (oy + 0.01f);
                    positions[e.key] = p; localMoved = true;
                }
                continue;
            }
            e.rect.anchoredPosition = new Vector2(4000, 4000) + p
                - Vector2.Scale(new Vector2(0.5f, 0.5f) - e.rect.pivot, e.rect.sizeDelta);
            localCursor = 0;
            if (!localMoved || ++localPass >= 16) { localPending.RemoveAt(0); localPass = 0; }
            localMoved = false;
        }
        if (edges) edges.Refresh();
    }
    public void FinishInitialMount() { initialLayoutComplete = true; }
    void UpdateAngleTargets()
    {
        angleTargets.Clear();
        for (int era = 0; era < 6; era++)
        {
            var group = entries.FindAll(e => e.era == era);
            if (group.Count < 2) continue;
            group.Sort((a, b) => Mathf.Atan2(positions[a.key].y, positions[a.key].x).CompareTo(Mathf.Atan2(positions[b.key].y, positions[b.key].x)));
            float step = 2f * Mathf.PI / group.Count, sumSin = 0, sumCos = 0;
            for (int i = 0; i < group.Count; i++)
            {
                Vector2 p = positions[group[i].key];
                float offset = Mathf.Atan2(p.y, p.x) - i * step;
                sumSin += Mathf.Sin(offset); sumCos += Mathf.Cos(offset);
            }
            float baseAngle = Mathf.Atan2(sumSin, sumCos);
            for (int i = 0; i < group.Count; i++) angleTargets[group[i].key] = baseAngle + i * step;
        }
    }
    void ResolveOverlaps()
    {
        // Both archives use the same rectangle constraints, including the larger
        // completed hub. Soft radial forces alone cannot keep card edges apart.
        const float gap = 12f;
        collisionCellSize = 12f; CollisionChecks = 0;
        if (candidateMarks == null || candidateMarks.Length < entries.Count) candidateMarks = new int[entries.Count];
        foreach (var entry in entries) { entry.workingPosition = positions[entry.key]; collisionCellSize = Mathf.Max(collisionCellSize, Mathf.Max(entry.size.x, entry.size.y) + gap); }
        for (int pass = 0; pass < 3; pass++)
        {
            bool moved = false;
            foreach (var bucket in collisionNearby.Values) bucket.Clear();
            for (int index = 0; index < entries.Count; index++) AddCollisionCell(index);
            // A dense pile has no useful spatial pruning; retain the original ordered scan.
            bool dense = false; foreach (var bucket in collisionNearby.Values) if (bucket.Count > 16) { dense = true; break; }
            for (int i = 0; i < entries.Count; i++)
            {
            collisionCandidates.Clear(); candidateStamp++;
            if (candidateStamp == int.MaxValue) { Array.Clear(candidateMarks, 0, candidateMarks.Length); candidateStamp = 1; }
            if (!dense) CollectCollisionCandidates(i, i);
            for (int cursor = 0; cursor < (dense ? entries.Count - i - 1 : collisionCandidates.Count); cursor++)
            {
                int j = dense ? i + 1 + cursor : collisionCandidates[cursor];
                CollisionChecks++;
                var a = entries[i]; var b = entries[j];
                if (a.era < 0 && b.era < 0) continue;
                Vector2 delta = a.workingPosition - b.workingPosition;
                float overlapX = (a.size.x + b.size.x) * 0.5f + gap - Mathf.Abs(delta.x);
                float overlapY = (a.size.y + b.size.y) * 0.5f + gap - Mathf.Abs(delta.y);
                if (overlapX <= 0f || overlapY <= 0f) continue;
                bool horizontal = overlapX < overlapY;
                float direction = (horizontal ? delta.x : delta.y) >= 0f ? 1f : -1f;
                Vector2 shift = horizontal ? new Vector2((overlapX + 0.01f) * direction, 0)
                    : new Vector2(0, (overlapY + 0.01f) * direction);
                float shareA = a.era < 0 ? 0f : b.era < 0 ? 1f : 0.5f;
                float shareB = b.era < 0 ? 0f : a.era < 0 ? 1f : 0.5f;
                Vector2 beforeA = a.workingPosition, beforeB = b.workingPosition;
                a.workingPosition += shift * shareA;
                b.workingPosition -= shift * shareB;
                if (!dense)
                {
                    MoveCollisionCell(i, beforeA); MoveCollisionCell(j, beforeB);
                    if (CollisionCell(beforeA) != CollisionCell(a.workingPosition)) CollectCollisionCandidates(i, j);
                }
                if (horizontal) { a.velocity.x = 0; b.velocity.x = 0; }
                else { a.velocity.y = 0; b.velocity.y = 0; }
                moved = true;
            }
            }
            if (!moved) break;
        }
        foreach (var entry in entries) positions[entry.key] = entry.workingPosition;
    }

    void Update()
    {
        if (positions == null || entries.Count == 0) return;
        if (localPending.Count > 0) UpdateLocalLayout();
        if (IsSleeping) return;
        foreach (var entry in entries) { entry.previousPosition = positions[entry.key]; if (stableFrames == 0) entry.restAnchor = entry.previousPosition; }
        float dt;
        if (updatesPerSecond <= 0f || updatesPerSecond >= 60f) {
            // 每帧顺滑推进，全面适配 120Hz+ 高刷屏幕。
            dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        } else {
            accumulatedTime += Time.unscaledDeltaTime;
            if (accumulatedTime < 1f / Mathf.Max(1f, updatesPerSecond)) return;
            dt = Mathf.Min(accumulatedTime, 0.1f); accumulatedTime = 0f;
        }
        if (Time.unscaledTime >= nextAngleUpdate) { UpdateAngleTargets(); nextAngleUpdate = Time.unscaledTime + 0.5f; }
        foreach (var cell in nearby.Values) cell.Clear();
        for (int i = 0; i < entries.Count; i++) {
            Vector2 p = positions[entries[i].key];
            var cell = new Vector2Int(Mathf.FloorToInt(p.x / 300f), Mathf.FloorToInt(p.y / 300f));
            if (!nearby.TryGetValue(cell, out var bucket)) nearby[cell] = bucket = new List<int>();
            bucket.Add(i);
        }
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i]; Vector2 p = positions[e.key];
            if (e.era < 0) { positions[e.key] = Vector2.zero; e.velocity = Vector2.zero; continue; }
            Vector2 force = Vector2.zero;
            float radius = p.magnitude;
            Vector2 outward = radius > 0.01f ? p / radius : Vector2.right;
            float targetRadius = 270f + e.era * 230f;
            force += outward * (targetRadius - radius) * 1.2f;
            if (angleTargets.TryGetValue(e.key, out float desired))
            {
                float current = Mathf.Atan2(p.y, p.x);
                float error = Mathf.DeltaAngle(current * Mathf.Rad2Deg, desired * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                Vector2 tangent = new Vector2(-outward.y, outward.x);
                force += tangent * Mathf.Clamp(error * targetRadius * 0.7f, -220f, 220f);
            }
            var cell = new Vector2Int(Mathf.FloorToInt(p.x / 300f), Mathf.FloorToInt(p.y / 300f));
            for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++) {
                if (!nearby.TryGetValue(new Vector2Int(cell.x + dx, cell.y + dy), out var bucket)) continue;
                foreach (int j in bucket) {
                    if (i == j) continue;
                    Vector2 delta = p - positions[entries[j].key]; float distance = delta.magnitude;
                    if (distance < 300f) {
                        Vector2 direction = distance > 0.1f ? delta / distance : new Vector2(Mathf.Cos(i * 2.4f), Mathf.Sin(i * 2.4f));
                        force += direction * (300f - distance) * 3f;
                    }
                }
            }
            foreach (var parent in e.parents)
            {
                if (!byKey.ContainsKey(parent)) continue;
                Vector2 delta = positions[parent] - p; float distance = delta.magnitude;
                if (distance > 1f) force += delta / distance * (distance - 260f) * 0.65f;
            }
            e.velocity = (e.velocity + force * dt) * Mathf.Exp(-3.5f * dt);
        }
        foreach (var e in entries)
        {
            if (e.era >= 0) positions[e.key] += e.velocity * dt;
        }
        ResolveOverlaps();
        float maxMove = 0f, maxSpeed = 0f;
        foreach (var e in entries)
        {
            maxMove = Mathf.Max(maxMove, Mathf.Max((positions[e.key] - e.previousPosition).sqrMagnitude, (positions[e.key] - e.restAnchor).sqrMagnitude));
            maxSpeed = Mathf.Max(maxSpeed, e.velocity.sqrMagnitude);
            var rect = e.rect; Vector2 center = new Vector2(4000, 4000) + positions[e.key];
            rect.anchoredPosition = center - Vector2.Scale(new Vector2(0.5f, 0.5f) - rect.pivot, rect.sizeDelta);
        }
        if (edges) edges.Refresh();
        ObserveMotion(maxMove, maxSpeed);
    }
    void ObserveMotion(float maxMove, float maxSpeed)
    {
        stableFrames = maxMove <= 0.000121f && maxSpeed <= 4f ? stableFrames + 1 : 0;
        if (stableFrames >= 24) IsSleeping = true;
    }
}

public class HistoryGraphEdges : MonoBehaviour
{
    class Link { public RectTransform from, to, line; public Image image; public Vector2 previousA, previousB; public bool positioned; }
    readonly List<Link> links = new List<Link>();
    readonly Dictionary<string, Link> byPair = new Dictionary<string, Link>();
    public Sprite lineSprite;
    int orderCursor;
    public void BeginOrder() { orderCursor = 0; }
    public void AddOrUpdate(string fromKey, string toKey, RectTransform from, RectTransform to, bool active)
    {
        string key = fromKey + "\n" + toKey;
        Color color = active ? new Color(0.27f, 0.46f, 0.60f, 0.95f) : new Color(0.66f, 0.47f, 0.37f, 0.82f);
        if (byPair.TryGetValue(key, out var existing)) { if (existing.line.GetSiblingIndex() != orderCursor) existing.line.SetSiblingIndex(orderCursor); orderCursor++; if (existing.image.color != color) existing.image.color = color; return; }
        Add(from, to, active); links[links.Count - 1].line.SetSiblingIndex(orderCursor++); byPair.Add(key, links[links.Count - 1]);
    }
    public void Add(RectTransform from, RectTransform to, bool active)
    {
        var go = new GameObject("Link", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(transform, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = Vector2.zero; rect.pivot = new Vector2(0, 0.5f);
        var image = go.GetComponent<Image>(); image.sprite = lineSprite;
        image.color = active ? new Color(0.27f, 0.46f, 0.60f, 0.95f) : new Color(0.66f, 0.47f, 0.37f, 0.82f);
        image.raycastTarget = false;
        links.Add(new Link { from = from, to = to, line = rect, image = image });
    }
    Vector2 Center(RectTransform r) { return r.anchoredPosition + Vector2.Scale(new Vector2(0.5f, 0.5f) - r.pivot, r.sizeDelta); }
    public void Refresh()
    {
        foreach (var link in links)
        {
            if (!link.from || !link.to || !link.line) continue;
            Vector2 a = Center(link.from), b = Center(link.to), delta = b - a;
            if (link.positioned && link.previousA.Equals(a) && link.previousB.Equals(b)) continue;
            link.previousA = a; link.previousB = b; link.positioned = true;
            link.line.anchoredPosition = a;
            link.line.sizeDelta = new Vector2(delta.magnitude, 3f);
            link.line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }
    }
}


public class HistoryGraphLayerFollow : MonoBehaviour
{
    public RectTransform source, target;
    public void Refresh()
    {
        if (!source || !target) return;
        target.anchorMin = source.anchorMin; target.anchorMax = source.anchorMax; target.pivot = source.pivot;
        if (target.sizeDelta != source.sizeDelta) target.sizeDelta = source.sizeDelta;
        if (target.anchoredPosition != source.anchoredPosition) target.anchoredPosition = source.anchoredPosition;
        if (target.localScale != source.localScale) target.localScale = source.localScale;
    }
    void OnEnable() { Canvas.preWillRenderCanvases += Refresh; }
    void OnDisable() { Canvas.preWillRenderCanvases -= Refresh; }
    void LateUpdate() { Refresh(); }
}
