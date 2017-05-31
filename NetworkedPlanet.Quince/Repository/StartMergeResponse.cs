using System.Text;

namespace NetworkedPlanet.Quince.Repository
{
    public class StartMergeResponse
    {
        public bool Success { get; private set; }
        public string FailureReason { get; private set; }
        public bool? RevalidationRequired { get; private set; }

        public StartMergeResponse(string failureReason)
        {
            Success = false;
            FailureReason = failureReason;
        }

        public StartMergeResponse(bool revalidationRequired)
        {
            Success = true;
            RevalidationRequired = revalidationRequired;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("Success: ");
            sb.Append(Success);
            if (!string.IsNullOrEmpty(FailureReason))
            {
                sb.Append("\nFailure Reason: ");
                sb.Append(FailureReason);
            }
            return sb.ToString();
        }
    }
}
