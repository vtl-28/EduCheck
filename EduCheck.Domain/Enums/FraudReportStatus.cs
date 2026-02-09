using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduCheck.Domain.Enums;

public enum FraudReportStatus
{
    Submitted = 0,
    UnderReview = 1,
    Verified = 2,
    Dismissed = 3,
    ActionTaken = 4
}