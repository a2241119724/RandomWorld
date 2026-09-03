namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Worker;
    using NUnit.Framework;

    /// <summary>
    /// 防守夜响应决策纯规则 — 三类行为分化：标准村民默认参战、
    /// 胆小高压者躲避、贪婪冷血者趁乱、觉醒/高境界者优先参战。
    /// </summary>
    [TestFixture]
    public class DefenceDraftRuleServiceTests
    {
        private static DefenceDraftInput Average()
        {
            // 各维全 50 的标准村民（无觉醒、凡人）
            return new DefenceDraftInput
            {
                Mood = 50f, Ambition = 50f, Diligence = 50f, Sociality = 50f,
                Greed = 50f, Stress = 50f, Morale = 50f, FavorWithPlayer = 50f,
                HasAwakenedPower = false, RealmIndex = 0,
            };
        }

        [Test]
        public void AverageWorker_Fights()
        {
            DefenceDraftInput input = Average();

            Assert.AreEqual(DefenceResponse.Fight, DefenceDraftRuleService.Decide(in input));
        }

        [Test]
        public void AwakenedWorker_Fights_EvenWhenScared()
        {
            DefenceDraftInput input = Average();
            input.Mood = 20f;
            input.Stress = 80f;
            input.Morale = 30f;
            input.HasAwakenedPower = true;

            Assert.AreEqual(DefenceResponse.Fight, DefenceDraftRuleService.Decide(in input));
        }

        [Test]
        public void HighRealm_Fights()
        {
            DefenceDraftInput input = Average();
            input.Mood = 20f;
            input.Stress = 80f;
            input.Morale = 30f;
            input.RealmIndex = 3; // 金丹：+30 参战分压过躲避优势

            Assert.AreEqual(DefenceResponse.Fight, DefenceDraftRuleService.Decide(in input));
        }

        [Test]
        public void TimidHighStressWorker_SheltersInBed()
        {
            DefenceDraftInput input = Average();
            input.Mood = 20f;
            input.Stress = 80f;
            input.Morale = 30f;
            input.Diligence = 30f;
            input.Greed = 20f;

            Assert.AreEqual(DefenceResponse.ShelterInBed, DefenceDraftRuleService.Decide(in input));
        }

        [Test]
        public void GreedyColdWorker_Loots()
        {
            DefenceDraftInput input = Average();
            input.Greed = 90f;
            input.FavorWithPlayer = 20f;
            input.Morale = 40f;
            input.Mood = 30f;
            input.Stress = 50f;

            Assert.AreEqual(DefenceResponse.Loot, DefenceDraftRuleService.Decide(in input));
        }

        [Test]
        public void LowGreed_NeverLoots_EvenWithNoFavor()
        {
            // 贪婪未过门槛：再冷血也不趁乱
            DefenceDraftInput input = Average();
            input.Greed = 40f;
            input.FavorWithPlayer = 0f;
            input.Morale = 10f;

            Assert.AreNotEqual(DefenceResponse.Loot, DefenceDraftRuleService.Decide(in input));
        }

        [Test]
        public void GoodFavorWithPlayer_WeightsTowardFight()
        {
            // 同一人（低野心胆小者）：好感从 20 → 90 足以把躲避者拉回战场（玩家经营的回报）
            DefenceDraftInput scared = Average();
            scared.Mood = 30f;
            scared.Stress = 50f;
            scared.Morale = 40f;
            scared.Greed = 20f;
            scared.FavorWithPlayer = 20f;
            scared.Ambition = 20f; // 低野心：否则 Ambition 参战分会把胆小者推出躲避
            DefenceDraftInput loyal = scared;
            loyal.FavorWithPlayer = 90f;

            Assert.AreEqual(DefenceResponse.ShelterInBed, DefenceDraftRuleService.Decide(in scared));
            Assert.AreEqual(DefenceResponse.Fight, DefenceDraftRuleService.Decide(in loyal));
        }

        [Test]
        public void HighAmbition_WeightsTowardFight()
        {
            // 同一人：事业心从 20 → 95 足以把躲避者推上战场（野心者渴望战功）
            DefenceDraftInput timid = Average();
            timid.Mood = 30f;
            timid.Stress = 50f;
            timid.Morale = 40f;
            timid.Greed = 20f;
            timid.FavorWithPlayer = 20f;
            timid.Ambition = 20f;
            DefenceDraftInput ambitious = timid;
            ambitious.Ambition = 95f;

            Assert.AreEqual(DefenceResponse.ShelterInBed, DefenceDraftRuleService.Decide(in timid));
            Assert.AreEqual(DefenceResponse.Fight, DefenceDraftRuleService.Decide(in ambitious));
        }

        [Test]
        public void Decide_IsDeterministic()
        {
            DefenceDraftInput input = Average();
            input.Greed = 90f;
            input.FavorWithPlayer = 20f;

            DefenceResponse first = DefenceDraftRuleService.Decide(in input);
            DefenceResponse second = DefenceDraftRuleService.Decide(in input);

            Assert.AreEqual(first, second);
        }
    }
}
