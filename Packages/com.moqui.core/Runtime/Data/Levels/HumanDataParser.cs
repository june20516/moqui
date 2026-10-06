using System.Collections.Generic;
using System.Numerics;
using Moqui.Core.Simulation;

namespace Moqui.Core.Data.Levels
{
    /// <summary>레벨 데이터의 human 객체를 HumanDefinition으로 읽는다 (tech/architecture.md §5 level JSON의 human).</summary>
    public static class HumanDataParser
    {
        public static HumanDefinition Parse(JsonAccess json)
        {
            var parts = new List<BodyPartDefinition>();
            foreach (var item in json.Get("parts").Items())
            {
                SkinSiteType? site = item.Has("site") ? item.Get("site").Enum<SkinSiteType>() : (SkinSiteType?)null;
                parts.Add(new BodyPartDefinition(
                    item.Get("id").String(),
                    item.Get("kind").Enum<BodyPartKind>(),
                    item.Get("a").Vector3(),
                    item.Get("b").Vector3(),
                    item.Get("radius").Float(),
                    site));
            }

            var shoulders = new List<Vector3>();
            foreach (var item in json.Get("shoulders").Items())
            {
                shoulders.Add(item.Vector3());
            }

            var idle = json.Get("idle");
            var lookYaws = new List<float>();
            foreach (var item in idle.Get("lookYaws").Items())
            {
                lookYaws.Add(item.Float());
            }

            IdleGlance glance = null;
            if (idle.Has("glance"))
            {
                var glanceJson = idle.Get("glance");
                glance = new IdleGlance(glanceJson.Get("interval").Range(), glanceJson.Get("angle").Float(), glanceJson.Get("duration").Float());
            }

            var actions = new List<HumanActionDefinition>();
            foreach (var item in json.Get("actions").Items())
            {
                var motions = new List<PartMotionDefinition>();
                foreach (var motion in item.Get("motions").Items())
                {
                    motions.Add(new PartMotionDefinition(motion.Get("part").String(), motion.Get("offsetA").Vector3(), motion.Get("offsetB").Vector3()));
                }

                actions.Add(new HumanActionDefinition(item.Get("name").String(), item.Get("weight").Float(), item.Get("duration").Float(), motions));
            }

            var modifiers = new List<HumanModifier>();
            foreach (var item in json.Get("modifiers").Items())
            {
                modifiers.Add(item.Enum<HumanModifier>());
            }

            var traits = new HumanTraits(modifiers, json.Get("canSpray").Bool(), glance);
            HumanWalkDefinition walk = null;
            if (json.Has("walk"))
            {
                var walkJson = json.Get("walk");
                var route = new List<Vector2>();
                foreach (var item in walkJson.Get("route").Items())
                {
                    var values = item.Items();
                    route.Add(new Vector2(values[0].Float(), values[1].Float()));
                }

                walk = new HumanWalkDefinition(route, walkJson.Get("pause").Range(), !walkJson.Has("loop") || walkJson.Get("loop").Bool());
            }

            try
            {
                return new HumanDefinition(
                    json.Get("id").String(),
                    json.Get("position").Vector3(),
                    json.Get("facingYaw").Float(),
                    parts,
                    json.Get("head").String(),
                    shoulders,
                    lookYaws,
                    actions,
                    traits,
                    json.Has("facingPitch") ? json.Get("facingPitch").Float() : 0f,
                    json.Has("restPitch") ? json.Get("restPitch").Float() : 0f,
                    json.Has("maxPosture") ? json.Get("maxPosture").Enum<PostureLevel>() : PostureLevel.Rise,
                    walk,
                    json.Has("tool") ? json.Get("tool").Enum<HumanTool>() : HumanTool.None);
            }
            catch (System.ArgumentException exception)
            {
                throw json.Error(exception.Message);
            }
        }
    }
}
