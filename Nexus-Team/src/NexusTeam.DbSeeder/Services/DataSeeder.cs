using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Oracle.ManagedDataAccess.Client;

namespace NexusTeam.DbSeeder.Services;

/// <summary>
/// Seeds a small, deterministic set of demo data: a fixed group of users
/// (Pavalo, Olen, Vlad) and a short chat history between them.
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
        new("seed-user-pavalo", "Pavalo", "pavalo@nexusteam.dev", "Pavalo"),
        new("seed-user-olen", "Olen", "olen@nexusteam.dev", "Olen"),
        new("seed-user-vlad", "Vlad", "vlad@nexusteam.dev", "Vlad"),
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

        var pavalo = Users[0].Id;
        var olen = Users[1].Id;
        var vlad = Users[2].Id;

        var baseTime = DateTime.UtcNow.AddHours(-3);

        var seededChats = new[]
        {
            new SeedChat(
                "seed-chat-pavalo-olen",
                "Pavalo & Olen",
                "private",
                new[] { pavalo, olen },
                pavalo,
                new[]
                {
                    new SeedMessage("seed-msg-po-1", pavalo, "Hey Olen, did you check the new build?"),
                    new SeedMessage("seed-msg-po-2", olen, "Yeah, looks great! The chat is finally real-time."),
                    new SeedMessage("seed-msg-po-3", pavalo, "Awesome, let's demo it tomorrow."),
                }),
            new SeedChat(
                "seed-chat-pavalo-vlad",
                "Pavalo & Vlad",
                "private",
                new[] { pavalo, vlad },
                pavalo,
                new[]
                {
                    new SeedMessage("seed-msg-pv-1", vlad, "Pavalo, can you review my PR?"),
                    new SeedMessage("seed-msg-pv-2", pavalo, "Sure, sending comments now."),
                }),
            new SeedChat(
                "seed-chat-team",
                "NexusTeam Devs",
                "group",
                new[] { pavalo, olen, vlad },
                pavalo,
                new[]
                {
                    new SeedMessage("seed-msg-team-1", pavalo, "Welcome to NexusTeam, everyone!"),
                    new SeedMessage("seed-msg-team-2", olen, "Glad to be here."),
                    new SeedMessage("seed-msg-team-3", vlad, "Let's build something great."),
                    new SeedMessage("seed-msg-team-4", pavalo, "First standup is at 10:00."),
                }),
        };

        var chatIds = seededChats.Select(c => c.Id).ToList();

        // Remove any previous copies so seeding is deterministic ("from scratch").
        await chats.DeleteManyAsync(Builders<BsonDocument>.Filter.In("_id", chatIds), cancellationToken);
        await messages.DeleteManyAsync(Builders<BsonDocument>.Filter.In("chatId", chatIds), cancellationToken);

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
