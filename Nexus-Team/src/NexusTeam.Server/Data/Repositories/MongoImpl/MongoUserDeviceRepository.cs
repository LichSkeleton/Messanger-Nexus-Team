namespace NexusTeam.Server.Data.Repositories.MongoImpl
{
    using System.Threading;
    using System.Threading.Tasks;
    using MongoDB.Bson;
    using MongoDB.Driver;
    using NexusTeam.Server.Data.Models;
    using NexusTeam.Server.Services.Abstractions;

    public class MongoUserDeviceRepository : IUserDeviceRepository
    {
        private readonly IMongoCollection<UserDevice> collection;

        public MongoUserDeviceRepository(IMongoClientFactory mongoClientFactory)
        {
            this.collection = mongoClientFactory.GetDatabase().GetCollection<UserDevice>("userDevices");
            var keys = Builders<UserDevice>.IndexKeys.Ascending(x => x.UserId).Ascending(x => x.DeviceId);
            this.collection.Indexes.CreateOne(new CreateIndexModel<UserDevice>(keys, new CreateIndexOptions { Unique = true }));
        }

        public async Task<UserDevice?> GetAsync(string userId, string deviceId, CancellationToken cancellationToken = default)
        {
            var filter = Builders<UserDevice>.Filter.Eq(x => x.UserId, userId) &
                Builders<UserDevice>.Filter.Eq(x => x.DeviceId, deviceId);
            return await this.collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }

        public Task UpsertAsync(UserDevice device, CancellationToken cancellationToken = default)
        {
            device.Id ??= ObjectId.GenerateNewId().ToString();
            var filter = Builders<UserDevice>.Filter.Eq(x => x.UserId, device.UserId) &
                Builders<UserDevice>.Filter.Eq(x => x.DeviceId, device.DeviceId);
            return this.collection.ReplaceOneAsync(filter, device, new ReplaceOptions { IsUpsert = true }, cancellationToken);
        }
    }
}
