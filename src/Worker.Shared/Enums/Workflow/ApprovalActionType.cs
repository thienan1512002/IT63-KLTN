namespace Worker.Shared.Enums.Workflow
{
    /// <summary>
    /// Approval action types (use as bitmask with Flags)
    /// </summary>
    [Flags]
    public enum ApprovalActionType
    {
        /// <summary>
        /// No action
        /// </summary>
        None = 0,

        /// <summary>
        /// Approve and move to next step
        /// </summary>
        Approve = 1,

        /// <summary>
        /// Reject and end workflow
        /// </summary>
        Reject = 2,

        /// <summary>
        /// Return to previous step for revision
        /// </summary>
        Return = 4,

        /// <summary>
        /// Confirm information (for requester)
        /// </summary>
        Confirm = 8,

        /// <summary>
        /// Can add comment when approving/rejecting
        /// </summary>
        Comment = 16,

        /// <summary>
        /// Can upload attachment
        /// </summary>
        Upload = 32,

        /// <summary>
        /// Agree 
        /// </summary>
        Agree = 64,

    }

    /// <summary>
    /// Extension methods for ApprovalActionType
    /// </summary>
    public static class ApprovalActionExtensions
    {
        public static string GetActionName(this ApprovalActionType action)
        {
            return action switch
            {
                ApprovalActionType.Approve => "Approve",
                ApprovalActionType.Reject => "Reject",
                ApprovalActionType.Return => "Return",
                ApprovalActionType.Confirm => "Confirm",
                ApprovalActionType.Comment => "Comment",
                ApprovalActionType.Upload => "Upload",
                _ => action.ToString()
            };
        }

        /// <summary>
        /// Get list of individual actions from combined bitmask
        /// </summary>
        public static List<ApprovalActionType> ToList(this ApprovalActionType actions)
        {
            if (actions == ApprovalActionType.None)
                return new List<ApprovalActionType>();

            return Enum.GetValues<ApprovalActionType>()
                .Where(a => a != ApprovalActionType.None && actions.HasFlag(a))
                .ToList();
        }

        /// <summary>
        /// Check if specific action is available in the bitmask
        /// </summary>
        /// <param name="actions">Combined actions bitmask</param>
        /// <param name="action">Action to check</param>
        /// <returns>True if action is available</returns>
        public static bool HasAction(this ApprovalActionType actions, ApprovalActionType action)
        {
            return actions.HasFlag(action);
        }

    }

}