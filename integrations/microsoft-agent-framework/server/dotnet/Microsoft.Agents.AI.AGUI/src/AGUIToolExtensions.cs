using AGUI;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace Microsoft.Agents.AI.AGUI;

public static class AGUIToolExtensions
{
    public static AITool AsAITool(this AGUITool tool) => 
        new AGUIToolDeclaration(tool);

    private class AGUIToolDeclaration(AGUITool tool) : AIFunctionDeclaration
    {
        public override string Name => tool.Name;

        public override string Description => tool.Description;

        // TODO: Figure out if we need to do a transform here.
        public override JsonElement JsonSchema => tool.Parameters;
    }
}
