using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manafont.Db.Model
{
    public class ManafontGameSession
    {
        public ManafontGameSession() {
            Id = Guid.NewGuid().ToString();
        }

        public ManafontGameSession(ManafontUser user) : this() {
            User = user;
        }

        [Key]
        public string Id { get; set; }

        #region Relations
        public ManafontUser User { get; set; } = null!;

        public string? CharacterId { get; set; } = null!;

        public ManafontCharacter? Character { get; set; } = null!;

        #endregion

        [Column(TypeName = "nvarchar(16)")]
        public GameSessionState Status { get; set; }
    }
}