using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace Manafont.Db.Model
{
    public class ManafontUser : IdentityUser
    {
        [ProtectedPersonalData]
        public List<ManafontCharacter> Characters { get; set; }
        
        [ProtectedPersonalData]
        public List<ManafontGameSession> GameSessions { get; set; }
    }
}