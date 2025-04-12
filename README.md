# Dispatching CLI Solution

## Running the CLI Application

[Repository URL](https://github.com/oO-Hacks-2025/dispatch-cli)

### Required Components

* [.NET](https://dotnet.microsoft.com/en-us/download)
* [Level 5 Distancify API](https://distancify.com/hackathon-2025-problem-5)

Clone the repository:

```bash
git clone https://github.com/oO-Hacks-2025/dispatch-cli.git
```

Navigate to the project directory:

```bash
cd dispatch-cli
```

Run the CLI (this will install all the required packages):

```bash
dotnet run
```

Ensure the Distancify API is running locally and your environment variables are properly configured in the `/Properties/launchSettings.json` file.

Example configuration:

```json
{
  "profiles": {
    "EmergencyDispatcher": {
      "commandName": "Project",
      "launchBrowser": true,
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "MAX_ACTIVE_CALLS": "3",
        "TARGET_DISPATCHES": "10",
        "SEED": "default",
        "LOG_LEVEL": "Debug",
        "DISPATCHING_DATA_ENDPOINT": "http://localhost:5000"
      },
      "applicationUrl": "https://localhost:49187;http://localhost:49188"
    }
  }
}
```

### Package Dependencies

* Microsoft.Extensions.Caching.Memory
* Microsoft.Extensions.Configuration.CommandLine
* Microsoft.Extensions.Configuration.EnvironmentVariables
* Microsoft.Extensions.Configuration.Json
* Microsoft.Extensions.Hosting
* Polly
* Polly.Extensions
* Polly.Extensions.Http

Note: If for whatever reason the `dotnet run` command does not automatically install the required packages, they can be added using:

```bash
dotnet add package <name>

# Example: dotnet add package Microsoft.Extensions.Caching.Memory
```

### Levels

The `main` branch is the version supporting the Level 5 of the challenge which includes authentication / authorization.

The `version/api-no-auth` branch contains the supporting levels 1-4 (contains the level 4 implementation, but that one is backwards compatible with the other levels).

### Results

```json
[
  {
    "seed": "revolutionrace",
    "targetDispatches": 10000,
    "maxActiveCalls": 100,
    "levels": [
      {
        "level": 1,
        "totalDispatches": 10000,
        "runningTime": "00:00:09.2850446",
        "distance": 23798.273415327,
        "penalty": 0,
        "httpRequests": 12110,
        "missedErrors": 0,
        "overDispatchedErrors": 0
      },
      {
        "level": 2,
        "totalDispatches": 10002,
        "runningTime": "00:00:10.1409149",
        "distance": 23813.143475427532,
        "penalty": 0,
        "httpRequests": 12157,
        "missedErrors": 0,
        "overDispatchedErrors": 0
      },
      {
        "level": 3,
        "totalDispatches": 10001,
        "runningTime": "00:00:09.9716582",
        "distance": 24062.96958086613,
        "penalty": 0,
        "httpRequests": 12327,
        "missedErrors": 0,
        "overDispatchedErrors": 0
      }
    ]
  },
  {
    "seed": "jollyroom",
    "targetDispatches": 100000,
    "maxActiveCalls": 1000,
    "levels": [
      {
        "level": 1,
        "totalDispatches": 100000,
        "runningTime": "00:01:15.6500158",
        "distance": 227519.25561589873,
        "penalty": 0,
        "httpRequests": 120302,
        "missedErrors": 0,
        "overDispatchedErrors": 0
      },
      {
        "level": 2,
        "totalDispatches": 100002,
        "runningTime": "00:00:46.6890373",
        "distance": 227093.21739922583,
        "penalty": 0,
        "httpRequests": 120274,
        "missedErrors": 0,
        "overDispatchedErrors": 0
      },
      {
        "level": 3,
        "totalDispatches": 100002,
        "runningTime": "00:00:36.0042406",
        "distance": 227450.44287390256,
        "penalty": 0,
        "httpRequests": 120300,
        "missedErrors": 0,
        "overDispatchedErrors": 0
      }
    ]
  },
  {
    "seed": "jaktia",
    "targetDispatches": 10000,
    "maxActiveCalls": 3,
    "levels": [
      {
        "level": 1,
        "totalDispatches": 10000,
        "runningTime": "00:00:12.5746161",
        "distance": 23125.33124750659,
        "penalty": 0,
        "httpRequests": 12199,
        "missedErrors": 0,
        "overDispatchedErrors": 0
      },
      {
        "level": 2,
        "totalDispatches": 10001,
        "runningTime": "00:00:10.5653308",
        "distance": 22933.775275936456,
        "penalty": 0,
        "httpRequests": 12232,
        "missedErrors": 0,
        "overDispatchedErrors": 0
      },
      {
        "level": 3,
        "totalDispatches": 10003,
        "runningTime": "00:00:09.2625748",
        "distance": 23307.11466834613,
        "penalty": 0,
        "httpRequests": 12385,
        "missedErrors": 0,
        "overDispatchedErrors": 0
      }
    ]
  },
  {
    "seed": "bellalite",
    "targetDispatches": 10000,
    "maxActiveCalls": 10000,
    "levels": [
      {
        "level": 1,
        "totalDispatches": 10000,
        "runningTime": "00:00:12.0697811",
        "distance": 22968.441047949047,
        "penalty": 0,
        "httpRequests": 12091,
        "missedErrors": 0,
        "overDispatchedErrors": 0
      },
      {
        "level": 2,
        "totalDispatches": 10001,
        "runningTime": "00:00:09.4370910",
        "distance": 23032.158340852642,
        "penalty": 0,
        "httpRequests": 12217,
        "missedErrors": 0,
        "overDispatchedErrors": 0
      },
      {
        "level": 3,
        "totalDispatches": 10002,
        "runningTime": "00:00:07.9656797",
        "distance": 23492.29997323989,
        "penalty": 0,
        "httpRequests": 12427,
        "missedErrors": 0,
        "overDispatchedErrors": 0
      }
    ]
  },
  {
    "seed": "gudrun",
    "levels": [
      {
        "level": 4,
        "targetDispatches": 25,
        "totalDispatches": 27,
        "runningTime": "00:01:01.0583033",
        "distance": 46.399370468775864,
        "penalty": 0,
        "httpRequests": 125,
        "missedErrors": 0,
        "overDispatchedErrors": 0
      },
      {
        "level": 5,
        "targetDispatches": 25,
        "totalDispatches": 27,
        "runningTime": "00:01:01.5444900",
        "distance": 46.399370468775864,
        "penalty": 0,
        "httpRequests": 125,
        "missedErrors": 0,
        "overDispatchedErrors": 0
      }
    ]
  }
]
```

Disclaimer: These results were obtained on a rusty MSI laptop which was ran over by a car and which overheats from the air conditioning.
