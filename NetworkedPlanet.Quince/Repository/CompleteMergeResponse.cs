namespace NetworkedPlanet.Quince.Repository
{
    public class CompleteMergeResponse
    {
        public bool Success { get; }

        public CompleteMergeResponse(bool success)
        {
            Success = success;
        }
    }
}
