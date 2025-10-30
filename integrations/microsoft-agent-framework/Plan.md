# AG-UI for .NET and agent framework implementation plan

- [X] Setup infrastructure and define initial layering (libraries and what goes where).
- [ ] Core scenarios
    - [X] Direct LLM communication (Chat client)
        - [X] Implement Agentic Chat for Agent-Framework.
        - [X] Implement Backend Tool Rendering for for Agent-Framework.
        - [X] Implement Human in the loop for for Agent-Framework.
    - [ ] Azure AI Agents Service (PersistentAgentsClient)
        - [ ] Implement Agentic Chat for Azure Foundry Agent-Framework.
        - [ ] Implement Backend Tool Rendering for Azure Foundry for Agent-Framework.
        - [ ] Implement Human in the loop for Azure Foundry for Agent-Framework.
    - [ ] .NET Client implementation
        - [ ] Consume AG-UI events (HttpAGUIAgent) and keep a list of the messages from previous runs.
        - [ ] Support consuming the updates from .NET UIs (Blazor, Maui)
- [ ] Release activities
    - [ ] Cleanup implementation
    - [ ] Support same TFMs as agent framework and chat client.
    - [ ] Get reviews for the implementation and namings.
    - [ ] Setup code in appropriate repositories.
    - [ ] Handle publishing packages.

