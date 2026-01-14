using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EduCheck.Domain.Enums;

public enum FraudActionType
{
    StatusChange,
    NoteAdded,
    AssignedTo,
    Escalated,
    Closed
}
