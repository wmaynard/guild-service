using MongoDB.Driver;
using Rumble.Platform.GuildService.Models;
using Rumble.Platform.Common.Services;

namespace Rumble.Platform.GuildService.Services;

public class GuildService : PlatformMongoService<Guild>
{
	public GuildService() : base("guilds") {  }
    
	// Search guild by name
	public List<Guild> SearchByQuery(string query)
	{
		query = query.ToLower();
		return _collection.Find(filter: guild => guild.Name.ToLower().Contains(query)).ToList();
	}
	
	// Look up guild by id
	public Guild SearchById(string id)
	{
		return _collection.Find(filter: guild => guild.Id == id).FirstOrDefault();
	}
	
	// Look up player's guild by player ID
	public Guild SearchByPlayerId(string playerId)
	{
		FilterDefinition<Guild> filter =
			Builders<Guild>.Filter.Eq("Members.PlayerId", playerId);

		return _collection.Find(filter).FirstOrDefault();
	}
	
	// Fetch member details
	public Member CheckPlayer(string playerId)
	{
		Guild guild = SearchByPlayerId(playerId);
		Member member = guild.Members.Find(member => member.PlayerId == playerId);
		return member;
	}
	
	// Add member
	public void AddMember(string playerName, string playerId, string guildId)
	{
		Member newMember = new Member(name: playerName, playerId: playerId);

		List<WriteModel<Guild>> listWrites = new List<WriteModel<Guild>>();

		// TODO member limits
		FilterDefinition<Guild> filter = Builders<Guild>.Filter.Eq(guild => guild.Id, guildId);
		UpdateDefinition<Guild> update = Builders<Guild>.Update.Push(guild => guild.Members, newMember);
	
		listWrites.Add(new UpdateOneModel<Guild>(filter, update));
		_collection.BulkWrite(listWrites);
	}
	
	// Remove member
	public void RemoveMember(string playerId, string guildId)
	{
		List<WriteModel<Guild>> listWrites = new List<WriteModel<Guild>>();

		FilterDefinition<Guild> filter = Builders<Guild>.Filter.Eq(guild => guild.Id, guildId);
		filter &= Builders<Guild>.Filter.Eq("Members.PlayerId", playerId);
		UpdateDefinition<Guild> update = Builders<Guild>.Update.Unset(guild => guild.Members[-1]);
	
		listWrites.Add(new UpdateOneModel<Guild>(filter, update));
		_collection.BulkWrite(listWrites);
	}
	
	// Ban member
	public void BanMember(string playerId, string guildId)
	{
		RemoveMember(playerId: playerId, guildId: guildId);
		
		List<WriteModel<Guild>> listWrites = new List<WriteModel<Guild>>();

		FilterDefinition<Guild> filter = Builders<Guild>.Filter.Eq(guild => guild.Id, guildId);
		UpdateDefinition<Guild> update = Builders<Guild>.Update.Push(guild => guild.Bans, playerId);
	
		listWrites.Add(new UpdateOneModel<Guild>(filter, update));
		_collection.BulkWrite(listWrites);
	}

	// Unban member
	public void UnbanMember(string playerId, string guildId)
	{
		List<WriteModel<Guild>> listWrites = new List<WriteModel<Guild>>();

		FilterDefinition<Guild> filter = Builders<Guild>.Filter.Eq(guild => guild.Id, guildId);
		filter &= Builders<Guild>.Filter.ElemMatch(guild => guild.Bans, playerId);
		UpdateDefinition<Guild> update = Builders<Guild>.Update.Unset(guild => guild.Bans[-1]);
	
		listWrites.Add(new UpdateOneModel<Guild>(filter, update));
		_collection.BulkWrite(listWrites);
	}
	
	// Check ban
	public bool CheckBan(string playerId, string guildId)
	{
		Guild guild = SearchById(guildId);
		return guild.Bans.Contains(playerId);
	}

	// Change leader
	public void ChangeLeader(string oldLeaderId, string newLeaderId, string guildId)
	{
		FilterDefinition<Guild> searchFilter =
			Builders<Guild>.Filter.Eq("Members.PlayerId", newLeaderId);
		
		string newLeaderName = _collection.Find(searchFilter).FirstOrDefault()?.Name;
		
		List<WriteModel<Guild>> listWrites = new List<WriteModel<Guild>>();

		FilterDefinition<Guild> filterOld = Builders<Guild>.Filter.Eq("Members.PlayerId", oldLeaderId);
		UpdateDefinition<Guild> updateOld = Builders<Guild>.Update.Set(guild => guild.Members[-1].Position, Member.Role.Officer);
		FilterDefinition<Guild> filterNew = Builders<Guild>.Filter.Eq("Members.PlayerId", newLeaderId);
		UpdateDefinition<Guild> updateNew = Builders<Guild>.Update.Set(guild => guild.Members[-1].Position, Member.Role.Leader);
		FilterDefinition<Guild> filterLeader = Builders<Guild>.Filter.Eq(guild => guild.Id, guildId);
		UpdateDefinition<Guild> updateLeader = Builders<Guild>.Update.Set(guild => guild.Leader, newLeaderName);

		listWrites.Add(new UpdateOneModel<Guild>(filterOld, updateOld));
		listWrites.Add(new UpdateOneModel<Guild>(filterNew, updateNew));
		listWrites.Add(new UpdateOneModel<Guild>(filterLeader, updateLeader));
		_collection.BulkWrite(listWrites);
	}
	
	// Update position
	public void UpdatePosition(string playerId, Member.Role position, string guildId)
	{
		List<WriteModel<Guild>> listWrites = new List<WriteModel<Guild>>();

		FilterDefinition<Guild> filter = Builders<Guild>.Filter.Eq(guild => guild.Id, guildId);
		filter &= Builders<Guild>.Filter.Eq("Members.PlayerId", playerId);
		UpdateDefinition<Guild> update = Builders<Guild>.Update.Set(guild => guild.Members[-1].Position, position);

		listWrites.Add(new UpdateOneModel<Guild>(filter, update));
		_collection.BulkWrite(listWrites);
	}
	
	// Update guild info
	public void UpdateGuild(Guild newGuild)
	{
		List<WriteModel<Guild>> listWrites = new List<WriteModel<Guild>>();
		
		FilterDefinition<Guild> filter = Builders<Guild>.Filter.Eq(guild => guild.Id, newGuild.Id);
		UpdateDefinition<Guild> update = Builders<Guild>.Update.Set(guild => guild, newGuild);

		listWrites.Add(new UpdateOneModel<Guild>(filter, update));
		_collection.BulkWrite(listWrites);
	}
}