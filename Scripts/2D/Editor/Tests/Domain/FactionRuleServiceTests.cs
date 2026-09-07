namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Gameplay.Faction;
    using NUnit.Framework;
    using System.Collections.Generic;

    /// <summary>
    /// 阵营 Domain 纯规则 — 创建门槛/入伙打分/核心选址/核心数值。
    /// </summary>
    [TestFixture]
    public class FactionRuleServiceTests
    {
        // ---- FactionCreationRuleService ----

        private static FactionFounderSnapshot QualifiedFounder()
        {
            return new FactionFounderSnapshot
            {
                Ambition = 75f, Sociality = 70f, Mood = 60f, Greed = 50f,
                WalletGold = 0, HasFaction = false,
                WorkerCount = 12, ExistingFactionCount = 0,
                LifeStageIndex = 2,
            };
        }

        [Test]
        public void QualifiedFounder_SavesForFaction()
        {
            FactionFounderSnapshot snapshot = QualifiedFounder();
            Assert.IsTrue(FactionCreationRuleService.ShouldSaveForFaction(in snapshot));
        }

        [Test]
        public void BootstrapStage_NeverSaves()
        {
            FactionFounderSnapshot snapshot = QualifiedFounder();
            snapshot.LifeStageIndex = 1; // Settled：定居但未 Established
            Assert.IsFalse(FactionCreationRuleService.ShouldSaveForFaction(in snapshot));
        }

        [Test]
        public void LowAmbition_NeverSaves()
        {
            FactionFounderSnapshot snapshot = QualifiedFounder();
            snapshot.Ambition = FactionCreationRuleService.MinAmbition - 1f;
            Assert.IsFalse(FactionCreationRuleService.ShouldSaveForFaction(in snapshot));
        }

        [Test]
        public void ExistingFactionMember_NeverSaves()
        {
            FactionFounderSnapshot snapshot = QualifiedFounder();
            snapshot.HasFaction = true;
            Assert.IsFalse(FactionCreationRuleService.ShouldSaveForFaction(in snapshot));
        }

        [Test]
        public void CanFound_RequiresCostPlusReserve()
        {
            FactionFounderSnapshot snapshot = QualifiedFounder();
            snapshot.WalletGold = FactionCreationRuleService.FoundingCostGold
                + FactionCreationRuleService.WalletReserveAfterFounding - 1;
            Assert.IsFalse(FactionCreationRuleService.CanFound(in snapshot));

            snapshot.WalletGold += 1;
            Assert.IsTrue(FactionCreationRuleService.CanFound(in snapshot));
        }

        [Test]
        public void FactionSlots_ScaleWithWorkerCount()
        {
            // 12 人 → 2 个阵营位；3 人 → 至少 1
            Assert.AreEqual(2, FactionCreationRuleService.GetMaxFactionCount(12));
            Assert.AreEqual(1, FactionCreationRuleService.GetMaxFactionCount(3));
            Assert.AreEqual(1, FactionCreationRuleService.GetMaxFactionCount(0));

            FactionFounderSnapshot snapshot = QualifiedFounder();
            snapshot.WorkerCount = 12;
            snapshot.ExistingFactionCount = 2;
            Assert.IsFalse(FactionCreationRuleService.ShouldSaveForFaction(in snapshot));
        }

        // ---- FactionJoinRuleService ----

        private static JoinPersonalitySnapshot AveragePersonality()
        {
            return new JoinPersonalitySnapshot
            {
                Ambition = 40f, Sociality = 50f, Greed = 50f, Mood = 50f,
            };
        }

        private static FactionCandidateSnapshot FriendlyNearbyFaction()
        {
            return new FactionCandidateSnapshot
            {
                FactionId = 1, MemberCount = 5, TreasuryGold = 800,
                CoreLevel = 1, CorePlaced = true,
                AvgAffinity = 40f, AvgFavor = 60f, DistanceHomeToCore = 15f,
            };
        }

        [Test]
        public void FriendlyNearbyFaction_Joins()
        {
            JoinPersonalitySnapshot personality = AveragePersonality();
            var candidates = new List<FactionCandidateSnapshot> { FriendlyNearbyFaction() };

            Assert.IsTrue(FactionJoinRuleService.DecideJoin(in personality, candidates, out int bestId, out _));
            Assert.AreEqual(1, bestId);
        }

        [Test]
        public void DistantStrangers_DoNotJoin()
        {
            JoinPersonalitySnapshot personality = AveragePersonality();
            var candidate = FriendlyNearbyFaction();
            candidate.AvgAffinity = -30f;  // 与成员交恶
            candidate.AvgFavor = 10f;
            candidate.DistanceHomeToCore = 150f; // 隔半个地图
            var candidates = new List<FactionCandidateSnapshot> { candidate };

            Assert.IsFalse(FactionJoinRuleService.DecideJoin(in personality, candidates, out int bestId, out _));
            Assert.AreEqual(0, bestId);
        }

        [Test]
        public void FullFaction_AlwaysExcluded()
        {
            JoinPersonalitySnapshot personality = AveragePersonality();
            var candidate = FriendlyNearbyFaction();
            candidate.MemberCount = FactionJoinRuleService.MaxMembers;
            Assert.AreEqual(float.MinValue, FactionJoinRuleService.ComputeJoinScore(in personality, in candidate));
        }

        [Test]
        public void HighAmbition_PenalizedVersusLowAmbition()
        {
            JoinPersonalitySnapshot low = AveragePersonality();
            JoinPersonalitySnapshot high = AveragePersonality();
            high.Ambition = 90f;

            FactionCandidateSnapshot candidate = FriendlyNearbyFaction();
            Assert.Greater(
                FactionJoinRuleService.ComputeJoinScore(in low, in candidate),
                FactionJoinRuleService.ComputeJoinScore(in high, in candidate));
        }

        [Test]
        public void BestOfTwo_PicksHigherScore()
        {
            JoinPersonalitySnapshot personality = AveragePersonality();
            var weak = FriendlyNearbyFaction(); // id=1
            var strong = FriendlyNearbyFaction();
            strong.FactionId = 2;
            strong.CoreLevel = 3;
            strong.TreasuryGold = 3000;

            var candidates = new List<FactionCandidateSnapshot> { weak, strong };
            Assert.IsTrue(FactionJoinRuleService.DecideJoin(in personality, candidates, out int bestId, out _));
            Assert.AreEqual(2, bestId);
        }

        // ---- FactionPlacementRuleService ----

        [Test]
        public void Centroid_IsMeanOfAnchors()
        {
            var anchors = new List<GameVector2>
            {
                new GameVector2(10f, 10f),
                new GameVector2(20f, 10f),
                new GameVector2(10f, 20f),
                new GameVector2(20f, 20f),
            };

            GameVector2 centroid = FactionPlacementRuleService.ComputeCentroid(anchors, new GameVector2(0f, 0f));
            Assert.AreEqual(15f, centroid.X);
            Assert.AreEqual(15f, centroid.Y);
        }

        [Test]
        public void EmptyAnchors_FallBackToMapCenter()
        {
            GameVector2 fallback = new GameVector2(50f, 50f);
            GameVector2 centroid = FactionPlacementRuleService.ComputeCentroid(
                new List<GameVector2>(), fallback);
            Assert.AreEqual(fallback, centroid);
        }

        [Test]
        public void Placement_TooCloseToMapCenter_Invalid()
        {
            var mapCenter = new GameVector2(50f, 50f);
            var candidate = new GameVector2(50f, 50f + FactionPlacementRuleService.MinCoreToMapCenter - 1f);

            Assert.IsFalse(FactionPlacementRuleService.IsPlacementValid(
                candidate, new List<GameVector2>(), new List<GameVector2>(), mapCenter));
        }

        [Test]
        public void Placement_TooCloseToOtherCore_Invalid()
        {
            var mapCenter = new GameVector2(50f, 50f);
            var otherCore = new GameVector2(90f, 90f);
            var candidate = new GameVector2(
                otherCore.X + FactionPlacementRuleService.MinCoreSpacing - 1f, otherCore.Y);

            Assert.IsFalse(FactionPlacementRuleService.IsPlacementValid(
                candidate, new List<GameVector2> { otherCore }, new List<GameVector2>(), mapCenter));
        }

        [Test]
        public void Placement_ValidSpot_Passes()
        {
            var mapCenter = new GameVector2(50f, 50f);
            var otherCore = new GameVector2(90f, 90f);
            var poi = new GameVector2(120f, 30f);
            var candidate = new GameVector2(30f, 90f); // 距中心/核/POI 均超限

            Assert.IsTrue(FactionPlacementRuleService.IsPlacementValid(
                candidate,
                new List<GameVector2> { otherCore },
                new List<GameVector2> { poi },
                mapCenter));
        }

        // ---- FactionCoreRuleService ----

        [Test]
        public void MaxHp_ScalesWithLevel()
        {
            Assert.AreEqual(FactionCoreRuleService.CoreMaxHpBase, FactionCoreRuleService.GetMaxHp(1));
            Assert.AreEqual(
                FactionCoreRuleService.CoreMaxHpBase + FactionCoreRuleService.CoreHpPerLevel,
                FactionCoreRuleService.GetMaxHp(2));
        }

        [Test]
        public void Damage_Full_Destroys()
        {
            FactionCoreRuleService.CoreDamageResult result =
                FactionCoreRuleService.ApplyDamage(FactionCoreRuleService.GetMaxHp(1), 999f);
            Assert.IsTrue(result.IsDestroyed);
            Assert.AreEqual(0f, result.RemainingHp);
        }

        [Test]
        public void Damage_Partial_NotDestroyed()
        {
            FactionCoreRuleService.CoreDamageResult result = FactionCoreRuleService.ApplyDamage(400f, 150f);
            Assert.IsFalse(result.IsDestroyed);
            Assert.AreEqual(250f, result.RemainingHp);
        }

        [Test]
        public void UpgradeCosts_MaxLevelZero()
        {
            Assert.Greater(FactionCoreRuleService.GetUpgradeGoldCost(1), 0);
            Assert.Greater(FactionCoreRuleService.GetUpgradeGoldCost(2), 0);
            Assert.AreEqual(0, FactionCoreRuleService.GetUpgradeGoldCost(3));
            Assert.AreEqual(0, FactionCoreRuleService.GetUpgradeContributionCost(3));
        }

        [Test]
        public void DownfallLimit_Dissolves()
        {
            Assert.IsFalse(FactionCoreRuleService.ShouldDissolve(FactionCoreRuleService.MaxDownfalls - 1));
            Assert.IsTrue(FactionCoreRuleService.ShouldDissolve(FactionCoreRuleService.MaxDownfalls));
        }

        // ---- FactionContributionRuleService（P2）----

        [Test]
        public void QuestCountToday_DeterministicCycle()
        {
            // 逢第 3 日 3 个，其余 2 个（同日全阵营一致，无随机）
            Assert.AreEqual(3, FactionContributionRuleService.GetQuestCountToday(0));
            Assert.AreEqual(2, FactionContributionRuleService.GetQuestCountToday(1));
            Assert.AreEqual(2, FactionContributionRuleService.GetQuestCountToday(2));
            Assert.AreEqual(3, FactionContributionRuleService.GetQuestCountToday(3));
            Assert.AreEqual(3, FactionContributionRuleService.GetQuestCountToday(9));
        }

        [Test]
        public void Donation_MapsOneToOne()
        {
            Assert.AreEqual(100f, FactionContributionRuleService.ComputeDonationContribution(100));
            Assert.AreEqual(0f, FactionContributionRuleService.ComputeDonationContribution(0));
        }

        [Test]
        public void ExchangeCost_TwoPerGold()
        {
            Assert.AreEqual(50f, FactionContributionRuleService.ComputeExchangeCost(25));
            Assert.AreEqual(
                FactionContributionRuleService.ExchangeContributionPerGold * 4,
                FactionContributionRuleService.ComputeExchangeCost(4));
        }

        [Test]
        public void ExchangeDailyCap_Clamps()
        {
            int cap = FactionContributionRuleService.ExchangeDailyGoldCap;
            Assert.AreEqual(25, FactionContributionRuleService.ClampExchangeToday(25, 0));
            Assert.AreEqual(cap, FactionContributionRuleService.ClampExchangeToday(cap + 30, 0));
            Assert.AreEqual(cap - 20, FactionContributionRuleService.ClampExchangeToday(30, 20));
            Assert.AreEqual(0, FactionContributionRuleService.ClampExchangeToday(30, cap)); // 当日已兑满
            Assert.AreEqual(0, FactionContributionRuleService.ClampExchangeToday(-5, 0)); // 负数按 0
            Assert.AreEqual(0, FactionContributionRuleService.ClampExchangeToday(10, cap + 5)); // 超额记账按满
        }

        // ---- FactionRankRuleService（P2）----

        [Test]
        public void Rank_Thresholds()
        {
            Assert.AreEqual(FactionRank.Novice, FactionRankRuleService.GetRank(0f));
            Assert.AreEqual(FactionRank.Novice, FactionRankRuleService.GetRank(FactionRankRuleService.MemberThreshold - 0.01f));
            Assert.AreEqual(FactionRank.Member, FactionRankRuleService.GetRank(FactionRankRuleService.MemberThreshold));
            Assert.AreEqual(FactionRank.Elite, FactionRankRuleService.GetRank(FactionRankRuleService.EliteThreshold));
            Assert.AreEqual(FactionRank.Elder, FactionRankRuleService.GetRank(FactionRankRuleService.ElderThreshold));
            Assert.AreEqual(FactionRank.Elder, FactionRankRuleService.GetRank(FactionRankRuleService.ElderThreshold * 10f));
        }

        [Test]
        public void RankProgression_PromotionOnlyUpward()
        {
            // 10 任务/次贡献：90 → 100 跨 Member 线触发晋职事件
            FactionRank before = FactionRankRuleService.GetRank(90f);
            FactionRank after = FactionRankRuleService.GetRank(90f + FactionContributionRuleService.QuestContribution);
            Assert.IsTrue(FactionRankRuleService.IsPromotion(before, after));
            Assert.AreEqual(FactionRank.Member, after);

            // 同级/降级（兑换扣贡献）不算晋职
            Assert.IsFalse(FactionRankRuleService.IsPromotion(FactionRank.Member, FactionRank.Member));
            Assert.IsFalse(FactionRankRuleService.IsPromotion(FactionRank.Elite, FactionRank.Member));
        }

        [Test]
        public void VoteWeight_SeniorityOrdered()
        {
            Assert.Greater(
                FactionRankRuleService.GetVoteWeight(FactionRank.Elder),
                FactionRankRuleService.GetVoteWeight(FactionRank.Elite));
            Assert.Greater(
                FactionRankRuleService.GetVoteWeight(FactionRank.Elite),
                FactionRankRuleService.GetVoteWeight(FactionRank.Member));
            Assert.AreEqual(1f, FactionRankRuleService.GetVoteWeight(FactionRank.Novice));
            Assert.AreEqual(1f, FactionRankRuleService.GetVoteWeight(FactionRank.Member)); // 新手/正式同权
        }

        [Test]
        public void RankNames_AllDefined()
        {
            Assert.IsNotEmpty(FactionRankRuleService.GetRankName(FactionRank.Novice));
            Assert.IsNotEmpty(FactionRankRuleService.GetRankName(FactionRank.Member));
            Assert.IsNotEmpty(FactionRankRuleService.GetRankName(FactionRank.Elite));
            Assert.IsNotEmpty(FactionRankRuleService.GetRankName(FactionRank.Elder));
        }

        // ---- FactionElectionRuleService（P3）----

        private static ElectionCandidateSnapshot Candidate(
            string name, float ambition = 60f, float sociality = 50f,
            float contribution = 100f, bool incumbent = false)
        {
            return new ElectionCandidateSnapshot
            {
                Name = name, Ambition = ambition, Sociality = sociality,
                Contribution = contribution, IsIncumbent = incumbent,
            };
        }

        private static ElectionVoterSnapshot Voter(
            string name, float weight, Dictionary<string, float> affinity = null)
        {
            return new ElectionVoterSnapshot
            {
                Name = name, VoteWeight = weight,
                AffinityByCandidate = affinity ?? new Dictionary<string, float>(),
            };
        }

        [Test]
        public void ElectionDue_AtOrPastNextDay()
        {
            Assert.IsTrue(FactionElectionRuleService.IsElectionDue(5, 5));
            Assert.IsTrue(FactionElectionRuleService.IsElectionDue(7, 5)); // 跳日错过也补选
            Assert.IsFalse(FactionElectionRuleService.IsElectionDue(4, 5));
        }

        [Test]
        public void Candidates_FilteredByAmbition_SortedByContribution()
        {
            var members = new List<ElectionCandidateSnapshot>
            {
                Candidate("低野心", ambition: 30f, contribution: 999f), // 资格线外
                Candidate("丙", contribution: 100f),
                Candidate("甲", contribution: 300f),
                Candidate("乙", contribution: 200f),
            };

            List<ElectionCandidateSnapshot> candidates = FactionElectionRuleService.BuildCandidates(members);
            Assert.AreEqual(3, candidates.Count);
            Assert.AreEqual("甲", candidates[0].Name);
            Assert.AreEqual("乙", candidates[1].Name);
            Assert.AreEqual("丙", candidates[2].Name);
        }

        [Test]
        public void Candidates_CappedAtFour()
        {
            var members = new List<ElectionCandidateSnapshot>();
            for (int i = 0; i < 8; i++)
            {
                members.Add(Candidate($"员{i}", contribution: i * 10f));
            }

            Assert.AreEqual(
                FactionElectionRuleService.MaxCandidates,
                FactionElectionRuleService.BuildCandidates(members).Count);
        }

        [Test]
        public void VoteWeight_RankPlusSocial()
        {
            // 长老 2.0 + 社交 100→+0.5 = 2.5；新手 1.0 + 社交 0 = 1.0
            Assert.AreEqual(2.5f, FactionElectionRuleService.ComputeVoteWeight(FactionRank.Elder, 100f), 0.001f);
            Assert.AreEqual(1.0f, FactionElectionRuleService.ComputeVoteWeight(FactionRank.Novice, 0f), 0.001f);
            Assert.AreEqual(1.5f, FactionElectionRuleService.ComputeVoteWeight(FactionRank.Elite, 0f), 0.001f);
        }

        [Test]
        public void Incumbent_GetsBonus()
        {
            var voter = Voter("投票人", 1f);
            ElectionCandidateSnapshot challenger = Candidate("挑战者");
            ElectionCandidateSnapshot incumbent = Candidate("现任", incumbent: true);

            Assert.Greater(
                FactionElectionRuleService.ComputeVoteScore(in voter, in incumbent),
                FactionElectionRuleService.ComputeVoteScore(in voter, in challenger));
        }

        [Test]
        public void Affinity_SwaysVotes_NegativeDrops()
        {
            var voters = new List<ElectionVoterSnapshot>
            {
                Voter("甲党", 1f, new Dictionary<string, float> { ["甲"] = 80f }),
                Voter("路人", 1f, new Dictionary<string, float> { ["甲"] = -100f }), // 交恶倒戈
            };
            var candidates = new List<ElectionCandidateSnapshot> { Candidate("甲"), Candidate("乙") };

            // 甲党亲甲；路人恨甲 → 投乙；各 1 票平 → 按贡献序（相同）取名单靠前=甲？
            // 防平局歧义：把乙的贡献抬高，平局时乙稳定胜出
            candidates[1] = Candidate("乙", contribution: 500f);

            string winner = FactionElectionRuleService.Tally(candidates, voters);
            Assert.AreEqual("乙", winner);
        }

        [Test]
        public void Tally_WeightedVotes_Win()
        {
            // 长老票重 2.5 一票顶三人：甲得长老 1 票 = 2.5，乙得两票新手 = 2.0
            var voters = new List<ElectionVoterSnapshot>
            {
                Voter("长老", 2.5f, new Dictionary<string, float> { ["甲"] = 60f }),
                Voter("新手1", 1f, new Dictionary<string, float> { ["乙"] = 60f }),
                Voter("新手2", 1f, new Dictionary<string, float> { ["乙"] = 60f }),
            };
            var candidates = new List<ElectionCandidateSnapshot> { Candidate("甲"), Candidate("乙") };

            Assert.AreEqual("甲", FactionElectionRuleService.Tally(candidates, voters));
        }

        [Test]
        public void Tally_EmptyInputs_ReturnsNull()
        {
            Assert.IsNull(FactionElectionRuleService.Tally(
                new List<ElectionCandidateSnapshot>(), new List<ElectionVoterSnapshot> { Voter("v", 1f) }));
            Assert.IsNull(FactionElectionRuleService.Tally(
                new List<ElectionCandidateSnapshot> { Candidate("甲") }, new List<ElectionVoterSnapshot>()));
        }

        [Test]
        public void Tally_Deterministic_SameInputSameResult()
        {
            var candidates = new List<ElectionCandidateSnapshot>
            {
                Candidate("甲", sociality: 70f, contribution: 300f, incumbent: true),
                Candidate("乙", sociality: 50f, contribution: 500f),
                Candidate("丙", sociality: 80f, contribution: 100f),
            };
            var voters = new List<ElectionVoterSnapshot>
            {
                Voter("v1", 1.5f, new Dictionary<string, float> { ["甲"] = 30f, ["丙"] = -20f }),
                Voter("v2", 2f, new Dictionary<string, float> { ["乙"] = 50f }),
                Voter("v3", 1f),
            };

            string first = FactionElectionRuleService.Tally(candidates, voters);
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(first, FactionElectionRuleService.Tally(candidates, voters));
            }
        }

        // ---- FactionRelationRuleService（P5）----

        [Test]
        public void Relation_Clamp_StaysInBounds()
        {
            Assert.AreEqual(FactionRelationRuleService.RelationMin,
                FactionRelationRuleService.ApplyEvent(-99f, -10f));
            Assert.AreEqual(FactionRelationRuleService.RelationMax,
                FactionRelationRuleService.ApplyEvent(99f, 10f));
            Assert.AreEqual(50f, FactionRelationRuleService.ApplyEvent(50f, 0f), 0.0001f);
        }

        [Test]
        public void Relation_CrossAttack_AppliesDelta()
        {
            Assert.AreEqual(-3f, FactionRelationRuleService.ApplyEvent(0f, FactionRelationRuleService.CrossAttackDelta), 0.0001f);
        }

        [Test]
        public void Relation_DailyCap_LimitsHitsPerDay()
        {
            // 每日每对封顶 3 次生效（3 × -3 = -9）
            Assert.IsTrue(FactionRelationRuleService.CanApplyCrossAttack(0));
            Assert.IsTrue(FactionRelationRuleService.CanApplyCrossAttack(2));
            Assert.IsFalse(FactionRelationRuleService.CanApplyCrossAttack(3));
        }

        [Test]
        public void Relation_Decay_MovesTowardZero_NeverCrosses()
        {
            Assert.AreEqual(10f - FactionRelationRuleService.DailyDecay,
                FactionRelationRuleService.ComputeDecay(10f), 0.0001f);
            Assert.AreEqual(-40f + FactionRelationRuleService.DailyDecay,
                FactionRelationRuleService.ComputeDecay(-40f), 0.0001f);
            Assert.AreEqual(0f, FactionRelationRuleService.ComputeDecay(0.1f), 0.0001f);
            Assert.AreEqual(0f, FactionRelationRuleService.ComputeDecay(0f), 0.0001f);
        }

        [Test]
        public void Relation_PairKey_IsOrdered()
        {
            Assert.AreEqual(FactionRelationRuleService.GetPairKey(3, 7), FactionRelationRuleService.GetPairKey(7, 3));
            Assert.AreEqual((2, 5), FactionRelationRuleService.GetPairKey(5, 2));
        }

        // ---- FactionWarRuleService（P5）----

        [Test]
        public void Bellicosity_WeightedAmbitionAndGreed()
        {
            Assert.AreEqual(66f, FactionWarRuleService.ComputeBellicosity(60f, 60f), 0.0001f); // 60*0.6+60*0.4
            Assert.AreEqual(80f, FactionWarRuleService.ComputeBellicosity(100f, 100f), 0.0001f);
            Assert.AreEqual(0f, FactionWarRuleService.ComputeBellicosity(0f, 0f), 0.0001f);
        }

        [Test]
        public void EffectiveWarThreshold_BellicoseLeader_RelaxesThreshold()
        {
            // 好战度 0 → -40；100 → -20
            Assert.AreEqual(FactionWarRuleService.WarThreshold,
                FactionWarRuleService.GetEffectiveWarThreshold(0f), 0.0001f);
            Assert.AreEqual(FactionWarRuleService.WarThreshold + FactionWarRuleService.BellicosityThresholdScale,
                FactionWarRuleService.GetEffectiveWarThreshold(100f), 0.0001f);
        }

        [Test]
        public void DeclareWar_Gate_RelationAndCouncilBothRequired()
        {
            // 关系未破线：不开战（即使支持率满）
            Assert.IsFalse(FactionWarRuleService.CanDeclareWar(-39f, 0f, 1f));
            // 关系破线但支持率不足：不开战
            Assert.IsFalse(FactionWarRuleService.CanDeclareWar(-41f, 0f, 0.59f));
            // 双条件满足：开战
            Assert.IsTrue(FactionWarRuleService.CanDeclareWar(-41f, 0f, 0.6f));
            // 好战领袖放宽：-35 低于其有效阈值 -30 也可开战（bellicosity 50 → 阈值 -30）
            Assert.IsTrue(FactionWarRuleService.CanDeclareWar(-31f, 50f, 0.6f));
        }

        [Test]
        public void CouncilSupport_RatioClamped_ZeroTotalIsFull()
        {
            Assert.AreEqual(1f, FactionWarRuleService.ComputeCouncilSupportRatio(0, 0), 0.0001f);
            Assert.AreEqual(0.75f, FactionWarRuleService.ComputeCouncilSupportRatio(3, 4), 0.0001f);
            Assert.AreEqual(1f, FactionWarRuleService.ComputeCouncilSupportRatio(5, 4), 0.0001f);
        }

        [Test]
        public void RaidWindow_EveryThirdDay_NotDayZero()
        {
            Assert.IsFalse(FactionWarRuleService.IsRaidWindowDay(0));
            Assert.IsTrue(FactionWarRuleService.IsRaidWindowDay(3));
            Assert.IsFalse(FactionWarRuleService.IsRaidWindowDay(4));
            Assert.IsTrue(FactionWarRuleService.IsRaidWindowDay(6));
        }

        [Test]
        public void RaiderCount_CappedAtFour()
        {
            Assert.AreEqual(2, FactionWarRuleService.ComputeRaiderCount(2));
            Assert.AreEqual(FactionWarRuleService.MaxRaidersPerWindow, FactionWarRuleService.ComputeRaiderCount(9));
            Assert.AreEqual(0, FactionWarRuleService.ComputeRaiderCount(0));
        }

        [Test]
        public void LootGold_TwentyPercentCeiling()
        {
            Assert.AreEqual(20, FactionWarRuleService.ComputeLootGold(100));
            Assert.AreEqual(21, FactionWarRuleService.ComputeLootGold(101)); // 向上取整
            Assert.AreEqual(0, FactionWarRuleService.ComputeLootGold(0));
            Assert.AreEqual(0, FactionWarRuleService.ComputeLootGold(-5));
        }

        [Test]
        public void AutoPeace_ReachedAtCooldownLine()
        {
            Assert.IsFalse(FactionWarRuleService.ShouldAutoPeace(FactionWarRuleService.AutoPeaceRelation - 0.1f));
            Assert.IsTrue(FactionWarRuleService.ShouldAutoPeace(FactionWarRuleService.AutoPeaceRelation));
            Assert.IsTrue(FactionWarRuleService.ShouldAutoPeace(-10f));
        }
    }
}
