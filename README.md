# Guild Service
A service for guild structure and management.

# Introduction
This service allows for `guilds` to be created, managed, deleted, or used in other intended ways.

A `guild` is constructed of a `name`, a `description`, a `type`, a `level requirement`, an optional piece of `art`, a `leader`, `members`, a `bans` list. 
This is subject to change in the future as new features are required. The current `types` that exist are as follows: `Invite Only`, `Public`, and `Private`.
The current member `positions` that exist are as follows: `Member`, `Officer`, and `Leader`.

A `member` consists of a `name`, a `playerId`, a `position`, and a `lastActive` timestamp.

A `history` consists of a `guildId`, a `log`, an `internalLog`, and a `timestamp`.

# Glossary

| Term      | Definition                                                                                                                                                                                                             |
|:----------|:-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Access    | How "open" a guild is.  Guilds can be Public (anyone can join), Private (requires application), or Invite Only.  Invite Only guilds never show up in Search.                                                           |
| Applicant | The lowest form of Member.  Applicants can not participate in Chat.  Applicants can be members of any number of guilds until an application is accepted by a guild, at which point, previous applications are deleted. |
| Ban       | An upgraded Kick.  Bans entirely prevent account IDs from ever discovering the guild, preventing reapplication / rejoining.  Very useful to stop abuse where someone is kicked, changes their name, and rejoins.       | 
| Chat      | Shorthand for "Guild Chat".  Chat is managed entirely on chat-service, and is a private room that is updated by guild-service.                                                                                         |
| Elder     | Someone who's seen as a valued Member, but not trustworthy enough to be an Officer.                                                                                                                                    |
| Guild     | A profile document shared between a group of players.                                                                                                                                                                  |
| History   | A complete record of every user's various memberships, including promotions or demotions, for the last six months.                                                                                                     |
| Kick      | Removes a Member from the Guild.  In order to kick, a player needs to be of higher rank than the player they're removing.                                                                                              |
| Leader    | The player with supreme power over the guild.  Only one leader can exist in a guild at a time.  Special rules apply to leaders regarding required activity and leaving the guild, which will be covered below.         |
| Member    | Standard player in a guild.  Members have no ability to manage the guild, but can participate in all guild activities.                                                                                                 |
| Officer   | Officers help manage guild members and are able to kick players out, promote or demote, and effectively do everything a leader could at one level down.  Officers can not impact other officers with their actions.    |
| Rank      | Ranks include, in order: Applicant, Member, Elder, Officer, Leader.  There is room to add more if needed at a later date.                                                                                              |
| Search    | Fuzzy scan of available guilds.  Searches scan guild names, descriptions, and language / region.                                                                                                                       |

# Guild Management

## Creating a Guild (Admin)

Because guild creation is gated by resource spend, this endpoint must be routed through the game server and passed an admin token to complete.  Guild names must be unique.  Creating a new guild forces the player to leave any other guild they were a part of, and they are placed into the guild as its leader.

```
POST /create
{
    "accountId": "deadbeefdeadbeefdeadbeef",
    "guild": {
        "name": "Avalanche",
        "language": "en-US",
        "region": "us",
        "access": 0,                                   // 0: Public, 1: Private, 2: Invite Only
        "requiredLevel": 10,                           // Stubbed!  Won't do anything, but it's in the roadmap
        "description": "Lorem ipsum dolor sit amet",
        "iconData": {                                  // Platform has no idea what this data is; purely for client usage.  Can be any JSON.
            "layer1": "atlas[foo]",
            "layer2": "atlas[bar]"
        }
    }
}

Response:
HTTP 200
{
    "guild": {
        "name": "Avalanche",
        "language": "en-US",
        "region": "us",
        "access": 0,
        "requiredLevel": 10,
        "description": "Lorem ipsum dolor sit amet",
        "iconData": {
            "layer1": "atlas[foo]",
            "layer2": "atlas[bar]"
        },
        "members": [
            {
                "accountId": "deadbeefdeadbeefdeadbeef",
                "approvedBy": null,
                "updatedBy": null,
                "kickedBy": null,
                "guildId": null,
                "joinedOn": 1706858868,
                "leftOn": 0,
                "lastActive": 0,
                "rank": 10,
                "rankVerbose": "Leader",
                "id": "65bc9974db681ef3687b48f1",
                "createdOn": 1706858868
            }
        ],
        "id": "65c3ffd2e549849f681be2ea",
        "createdOn": 1706861398
    }
}

Notable Error: Guild name is not unique
HTTP 400
{
    "message": "Unique constraint violated; operation cannot proceed",
    "errorCode": "PLATF-0201: InvalidRequestData"
}
```

## Joining a Guild

Sucessfully joining a guild forces players out of existing membership, if any.  For Private guilds, this happens on application approval.

* Open Guild: Player is added to the guild and chat immediately.
* Private Guild: Player is added to the guild as an applicant.  An officer must approve them before they become an official member.
* Invite Only Guild: Stubbed / not currently supported; will require an invitation to join a guild.

Guild member cap is defined in Dynamic Config (`capacity`).  The default value is 20.

```
POST /join
{
    "guildId": "65bca0705a1498d3a4d22d7c"
}

Response:
HTTP 200
{
    "guild": { ... }
}

Notable Error:
HTTP 400
{
    "message": "Unable to perform update; guild is full.",
    "errorCode": "PLATF-3001: Ineligible"
}
```

## Leaving a Guild

Leaving is incredibly simple; hitting this endpoint will remove ALL membership for the calling player.

```
DELETE /leave

Response:
HTTP 204 (empty)
```

* When the last member of a guild leaves, that guild will be destroyed.
* If the leader of a guild leaves, the successor is chosen:
  * Take the array of all members above Applicant level
  * Order by descending rank, then by descending join date
  * Promote the first member in the array to Leader.

## Kicking a Guild Member

Similarly simple, the only data point needed is the target account ID.

```
DELETE /kick?accountId=deadbeefdeadbeefdeadbeef

Response:
HTTP 204 (empty)

Notable Error: Requesting player does not have permission to kick
HTTP 400
{
    "message": "Unable to affect player; requester is not an officer.",
    "errorCode": "PLATF-0101: Unauthorized"
}
```

## Guild Details

Viewing guild details requires a `guildId`.  You can view guilds you're not a member of, including member lists, but you can only see the applicant list for one you _are_ a member of.

Unlike some of the endpoints you've seen so far, the `guildId` here should be provided because it saves an extra database hit, whereas comparing the relationship between two players requires two lookups even if the ID is provided.

**Note:** if you _don't_ provide the `guildId`, you will get the guild you're currently enrolled in or an error if you are not in one.  This is less performant though and best practice is to avoid this.

```
GET {base url}?guildId=65bca0705a1498d3a4d22d7c

Response:
HTTP 200{
    "guild": {
        "name": "Avalanche",
        "language": "en-US",
        "region": "us",
        "access": 0,
        "requiredLevel": 10,
        "description": "Lorem ipsum dolor sit amet",
        "iconData": {
            "layer1": "atlas[foo]",
            "layer2": "atlas[bar]"
        },
        "members": [ ... ],
        "id": 65bca0705a1498d3a4d22d7c,
        "createdOn": 1706861398
    }
}
```

## Guild Search

Guild search takes a string, `terms`, and returns weighted results based on the search.  The string can be either CSV separated or whitespace separated.  When searching with whitespace, this searches on the whole string as well as the split components.  For example, searching `foo bar` searches for `foo bar,foo,bar`, with `foo bar` carrying the most weight because it is the longest term.  This is just a brief summary of how search works; for the full documentation and limitations of this feature, refer to platform-common's `MINQ.md > Searching with MINQ` section.

Invite Only guilds are not searchable.

```
GET /search?terms=foo%20bar

Response:
HTTP 200
{
    "guilds": [ ... ]
}
```

## Approving Applicants

Any officer can approve applicants.  Applicants are a rank only seen in Private guilds.

```
PATCH /approve
{
    "accountId": "65bae6cede38ba339d04b7e8"
}

Response:
HTTP 200
{
    "guildMember": {
        "accountId": "65bae6cede38ba339d04b7e8",
        "approvedBy": "deadbeefdeadbeefdeadbeef",
        "updatedBy": null,
        "kickedBy": null,
        "guildId": "65bca0705a1498d3a4d22d7c",
        "joinedOn": 1706861850,
        "leftOn": 0,
        "lastActive": 0,
        "rank": 1,
        "rankVerbose": "Member",
        "id": "65bca51a5536aadcdbb91e05",
        "createdOn": 1706861850
    }
}

Notable Errors:
HTTP 400
{
    "message": "Unable to affect player; requester is not an officer.",
    "errorCode": "PLATF-0101: Unauthorized"
}
HTTP 400
{
    "message": "Unable to perform update; guild is full.",
    "errorCode": "PLATF-3001: Ineligible"
}

```

## Promoting & Demoting Members

Guilds currently supports 5 distinct ranks:

* Applicant: Can't participate in the guild until approved by an officer.
* Member: No special permissions.
* Elder: No ability to modify the guild; this rank exists to show appreciation to memebrs without granting them the role of an officer.
* Officer: Can demote / kick players and accept applicants.
* Leader: No more than one player can hold this rank in a guild.  All the privileges of an Officer, but can also modify guild details.

To promote or demote players, pass the following endpoint a player token from an officer or leader:

```
PATCH /rank
{
    "accountId": "deadbeefdeadbeefdeadbeef",
    "isPromotion": true
}
```

Players must exceed the rank of the target account for the rank change to take effect; an officer cannot promote another officer.

If a leader promotes an officer, it will _also_ act as a self-demotion.  The target player will become the leader and the leader will become an officer.

Members cannot be demoted; they must be kicked.

# Guild Chat

Guild chat functionality is actually handled by chat-service, not guilds.  Guilds manages a chat room's participants and room data, but otherwise has no knowledge of what is happening on the chat side of things.  Consequently, there are no endpoints to manage Guild chat in this project.

The following actions impact a guild's chat room:

* Creating a guild
* Joining a guild
* Approving an applicant
* Kicking a member

Guilds takes care of these events automatically.  There is also a background task that runs as a redundancy to make sure rooms stay updated.  For example, if someone leaves the guild but chat was down at the time, the membership update would have failed.  Failed room updates are retried.  Guilds are resynced with chat any time a membership change is detected and around once per hour.

However, when creating a guild, if the chat room creation _also_ fails, the entire request is rejected.  In this way, guilds has a dependency on chat; we don't want a situation where a guild does not have chat.