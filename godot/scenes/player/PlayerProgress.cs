namespace PiratesQuest;

using PiratesQuest.Data;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Tracks long-term player progression and per-quest progress snapshots.
/// Each active quest gets its own "since accepted" counters so quests can run in parallel.
/// </summary>
public class PlayerProgress
{
  private readonly ProgressSnapshot _lifetime = new();
  private readonly ProgressSnapshot _sinceQuestStart = new();
  private readonly List<string> _completedQuestIds = [];
  private readonly List<string> _recentlyCompletedQuestIds = [];
  private readonly HashSet<string> _unlockedFeatures = new(StringComparer.Ordinal);
  private string _currentQuestId;
  private string _acceptedQuestNpcId;

  public ProgressSnapshot Lifetime => _lifetime;
  public ProgressSnapshot SinceQuestStart => _sinceQuestStart;
  public string CurrentQuestId => _currentQuestId;
  public string AcceptedQuestNpcId => _acceptedQuestNpcId;
  public IReadOnlyList<string> CompletedQuestIds => _completedQuestIds;
  public IReadOnlyCollection<string> UnlockedFeatures => _unlockedFeatures;

  public void ResetForNewQuest(string questId, string npcId)
  {
    if (string.IsNullOrWhiteSpace(questId))
      return;

    _currentQuestId = questId;
    _acceptedQuestNpcId = npcId ?? "";
    _sinceQuestStart.Reset();
  }

  public void RecordPortVisited(string portName)
  {
    _lifetime.RecordPortVisited(portName);
    if (HasActiveQuest())
      _sinceQuestStart.RecordPortVisited(portName);
  }

  public void RecordItemCollected(InventoryItemType item, int amount)
  {
    _lifetime.RecordItemCollected(item, amount);
    if (HasActiveQuest())
      _sinceQuestStart.RecordItemCollected(item, amount);
  }

  public void RecordItemBought(InventoryItemType item, int qty, int totalCost)
  {
    _lifetime.RecordItemBought(item, qty, totalCost);
    if (HasActiveQuest())
      _sinceQuestStart.RecordItemBought(item, qty, totalCost);
  }

  public void RecordItemSold(InventoryItemType item, int qty, int totalRevenue)
  {
    int lifetimeProfit = Math.Max(0, totalRevenue - _lifetime.GetAverageCostRounded(item, qty));
    _lifetime.RecordItemSold(item, qty, totalRevenue, lifetimeProfit);
    if (HasActiveQuest())
    {
      int questProfit = Math.Max(0, totalRevenue - _sinceQuestStart.GetAverageCostRounded(item, qty));
      _sinceQuestStart.RecordItemSold(item, qty, totalRevenue, questProfit);
    }
  }

  public void RecordMoneySpent(int amount)
  {
    _lifetime.TotalMoneySpent += Math.Max(0, amount);
    if (HasActiveQuest())
      _sinceQuestStart.TotalMoneySpent += Math.Max(0, amount);
  }

  public void RecordComponentBought(string componentName, bool autoEquipped)
  {
    _lifetime.RecordComponentBought(componentName);
    if (HasActiveQuest())
      _sinceQuestStart.RecordComponentBought(componentName);
  }

  public void RecordCrewHired(string characterId)
  {
    _lifetime.RecordCrewHired(characterId);
    if (HasActiveQuest())
      _sinceQuestStart.RecordCrewHired(characterId);
  }

  public void RecordNpcTalkedTo(string characterId)
  {
    _lifetime.RecordNpcTalkedTo(characterId);
    if (HasActiveQuest())
      _sinceQuestStart.RecordNpcTalkedTo(characterId);
  }

  public void RecordCannonballShot(int amount = 1)
  {
    _lifetime.CannonballsShot += amount;
    if (HasActiveQuest())
      _sinceQuestStart.CannonballsShot += amount;
  }

  public void RecordShipMovementInput()
  {
    _lifetime.ShipMovementInputs += 1;
    if (HasActiveQuest())
      _sinceQuestStart.ShipMovementInputs += 1;
  }

  public void RecordCameraDrag()
  {
    _lifetime.CameraDrags += 1;
    if (HasActiveQuest())
      _sinceQuestStart.CameraDrags += 1;
  }

  public void RecordShipHit()
  {
    _lifetime.ShipsHit += 1;
    if (HasActiveQuest())
      _sinceQuestStart.ShipsHit += 1;
  }

  public void RecordShipSunk()
  {
    _lifetime.ShipsSunk += 1;
    if (HasActiveQuest())
      _sinceQuestStart.ShipsSunk += 1;
  }

  public void RecordShipTierReached(int shipTier)
  {
    _lifetime.HighestShipTierReached = Math.Max(_lifetime.HighestShipTierReached, shipTier);
    if (HasActiveQuest())
      _sinceQuestStart.HighestShipTierReached = Math.Max(_sinceQuestStart.HighestShipTierReached, shipTier);
  }

  public bool IsFeatureUnlocked(FeatureUnlock feature)
  {
    return _unlockedFeatures.Contains(feature.ToString());
  }

  public QuestDefinition[] GetAvailableQuests()
  {
    return QuestData.GetAvailableQuests(_completedQuestIds, _currentQuestId);
  }

  public bool CanAcceptQuest(string questId, string npcId)
  {
    if (!string.IsNullOrWhiteSpace(_currentQuestId) || _completedQuestIds.Contains(questId))
      return false;

    var quest = QuestData.GetQuest(questId);
    if (quest == null)
      return false;

    bool isAvailable = GetAvailableQuests().Any(q => string.Equals(q.Id, quest.Id, StringComparison.Ordinal));
    if (!isAvailable)
      return false;

    return string.Equals(quest.GiverNpcId, npcId, StringComparison.Ordinal);
  }

  public void EnsureAutoAcceptedQuestActive()
  {
    if (HasActiveQuest())
      return;

    TryAutoAcceptNextQuest();
  }

  public bool ReevaluateQuestProgress(int equippedComponentCount)
  {
    var quest = QuestData.GetQuest(_currentQuestId);
    if (quest == null)
      return false;

    bool isComplete = quest.Steps.All(step => GetMetricValue(_sinceQuestStart, step, equippedComponentCount) >= step.RequiredValue);
    if (!isComplete)
      return false;

    CompleteQuest(quest);
    return true;
  }

  public bool ForceCompleteQuest(string questId = null)
  {
    string resolvedQuestId = string.IsNullOrWhiteSpace(questId)
      ? _currentQuestId
      : questId;

    var quest = QuestData.GetQuest(resolvedQuestId);
    if (quest == null)
      return false;

    CompleteQuest(quest);
    return true;
  }

  public bool ForceUncompleteQuest(string questId)
  {
    var quest = QuestData.GetQuest(questId);
    if (quest == null)
      return false;

    _completedQuestIds.RemoveAll(id => string.Equals(id, quest.Id, StringComparison.Ordinal));
    if (string.Equals(_currentQuestId, quest.Id, StringComparison.Ordinal))
    {
      _currentQuestId = null;
      _acceptedQuestNpcId = null;
      _sinceQuestStart.Reset();
    }

    RebuildUnlockedFeatures();
    return true;
  }

  public bool ForceSetActiveQuest(string questId)
  {
    var quest = QuestData.GetQuest(questId);
    if (quest == null)
      return false;

    _completedQuestIds.RemoveAll(id => string.Equals(id, quest.Id, StringComparison.Ordinal));
    _currentQuestId = quest.Id;
    _acceptedQuestNpcId = "creative";
    _sinceQuestStart.Reset();
    RebuildUnlockedFeatures();
    return true;
  }

  public bool CancelActiveQuest()
  {
    var quest = QuestData.GetQuest(_currentQuestId);
    if (quest == null || quest.AutoAcceptWhenAvailable)
      return false;

    _currentQuestId = null;
    _acceptedQuestNpcId = null;
    _sinceQuestStart.Reset();
    return true;
  }

  public QuestHudStateDto ExportHudState(int equippedComponentCount)
  {
    var hudState = new QuestHudStateDto
    {
      Active = QuestData.GetQuest(_currentQuestId) is QuestDefinition activeQuest
        ? BuildSummary(activeQuest, _sinceQuestStart, equippedComponentCount)
        : null,
      Available = GetAvailableQuests()
        .Select(quest => BuildSummary(quest, new ProgressSnapshot(), equippedComponentCount))
        .ToArray(),
      All = QuestData.Quests.Select(quest =>
      {
        ProgressSnapshot source = string.Equals(quest.Id, _currentQuestId, StringComparison.Ordinal)
          ? _sinceQuestStart
          : new ProgressSnapshot();
        return BuildSummary(quest, source, equippedComponentCount);
      }).ToArray(),
      CompletedIds = _completedQuestIds.ToArray(),
      RecentlyCompletedIds = _recentlyCompletedQuestIds.ToArray(),
      UnlockedFeatures = _unlockedFeatures.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
    };

    _recentlyCompletedQuestIds.Clear();
    return hudState;
  }

  public PlayerProgressDto ToDto()
  {
    return new PlayerProgressDto
    {
      Lifetime = _lifetime.ToDto(),
      SinceQuestStart = _sinceQuestStart.ToDto(),
      CurrentQuestId = _currentQuestId,
      CompletedQuestIds = _completedQuestIds.ToList(),
      UnlockedFeatures = _unlockedFeatures.OrderBy(x => x, StringComparer.Ordinal).ToList(),
      AcceptedQuestNpcId = _acceptedQuestNpcId,
    };
  }

  public void LoadFromDto(PlayerProgressDto dto)
  {
    _lifetime.LoadFromDto(dto?.Lifetime);
    _currentQuestId = QuestData.GetQuest(dto?.CurrentQuestId) != null ? dto.CurrentQuestId : null;
    _acceptedQuestNpcId = _currentQuestId != null ? dto?.AcceptedQuestNpcId : null;
    _sinceQuestStart.LoadFromDto(dto?.SinceQuestStart);

    _completedQuestIds.Clear();
    foreach (var questId in dto?.CompletedQuestIds ?? [])
    {
      if (QuestData.GetQuest(questId) == null || _completedQuestIds.Contains(questId))
        continue;
      _completedQuestIds.Add(questId);
    }

    RebuildUnlockedFeatures();
    foreach (var feature in dto?.UnlockedFeatures ?? [])
      _unlockedFeatures.Add(feature);

    // New players should begin with the first auto-accept tutorial quest
    // already active, but without any special UI popup logic.
    EnsureAutoAcceptedQuestActive();
  }

  private void CompleteQuest(QuestDefinition quest)
  {
    if (!_completedQuestIds.Contains(quest.Id))
      _completedQuestIds.Add(quest.Id);
    if (!_recentlyCompletedQuestIds.Contains(quest.Id))
      _recentlyCompletedQuestIds.Add(quest.Id);

    foreach (var unlock in quest.Unlocks)
      _unlockedFeatures.Add(unlock.ToString());

    if (string.Equals(_currentQuestId, quest.Id, StringComparison.Ordinal))
    {
      _currentQuestId = null;
      _acceptedQuestNpcId = null;
      _sinceQuestStart.Reset();
    }

    TryAutoAcceptNextQuest();
  }

  private void TryAutoAcceptNextQuest()
  {
    if (HasActiveQuest())
      return;

    var nextQuest = GetAvailableQuests()
      .FirstOrDefault(quest => quest.AutoAcceptWhenAvailable);

    if (nextQuest == null)
      return;

    ResetForNewQuest(nextQuest.Id, nextQuest.GiverNpcId);
  }

  private static QuestSummaryDto BuildSummaryCore(QuestDefinition quest, ProgressSnapshot source, int equippedComponentCount)
  {
    return new QuestSummaryDto(
      quest.Id,
      quest.Title,
      quest.GiverNpcId,
      quest.GiverName,
      QuestData.GetQuestGiverPortrait(quest.GiverNpcId),
      quest.GiverPortName,
      quest.RevealGiverInQuestLog,
      quest.CanAcceptFromQuestLog,
      !quest.AutoAcceptWhenAvailable,
      quest.OfferText ?? "",
      quest.AcceptedText ?? "",
      quest.Description ?? "",
      quest.CompletionText ?? "",
      quest.Unlocks.Select(x => x.ToString()).ToArray(),
      quest.Steps.Select(step =>
      {
        int currentValue = GetMetricValue(source, step, equippedComponentCount);
        return new QuestStepProgressDto(
          step.Label,
          step.PreStepPopupText ?? "",
          step.PostStepPopupText ?? "",
          Math.Min(currentValue, step.RequiredValue),
          step.RequiredValue,
          currentValue >= step.RequiredValue
        );
      }).ToArray()
    );
  }

  private QuestSummaryDto BuildSummary(QuestDefinition quest, ProgressSnapshot source, int equippedComponentCount)
  {
    return BuildSummaryCore(quest, source, equippedComponentCount);
  }

  private static int GetMetricValue(ProgressSnapshot snapshot, QuestStepDefinition step, int equippedComponentCount)
  {
    return step.Metric switch
    {
      QuestMetricKind.PortsVisitedCount => snapshot.PortsVisitedCount,
      QuestMetricKind.ShipMovementInputs => snapshot.ShipMovementInputs,
      QuestMetricKind.CameraDrags => snapshot.CameraDrags,
      QuestMetricKind.CannonballsShot => snapshot.CannonballsShot,
      QuestMetricKind.ItemsCollected => snapshot.GetDictValue(snapshot.ItemsCollected, step.ItemType),
      QuestMetricKind.ItemsBought => snapshot.GetDictValue(snapshot.ItemsBought, step.ItemType),
      QuestMetricKind.ItemsSold => snapshot.GetDictValue(snapshot.ItemsSold, step.ItemType),
      QuestMetricKind.SoldProfit => snapshot.GetDictValue(snapshot.SoldProfit, step.ItemType),
      QuestMetricKind.TotalMoneyEarned => snapshot.TotalMoneyEarned,
      QuestMetricKind.EquippedComponentCount => equippedComponentCount,
      QuestMetricKind.ShipsSunk => snapshot.ShipsSunk,
      _ => 0,
    };
  }

  private void RebuildUnlockedFeatures()
  {
    _unlockedFeatures.Clear();
    foreach (var completedQuestId in _completedQuestIds)
    {
      var quest = QuestData.GetQuest(completedQuestId);
      if (quest == null) continue;
      foreach (var unlock in quest.Unlocks)
        _unlockedFeatures.Add(unlock.ToString());
    }
  }

  private bool HasActiveQuest()
  {
    return !string.IsNullOrWhiteSpace(_currentQuestId);
  }

  public class ProgressSnapshot
  {
    public readonly Dictionary<string, int> ItemsCollected = new(StringComparer.Ordinal);
    public readonly Dictionary<string, int> ItemsBought = new(StringComparer.Ordinal);
    public readonly Dictionary<string, int> ItemsSold = new(StringComparer.Ordinal);
    public readonly Dictionary<string, int> SoldProfit = new(StringComparer.Ordinal);
    public readonly Dictionary<string, float> AverageAcquisitionCostByItem = new(StringComparer.Ordinal);
    public readonly Dictionary<string, int> QuantityAcquiredForCostByItem = new(StringComparer.Ordinal);
    public readonly HashSet<string> PortsVisited = new(StringComparer.Ordinal);
    public readonly HashSet<string> BoughtComponentNames = new(StringComparer.Ordinal);
    public readonly HashSet<string> HiredCrewIds = new(StringComparer.Ordinal);
    public readonly HashSet<string> TalkedToNpcIds = new(StringComparer.Ordinal);

    public int ShipMovementInputs { get; set; }
    public int PortsVisitedCount { get; set; }
    public int CameraDrags { get; set; }
    public int CannonballsShot { get; set; }
    public int ShipsHit { get; set; }
    public int ShipsSunk { get; set; }
    public int HighestShipTierReached { get; set; }
    public int TotalMoneyEarned { get; set; }
    public int TotalMoneySpent { get; set; }

    public void Reset()
    {
      ItemsCollected.Clear();
      ItemsBought.Clear();
      ItemsSold.Clear();
      SoldProfit.Clear();
      AverageAcquisitionCostByItem.Clear();
      QuantityAcquiredForCostByItem.Clear();
      PortsVisited.Clear();
      BoughtComponentNames.Clear();
      HiredCrewIds.Clear();
      TalkedToNpcIds.Clear();
      ShipMovementInputs = 0;
      PortsVisitedCount = 0;
      CameraDrags = 0;
      CannonballsShot = 0;
      ShipsHit = 0;
      ShipsSunk = 0;
      HighestShipTierReached = 0;
      TotalMoneyEarned = 0;
      TotalMoneySpent = 0;
    }

    public void RecordPortVisited(string portName)
    {
      PortsVisitedCount += 1;
      if (!string.IsNullOrWhiteSpace(portName))
        PortsVisited.Add(portName);
    }

    public void RecordItemCollected(InventoryItemType item, int amount)
    {
      if (amount <= 0)
        return;

      string key = item.ToString();
      Increment(ItemsCollected, key, amount);
      UpdateAverageCost(key, 0.0f, amount);
    }

    public void RecordItemBought(InventoryItemType item, int qty, int totalCost)
    {
      if (qty <= 0)
        return;

      string key = item.ToString();
      Increment(ItemsBought, key, qty);
      TotalMoneySpent += Math.Max(0, totalCost);
      float unitCost = qty > 0 ? (float)totalCost / qty : 0.0f;
      UpdateAverageCost(key, unitCost, qty);
    }

    public void RecordItemSold(InventoryItemType item, int qty, int totalRevenue, int profit)
    {
      if (qty <= 0)
        return;

      string key = item.ToString();
      Increment(ItemsSold, key, qty);
      if (profit > 0)
        Increment(SoldProfit, key, profit);

      TotalMoneyEarned += Math.Max(0, totalRevenue);

      if (QuantityAcquiredForCostByItem.TryGetValue(key, out int currentQty) && currentQty > 0)
      {
        int remaining = Math.Max(0, currentQty - qty);
        if (remaining == 0)
        {
          QuantityAcquiredForCostByItem.Remove(key);
          AverageAcquisitionCostByItem.Remove(key);
        }
        else
        {
          QuantityAcquiredForCostByItem[key] = remaining;
        }
      }
    }

    public void RecordComponentBought(string componentName)
    {
      if (!string.IsNullOrWhiteSpace(componentName))
        BoughtComponentNames.Add(componentName);
    }

    public void RecordCrewHired(string characterId)
    {
      if (!string.IsNullOrWhiteSpace(characterId))
        HiredCrewIds.Add(characterId);
    }

    public void RecordNpcTalkedTo(string characterId)
    {
      if (!string.IsNullOrWhiteSpace(characterId))
        TalkedToNpcIds.Add(characterId);
    }

    public int GetAverageCostRounded(InventoryItemType item, int qty)
    {
      string key = item.ToString();
      float unitCost = AverageAcquisitionCostByItem.TryGetValue(key, out var foundCost) ? foundCost : 0.0f;
      return (int)MathF.Round(unitCost * qty);
    }

    public int GetDictValue(Dictionary<string, int> dict, string key)
    {
      return !string.IsNullOrWhiteSpace(key) && dict.TryGetValue(key, out var value) ? value : 0;
    }

    public PlayerProgressSnapshotDto ToDto()
    {
      return new PlayerProgressSnapshotDto
      {
        ItemsCollected = new Dictionary<string, int>(ItemsCollected, StringComparer.Ordinal),
        ItemsBought = new Dictionary<string, int>(ItemsBought, StringComparer.Ordinal),
        ItemsSold = new Dictionary<string, int>(ItemsSold, StringComparer.Ordinal),
        SoldProfit = new Dictionary<string, int>(SoldProfit, StringComparer.Ordinal),
        AverageAcquisitionCostByItem = new Dictionary<string, float>(AverageAcquisitionCostByItem, StringComparer.Ordinal),
        QuantityAcquiredForCostByItem = new Dictionary<string, int>(QuantityAcquiredForCostByItem, StringComparer.Ordinal),
        ShipMovementInputs = ShipMovementInputs,
        PortsVisitedCount = PortsVisitedCount,
        PortsVisited = PortsVisited.OrderBy(x => x, StringComparer.Ordinal).ToList(),
        CameraDrags = CameraDrags,
        CannonballsShot = CannonballsShot,
        ShipsHit = ShipsHit,
        ShipsSunk = ShipsSunk,
        BoughtComponentNames = BoughtComponentNames.OrderBy(x => x, StringComparer.Ordinal).ToList(),
        HiredCrewIds = HiredCrewIds.OrderBy(x => x, StringComparer.Ordinal).ToList(),
        TalkedToNpcIds = TalkedToNpcIds.OrderBy(x => x, StringComparer.Ordinal).ToList(),
        HighestShipTierReached = HighestShipTierReached,
        TotalMoneyEarned = TotalMoneyEarned,
        TotalMoneySpent = TotalMoneySpent,
      };
    }

    public void LoadFromDto(PlayerProgressSnapshotDto dto)
    {
      Reset();
      if (dto == null)
        return;

      Copy(dto.ItemsCollected, ItemsCollected);
      Copy(dto.ItemsBought, ItemsBought);
      Copy(dto.ItemsSold, ItemsSold);
      Copy(dto.SoldProfit, SoldProfit);
      Copy(dto.AverageAcquisitionCostByItem, AverageAcquisitionCostByItem);
      Copy(dto.QuantityAcquiredForCostByItem, QuantityAcquiredForCostByItem);
      ShipMovementInputs = dto.ShipMovementInputs;
      PortsVisitedCount = dto.PortsVisitedCount;
      foreach (var port in dto.PortsVisited ?? [])
        PortsVisited.Add(port);
      CameraDrags = dto.CameraDrags;
      CannonballsShot = dto.CannonballsShot;
      ShipsHit = dto.ShipsHit;
      ShipsSunk = dto.ShipsSunk;
      foreach (var component in dto.BoughtComponentNames ?? [])
        BoughtComponentNames.Add(component);
      foreach (var crewId in dto.HiredCrewIds ?? [])
        HiredCrewIds.Add(crewId);
      foreach (var npcId in dto.TalkedToNpcIds ?? [])
        TalkedToNpcIds.Add(npcId);
      HighestShipTierReached = dto.HighestShipTierReached;
      TotalMoneyEarned = dto.TotalMoneyEarned;
      TotalMoneySpent = dto.TotalMoneySpent;
    }

    private void UpdateAverageCost(string itemKey, float unitCost, int qty)
    {
      int previousQty = QuantityAcquiredForCostByItem.TryGetValue(itemKey, out var foundQty) ? foundQty : 0;
      float previousAvg = AverageAcquisitionCostByItem.TryGetValue(itemKey, out var foundAvg) ? foundAvg : 0.0f;
      int totalQty = previousQty + qty;
      if (totalQty <= 0)
      {
        QuantityAcquiredForCostByItem.Remove(itemKey);
        AverageAcquisitionCostByItem.Remove(itemKey);
        return;
      }

      float blendedAverage = ((previousAvg * previousQty) + (unitCost * qty)) / totalQty;
      QuantityAcquiredForCostByItem[itemKey] = totalQty;
      AverageAcquisitionCostByItem[itemKey] = blendedAverage;
    }

    private static void Increment(Dictionary<string, int> dict, string key, int amount)
    {
      dict[key] = (dict.TryGetValue(key, out var current) ? current : 0) + amount;
    }

    private static void Copy<T>(Dictionary<string, T> source, Dictionary<string, T> target)
    {
      foreach (var kvp in source ?? [])
        target[kvp.Key] = kvp.Value;
    }
  }
}
