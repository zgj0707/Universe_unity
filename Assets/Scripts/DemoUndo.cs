using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class DemoBootstrap
{
    sealed class UndoLine
    {
        public Line line; public string name; public Color color; public int skin; public List<int> path; public int[] dispatch; public int trains; public float fare; public bool hasBaseTrain;
    }
    sealed class UndoVehicle { public ResearchVehicle vehicle; public int lineId; }
    sealed class UndoState
    {
        public string label, region, externalProgress; public double coinsBefore, coinsDelta; public float diamondsBefore, rpBefore, diamondsDelta, rpDelta;
        public int nextId, speed, storyStage; public bool macau, guangdong, loanOutstanding, tutorialSkipped;
        public List<UndoLine> lines = new List<UndoLine>();
        public int companyPolicy, bondDaysLeft, lastPolicyDay, bondPrincipal, bondDailyDue, bondsRepaid;
        public bool bondSubsidized;
        public List<int> boardPartners;
        public List<MunicipalContract> contracts = new List<MunicipalContract>();
        public int nextContractId;
        public List<string> plaques, tickets;
        public int[] levels, facLevels; public float[] riderBonuses; public bool[] built, closed, sceneryUnlocked, sceneryFound; public string[] customNames;
        public List<int> ownedSkins, ownedThemes; public int theme; public List<string> technologies, companyTech = new List<string>(), storySeen = new List<string>();
        public List<UndoVehicle> vehicles = new List<UndoVehicle>();
        public Dictionary<Line, FrozenLineTraffic> frozen = new Dictionary<Line, FrozenLineTraffic>();
    }
    const int MaxUndoSteps = 5;
    UndoState pendingUndo;
    readonly List<UndoState> undoStack = new List<UndoState>();
    Button undoButton;
    Text undoButtonText;
    void ClearUndoHistory()
    {
        pendingUndo = null;
        undoStack.Clear();
        RefreshUndoButton();
    }
    void BuildUndoButton()
    {
        undoButton = Btn(root, "Undo", "↶\n撤销", 26, 962, 190, 108, 98, CardBg, UndoLastAction);
        undoButtonText = undoButton.GetComponentInChildren<Text>();
        TInk(undoButton, InkSoft); SoftShadow(undoButton.gameObject); RefreshUndoButton();
    }
    void RefreshUndoButton()
    {
        if (!undoButton) return;
        undoButton.GetComponent<Image>().color = undoStack.Count == 0 ? new Color(Paper2.r, Paper2.g, Paper2.b, 0.90f) : CardBg;
        if (undoButtonText) undoButtonText.text = undoStack.Count > 1 ? "↶\n撤销(" + undoStack.Count + ")" : "↶\n撤销";
    }
    void BeginUndo(string label)
    {
        var state = new UndoState { label=label, region=currentRegion, coinsBefore=coins, diamondsBefore=diamonds, rpBefore=rp, nextId=nextLineId,
            macau=macauUnlocked, guangdong=guangdongUnlocked, loanOutstanding=loanOutstanding, ownedSkins=new List<int>(ownedLineSkins), speed=timeSpeed,
            storyStage=storyStage, tutorialSkipped=tutorialSkipped,
            levels=new int[stations.Count], built=new bool[stations.Count], closed=new bool[stations.Count], customNames=new string[stations.Count],
            companyPolicy=companyPolicy, bondDaysLeft=bondDaysLeft, lastPolicyDay=lastPolicyDay, bondPrincipal=bondPrincipal, bondDailyDue=bondDailyDue, bondsRepaid=bondsRepaid, bondSubsidized=bondSubsidized, boardPartners=new List<int>(boardPartners),
            facLevels=new int[stations.Count], riderBonuses=new float[stations.Count],
            sceneryUnlocked=new bool[sceneries.Count], sceneryFound=new bool[sceneries.Count],
            technologies=new List<string>(techDone), companyTech=new List<string>(companyTechDone), storySeen=new List<string>(storySeen) };
        state.ownedThemes = new List<int>(ownedThemes); state.theme = currentTheme;
        state.nextContractId = nextContractId;
        state.plaques = new List<string>(earnedPlaques); state.tickets = new List<string>(earnedMemorialTickets);
        foreach (var c in activeContracts) state.contracts.Add(CopyUndoContract(c));
        foreach (var line in lines) state.lines.Add(new UndoLine { line=line, name=line.name, color=line.color, skin=line.skin, path=new List<int>(line.st), dispatch=(int[])line.dispatch.Clone(), trains=line.trains, fare=line.fare, hasBaseTrain=line.hasBaseTrain });
        for (int i=0;i<stations.Count;i++) { state.levels[i]=stations[i].level; state.built[i]=stations[i].built; state.closed[i]=stations[i].closed; state.customNames[i]=stations[i].customName ?? ""; }
        for (int i=0;i<sceneries.Count;i++) { state.sceneryUnlocked[i]=sceneries[i].unlocked; state.sceneryFound[i]=sceneries[i].found; }
        for (int i=0;i<stations.Count;i++) { state.facLevels[i]=stations[i].facLevel; state.riderBonuses[i]=stations[i].riderBonus; }
        foreach (var item in researchVehicles) state.vehicles.Add(new UndoVehicle { vehicle=item, lineId=item.lineId });
        foreach (var item in frozenTraffic) state.frozen[item.Key] = item.Value.Copy();
        pendingUndo=state;
    }
    static MunicipalContract CopyUndoContract(MunicipalContract c)
    {
        return new MunicipalContract { id=c.id, type=c.type, startValue=c.startValue, targetValue=c.targetValue,
            rewardCoins=c.rewardCoins, rewardRp=c.rewardRp, rewardDiamonds=c.rewardDiamonds,
            deadlineDay=c.deadlineDay, claimed=c.claimed };
    }
    string UndoExternalProgressSignature()
    {
        // These systems progress outside BeginUndo/EndUndo. A stale snapshot must
        // never refund an older action while erasing later purchases or rewards.
        var external = new SaveData();
        external.talents.AddRange(talents); external.talentPool.AddRange(talentPool); external.contracts.AddRange(activeContracts);
        return JsonUtility.ToJson(external)
            + "|" + string.Join(",", earnedPlaques) + "|" + string.Join(",", earnedMemorialTickets)
            + "|" + string.Join(",", ownedThemes) + "|" + string.Join(",", ownedLineSkins) + "|" + retiredVehiclesCount
            + "|" + storyStage + ":" + tutorialSkipped + ":" + string.Join(",", storySeen)
            + "|" + consultantId + ":" + consultantDaysLeft + ":" + turboDaysLeft + ":" + autoDispatchDaysLeft
            + "|" + companyName + ":" + companySeal + ":" + dailyTaskClaimedDay + ":" + lastAdSubsidyDay;
    }
    void EndUndo()
    {
        if (pendingUndo == null) return;
        pendingUndo.coinsDelta=coins-pendingUndo.coinsBefore; pendingUndo.diamondsDelta=diamonds-pendingUndo.diamondsBefore; pendingUndo.rpDelta=rp-pendingUndo.rpBefore;
        pendingUndo.externalProgress = UndoExternalProgressSignature();
        undoStack.Add(pendingUndo);
        if (undoStack.Count > MaxUndoSteps) undoStack.RemoveAt(0);
        pendingUndo=null; RefreshUndoButton(); RequestSaveGame();
    }
    void UndoLastAction()
    {
        if (dragging || activePanel || pageRoot || sceneryViewer) return;
        if (undoStack.Count == 0) { Toast("没有可撤销的操作"); return; }
        var state = undoStack[undoStack.Count - 1];
        if (state.region != currentRegion) { Toast("请先回到" + DisplayPlaceAliases(state.region) + "撤销「" + state.label + "」"); return; }
        if (state.externalProgress != UndoExternalProgressSignature())
        { ClearUndoHistory(); Toast("后续经营进度已更新，旧操作不再撤销"); return; }
        undoStack.RemoveAt(undoStack.Count - 1);
        pendingUndo=null; CloseLineBubble(); CancelDraft();
        coins-=state.coinsDelta; diamonds-=state.diamondsDelta; rp-=state.rpDelta; nextLineId=state.nextId; timeSpeed=state.speed; UpdateTimeText();
        lines.Clear(); foreach (var record in state.lines)
        { record.line.name=record.name; record.line.color=record.color; record.line.skin=record.skin; record.line.st=new List<int>(record.path); record.line.dispatch=(int[])record.dispatch.Clone(); record.line.trains=record.trains; record.line.fare=record.fare; record.line.hasBaseTrain=record.hasBaseTrain; lines.Add(record.line); }
        for (int i=0;i<stations.Count;i++) {
            stations[i].level=state.levels[i]; stations[i].built=state.built[i]; stations[i].closed=state.closed[i];
            if (state.customNames != null && i < state.customNames.Length) stations[i].customName = state.customNames[i];
            if (stationLabels.TryGetValue(i, out var stLbl) && stLbl) stLbl.text = StationDisplayName(i, currentRegion);
        }
        LayoutStationLabels();
        for (int i=0;i<sceneries.Count;i++) { sceneries[i].unlocked=state.sceneryUnlocked[i]; sceneries[i].found=state.sceneryFound[i]; }
        if (state.facLevels != null && state.riderBonuses != null)
            for (int i=0;i<stations.Count && i<state.facLevels.Length && i<state.riderBonuses.Length;i++) { stations[i].facLevel=state.facLevels[i]; stations[i].riderBonus=state.riderBonuses[i]; }
        companyPolicy=state.companyPolicy; bondDaysLeft=state.bondDaysLeft; lastPolicyDay=state.lastPolicyDay;
        bondPrincipal=state.bondPrincipal; bondDailyDue=state.bondDailyDue; bondsRepaid=state.bondsRepaid; bondSubsidized=state.bondSubsidized;
        boardPartners.Clear(); if (state.boardPartners != null) boardPartners.AddRange(state.boardPartners);
        activeContracts.Clear(); foreach (var c in state.contracts) activeContracts.Add(CopyUndoContract(c));
        nextContractId = state.nextContractId;
        earnedPlaques.Clear(); earnedPlaques.AddRange(state.plaques);
        earnedMemorialTickets.Clear(); earnedMemorialTickets.AddRange(state.tickets);
        techDone.Clear(); techDone.AddRange(state.technologies);
        companyTechDone.Clear(); if (state.companyTech != null) companyTechDone.AddRange(state.companyTech);
        storyStage = state.storyStage; tutorialSkipped = state.tutorialSkipped;
        storySeen.Clear(); if (state.storySeen != null) storySeen.UnionWith(state.storySeen);
        frozenTraffic.Clear(); foreach (var item in state.frozen) frozenTraffic[item.Key] = item.Value.Copy();
        researchVehicles.Clear(); foreach (var item in state.vehicles) { item.vehicle.lineId=item.lineId; researchVehicles.Add(item.vehicle); }
        macauUnlocked=state.macau; guangdongUnlocked=state.guangdong; loanOutstanding=state.loanOutstanding;
        ownedLineSkins.Clear(); foreach (int skin in state.ownedSkins) ownedLineSkins.Add(skin);
        ownedThemes.Clear(); foreach (int theme in state.ownedThemes) ownedThemes.Add(theme);
        ApplyTheme(state.theme, false);
        pendingCheckin.Clear(); foreach (var sc in sceneries) if (sc.unlocked && !sc.found && TryGetSceneryContent(sc,out _,out _)) pendingCheckin.Add(sc);
        researchProfitEstimates.Clear(); hasResearchProfitSignature = false;
        RefreshSceneryViews(); RefreshPendingBadge(); RefreshFeatureGateVisuals(); UpdateStoryObjectiveBanner();
        CreateLineVisuals(); RefreshStationDots(); RecalcIncome();
        if (!RegionUnlocked(currentRegion)) SwitchRegion("月之亮面");
        RefreshHUD(); RefreshUndoButton(); SaveGame(); Toast("已撤销：" + state.label);
    }
}
