namespace Entrenate.Api.Requests.Coach
{
    public sealed record AskRoutinePlannerConversationRequest(
        string? ProposalHash,
        string Question);
}
