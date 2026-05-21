using System;
using Newtonsoft.Json;
namespace TyriaPlanner.Hud.Api
{
    public sealed class UpcomingResponse
    {
        [JsonProperty("mySignups")]      public MySignup[] MySignups { get; set; } = Array.Empty<MySignup>();
        [JsonProperty("newGuildEvents")] public NewGuildEvent[] NewGuildEvents { get; set; } = Array.Empty<NewGuildEvent>();
        [JsonProperty("serverTime")]     public DateTime ServerTime { get; set; }
    }
    public abstract class EventBase
    {
        [JsonProperty("id")]                     public string Id { get; set; }
        [JsonProperty("title")]                  public string Title { get; set; }
        [JsonProperty("type")]                   public string Type { get; set; }
        [JsonProperty("scheduledAt")]            public DateTime ScheduledAt { get; set; }
        [JsonProperty("durationMinutes")]        public int DurationMinutes { get; set; }
        [JsonProperty("isPublic")]               public bool IsPublic { get; set; }
        [JsonProperty("wvwMap")]                 public string WvwMap { get; set; }
        [JsonProperty("voiceChannelUrl")]        public string VoiceChannelUrl { get; set; }
        [JsonProperty("isRecurring")]            public bool IsRecurring { get; set; }
        [JsonProperty("bossSlugs")]              public string[] BossSlugs { get; set; } = Array.Empty<string>();
        [JsonProperty("kpRequirement")]          public KpRequirement KpRequirement { get; set; }
        [JsonProperty("guildName")]              public string GuildName { get; set; }
        [JsonProperty("guildTag")]               public string GuildTag { get; set; }
        [JsonProperty("commanderAccountName")]   public string CommanderAccountName { get; set; }
        [JsonProperty("commanderDisplayName")]   public string CommanderDisplayName { get; set; }
        [JsonProperty("commanderUsername")]      public string CommanderUsername { get; set; }
        [JsonProperty("signupCount")]            public int SignupCount { get; set; }
        [JsonProperty("maxSignups")]             public int MaxSignups { get; set; }
    }
    public sealed class KpRequirement
    {
        [JsonProperty("amount")] public int Amount { get; set; }
        [JsonProperty("mode")]   public string Mode { get; set; }
    }
    public sealed class SignupCharacter
    {
        [JsonProperty("name")]       public string Name { get; set; }
        [JsonProperty("profession")] public string Profession { get; set; }
        [JsonProperty("eliteSpec")]  public string EliteSpec { get; set; }
    }
    public sealed class MySignup : EventBase
    {
        [JsonProperty("checkinReminderMinutes")] public int? CheckinReminderMinutes { get; set; }
        [JsonProperty("checkinStatus")]          public string CheckinStatus { get; set; }
        [JsonProperty("approvalStatus")]         public string ApprovalStatus { get; set; }
        [JsonProperty("isBench")]                public bool IsBench { get; set; }
        [JsonProperty("signupCharacter")]        public SignupCharacter SignupCharacter { get; set; }
    }
    public sealed class NewGuildEvent : EventBase
    {
        [JsonProperty("createdAt")] public DateTime CreatedAt { get; set; }
    }
}
