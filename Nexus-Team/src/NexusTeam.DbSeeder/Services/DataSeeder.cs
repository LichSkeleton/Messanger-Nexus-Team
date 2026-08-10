using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Oracle.ManagedDataAccess.Client;

namespace NexusTeam.DbSeeder.Services;

/// <summary>
/// Seeds a small, deterministic set of demo data: a fixed group of users
/// (Vlad, Sofia, Hakan, Anna) and a short chat history between them.
/// Runs "from scratch" every time - existing seed entities are removed and
/// recreated so the demo data is always identical and ready to log in with.
/// </summary>
public class DataSeeder
{
    /// <summary>
    /// Shared password for every seeded demo account (plain text).
    /// </summary>
    public const string DemoPassword = "Aa123456";

    private readonly string oracleConnectionString;
    private readonly string mongoConnectionString;
    private readonly string mongoDatabaseName;
    private readonly ILogger<DataSeeder> logger;

    private static readonly DemoUser[] Users =
    {
        new("seed-user-vlad", "Vlad", "vlad@nexusteam.dev", "Vlad"),
        new("seed-user-sofia", "Sofia", "sofia@nexusteam.dev", "Sofia"),
        new("seed-user-hakan", "Hakan", "hakan@nexusteam.dev", "Hakan"),
        new("seed-user-anna", "Anna", "anna@nexusteam.dev", "Anna"),
    };

    public DataSeeder(
        string oracleConnectionString,
        string mongoConnectionString,
        string mongoDatabaseName,
        ILogger<DataSeeder> logger)
    {
        this.oracleConnectionString = oracleConnectionString;
        this.mongoConnectionString = mongoConnectionString;
        this.mongoDatabaseName = mongoDatabaseName;
        this.logger = logger;
    }

    /// <summary>
    /// Seeds the demo users and their chat history.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        this.logger.LogInformation("Starting demo data seeding (users + chat history)...");

        await this.SeedUsersAsync(cancellationToken);
        await this.SeedChatsAndMessagesAsync(cancellationToken);

        this.logger.LogInformation("Demo data seeding completed successfully");
    }

    /// <summary>
    /// Inserts the fixed demo users into Oracle, recreating them from scratch.
    /// </summary>
    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword, 12);
        var now = DateTime.UtcNow;

        using var connection = new OracleConnection(this.oracleConnectionString);
        await connection.OpenAsync(cancellationToken);

        // Also clear previous demo usernames that are no longer seeded.
        using (var cleanupCommand = new OracleCommand(
            "DELETE FROM users WHERE id LIKE 'seed-user-%' OR username IN ('Pavalo', 'Olen')",
            connection))
        {
            await cleanupCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var user in Users)
        {
            // Remove any previous copy so seeding is deterministic ("from scratch").
            using (var deleteCommand = new OracleCommand("DELETE FROM users WHERE id = :id OR username = :username", connection))
            {
                deleteCommand.Parameters.Add(new OracleParameter("id", user.Id));
                deleteCommand.Parameters.Add(new OracleParameter("username", user.Username));
                await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            using var insertCommand = new OracleCommand(
                @"INSERT INTO users (id, username, email, password_hash, display_name, avatar_url, status, created_at, updated_at, last_seen_at)
                  VALUES (:id, :username, :email, :password_hash, :display_name, :avatar_url, 0, :created_at, :updated_at, :last_seen_at)",
                connection);

            insertCommand.Parameters.Add(new OracleParameter("id", user.Id));
            insertCommand.Parameters.Add(new OracleParameter("username", user.Username));
            insertCommand.Parameters.Add(new OracleParameter("email", user.Email));
            insertCommand.Parameters.Add(new OracleParameter("password_hash", passwordHash));
            insertCommand.Parameters.Add(new OracleParameter("display_name", user.DisplayName));
            insertCommand.Parameters.Add(new OracleParameter("avatar_url", "/api/users/avatar/default"));
            insertCommand.Parameters.Add(new OracleParameter("created_at", OracleDbType.TimeStamp) { Value = now });
            insertCommand.Parameters.Add(new OracleParameter("updated_at", OracleDbType.TimeStamp) { Value = now });
            insertCommand.Parameters.Add(new OracleParameter("last_seen_at", OracleDbType.TimeStamp) { Value = now });

            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
            this.logger.LogInformation("Seeded user '{Username}' (login: {Username} / password: {Password})", user.Username, user.Username, DemoPassword);
        }
    }

    /// <summary>
    /// Inserts the fixed demo chats and their message history into MongoDB,
    /// recreating them from scratch.
    /// </summary>
    private async Task SeedChatsAndMessagesAsync(CancellationToken cancellationToken)
    {
        var client = new MongoClient(this.mongoConnectionString);
        var database = client.GetDatabase(this.mongoDatabaseName);
        var chats = database.GetCollection<BsonDocument>("chats");
        var messages = database.GetCollection<BsonDocument>("messages");

        var vlad = Users[0].Id;
        var sofia = Users[1].Id;
        var hakan = Users[2].Id;
        var anna = Users[3].Id;

        var baseTime = DateTime.UtcNow.AddHours(-3);

        var seededChats = new[]
        {
            new SeedChat(
                "seed-chat-vlad-sofia",
                "Vlad & Sofia",
                "private",
                new[] { vlad, sofia },
                vlad,
                new[]
                {
                    new SeedMessage("seed-msg-vs-1", vlad, "Hey Sofia, ready for the project demo?"),
                    new SeedMessage("seed-msg-vs-2", sofia, "Almost! Just polishing the UI a bit."),
                    new SeedMessage("seed-msg-vs-3", vlad, "Nice — ping me when you push."),
                }),
            new SeedChat(
                "seed-chat-hakan-anna",
                "Hakan & Anna",
                "private",
                new[] { hakan, anna },
                hakan,
                new[]
                {
                    new SeedMessage("seed-msg-ha-1", hakan, "Anna, did the Mongo indexes look okay?"),
                    new SeedMessage("seed-msg-ha-2", anna, "Yes, queries are much faster now."),
                    new SeedMessage("seed-msg-ha-3", hakan, "Perfect, thanks!"),
                }),
            new SeedChat(
                "seed-chat-sofia-anna",
                "Sofia & Anna",
                "private",
                new[] { sofia, anna },
                sofia,
                new[]
                {
                    new SeedMessage("seed-msg-sa-1", sofia, "Want to grab coffee after standup?"),
                    new SeedMessage("seed-msg-sa-2", anna, "Sure! Meet you in the lobby."),
                }),
            new SeedChat(
                "seed-chat-vlad-hakan",
                "Vlad & Hakan",
                "private",
                new[] { vlad, hakan },
                vlad,
                new[]
                {
                    new SeedMessage("seed-msg-vh-1", hakan, "Vlad, can you review my API PR?"),
                    new SeedMessage("seed-msg-vh-2", vlad, "On it — leaving comments shortly."),
                }),
            new SeedChat(
                "seed-chat-team",
                "NexusTeam Devs",
                "group",
                new[] { vlad, sofia, hakan, anna },
                vlad,
                new[]
                {
                    new SeedMessage("seed-msg-team-1", vlad, "Welcome to NexusTeam, everyone!"),
                    new SeedMessage("seed-msg-team-2", sofia, "Excited to ship this together."),
                    new SeedMessage("seed-msg-team-3", hakan, "Backend is ready for the first demo."),
                    new SeedMessage("seed-msg-team-4", anna, "Same here — data layer looks solid."),
                    new SeedMessage("seed-msg-team-5", vlad, "Great. Standup at 10:00."),
                }),
        };

        var chatIds = seededChats.Select(c => c.Id).ToList();

        // Also remove chats from the previous demo cast so leftovers don't linger.
        var legacyChatIds = new[]
        {
            "seed-chat-pavalo-olen",
            "seed-chat-pavalo-vlad",
        };
        var allChatIdsToDelete = chatIds.Concat(legacyChatIds).ToList();

        // Remove any previous copies so seeding is deterministic ("from scratch").
        await chats.DeleteManyAsync(Builders<BsonDocument>.Filter.In("_id", allChatIdsToDelete), cancellationToken);
        await messages.DeleteManyAsync(Builders<BsonDocument>.Filter.In("chatId", allChatIdsToDelete), cancellationToken);

        var messageTime = baseTime;
        foreach (var chat in seededChats)
        {
            var messageDocs = new List<BsonDocument>();
            DateTime lastMessageAt = chat.CreatedAt(baseTime);

            foreach (var message in chat.Messages)
            {
                messageTime = messageTime.AddMinutes(2);
                lastMessageAt = messageTime;

                messageDocs.Add(new BsonDocument
                {
                    { "_id", message.Id },
                    { "chatId", chat.Id },
                    { "senderId", message.SenderId },
                    { "content", message.Content },
                    { "status", 2 },
                    { "createdAt", messageTime },
                    { "editedAt", BsonNull.Value },
                    { "replyToId", BsonNull.Value },
                    { "isDeleted", false },
                    { "reactions", BsonNull.Value },
                });
            }

            var chatDoc = new BsonDocument
            {
                { "_id", chat.Id },
                { "name", chat.Name },
                { "type", chat.Type },
                { "description", BsonNull.Value },
                { "avatarUrl", BsonNull.Value },
                { "participants", new BsonArray(chat.Participants) },
                { "createdBy", chat.CreatedBy },
                { "createdAt", chat.CreatedAt(baseTime) },
                { "updatedAt", lastMessageAt },
                { "lastMessageAt", lastMessageAt },
            };

            await chats.InsertOneAsync(chatDoc, null, cancellationToken);
            if (messageDocs.Count > 0)
            {
                await messages.InsertManyAsync(messageDocs, null, cancellationToken);
            }

            this.logger.LogInformation("Seeded chat '{Name}' with {Count} messages", chat.Name, messageDocs.Count);
        }
    }

    private record DemoUser(string Id, string Username, string Email, string DisplayName);

    private record SeedMessage(string Id, string SenderId, string Content);

    private record SeedChat(string Id, string Name, string Type, string[] Participants, string CreatedBy, SeedMessage[] Messages)
    {
        public DateTime CreatedAt(DateTime baseTime) => baseTime;
    }
}
