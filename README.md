# Dispatching CLI Solution

## Running the CLI Application

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