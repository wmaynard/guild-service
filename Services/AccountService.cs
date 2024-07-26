using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Utilities.JsonTools;

namespace Rumble.Platform.Guilds.Services;

public class AccountService : MinqService<Account>
{
    public AccountService() : base("accounts") { }
    
    public Account UpdateAccount(string accountId, long score) => mongo
        .Where(query => query.EqualTo(member => member.AccountId, accountId))
        .Upsert(update => update.Set(member => member.TotalHeroScore, score));

    public Account[] FromAccountIds(params string[] accountIds) => accountIds.Any()
        ? mongo
            .Where(query => query.ContainedIn(member => member.AccountId, accountIds))
            .Limit(100)
            .ToArray()
        : Array.Empty<Account>();
}

public class Account : PlatformCollectionDocument
{
    public const string FRIENDLY_KEY_HERO_SCORE = "totalHeroScore";
    
    [BsonElement(TokenInfo.DB_KEY_ACCOUNT_ID)]
    [JsonPropertyName(TokenInfo.FRIENDLY_KEY_ACCOUNT_ID)]
    public string AccountId { get; set; }
    
    [BsonElement("score")]
    [JsonPropertyName(FRIENDLY_KEY_HERO_SCORE)]
    public long TotalHeroScore { get; set; }
}