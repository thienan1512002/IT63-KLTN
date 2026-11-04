

namespace Worker.Shared.Enums.Workflow
{
    public enum ApprovalStatus
    {
        Approved = 1,
        Confirmed,
        Rejected,
        Returned,
        Waiting,
        Bypassing,
        Agreed, // For agreement type approval steps        
    }
   
}