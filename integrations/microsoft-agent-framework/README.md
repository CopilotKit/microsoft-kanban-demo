# Microsoft Agent Framework integration

This integration demonstrates the minimal set of events an AG-UI compatible server must emit. Two reference implementations are provided: one in Python (FastAPI) and one in ASP.NET Core.

## Python (FastAPI)

```bash
cd typescript-sdk/integrations/microsoft-agent-framework/server/python
poetry install
poetry run dev
```

## .NET (ASP.NET Core)

Requirements: [.NET 8 SDK](https://dotnet.microsoft.com/download)

```powershell
cd typescript-sdk/integrations/microsoft-agent-framework/server/dotnet
dotnet restore
dotnet run
```

The ASP.NET Core project exposes a `POST /` endpoint that mirrors the Python sample: it accepts `RunAgentInput`, determines the preferred response format via the `Accept` header, and streams a short "Hello world!" response using Server-Sent Events. Protobuf streaming can be enabled once the .NET encoder is available.
