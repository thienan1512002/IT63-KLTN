using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Worker.Data;
using Microsoft.EntityFrameworkCore;

namespace Worker.Utils
{
    public class ResolveActiveApproverMail
    {
        // Always include the main approver; also include the online backup if the main approver is offline.
        public static List<string> ResolveOnlineOfflineReceiversAsync(List<string> approvers, Dictionary<string, bool> userStatusMap)
        {
            var receivers = new List<string>();
            bool mainAdded = false;

            foreach (var approver in approvers)
            {
                if (!userStatusMap.ContainsKey(approver))
                    continue;

                if (!mainAdded)
                {
                    // Always notify the main approver (first in list)
                    receivers.Add(approver);
                    mainAdded = true;

                    // Main is online — no need to notify backup
                    if (userStatusMap[approver])
                        break;
                }
                else if (userStatusMap[approver])
                {
                    // Add online backup approver when main is offline
                    receivers.Add(approver);
                }
            }

            return receivers;
        }
    }
}