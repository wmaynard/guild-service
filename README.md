# Guild Service
A service for guild structure and management.

# Introduction
This service allows for `guilds` to be created, managed, deleted, or used in other intended ways.

A `guild` is constructed of a `name`, a `description`, a `type`, a `level requirement`, an optional piece of `art`, a `leader`, `members`, a `bans` list. 
This is subject to change in the future as new features are required. The current `types` that exist are as follows: `Private`, `Public`, and `Closed`.
The current member `positions` that exist are as follows: `Member`, `Officer`, and `Leader`.

A `member` consists of a `name`, a `playerId`, a `position`, and a `lastActive` timestamp.

A `history` consists of a `guildId`, a `log`, an `internalLog`, and a `timestamp`.

# Required Environment Variables
|                  Variable | Description                                                                            |
|--------------------------:|:---------------------------------------------------------------------------------------|
|                  GRAPHITE | Link to hosted _graphite_ for analytics and monitoring.                                |
|                LOGGLY_URL | Link to _Loggly_ to analyze logs in greater detail.                                    |
|              MONGODB_NAME | The _MongoDB_ name which the service connects to.                                      |
|               MONGODB_URI | The connection string for the environment's MongoDB.                                   |
|          RUMBLE_COMPONENT | The name of the service.                                                               |
|         RUMBLE_DEPLOYMENT | Signifies the deployment environment.                                                  |
|                RUMBLE_KEY | Key to validate for each deployment environment.                                       |
|   RUMBLE_TOKEN_VALIDATION | Link to current validation for player tokens.                                          |
| RUMBLE_TOKEN_VERIFICATION | Link to current validation for admin tokens. Will include player tokens in the future. |
|           VERBOSE_LOGGING | Logs in greater detail.                                                                |

# Glossary
|              Term | Description                                                                                           |
|------------------:|:------------------------------------------------------------------------------------------------------|
|      (Guild) Name | An unique player-friendly identifier for a guild.                                                     |
|       Description | A description that allows guild management to say something about themselves.                         |
|              Type | A setting that allows guild management to control how players can or cannot join or apply to a guild. |
| Level Requirement | A level requirement for all new applicants to the guild.                                              |
|               Art | An optional identifier for a guild art/icon.                                                          |
|            Leader | The guild leader's screenname.                                                                        |
|           Members | The members that are part of the guild.                                                               |
|              Bans | Players who are banned from a guild; they cannot rejoin or apply unless removed from the list.        |
|            Member | A member that is part of a guild.                                                                     |
|     (Member) Name | The member's screenname.                                                                              |
|          PlayerId | An unique identifier for a player.                                                                    |
|          Position | A role that is given to a member in a guild. This determines their privileges.                        |
|        LastActive | A timestamp to determine when the member was last active.                                             |
|           History | A means of logs for both guild use and internal use.                                                  |
|           GuildId | An identifier to link a log with a guild.                                                             |
|               Log | A guild facing log to show members changes and activities of their guild.                             |
|       InternalLog | An internal facing log that is in more detail for CS use.                                             |
|         Timestamp | The timestamp at which the event detailed in the log occurred.                                        |

# Using the Service
All non-health endpoints require a valid token from `token-service`. The admin endpoints require a valid admin token.
Requests to these endpoints should have an `Authorization` header with a `Bearer {token}`, where token is the aforementioned `token-service` token.

All `timestamps` in the service are in the format of a `Unix timestamp`. This is to allow consistency and reduce confusion between time zones.

# Endpoints
All endpoints are reached with the base route `/guild/`. Any following endpoints listed are appended on to the base route.

**Example**: `GET /guild/health`

## Top Level
All non-health endpoints require a valid standard token.

| Method | Endpoint    | Description                                              | Required Parameters                                          | Optional Parameters |
|-------:|:------------|:---------------------------------------------------------|:-------------------------------------------------------------|:--------------------|
|    GET | `/health`   | Health check on the status of the relevant microservices |                                                              |                     |
|    GET | `/player`   | Search for a guild that a player is in                   |                                                              |                     |
|    GET | `/search`   | Search for a list of guilds matching query               | *string*`query`                                              |                     |
|    GET | `/info`     | Get guild information                                    | *string*`guildId`                                            |                     |
|  PATCH | `/info`     | Modify guild information                                 | *Guild* `guild`                                              |                     |
|  PATCH | `/position` | Change a member's role in the guild                      | *string*`playerId` *Role*`position`                          |                     |
|  PATCH | `/leader`   | Change guild leader                                      | *string*`newLeaderId`                                        |                     |
|   POST | `/leave`    | Leaves guild                                             |                                                              |                     |
|   POST | `/`         | Creates guild                                            | *string*`name` *string*`desc` *GuildType*`type` *int*`level` |                     |
| DELETE | `/`         | Deletes guild; leader must be the only member            | *string*`guildId`                                            |                     |
|   POST | `/expel`    | Expels a member from the guild                           | *string*`playerId`                                           |                     |
|   POST | `/ban`      | Bans a player from the guild                             | *string*`playerId`                                           |                     |
|   POST | `/unban`    | Unbans a player from the guild                           | *string*`playerId`                                           |                     |

### Notes
*PATCH*, *POST*, and *DELETE* requests check for appropriate guild permissions by using the token from the player.

**PATCH /info Request Body Example**
```
{
    "guild": {
        "name": "GuildName",
        "description": "GuildDescription",
        "type": 1,
        "levelRequirement": 10,
        "art": "ArtName",
        "leader": "GuildLeader",
        "members": [
            {
                "name": "GuildLeader",
                "playerId": "63b743563090a47d42146b1a",
                "position": 2,
                "lastActive": 1680332400
            },
            {
                "name": "GuildMember",
                "playerId": "63b743563090a47d42146b1b",
                "position": 0,
                "lastActive": 1680320000
            }
        ],
        "bans": [
            "63b743563090a47d42146b1e",
            "63b743563090a47d42146b1f"
        ]
    }
}
```

**Role Enum**
```
public enum Role
{
    Member,
    Officer,
    Leader
}
```

**GuildType Enum**
```
public enum GuildType
{
    Private,
    Public,
    Closed
}
```


## Request
All non-health endpoints require a valid standard token.

| Method | Endpoint           | Description                                              | Required Parameters                           | Optional Parameters |
|-------:|:-------------------|:---------------------------------------------------------|:----------------------------------------------|:--------------------|
|    GET | `/requests/health` | Health check on the status of the relevant microservices |                                               |                     |
|   POST | `/accept`          | Accepts a request to join the guild                      | *string*`requestId`                           |                     |
|   POST | `/reject`          | Rejects a request to join the guild                      | *string*`requestId`                           |                     |
|   POST | `/`                | Creates a request to join the guild                      | *string*`desc` *string*`guildId` *int*`level` |                     |

## Admin
All non-health endpoints require a valid admin token.

| Method | Endpoint        | Description                                              | Required Parameters | Optional Parameters |
|-------:|:----------------|:---------------------------------------------------------|:--------------------|:--------------------|
|    GET | `/admin/health` | Health check on the status of the relevant microservices |                     |                     |


# Future Updates
- Admin endpoints depending on CS needs will be implemented when details are flushed out
- Recommended guilds
- Automatic leader change if leader is inactive
- Ban list to show current player screenname instead of id
- Prevent inappropriate guild names
- Member limits

# Troubleshooting
- Any issues should be recorded as a log in _Loggly_.