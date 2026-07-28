#!/bin/bash

# Function to check if a command succeeded
check_status() {
    if [ $? -eq 0 ]; then
        echo " Success: $1"
    else
        echo " Failed: $1"
        exit 1
    fi
}

echo " Starting MongoDB Cluster Initialization..."

# Initialize Config Server
echo " Initializing Config Server Replica Set..."
docker exec nexusteam_mongo_config mongosh --port 27017 --eval "rs.initiate({_id: 'configReplSet', configsvr: true, members: [{_id: 0, host: 'mongo-config:27017'}]})" 
check_status "Config Server Init"

# Initialize Shard 1
echo " Initializing Shard 1 Replica Set..."
docker exec nexusteam_mongo_shard1 mongosh --port 27017 --eval "rs.initiate({_id: 'shard1ReplSet', members: [{_id: 0, host: 'mongo-shard1:27017'}]})"
check_status "Shard 1 Init"

# Initialize Shard 2
echo " Initializing Shard 2 Replica Set..."
docker exec nexusteam_mongo_shard2 mongosh --port 27017 --eval "rs.initiate({_id: 'shard2ReplSet', members: [{_id: 0, host: 'mongo-shard2:27017'}]})"
check_status "Shard 2 Init"

# Wait for elections to finish
echo " Waiting 10 seconds for replica sets to stabilize..."
sleep 10

# Initialize Mongos Router
echo " restarting Mongos Router..."
docker restart nexusteam_mongos
sleep 5

# Add Shards
echo " Adding Shards to Router..."
docker exec nexusteam_mongos mongosh --port 27017 --eval "sh.addShard('shard1ReplSet/mongo-shard1:27017')"
check_status "Add Shard 1"

docker exec nexusteam_mongos mongosh --port 27017 --eval "sh.addShard('shard2ReplSet/mongo-shard2:27017')"
check_status "Add Shard 2"

# Restart Seeder
echo " Restarting Database Seeder..."
docker restart nexusteam_db_seeder

echo " MongoDB Cluster Initialization Complete!"
