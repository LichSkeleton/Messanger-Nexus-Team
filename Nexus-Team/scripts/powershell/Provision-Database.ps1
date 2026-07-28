# ============================================================================
# Script: Provision-Database.ps1
# Description: Provisions Oracle, MongoDB, and Redis for NexusTeam MVP
# Usage: .\Provision-Database.ps1 [-SkipOracle] [-SkipMongo] [-SkipRedis]
# ============================================================================

[CmdletBinding()]
param(
    [switch]$SkipOracle,
    [switch]$SkipMongo,
    [switch]$SkipRedis
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Script paths
$scriptRoot = $PSScriptRoot
$projectRoot = Split-Path (Split-Path $scriptRoot -Parent) -Parent
$configRoot = Join-Path $projectRoot "config"
$oracleScriptsPath = Join-Path $configRoot "oracle"
$mongoScriptsPath = Join-Path $configRoot "mongodb"

# Load configuration from appsettings.json
function Load-Configuration {
    param([string]$configPath)
    
    if (-not (Test-Path $configPath)) {
        Write-Error "Configuration file not found: $configPath"
        Write-Host "Please ensure appsettings.json exists in the Server project."
        exit 1
    }
    
    Write-Host "Loading configuration from: $configPath" -ForegroundColor Cyan
    
    try {
        $configContent = Get-Content $configPath -Raw -Encoding UTF8
        $config = $configContent | ConvertFrom-Json
        
        Write-Host "Configuration loaded successfully" -ForegroundColor Green
        return $config
    }
    catch {
        Write-Error "Failed to parse configuration file: $_"
        exit 1
    }
}

# Provision Oracle database
function Provision-Oracle {
    param([PSCustomObject]$config)
    
    Write-Host "`n========================================" -ForegroundColor Green
    Write-Host "Provisioning Oracle Database" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    
    if (-not $config.Oracle -or -not $config.Oracle.ConnectionString) {
        Write-Warning "Oracle configuration not found in appsettings.json. Skipping Oracle provisioning."
        return
    }
    
    # Parse Oracle connection string: "User Id=nexusteam_admin;Password=Blend100!;Data Source=localhost:1521/XEPDB1"
    $oracleConnStr = $config.Oracle.ConnectionString
    
    if ($oracleConnStr -match "User Id=([^;]+)") { $oracleUser = $matches[1] } else { $oracleUser = "" }
    if ($oracleConnStr -match "Password=([^;]+)") { $oraclePassword = $matches[1] } else { $oraclePassword = "" }
    if ($oracleConnStr -match "Data Source=([^;]+)") { $dataSource = $matches[1] } else { $dataSource = "" }
    
    if (-not $oracleUser -or -not $oraclePassword -or -not $dataSource) {
        Write-Warning "Oracle credentials not properly configured. Skipping Oracle provisioning."
        return
    }
    
    $connectionString = "$oracleUser/$oraclePassword@$dataSource"
    
    # Get Oracle SQL scripts in order
    $sqlScripts = Get-ChildItem -Path $oracleScriptsPath -Filter "*.sql" | Sort-Object Name
    
    if ($sqlScripts.Count -eq 0) {
        Write-Warning "No SQL scripts found in $oracleScriptsPath"
        return
    }
    
    Write-Host "Found $($sqlScripts.Count) SQL scripts to execute" -ForegroundColor Cyan
    
    foreach ($script in $sqlScripts) {
        Write-Host "`nExecuting: $($script.Name)" -ForegroundColor Yellow
        
        try {
            # Check if sqlplus is available
            $sqlPlusPath = Get-Command sqlplus -ErrorAction SilentlyContinue
            
            if ($sqlPlusPath) {
                # Execute using SQL*Plus
                $output = & sqlplus -S $connectionString "@$($script.FullName)" 2>&1
                
                if ($LASTEXITCODE -ne 0) {
                    Write-Error "Failed to execute $($script.Name): $output"
                } else {
                    Write-Host "Successfully executed $($script.Name)" -ForegroundColor Green
                    if ($Verbose) {
                        Write-Host $output -ForegroundColor Gray
                    }
                }
            } else {
                Write-Warning "SQL*Plus not found. Please install Oracle Instant Client or skip Oracle provisioning."
                Write-Host "Script would execute: $($script.FullName)" -ForegroundColor Gray
            }
        }
        catch {
            Write-Error "Error executing $($script.Name): $_"
            throw
        }
    }
    
    Write-Host "`nOracle database provisioning completed" -ForegroundColor Green
}

# Provision MongoDB database
function Provision-MongoDB {
    param([PSCustomObject]$config)
    
    Write-Host "`n========================================" -ForegroundColor Green
    Write-Host "Provisioning MongoDB Database (From JSON Configs)" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    
    if (-not $config.MongoDB -or -not $config.MongoDB.ConnectionString) {
        Write-Warning "MongoDB configuration not found in appsettings.json. Skipping MongoDB provisioning."
        return
    }
    
    $mongoConnection = $config.MongoDB.ConnectionString
    
    # Get all JSON config files
    $configFiles = Get-ChildItem -Path $mongoScriptsPath -Filter "*_config.json"
    
    if ($configFiles.Count -eq 0) {
        Write-Warning "No MongoDB config files found in $mongoScriptsPath"
        return
    }
    
    Write-Host "Found $($configFiles.Count) MongoDB config files" -ForegroundColor Cyan
    
    # Check if mongosh/mongo is available once
    $mongoshPath = Get-Command mongosh -ErrorAction SilentlyContinue
    $mongoPath = Get-Command mongo -ErrorAction SilentlyContinue
    
    if (-not $mongoshPath -and -not $mongoPath) {
        Write-Warning "MongoDB shell (mongosh or mongo) not found."
        return
    }

    foreach ($configFile in $configFiles) {
        Write-Host "`nProcessing: $($configFile.Name)" -ForegroundColor Cyan
        
        # 1. Читаем контент JSON файла как сырую строку
        $jsonContent = Get-Content $configFile.FullName -Raw
        
        # 2. Создаем временный JS файл, который применит этот JSON
        $tempJsScript = @"
        // Auto-generated provisioning script
        try {
            const cfg = $jsonContent;
            const dbName = cfg.database || 'NexusTeam';
            db = db.getSiblingDB(dbName);
            
            print('Using database: ' + dbName);

            // Get collection name
            const collName = cfg.collectionName;
            if (!collName) {
                throw new Error('collectionName not specified in config');
            }

            // 1. Create Collection with Validator (if not exists or update)
            const collectionNames = db.getCollectionNames();
            if (collectionNames.indexOf(collName) === -1) {
                print('Creating collection: ' + collName);
                
                // Build collection options
                const createOptions = {};
                if (cfg.validator) {
                    createOptions.validator = cfg.validator;
                }
                if (cfg.validationLevel) {
                    createOptions.validationLevel = cfg.validationLevel;
                }
                if (cfg.validationAction) {
                    createOptions.validationAction = cfg.validationAction;
                }
                
                db.createCollection(collName, createOptions);
            } else {
                print('Collection ' + collName + ' already exists. Updating validator...');
                if (cfg.validator) {
                    db.runCommand({
                        collMod: collName,
                        validator: cfg.validator,
                        validationLevel: cfg.validationLevel || 'moderate',
                        validationAction: cfg.validationAction || 'error'
                    });
                }
            }

            // 2. Create Indexes
            if (cfg.indexes && cfg.indexes.length > 0) {
                print('Creating indexes...');
                cfg.indexes.forEach(idx => {
                    let keys = idx.key;
                    let options = {};
                    for (let prop in idx) {
                        if (prop !== 'key') {
                            options[prop] = idx[prop];
                        }
                    }
                    db.getCollection(collName).createIndex(keys, options);
                    print(' + Index created: ' + (options.name || JSON.stringify(keys)));
                });
            }
            print('✓ Provisioning successful for ' + collName);
        } catch (e) {
            print('Error: ' + e);
            quit(1);
        }
"@
        
        $tempJsFile = Join-Path $mongoScriptsPath "temp_provision_$(Get-Date -Format 'yyyyMMddHHmmss')_$($configFile.BaseName).js"
        Set-Content -Path $tempJsFile -Value $tempJsScript -Encoding UTF8
        
        try {
            if ($mongoshPath) {
                $output = & mongosh $mongoConnection --file $tempJsFile 2>&1
            }
            elseif ($mongoPath) {
                $output = & mongo $mongoConnection --quiet $tempJsFile 2>&1
            }

            if ($LASTEXITCODE -ne 0) {
                Write-Error "Failed to execute MongoDB provisioning for $($configFile.Name): $output"
            } else {
                Write-Host "Successfully applied configuration for $($configFile.Name)." -ForegroundColor Green
                if ($Verbose) { Write-Host $output -ForegroundColor Gray }
            }
        }
        catch {
            Write-Error "Error executing MongoDB script for $($configFile.Name): $_"
        }
        finally {
            if (Test-Path $tempJsFile) {
                Remove-Item $tempJsFile -Force
            }
        }
    }
    
    Write-Host "`nMongoDB database provisioning completed" -ForegroundColor Green
}

# Verify Redis connection
function Provision-Redis {
    param([PSCustomObject]$config)
    
    Write-Host "`n========================================" -ForegroundColor Green
    Write-Host "Verifying Redis Connection" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    
    if (-not $config.Redis -or -not $config.Redis.ConnectionString) {
        Write-Warning "Redis configuration not found in appsettings.json. Skipping Redis verification."
        return
    }
    
    # Parse Redis connection string: "localhost:6379" or "localhost:6379,password=xxx"
    $redisConnStr = $config.Redis.ConnectionString
    
    if ($redisConnStr -match "^([^:,]+):(\d+)") {
        $redisHost = $matches[1]
        $redisPort = $matches[2]
    } else {
        Write-Warning "Invalid Redis connection string format. Skipping Redis verification."
        return
    }
    
    # Extract password if present
    $redisPassword = $null
    if ($redisConnStr -match "password=([^,]+)") {
        $redisPassword = $matches[1]
    }
    
    Write-Host "Testing Redis connection to ${redisHost}:${redisPort}" -ForegroundColor Cyan
    
    try {
        # Check if redis-cli is available
        $redisCliPath = Get-Command redis-cli -ErrorAction SilentlyContinue
        
        if ($redisCliPath) {
            # Test connection with PING
            if ($redisPassword) {
                $output = & redis-cli -h $redisHost -p $redisPort -a $redisPassword PING 2>&1
            } else {
                $output = & redis-cli -h $redisHost -p $redisPort PING 2>&1
            }
            
            if ($output -match "PONG") {
                Write-Host "Redis connection successful" -ForegroundColor Green
                
                # Get Redis info
                if ($redisPassword) {
                    $info = & redis-cli -h $redisHost -p $redisPort -a $redisPassword INFO server 2>&1
                } else {
                    $info = & redis-cli -h $redisHost -p $redisPort INFO server 2>&1
                }
                
                if ($Verbose -and $info) {
                    Write-Host "`nRedis Server Info:" -ForegroundColor Cyan
                    Write-Host $info -ForegroundColor Gray
                }
            } else {
                Write-Warning "Redis connection failed: $output"
            }
        }
        else {
            Write-Warning "redis-cli not found. Please install Redis or skip Redis verification."
            Write-Host "Would test connection to: ${redisHost}:${redisPort}" -ForegroundColor Gray
        }
    }
    catch {
        Write-Warning "Error connecting to Redis: $_"
        Write-Host "Please ensure Redis is running and the configuration is correct." -ForegroundColor Yellow
    }
    
    Write-Host "`nRedis verification completed" -ForegroundColor Green
}

# Main execution
function Main {
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "NexusTeam MVP Database Provisioning Script" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    
    # Load configuration from appsettings.json
    $serverPath = Join-Path (Split-Path (Split-Path $scriptRoot -Parent) -Parent) "src\NexusTeam.Server"
    $appSettingsPath = Join-Path $serverPath "appsettings.json"
    
    Write-Host "Loading configuration from: $appSettingsPath" -ForegroundColor Cyan
    $config = Load-Configuration -configPath $appSettingsPath
    
    # Provision databases based on flags
    if (-not $SkipOracle) {
        Provision-Oracle -config $config
    } else {
        Write-Host "`nSkipping Oracle provisioning" -ForegroundColor Yellow
    }
    
    if (-not $SkipMongo) {
        Provision-MongoDB -config $config
    } else {
        Write-Host "`nSkipping MongoDB provisioning" -ForegroundColor Yellow
    }
    
    if (-not $SkipRedis) {
        Provision-Redis -config $config
    } else {
        Write-Host "`nSkipping Redis verification" -ForegroundColor Yellow
    }
    
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "Database provisioning completed!" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
}

# Run main function
Main
